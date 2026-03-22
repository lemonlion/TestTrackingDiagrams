using TestStack.BDDfy;

namespace TestTrackingDiagrams.BDDfy.xUnit3;

internal static class StepExtensions
{
    private static readonly string[] Keywords = ["Given", "When", "Then", "And", "But"];

    public static BDDfyStepInfo ToBDDfyStepInfo(this Step step)
    {
        var title = step.Title.Trim();
        var firstSpace = title.IndexOf(' ');

        if (firstSpace > 0)
        {
            var firstWord = title[..firstSpace];
            if (Keywords.Any(k => k.Equals(firstWord, StringComparison.OrdinalIgnoreCase)))
                return new BDDfyStepInfo(firstWord, title[(firstSpace + 1)..]);
        }

        return new BDDfyStepInfo("Step", title);
    }
}
