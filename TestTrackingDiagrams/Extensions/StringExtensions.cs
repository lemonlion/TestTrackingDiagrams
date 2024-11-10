namespace TestTrackingDiagrams.Extensions;

public static class StringExtensions
{
    public static IEnumerable<string> ChunksUpTo(this string value, int chunkLength)
    {
        return value.Chunk(chunkLength).Select(x => new string(x));
    }

    public static string StringJoin(this IEnumerable<string> value, string separator) => string.Join(separator, value);
}
