namespace Example.Api.Tests.Integration.Helpers;

public static class TestProjects
{
    public static readonly string SolutionRoot = Path.GetFullPath(
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."));

    public static readonly string TestsRoot = Path.Combine(SolutionRoot, "tests");

    public const string XUnit2 = "Example.Api.Tests.Component.xUnit2";
    public const string XUnit3 = "Example.Api.Tests.Component.xUnit3";
    public const string NUnit4 = "Example.Api.Tests.Component.NUnit4";
    public const string LightBddXUnit2 = "Example.Api.Tests.Component.LightBDD.xUnit2";
    public const string BDDfyXUnit3 = "Example.Api.Tests.Component.BDDfy.xUnit3";
    public const string ReqNRollXUnit2 = "Example.Api.Tests.Component.ReqNRoll.xUnit2";
    public const string ReqNRollXUnit3 = "Example.Api.Tests.Component.ReqNRoll.xUnit3";
    public const string TUnit = "Example.Api.Tests.Component.TUnit";

    /// <summary>
    /// Projects that use Microsoft.Testing.Platform (e.g. TUnit) and require
    /// <c>dotnet run</c> instead of <c>dotnet test</c> on .NET 10+.
    /// </summary>
    public static readonly HashSet<string> MicrosoftTestingPlatformProjects = [TUnit];

    public static readonly string[] All =
    [
        XUnit2,
        XUnit3,
        NUnit4,
        LightBddXUnit2,
        BDDfyXUnit3,
        ReqNRollXUnit2,
        ReqNRollXUnit3,
        TUnit
    ];

    public static string GetProjectPath(string projectName) =>
        Path.Combine(TestsRoot, projectName);

    public static string GetReportsFolderPath(string projectName) =>
        Path.Combine(GetProjectPath(projectName), "bin", "Debug", "net10.0", "Reports");
}
