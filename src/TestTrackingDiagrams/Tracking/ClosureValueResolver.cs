using System.Collections;
using System.Reflection;

namespace TestTrackingDiagrams.Tracking;

/// <summary>
/// Inspects a delegate's closure (Target) to resolve runtime values of captured variables.
/// Returns a dictionary of variable-name → display-value for variables that appear
/// as assertion arguments in the expression text.
/// </summary>
public static class ClosureValueResolver
{
    private const int MaxValueLength = 50;

    private static readonly HashSet<char> ArithmeticOperators = ['+', '-', '*', '/', '%'];

    /// <summary>
    /// Attempts to resolve captured variable values from the delegate's closure.
    /// Returns resolved values and any fallback reasons (for diagnostic logging).
    /// </summary>
    public static ResolveResult ResolveValues(Delegate assertion, string? expression)
    {
        var result = new ResolveResult();

        if (string.IsNullOrWhiteSpace(expression))
            return result;

        var target = assertion.Target;
        if (target is null)
            return result;

        var argsText = ExtractArgsText(expression);
        if (argsText is null)
            return result;

        var closureFields = GetClosureFields(target);

        foreach (var (name, value) in closureFields)
        {
            if (!IsStandaloneToken(argsText, name))
                continue;

            if (IsInComputedExpression(argsText, name))
            {
                result.AddFallback(name, "computed expression");
                continue;
            }

            var displayValue = FormatValue(value, name, result);
            if (displayValue is not null)
                result.ResolvedValues[name] = displayValue;
        }

        return result;
    }

    private static string? ExtractArgsText(string expression)
    {
        // Strip lambda prefix
        var expr = expression.StartsWith("() => ")
            ? expression["() => ".Length..]
            : expression;

        // Find .Should(). and then the method args
        var shouldIdx = expr.IndexOf(".Should().", StringComparison.Ordinal);
        if (shouldIdx < 0)
            return null;

        var afterShould = expr[(shouldIdx + ".Should().".Length)..];

        // Find the opening paren of the assertion method
        var parenIdx = afterShould.IndexOf('(');
        if (parenIdx < 0)
            return null;

        // Extract content between outermost parens
        var depth = 0;
        var start = parenIdx + 1;
        for (var i = parenIdx; i < afterShould.Length; i++)
        {
            switch (afterShould[i])
            {
                case '(':
                    depth++;
                    break;
                case ')':
                    depth--;
                    if (depth == 0)
                        return afterShould[start..i];
                    break;
            }
        }

        return null;
    }

    private static List<(string Name, object? Value)> GetClosureFields(object target)
    {
        var results = new List<(string, object?)>();
        var type = target.GetType();
        var isCompilerGenerated = type.IsDefined(
            typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false);

        // If target is NOT compiler-generated, it means the lambda captured 'this' directly
        // (no local variables captured). Inspect the instance's fields.
        if (!isCompilerGenerated)
        {
            var instanceFields = type.GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var f in instanceFields)
                results.Add((f.Name, f.GetValue(target)));
            return results;
        }

        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var field in fields)
        {
            var name = field.Name;
            var value = field.GetValue(target);

            // Handle 'this' capture — compiler names it <>4__this or similar
            if (name.Contains("__this") || name.Contains("<>4__this"))
            {
                if (value is not null)
                {
                    var thisFields = value.GetType().GetFields(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    foreach (var tf in thisFields)
                        results.Add((tf.Name, tf.GetValue(value)));
                }
                continue;
            }

            results.Add((name, value));
        }

        return results;
    }

    internal static bool IsStandaloneToken(string text, string token)
    {
        var idx = 0;
        while (idx <= text.Length - token.Length)
        {
            idx = text.IndexOf(token, idx, StringComparison.Ordinal);
            if (idx < 0)
                return false;

            var before = idx > 0 ? text[idx - 1] : ' ';
            var after = idx + token.Length < text.Length ? text[idx + token.Length] : ' ';

            if (!IsIdentifierChar(before) && !IsIdentifierChar(after))
                return true;

            idx += token.Length;
        }

        return false;
    }

    internal static bool IsInComputedExpression(string argsText, string token)
    {
        var idx = 0;
        while (idx <= argsText.Length - token.Length)
        {
            idx = argsText.IndexOf(token, idx, StringComparison.Ordinal);
            if (idx < 0)
                return false;

            var before = idx > 0 ? argsText[idx - 1] : ' ';
            var after = idx + token.Length < argsText.Length ? argsText[idx + token.Length] : ' ';

            if (!IsIdentifierChar(before) && !IsIdentifierChar(after))
            {
                // Check chars around the token (skip whitespace) for arithmetic operators
                if (HasAdjacentOperator(argsText, idx, token.Length))
                    return true;
            }

            idx += token.Length;
        }

        return false;
    }

    private static bool HasAdjacentOperator(string text, int tokenStart, int tokenLength)
    {
        // Check after token
        for (var i = tokenStart + tokenLength; i < text.Length; i++)
        {
            if (char.IsWhiteSpace(text[i]))
                continue;
            if (ArithmeticOperators.Contains(text[i]))
                return true;
            break;
        }

        // Check before token
        for (var i = tokenStart - 1; i >= 0; i--)
        {
            if (char.IsWhiteSpace(text[i]))
                continue;
            if (ArithmeticOperators.Contains(text[i]))
                return true;
            break;
        }

        return false;
    }

    private static string? FormatValue(object? value, string fieldName, ResolveResult result)
    {
        if (value is null)
            return "null";

        if (value is string s)
            return s.Length > MaxValueLength
                ? s[..MaxValueLength] + "..."
                : s;

        // Collections — show count
        if (value is IEnumerable enumerable and not string)
        {
            var count = 0;
            foreach (var _ in enumerable)
            {
                count++;
                if (count > 1000) break; // safety cap
            }

            return $"[{count} items]";
        }

        var str = value.ToString();
        var typeName = value.GetType().FullName;
        var shortTypeName = value.GetType().Name;

        // If ToString() just returns the type name, it's an unresolvable complex object
        if (str is null || str == typeName || str == shortTypeName)
        {
            result.AddFallback(fieldName, "ToString returned type name");
            return null;
        }

        return str.Length > MaxValueLength
            ? str[..MaxValueLength] + "..."
            : str;
    }

    private static bool IsIdentifierChar(char c) => char.IsLetterOrDigit(c) || c == '_';
}

/// <summary>
/// Result of closure value resolution, containing resolved values and any fallback diagnostics.
/// </summary>
public class ResolveResult
{
    public Dictionary<string, string> ResolvedValues { get; } = new();
    public List<(string FieldName, string Reason)> Fallbacks { get; } = [];

    public void AddFallback(string fieldName, string reason)
    {
        Fallbacks.Add((fieldName, reason));
    }
}
