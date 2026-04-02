# PlantUML IKVM — Local Diagram Rendering

For full documentation, see the [PlantUML IKVM wiki page](https://github.com/lemonlion/TestTrackingDiagrams/wiki/Integration-PlantUML-IKVM).

## Quick Start

```
dotnet add package TestTrackingDiagrams.PlantUml.Ikvm
```

### Inline Base64 Images

```csharp
new ReportConfigurationOptions
{
    PlantUmlImageFormat = PlantUmlImageFormat.Base64Png,
    LocalDiagramRenderer = IkvmPlantUmlRenderer.Render
}
```

### File-Based Images

```csharp
var reportsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
new ReportConfigurationOptions
{
    PlantUmlImageFormat = PlantUmlImageFormat.Png,
    LocalDiagramRenderer = IkvmPlantUmlRenderer.Render,
    LocalDiagramImageDirectory = Path.Combine(reportsDir, "images")
}
```
