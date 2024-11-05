namespace TestTrackingDiagrams.Extensions;

public static class StringExtensions
{
    public static IEnumerable<string> SplitBy(this string value, int chunkLength)
    {
        if (string.IsNullOrEmpty(value)) throw new ArgumentException(nameof(value));
        if (chunkLength < 1) throw new ArgumentException(nameof(chunkLength));

        for (int i = 0; i < value.Length; i += chunkLength)
        {
            if (chunkLength + i > value.Length)
                chunkLength = value.Length - i;

            yield return value.Substring(i, chunkLength);
        }
    }
}
