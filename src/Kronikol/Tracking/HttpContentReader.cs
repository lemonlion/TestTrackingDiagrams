using System.IO.Compression;

namespace Kronikol.Tracking;

public static class HttpContentReader
{
    public static async Task<string?> ReadContentAsStringAsync(HttpContent content, CancellationToken ct)
    {
        var encoding = content.Headers.ContentEncoding;

        if (encoding.Contains("gzip"))
        {
            var bytes = await content.ReadAsByteArrayAsync(ct);
            using var compressed = new MemoryStream(bytes);
            using var gzip = new GZipStream(compressed, CompressionMode.Decompress);
            using var reader = new StreamReader(gzip);
            return await reader.ReadToEndAsync(ct);
        }

        if (encoding.Contains("deflate"))
        {
            var bytes = await content.ReadAsByteArrayAsync(ct);
            using var compressed = new MemoryStream(bytes);
            using var deflate = new DeflateStream(compressed, CompressionMode.Decompress);
            using var reader = new StreamReader(deflate);
            return await reader.ReadToEndAsync(ct);
        }

        if (encoding.Contains("br"))
        {
            var bytes = await content.ReadAsByteArrayAsync(ct);
            using var compressed = new MemoryStream(bytes);
            using var brotli = new BrotliStream(compressed, CompressionMode.Decompress);
            using var reader = new StreamReader(brotli);
            return await reader.ReadToEndAsync(ct);
        }

        return await content.ReadAsStringAsync(ct);
    }
}
