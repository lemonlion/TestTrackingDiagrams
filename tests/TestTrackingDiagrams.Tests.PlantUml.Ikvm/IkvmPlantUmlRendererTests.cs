namespace TestTrackingDiagrams.Tests.PlantUml.Ikvm;

public class IkvmPlantUmlRendererTests
{
    private const string SimpleDiagram = """
        @startuml
        Bob -> Alice : hello
        @enduml
        """;

    [Fact]
    public void Render_png_produces_valid_png_bytes()
    {
        var result = IkvmPlantUmlRenderer.Render(SimpleDiagram, PlantUmlImageFormat.Png);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
        // PNG magic bytes
        Assert.Equal(0x89, result[0]);
        Assert.Equal(0x50, result[1]);
        Assert.Equal(0x4E, result[2]);
        Assert.Equal(0x47, result[3]);
    }

    [Fact]
    public void Render_svg_produces_valid_svg_content()
    {
        var result = IkvmPlantUmlRenderer.Render(SimpleDiagram, PlantUmlImageFormat.Svg);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
        var svg = System.Text.Encoding.UTF8.GetString(result);
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Render_base64png_renders_as_png()
    {
        var result = IkvmPlantUmlRenderer.Render(SimpleDiagram, PlantUmlImageFormat.Base64Png);

        // Base64Png should render actual PNG bytes (the caller handles base64 encoding)
        Assert.Equal(0x89, result[0]);
        Assert.Equal(0x50, result[1]);
    }

    [Fact]
    public void Render_base64svg_renders_as_svg()
    {
        var result = IkvmPlantUmlRenderer.Render(SimpleDiagram, PlantUmlImageFormat.Base64Svg);

        var svg = System.Text.Encoding.UTF8.GetString(result);
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Render_complex_diagram_produces_output()
    {
        var plantUml = """
            @startuml
            participant "Test Caller" as caller
            participant "Order Service" as orderService
            participant "Payment Gateway" as paymentGateway
            
            caller -> orderService: POST /api/orders
            note left
            {"customerId": "C123", "amount": 99.99}
            end note
            orderService -> paymentGateway: POST /api/payments
            paymentGateway --> orderService: 200 OK
            orderService --> caller: 201 Created
            @enduml
            """;

        var result = IkvmPlantUmlRenderer.Render(plantUml, PlantUmlImageFormat.Png);

        Assert.NotNull(result);
        Assert.True(result.Length > 100, "Complex diagram should produce substantial output");
    }
}
