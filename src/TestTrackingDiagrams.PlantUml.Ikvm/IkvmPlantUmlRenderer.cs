using java.io;
using net.sourceforge.plantuml;

namespace TestTrackingDiagrams;

/// <summary>
/// Provides local PlantUML rendering using the IKVM Java bridge, converting PlantUML source text to PNG or SVG images.
/// </summary>
public static class IkvmPlantUmlRenderer
{
    public static byte[] Render(string plantUml, PlantUmlImageFormat format)
    {
        var fileFormat = format switch
        {
            PlantUmlImageFormat.Png or PlantUmlImageFormat.Base64Png => FileFormat.PNG,
            PlantUmlImageFormat.Svg or PlantUmlImageFormat.Base64Svg => FileFormat.SVG,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported image format for local rendering.")
        };

        var reader = new SourceStringReader(plantUml);
        var outputStream = new ByteArrayOutputStream();
        reader.outputImage(outputStream, new FileFormatOption(fileFormat));
        return outputStream.toByteArray();
    }
}