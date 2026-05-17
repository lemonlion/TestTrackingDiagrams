using System.IO.Compression;
using System.Text;
using System.Text.Json;

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

        // Read bytes to detect binary content before lossy UTF-8 conversion
        var raw = await content.ReadAsByteArrayAsync(ct);
        if (raw.Length == 0)
            return "";

        if (!ContainsBinaryContent(raw))
            return Encoding.UTF8.GetString(raw);

        // Binary content detected — try to extract embedded JSON documents
        var extracted = ExtractJsonDocuments(raw);
        return extracted ?? "[binary content]";
    }

    /// <summary>
    /// Checks whether a byte array contains binary (non-text) content by scanning
    /// the first portion for control characters outside of standard whitespace.
    /// </summary>
    internal static bool ContainsBinaryContent(byte[] bytes)
    {
        var scanLength = Math.Min(bytes.Length, 256);
        var binaryCount = 0;

        for (var i = 0; i < scanLength; i++)
        {
            var b = bytes[i];
            // Control characters below TAB (0x09), or between 0x0E-0x1F (excluding CR, LF, TAB)
            // Also null bytes (0x00) are a strong binary indicator
            if (b == 0x00 || (b < 0x09) || (b > 0x0D && b < 0x20))
                binaryCount++;
        }

        // If more than ~3% of scanned bytes are binary control chars, treat as binary
        return binaryCount > 0 && (binaryCount * 100 / scanLength) >= 3;
    }

    /// <summary>
    /// Scans raw bytes for embedded JSON objects. Uses brace-depth counting with
    /// string-literal awareness to correctly handle nested objects and braces within strings.
    /// </summary>
    internal static string? ExtractJsonDocuments(byte[] bytes)
    {
        var documents = new List<string>();

        for (var i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] != (byte)'{')
                continue;

            // Found a potential JSON start — track brace depth
            var start = i;
            var depth = 0;
            var inString = false;
            var escaped = false;
            var end = -1;

            for (var j = i; j < bytes.Length; j++)
            {
                var b = bytes[j];

                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (b == (byte)'\\' && inString)
                {
                    escaped = true;
                    continue;
                }

                if (b == (byte)'"')
                {
                    inString = !inString;
                    continue;
                }

                if (inString)
                    continue;

                if (b == (byte)'{')
                    depth++;
                else if (b == (byte)'}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        end = j;
                        break;
                    }
                }
            }

            if (end <= start)
                continue;

            var candidate = Encoding.UTF8.GetString(bytes, start, end - start + 1);

            // Validate it's actually parseable JSON
            try
            {
                using var doc = JsonDocument.Parse(candidate);
                documents.Add(candidate);
            }
            catch (JsonException)
            {
                // Not valid JSON — skip this candidate
            }

            // Advance past this document
            i = end;
        }

        if (documents.Count == 0)
            return null;

        if (documents.Count == 1)
            return documents[0];

        return "[" + string.Join(",", documents) + "]";
    }
}
