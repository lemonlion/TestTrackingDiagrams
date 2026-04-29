using System.Text;

namespace TestTrackingDiagrams.PlantUml;

/// <summary>
/// Pretty-prints GraphQL query strings with proper indentation for display in diagram notes.
/// </summary>
public static class GraphQlQueryFormatter
{
    private const int IndentSize = 2;

    /// <summary>
    /// Formats a GraphQL query string with proper indentation.
    /// Braces control indentation depth; content inside parentheses stays inline.
    /// </summary>
    public static string FormatQuery(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return string.Empty;

        var sb = new StringBuilder(query.Length * 2);
        var braceDepth = 0;
        var parenDepth = 0;
        var i = 0;
        var justOutputNewline = false;
        var currentWord = new StringBuilder();
        var inSpreadContext = false;

        while (i < query.Length)
        {
            var c = query[i];

            // ── String literals: copy verbatim ──
            if (c == '"')
            {
                sb.Append(c);
                i++;
                while (i < query.Length)
                {
                    sb.Append(query[i]);
                    if (query[i] == '\\' && i + 1 < query.Length)
                    {
                        i++;
                        sb.Append(query[i]);
                    }
                    else if (query[i] == '"')
                    {
                        break;
                    }
                    i++;
                }
                i++;
                justOutputNewline = false;
                currentWord.Clear();
                continue;
            }

            // ── Inside parentheses: copy everything as-is ──
            if (parenDepth > 0)
            {
                sb.Append(c);
                if (c == '(') parenDepth++;
                else if (c == ')') parenDepth--;
                i++;
                justOutputNewline = false;
                continue;
            }

            if (c == '(')
            {
                parenDepth++;
                sb.Append(c);
                i++;
                justOutputNewline = false;
                currentWord.Clear();
                continue;
            }

            // ── Open brace: increase depth, newline + indent ──
            if (c == '{')
            {
                braceDepth++;
                TrimTrailingWhitespace(sb);
                if (sb.Length > 0) sb.Append(' ');
                sb.Append('{');
                sb.Append('\n');
                AppendIndent(sb, braceDepth);
                i++;
                justOutputNewline = true;
                currentWord.Clear();
                inSpreadContext = false;
                continue;
            }

            // ── Close brace: decrease depth, newline + dedent ──
            if (c == '}')
            {
                braceDepth--;
                TrimTrailingWhitespace(sb);
                sb.Append('\n');
                AppendIndent(sb, braceDepth);
                sb.Append('}');
                i++;
                justOutputNewline = false;
                currentWord.Clear();
                inSpreadContext = false;
                continue;
            }

            // ── Whitespace handling ──
            if (char.IsWhiteSpace(c))
            {
                // Consume all consecutive whitespace
                while (i < query.Length && char.IsWhiteSpace(query[i])) i++;

                if (justOutputNewline) continue;
                if (sb.Length == 0) continue;

                var word = currentWord.ToString();
                currentWord.Clear();

                // Peek: directive (@) stays attached to previous field
                if (braceDepth > 0 && i < query.Length && query[i] == '@')
                {
                    sb.Append(' ');
                    continue;
                }

                if (braceDepth == 0)
                {
                    // Top-level: double newline between top-level constructs (e.g. fragments)
                    if (sb.Length > 0 && sb[sb.Length - 1] == '}')
                        sb.Append("\n\n");
                    else
                        sb.Append(' ');
                }
                else if (sb.Length > 0 && sb[sb.Length - 1] == '}')
                {
                    // After closing brace inside selection set → sibling on new line
                    sb.Append('\n');
                    AppendIndent(sb, braceDepth);
                    justOutputNewline = true;
                }
                else if (word.EndsWith(':'))
                {
                    // Alias: "completed: orderSummaries" stays on same line
                    sb.Append(' ');
                }
                else if (word == "...")
                {
                    // Start of spread: "... on Type" stays inline
                    inSpreadContext = true;
                    sb.Append(' ');
                }
                else if (inSpreadContext)
                {
                    // Continue spread context: "on", type name stay inline
                    sb.Append(' ');
                }
                else if (word.Length == 0)
                {
                    // After paren close or other non-word: space
                    sb.Append(' ');
                }
                else
                {
                    // Selection item separator: newline + indent
                    sb.Append('\n');
                    AppendIndent(sb, braceDepth);
                    justOutputNewline = true;
                    inSpreadContext = false;
                }

                continue;
            }

            // ── Regular character ──
            sb.Append(c);
            currentWord.Append(c);
            justOutputNewline = false;
            i++;
        }

        return sb.ToString().TrimEnd();
    }

    private static void TrimTrailingWhitespace(StringBuilder sb)
    {
        while (sb.Length > 0 && char.IsWhiteSpace(sb[sb.Length - 1]))
            sb.Length--;
    }

    private static void AppendIndent(StringBuilder sb, int depth)
    {
        sb.Append(' ', depth * IndentSize);
    }
}
