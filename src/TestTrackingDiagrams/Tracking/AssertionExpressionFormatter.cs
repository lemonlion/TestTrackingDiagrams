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
        if (string.IsNullOrWhiteSpace(expression))
            return "";

        // Strip lambda prefix: "() => "
        var expr = expression.StartsWith("() => ")
            ? expression["() => ".Length..]
            : expression;

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
        var formattedArgs = FormatArgs(args);

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
            sb.Append(SplitPascalCase(segments[i]).ToLowerInvariant());
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

    private static string FormatArgs(string? args)
    {
        if (string.IsNullOrWhiteSpace(args))
            return "";

        // Strip enum prefixes: TaskStatus.RanToCompletion → RanToCompletion, HttpStatusCode.OK → OK
        // But preserve complex expressions like DateTime.UtcNow, TimeSpan.FromSeconds(5)
        args = StripSimpleEnumPrefixes(args);

        return args;
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
