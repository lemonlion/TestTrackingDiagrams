using System.Text;
using System.Text.RegularExpressions;

namespace TestTrackingDiagrams.Tracking;

/// <summary>
/// Parses assertion expressions (typically captured via <c>[CallerArgumentExpression]</c>)
/// into readable English sentences. Optimised for FluentAssertions <c>.Should().Method(args)</c>
/// patterns but falls back gracefully for other assertion styles.
/// </summary>
public static partial class AssertionExpressionFormatter
{
    private static readonly Regex ShouldSplitRegex = CreateShouldSplitRegex();

    [GeneratedRegex(@"\.Should\(\)\.", RegexOptions.None)]
    private static partial Regex CreateShouldSplitRegex();

    public static string Format(string? expression)
    {
        return Format(expression, resolvedValues: null);
    }

    public static string Format(string? expression, Dictionary<string, string>? resolvedValues)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return "";

        // Strip lambda prefix: "() => "
        var expr = expression.StartsWith("() => ")
            ? expression["() => ".Length..]
            : expression;

        // Remove null-forgiving operators (!)
        expr = expr.Replace("!", "");

        // Split on .Should().
        var match = ShouldSplitRegex.Match(expr);
        if (!match.Success)
            return expr;

        var subject = expr[..match.Index];
        var assertionPart = expr[(match.Index + match.Length)..];

        // Format subject: split on '.', PascalCase-split each segment, rejoin with spaces
        var formattedSubject = FormatSubject(subject);

        // Format assertion: method(args) — handle .And. chaining by taking first assertion
        var andIndex = assertionPart.IndexOf(".And.", StringComparison.Ordinal);
        if (andIndex >= 0)
            assertionPart = assertionPart[..andIndex];

        var (method, args) = ParseMethodAndArgs(assertionPart);
        var formattedMethod = SplitPascalCase(method).ToLowerInvariant();
        var formattedArgs = FormatArgs(args, resolvedValues);

        var result = string.IsNullOrEmpty(formattedArgs)
            ? $"{formattedSubject} should {formattedMethod}"
            : $"{formattedSubject} should {formattedMethod} {formattedArgs}";

        return result;
    }

    private static string FormatSubject(string subject)
    {
        var segments = subject.Split('.');
        var sb = new StringBuilder();
        for (var i = 0; i < segments.Length; i++)
        {
            if (i > 0) sb.Append(' ');
            var segment = segments[i].Trim('_');
            sb.Append(SplitPascalCase(segment).ToLowerInvariant());
        }

        // Capitalize first letter
        if (sb.Length > 0)
            sb[0] = char.ToUpperInvariant(sb[0]);

        return sb.ToString();
    }

    private static (string Method, string? Args) ParseMethodAndArgs(string assertionPart)
    {
        // Handle generic methods: BeOfType<string>()
        var parenDepth = 0;
        var angleBracketDepth = 0;
        var methodEnd = -1;
        var argsStart = -1;
        var argsEnd = -1;

        for (var i = 0; i < assertionPart.Length; i++)
        {
            var c = assertionPart[i];
            switch (c)
            {
                case '<' when parenDepth == 0:
                    if (methodEnd < 0) methodEnd = i;
                    angleBracketDepth++;
                    break;
                case '>' when parenDepth == 0 && angleBracketDepth > 0:
                    angleBracketDepth--;
                    break;
                case '(':
                    if (angleBracketDepth == 0 && methodEnd < 0) methodEnd = i;
                    if (angleBracketDepth == 0 && parenDepth == 0) argsStart = i + 1;
                    parenDepth++;
                    break;
                case ')':
                    parenDepth--;
                    if (parenDepth == 0 && angleBracketDepth == 0) argsEnd = i;
                    break;
            }
        }

        if (methodEnd < 0) return (assertionPart, null);

        var method = assertionPart[..methodEnd];

        // Extract generic type suffix if present
        var genericSuffix = "";
        if (assertionPart.Length > methodEnd && assertionPart[methodEnd] == '<')
        {
            var closeAngle = assertionPart.IndexOf('>', methodEnd);
            if (closeAngle >= 0)
                genericSuffix = " " + assertionPart[methodEnd..(closeAngle + 1)];
        }

        string? args = null;
        if (argsStart >= 0 && argsEnd > argsStart)
            args = assertionPart[argsStart..argsEnd];

        if (!string.IsNullOrEmpty(genericSuffix))
            args = genericSuffix.TrimStart() + (args is not null ? ", " + args : "");

        return (method, string.IsNullOrWhiteSpace(args) ? null : args);
    }

    private static string FormatArgs(string? args, Dictionary<string, string>? resolvedValues)
    {
        if (string.IsNullOrWhiteSpace(args))
            return "";

        // Substitute resolved values for standalone variable tokens in the args
        if (resolvedValues is { Count: > 0 })
            args = SubstituteResolvedValues(args, resolvedValues);

        // Wrap lambda expressions in square brackets for readability
        if (args.Contains("=>"))
            return $"[{args}]";

        // Simplify dotted member access chains: _eggsSteps.EggsResponse.Eggs → 'Eggs'
        args = SimplifyMemberAccessPaths(args);

        // Strip enum prefixes: TaskStatus.RanToCompletion → RanToCompletion, HttpStatusCode.OK → OK
        // But preserve complex expressions like DateTime.UtcNow, TimeSpan.FromSeconds(5)
        // Only strip if no substitutions were made (resolved values are already formatted)
        if (resolvedValues is not { Count: > 0 } || !args.Contains('\''))
            args = StripSimpleEnumPrefixes(args);

        return args;
    }

    private static string SubstituteResolvedValues(string args, Dictionary<string, string> resolvedValues)
    {
        // Process longer keys first to prevent partial matches
        // (e.g. "expected.ExpectedIngredientCount" before "expected")
        var orderedKeys = resolvedValues.Keys.OrderByDescending(k => k.Length);

        foreach (var name in orderedKeys)
        {
            var value = resolvedValues[name];
            var idx = 0;
            while (idx <= args.Length - name.Length)
            {
                idx = args.IndexOf(name, idx, StringComparison.Ordinal);
                if (idx < 0) break;

                var before = idx > 0 ? args[idx - 1] : ' ';
                var after = idx + name.Length < args.Length ? args[idx + name.Length] : ' ';

                // Must be a standalone token (not part of a larger identifier)
                if (IsIdentifierChar(before) || IsIdentifierChar(after))
                {
                    idx += name.Length;
                    continue;
                }

                // Don't substitute inside quoted strings
                if (IsInsideQuotes(args, idx))
                {
                    idx += name.Length;
                    continue;
                }

                var replacement = $"'{value}'";
                args = args[..idx] + replacement + args[(idx + name.Length)..];
                idx += replacement.Length;
            }
        }

        return args;
    }

    private static bool IsInsideQuotes(string text, int position)
    {
        var quoteCount = 0;
        for (var i = 0; i < position; i++)
        {
            if (text[i] == '"' && (i == 0 || text[i - 1] != '\\'))
                quoteCount++;
        }
        return quoteCount % 2 != 0;
    }

    private static bool IsIdentifierChar(char c) => char.IsLetterOrDigit(c) || c == '_';

    private static string SimplifyMemberAccessPaths(string args)
    {
        // If already contains quotes (resolved values), don't re-process
        if (args.Contains('\''))
            return args;

        // Handle comma-separated args by processing each individually
        if (args.Contains(','))
        {
            var parts = SplitTopLevelCommas(args);
            var simplified = new string[parts.Length];
            for (var i = 0; i < parts.Length; i++)
                simplified[i] = SimplifySingleMemberAccess(parts[i].Trim());
            return string.Join(", ", simplified);
        }

        return SimplifySingleMemberAccess(args);
    }

    private static string SimplifySingleMemberAccess(string arg)
    {
        // Skip if it contains parens, quotes, operators, or spaces — not a simple member access
        if (arg.Contains('(') || arg.Contains('"') || arg.Contains('\'') ||
            arg.Contains(' ') || arg.Contains('+') || arg.Contains('-'))
            return arg;

        // Must contain a dot to be a member access path
        if (!arg.Contains('.'))
            return arg;

        var segments = arg.Split('.');

        // Two-segment paths where both start with uppercase and no underscore prefix
        // are likely enums (HttpStatusCode.OK) — leave for StripSimpleEnumPrefixes
        if (segments.Length == 2 && !arg.StartsWith("_") &&
            segments[0].Length > 0 && char.IsUpper(segments[0][0]) &&
            segments[1].Length > 0 && char.IsUpper(segments[1][0]))
            return arg;

        // For multi-segment paths or underscore-prefixed paths, extract last segment
        var lastSegment = segments[^1].Trim('_');
        if (string.IsNullOrEmpty(lastSegment))
            return arg;

        return $"'{lastSegment}'";
    }

    private static string[] SplitTopLevelCommas(string text)
    {
        var parts = new List<string>();
        var depth = 0;
        var start = 0;
        for (var i = 0; i < text.Length; i++)
        {
            switch (text[i])
            {
                case '(' or '<': depth++; break;
                case ')' or '>': depth--; break;
                case ',' when depth == 0:
                    parts.Add(text[start..i]);
                    start = i + 1;
                    break;
            }
        }
        parts.Add(text[start..]);
        return parts.ToArray();
    }

    private static string StripSimpleEnumPrefixes(string args)
    {
        // Only strip "EnumType.Value" patterns for simple single-value args
        // (no commas, no parentheses). This avoids mangling complex expressions
        // like "DateTime.UtcNow, TimeSpan.FromSeconds(5)".
        if (args.Contains(',') || args.Contains('('))
            return args;

        return Regex.Replace(args, @"^([A-Z][a-zA-Z0-9]*)\.([A-Z][a-zA-Z0-9]*)$", "$2");
    }

    private static string SplitPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var sb = new StringBuilder();
        for (var i = 0; i < input.Length; i++)
        {
            if (i > 0 && char.IsUpper(input[i]) && !char.IsUpper(input[i - 1]))
                sb.Append(' ');
            else if (i > 1 && char.IsUpper(input[i]) && char.IsUpper(input[i - 1]) && i + 1 < input.Length && char.IsLower(input[i + 1]))
                sb.Append(' ');
            sb.Append(input[i]);
        }

        return sb.ToString();
    }
}
