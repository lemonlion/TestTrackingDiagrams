using System.IO.Compression;
using System.Net.Http;
using System.Text;
using Kronikol.Tracking;

namespace Kronikol.Tests;

public class HttpContentReaderTests
{
    [Fact]
    public async Task ReadContentAsStringAsync_PlainText_ReturnsContent()
    {
        var content = new StringContent("hello world", Encoding.UTF8, "text/plain");

        var result = await HttpContentReader.ReadContentAsStringAsync(content, CancellationToken.None);

        Assert.Equal("hello world", result);
    }

    [Fact]
    public async Task ReadContentAsStringAsync_Json_ReturnsContent()
    {
        var json = """{"id":"123","name":"test"}""";
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var result = await HttpContentReader.ReadContentAsStringAsync(content, CancellationToken.None);

        Assert.Equal(json, result);
    }

    [Fact]
    public async Task ReadContentAsStringAsync_GzipEncoded_DecompressesContent()
    {
        var json = """{"id":"123","name":"gzipped-data"}""";
        var compressed = GzipCompress(json);

        var content = new ByteArrayContent(compressed);
        content.Headers.ContentEncoding.Add("gzip");

        var result = await HttpContentReader.ReadContentAsStringAsync(content, CancellationToken.None);

        Assert.Equal(json, result);
    }

    [Fact]
    public async Task ReadContentAsStringAsync_DeflateEncoded_DecompressesContent()
    {
        var json = """{"id":"456","name":"deflated-data"}""";
        var compressed = DeflateCompress(json);

        var content = new ByteArrayContent(compressed);
        content.Headers.ContentEncoding.Add("deflate");

        var result = await HttpContentReader.ReadContentAsStringAsync(content, CancellationToken.None);

        Assert.Equal(json, result);
    }

    [Fact]
    public async Task ReadContentAsStringAsync_BrotliEncoded_DecompressesContent()
    {
        var json = """{"id":"789","name":"brotli-data"}""";
        var compressed = BrotliCompress(json);

        var content = new ByteArrayContent(compressed);
        content.Headers.ContentEncoding.Add("br");

        var result = await HttpContentReader.ReadContentAsStringAsync(content, CancellationToken.None);

        Assert.Equal(json, result);
    }

    [Fact]
    public async Task ReadContentAsStringAsync_NoContentEncoding_ReturnsAsString()
    {
        var content = new ByteArrayContent(Encoding.UTF8.GetBytes("raw bytes"));

        var result = await HttpContentReader.ReadContentAsStringAsync(content, CancellationToken.None);

        Assert.Equal("raw bytes", result);
    }

    [Fact]
    public async Task ReadContentAsStringAsync_LargeGzipPayload_DecompressesCorrectly()
    {
        var json = new string('x', 10_000);
        var compressed = GzipCompress(json);

        var content = new ByteArrayContent(compressed);
        content.Headers.ContentEncoding.Add("gzip");

        var result = await HttpContentReader.ReadContentAsStringAsync(content, CancellationToken.None);

        Assert.Equal(json, result);
    }

    private static byte[] GzipCompress(string text)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
        using (var writer = new StreamWriter(gzip))
            writer.Write(text);
        return output.ToArray();
    }

    private static byte[] DeflateCompress(string text)
    {
        using var output = new MemoryStream();
        using (var deflate = new DeflateStream(output, CompressionLevel.Optimal))
        using (var writer = new StreamWriter(deflate))
            writer.Write(text);
        return output.ToArray();
    }

    private static byte[] BrotliCompress(string text)
    {
        using var output = new MemoryStream();
        using (var brotli = new BrotliStream(output, CompressionLevel.Optimal))
        using (var writer = new StreamWriter(brotli))
            writer.Write(text);
        return output.ToArray();
    }
}
