using TUnit.Core;

namespace TestTrackingDiagrams.TUnit;

/// <summary>
/// Shared helper for extracting raw test method arguments from TUnit's test context.
/// Used by TestTrackingDiagrams.TUnit and TestTrackingDiagrams.LightBDD.TUnit to avoid
/// duplicating the extraction pattern.
/// </summary>
public static class TUnitArgumentExtractor
{
    /// <summary>
    /// Extracts raw test method arguments and their parameter names from a TUnit TestContext.
    /// </summary>
    public static (object?[]? Args, string[]? ParamNames) Extract(TestContext? context)
    {
        try
        {
            if (context is null)
                return (null, null);

            var args = context.Metadata.TestDetails.TestMethodArguments;
            var parameterMetadata = context.Metadata.TestDetails.MethodMetadata?.Parameters;
            if (args is not { Length: > 0 } || parameterMetadata is not { Length: > 0 })
                return (null, null);

            var paramNames = parameterMetadata.Select(p => p.Name).ToArray();

            if (paramNames.Length != args.Length)
                return (args, null);

            return (args, paramNames!);
        }
        catch
        {
            return (null, null);
        }
    }
}
