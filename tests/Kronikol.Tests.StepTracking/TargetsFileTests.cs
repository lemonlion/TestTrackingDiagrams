using System.Text.RegularExpressions;

namespace Kronikol.Tests.StepTracking;

/// <summary>
/// Validates that the .targets file generates valid C# source files.
/// MSBuild's WriteLinesToFile treats ';' as a list separator in the Lines property,
/// so semicolons must be escaped as %3B to produce correct output.
/// See: https://github.com/lemonlion/Kronikol/issues/46
/// </summary>
public class TargetsFileTests
{
    private static readonly string TargetsFilePath = Path.GetFullPath(
        Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Kronikol.StepTracking", "build",
            "Kronikol.StepTracking.targets"));

    [Fact]
    public void TargetsFile_WriteLinesToFile_DoesNotContainUnescapedSemicolons()
    {
        var content = File.ReadAllText(TargetsFilePath);

        // Extract all Lines="..." attribute values
        var linesPattern = new Regex(@"Lines=""([^""]+)""", RegexOptions.Singleline);
        var matches = linesPattern.Matches(content);

        Assert.True(matches.Count > 0, "Should find WriteLinesToFile Lines attributes");

        foreach (Match match in matches)
        {
            var linesValue = match.Groups[1].Value;

            // Remove valid encodings: %3B (MSBuild-escaped semicolons) and XML entities (&lt; &gt; etc.)
            var withoutEscaped = Regex.Replace(linesValue, @"%3B|&\w+;", "");
            Assert.DoesNotContain(";", withoutEscaped);
        }
    }

    [Fact]
    public void TargetsFile_GeneratedAttributes_ProduceValidCSharp()
    {
        var content = File.ReadAllText(TargetsFilePath);

        var linesPattern = new Regex(@"Lines=""([^""]+)""", RegexOptions.Singleline);
        var matches = linesPattern.Matches(content);

        foreach (Match match in matches)
        {
            var linesValue = match.Groups[1].Value;

            // Simulate what WriteLinesToFile produces: %3B → ;, &lt; → <, &gt; → >
            var decoded = linesValue
                .Replace("%3B", ";")
                .Replace("&lt;", "<")
                .Replace("&gt;", ">");

            // Every attribute with a Description property should have valid auto-property syntax
            if (decoded.Contains("Description"))
            {
                Assert.Contains("{ get; set; }", decoded);
                Assert.DoesNotContain("{ get\n", decoded);
                Assert.DoesNotContain("{ get\r", decoded);
            }
        }
    }
}
