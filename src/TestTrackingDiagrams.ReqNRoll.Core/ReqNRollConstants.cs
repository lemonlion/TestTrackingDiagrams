namespace TestTrackingDiagrams.ReqNRoll;

/// <summary>
/// Provides constants used by the Reqnroll integration for scenario identification and tag-based classification.
/// </summary>
public static class ReqNRollConstants
{
    public const string ScenarioRuntimeIdKey = "TestTrackingDiagrams.RuntimeId";
    public const string StepsCollectionKey = "TestTrackingDiagrams.Steps";
    public const string HappyPathTag = "happy-path";
    public const string EndpointTagPrefix = "endpoint:";
    public const string CategoryTagPrefix = "category:";
    public const string StepStopwatchKey = "TestTrackingDiagrams.StepStopwatch";
}