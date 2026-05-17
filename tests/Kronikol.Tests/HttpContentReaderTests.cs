using System.IO.Compression;
using System.Net.Http;
using System.Text;
using Kronikol.Tracking;

namespace Kronikol.Tests;

public class HttpContentReaderTests
{
    // --- Binary content detection tests ---

    [Fact]
    public async Task ReadContentAsStringAsync_BinaryWithEmbeddedJson_ExtractsJsonDocuments()
    {
        // Simulate HybridRow batch response: binary framing + embedded JSON documents
        var json1 = """{"id":"doc1","name":"Order"}""";
        var json2 = """{"id":"doc2","name":"Event"}""";
        var binaryPayload = BuildBinaryWithEmbeddedJson(json1, json2);

        var content = new ByteArrayContent(binaryPayload);

        var result = await HttpContentReader.ReadContentAsStringAsync(content, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Contains("doc1", result);
        Assert.Contains("doc2", result);
        Assert.DoesNotContain("\uFFFD", result); // No replacement characters
    }

    [Fact]
    public async Task ReadContentAsStringAsync_BinaryWithSingleJson_ExtractsSingleDocument()
    {
        var json = """{"id":"abc","status":"created","_etag":"\"00001234\""}""";
        var binaryPayload = BuildBinaryWithEmbeddedJson(json);

        var content = new ByteArrayContent(binaryPayload);

        var result = await HttpContentReader.ReadContentAsStringAsync(content, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Contains("abc", result);
        Assert.Contains("created", result);
    }

    [Fact]
    public async Task ReadContentAsStringAsync_BinaryWithNestedJson_ExtractsNestedDocuments()
    {
        var json = """{"id":"x1","address":{"city":"Auckland","zip":"1010"},"tags":["a","b"]}""";
        var binaryPayload = BuildBinaryWithEmbeddedJson(json);

        var content = new ByteArrayContent(binaryPayload);

        var result = await HttpContentReader.ReadContentAsStringAsync(content, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Contains("Auckland", result);
        Assert.Contains("1010", result);
    }

    [Fact]
    public async Task ReadContentAsStringAsync_PureBinaryNoJson_ReturnsBinaryPlaceholder()
    {
        // Pure binary with no embedded JSON
        var binaryPayload = new byte[] { 0x01, 0x00, 0x02, 0x03, 0xFF, 0xFE, 0x04, 0x05, 0x80, 0x90, 0xA0, 0xB0, 0xC0 };

        var content = new ByteArrayContent(binaryPayload);

        var result = await HttpContentReader.ReadContentAsStringAsync(content, CancellationToken.None);

        Assert.Equal("[binary content]", result);
    }

    [Fact]
    public async Task ReadContentAsStringAsync_NormalJson_ReturnedUnchanged()
    {
        // Normal JSON should pass through without any binary detection overhead affecting output
        var json = """{"id":"123","name":"test","nested":{"a":1}}""";
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var result = await HttpContentReader.ReadContentAsStringAsync(content, CancellationToken.None);

        Assert.Equal(json, result);
    }

    [Fact]
    public async Task ReadContentAsStringAsync_EmptyContent_ReturnsEmpty()
    {
        var content = new ByteArrayContent([]);

        var result = await HttpContentReader.ReadContentAsStringAsync(content, CancellationToken.None);

        Assert.Equal("", result);
    }

    [Fact]
    public async Task ReadContentAsStringAsync_BinaryWithJsonContainingBraces_HandlesStringLiterals()
    {
        // JSON with braces inside string values - must not confuse the extractor
        var json = """{"id":"x","template":"Hello {name}, welcome to {city}"}""";
        var binaryPayload = BuildBinaryWithEmbeddedJson(json);

        var content = new ByteArrayContent(binaryPayload);

        var result = await HttpContentReader.ReadContentAsStringAsync(content, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Contains("Hello {name}", result);
    }

    /// <summary>
    /// Builds a byte array that simulates binary framing (like HybridRow+RecordIO)
    /// with JSON documents embedded within. This mimics what CosmosDB TransactionalBatch
    /// responses look like at the HTTP level.
    /// </summary>
    private static byte[] BuildBinaryWithEmbeddedJson(params string[] jsonDocuments)
    {
        using var ms = new MemoryStream();
        // Binary header (HybridRow version byte + binary framing)
        ms.Write(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00 });

        foreach (var json in jsonDocuments)
        {
            // Binary field framing (status code, sub-status, etc.)
            ms.Write(new byte[] { 0xC8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
            // etag-like string prefixed with binary length
            var etagBytes = Encoding.UTF8.GetBytes("$" + Guid.NewGuid().ToString());
            ms.WriteByte((byte)etagBytes.Length);
            ms.Write(etagBytes);
            // Binary field marker before resourceBody
            ms.Write(new byte[] { 0x06, 0x00 });
            // The JSON document bytes
            var jsonBytes = Encoding.UTF8.GetBytes(json);
            ms.Write(jsonBytes);
            // Binary trailer (request charge, retry-after)
            ms.Write(new byte[] { 0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x00, 0x00 });
        }

        // Binary footer
        ms.Write(new byte[] { 0x00, 0x00, 0x00, 0x00 });
        return ms.ToArray();
    }

    // --- Existing tests ---
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
