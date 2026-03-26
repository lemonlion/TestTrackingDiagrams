using System.Text.RegularExpressions;
using TestTrackingDiagrams;

namespace Example.Api.Tests.Integration.Helpers;

public static class PlantUmlAssertions
{
    public static void AssertContainsParticipants(string plantUml, params string[] names)
    {
        foreach (var name in names)
            Assert.Contains($"\"{name}\"", plantUml);
    }

    public static void AssertContainsSequenceArrow(string plantUml, string from, string to)
    {
        // Normalize alias names (spaces removed, used as identifiers)
        var fromAlias = from.Replace(" ", "");
        var toAlias = to.Replace(" ", "");
        // Match arrows like: FromAlias -> ToAlias or FromAlias --> ToAlias
        var pattern = $@"{Regex.Escape(fromAlias)}\s*-+>+\s*{Regex.Escape(toAlias)}";
        Assert.Matches(pattern, plantUml);
    }

    public static void AssertContainsFocusMarkup(string plantUml, string fieldName, FocusEmphasis emphasis)
    {
        // Find lines containing the field name and verify emphasis markup
        var lines = GetLinesContaining(plantUml, fieldName);
        Assert.NotEmpty(lines);

        foreach (var line in lines)
        {
            if (emphasis.HasFlag(FocusEmphasis.Bold))
                Assert.Contains("<b>", line);

            if (emphasis.HasFlag(FocusEmphasis.Colored))
                Assert.Contains("<color:blue>", line);
        }
    }

    public static void AssertContainsDeEmphasisMarkup(string plantUml, string fieldName, FocusDeEmphasis deEmphasis)
    {
        var lines = GetLinesContaining(plantUml, fieldName);
        Assert.NotEmpty(lines);

        foreach (var line in lines)
        {
            if (deEmphasis.HasFlag(FocusDeEmphasis.LightGray))
                Assert.Contains("<color:lightgray>", line);

            if (deEmphasis.HasFlag(FocusDeEmphasis.SmallerText))
                Assert.Contains("<size:9>", line);
        }
    }

    public static void AssertHiddenDeEmphasis(string plantUml, string fieldName)
    {
        // When Hidden, the non-focused field should not appear — replaced by "..."
        var lines = GetLinesContaining(plantUml, fieldName);
        Assert.Empty(lines);
        Assert.Contains("...", plantUml);
    }

    public static void AssertContainsSetupPartition(string plantUml, bool highlighted)
    {
        if (highlighted)
            Assert.Contains("partition Setup #E2E2F0", plantUml);
        else
            Assert.Matches(@"partition Setup\s", plantUml);
    }

    public static void AssertNoSetupPartition(string plantUml)
    {
        Assert.DoesNotContain("partition Setup", plantUml);
    }

    public static void AssertNoFocusMarkup(string plantUml)
    {
        // No focus-related PlantUML markup tags should be present in note content
        // Only check within note blocks (between "note left"/"note right" and "end note")
        var noteBlocks = ExtractNoteBlocks(plantUml);
        foreach (var note in noteBlocks)
        {
            Assert.DoesNotContain("<b>", note);
            Assert.DoesNotContain("</b>", note);
            Assert.DoesNotContain("<color:blue>", note);
            Assert.DoesNotContain("<color:lightgray>", note);
            Assert.DoesNotContain("<size:9>", note);
        }
    }

    public static void AssertFieldHasNoEmphasisMarkup(string plantUml, string fieldName)
    {
        var lines = GetLinesContaining(plantUml, fieldName);
        Assert.NotEmpty(lines);
        foreach (var line in lines)
        {
            Assert.DoesNotContain("<b>", line);
            Assert.DoesNotContain("<color:blue>", line);
        }
    }

    private static string[] GetLinesContaining(string plantUml, string fieldName)
    {
        return plantUml
            .Split('\n')
            .Where(line => line.Contains($"\"{fieldName}\"", StringComparison.OrdinalIgnoreCase)
                        || line.Contains($"\"{char.ToLower(fieldName[0])}{fieldName[1..]}\"", StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private static string[] ExtractNoteBlocks(string plantUml)
    {
        var blocks = new List<string>();
        var lines = plantUml.Split('\n');
        var inNote = false;
        var current = new List<string>();

        foreach (var line in lines)
        {
            if (line.TrimStart().StartsWith("note left") || line.TrimStart().StartsWith("note right"))
            {
                inNote = true;
                current.Clear();
            }
            else if (line.TrimStart().StartsWith("end note") && inNote)
            {
                blocks.Add(string.Join('\n', current));
                inNote = false;
            }
            else if (inNote)
            {
                current.Add(line);
            }
        }

        return blocks.ToArray();
    }
}
