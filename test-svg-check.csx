using System.Text;
using TestTrackingDiagrams.PlantUml.Ikvm;
using TestTrackingDiagrams;

var puml = @"@startuml
participant A
participant B
A -> B: [[#iflow-test-123 Click me]]
B --> A: [[#iflow-test-123-res Response]]
@enduml";

var bytes = IkvmPlantUmlRenderer.Render(puml, PlantUmlImageFormat.Svg);
var svg = Encoding.UTF8.GetString(bytes);
Console.WriteLine(svg);
