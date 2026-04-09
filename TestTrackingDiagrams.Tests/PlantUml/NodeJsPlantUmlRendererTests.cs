using System.Diagnostics;
using TestTrackingDiagrams.PlantUml;

namespace TestTrackingDiagrams.Tests.PlantUml;

public class NodeJsPlantUmlRendererTests
{
    private static bool IsNodeAvailable()
    {
        try
        {
            var psi = new ProcessStartInfo("node", "--version")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            p?.WaitForExit(5000);
            return p?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void Renders_sequence_diagram_svg()
    {
        Assert.SkipWhen(!IsNodeAvailable(), "Node.js not available on PATH");

        var plantUml = """
            @startuml
            Alice -> Bob : Hello
            @enduml
            """;

        var svgBytes = NodeJsPlantUmlRenderer.Render(plantUml, PlantUmlImageFormat.Svg);
        var svg = System.Text.Encoding.UTF8.GetString(svgBytes);

        Assert.Contains("<svg", svg);
        Assert.Contains("Alice", svg);
        Assert.Contains("Bob", svg);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void Renders_class_diagram_svg()
    {
        Assert.SkipWhen(!IsNodeAvailable(), "Node.js not available on PATH");

        var plantUml = """
            @startuml
            class Foo {
              +bar(): void
            }
            class Bar
            Foo --> Bar
            @enduml
            """;

        var svgBytes = NodeJsPlantUmlRenderer.Render(plantUml, PlantUmlImageFormat.Svg);
        var svg = System.Text.Encoding.UTF8.GetString(svgBytes);

        Assert.Contains("<svg", svg);
        Assert.Contains("Foo", svg);
        Assert.Contains("Bar", svg);
    }
}
