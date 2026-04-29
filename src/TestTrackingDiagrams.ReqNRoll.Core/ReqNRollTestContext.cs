namespace TestTrackingDiagrams.ReqNRoll;

/// <summary>
/// Manages the current test information and step type for Reqnroll scenarios using async-local storage.
/// </summary>
public static class ReqNRollTestContext
{
    private static readonly AsyncLocal<(string Name, string Id)?> CurrentTestInfoLocal = new();
    private static readonly AsyncLocal<string?> CurrentStepTypeLocal = new();

    public static (string Name, string Id)? CurrentTestInfo
    {
        get => CurrentTestInfoLocal.Value;
        set => CurrentTestInfoLocal.Value = value;
    }

    public static string? CurrentStepType
    {
        get => CurrentStepTypeLocal.Value;
        set => CurrentStepTypeLocal.Value = value;
    }
}