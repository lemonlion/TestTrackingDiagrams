using System.IO.Compression;
using System.Text;

namespace TestTrackingDiagrams.PlantUml;

/// <summary>
/// Encodes PlantUML source text into the compressed format used by PlantUML server URLs.
/// </summary>
public static class PlantUmlTextEncoder
{
    public static string Encode(string plantUml)
    {
        using var plantUmlStringReader = new StringReader(plantUml);
        return Encode(plantUmlStringReader);
    }

    public static string Encode(TextReader reader)
    {
        using var output = new MemoryStream();
        using (var writer = new StreamWriter(new DeflateStream(output, CompressionLevel.Optimal), Encoding.UTF8))
            writer.Write(reader.ReadToEnd());
        return Encode(output.ToArray());
    }

    private static string Encode(IReadOnlyList<byte> bytes)
    {
        var length = bytes.Count;
        var encodedString = new StringBuilder((length + 2) / 3 * 4);
        for (var i = 0; i < length; i += 3)
        {
            var b1 = bytes[i];
            var b2 = i + 1 < length ? bytes[i + 1] : (byte)0;
            var b3 = i + 2 < length ? bytes[i + 2] : (byte)0;
            Append3Bytes(encodedString, b1, b2, b3);
        }
        return encodedString.ToString();
    }

    private static void Append3Bytes(StringBuilder sb, byte b1, byte b2, byte b3)
    {
        var c1 = b1 >> 2;
        var c2 = (b1 & 0x3) << 4 | b2 >> 4;
        var c3 = (b2 & 0xF) << 2 | b3 >> 6;
        var c4 = b3 & 0x3F;
        sb.Append(EncodeByte((byte)(c1 & 0x3F)));
        sb.Append(EncodeByte((byte)(c2 & 0x3F)));
        sb.Append(EncodeByte((byte)(c3 & 0x3F)));
        sb.Append(EncodeByte((byte)(c4 & 0x3F)));
    }

    private static char EncodeByte(byte b)
    {
        if (b < 10)
            return (char)(48 + b);
        b -= 10;
        if (b < 26)
            return (char)(65 + b);
        b -= 26;
        if (b < 26)
            return (char)(97 + b);
        b -= 26;
        if (b == 0)
            return '-';
        if (b == 1)
            return '_';
        return '?';
    }
}