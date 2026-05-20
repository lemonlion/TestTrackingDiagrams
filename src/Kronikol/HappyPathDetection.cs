namespace Kronikol;

/// <summary>
/// Provides centralized detection of happy path tags across all tag-based adapters.
/// Recognizes "happy-path", "happy_path", and "happypath" (case-insensitive).
/// </summary>
public static class HappyPathDetection
{
    /// <summary>
    /// Returns true if the given tag represents a happy path designation.
    /// Matches "happy-path", "happy_path", or "happypath" case-insensitively.
    /// </summary>
    public static bool IsHappyPathTag(string tag)
    {
        return tag.Equals("happy-path", StringComparison.OrdinalIgnoreCase)
            || tag.Equals("happy_path", StringComparison.OrdinalIgnoreCase)
            || tag.Equals("happypath", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns true if any tag in the collection is a happy path tag.
    /// </summary>
    public static bool AnyHappyPathTag(IEnumerable<string> tags)
    {
        return tags.Any(IsHappyPathTag);
    }
}
