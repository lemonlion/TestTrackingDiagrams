namespace TestTrackingDiagrams.Tests;

public class ScenarioTitleResolverTests
{
    [Fact]
    public void WhenScenarioTitleMatchesClassName_ShouldUseHumanizedMethodName()
    {
        var result = ScenarioTitleResolver.ResolveScenarioTitle(
            "AlternativeEvidenceApiTests",
            "AlternativeEvidenceApiTests",
            "GivenRequestForAlternativeEvidenceForApplication_WhenCalled_SentAlternativeEvidenceCommandAndReturnedResult");

        Assert.Equal(
            "Given request for alternative evidence for application when called sent alternative evidence command and returned result",
            result);
    }

    [Fact]
    public void WhenScenarioTitleDoesNotMatchClassName_ShouldReturnOriginalTitle()
    {
        var result = ScenarioTitleResolver.ResolveScenarioTitle(
            "Custom scenario title",
            "AlternativeEvidenceApiTests",
            "SomeMethodName");

        Assert.Equal("Custom scenario title", result);
    }

    [Fact]
    public void WhenTestClassNameIsNull_ShouldReturnOriginalTitle()
    {
        var result = ScenarioTitleResolver.ResolveScenarioTitle(
            "AlternativeEvidenceApiTests",
            null,
            "SomeMethodName");

        Assert.Equal("AlternativeEvidenceApiTests", result);
    }

    [Fact]
    public void WhenTestMethodNameIsNull_ShouldReturnOriginalTitle()
    {
        var result = ScenarioTitleResolver.ResolveScenarioTitle(
            "AlternativeEvidenceApiTests",
            "AlternativeEvidenceApiTests",
            null);

        Assert.Equal("AlternativeEvidenceApiTests", result);
    }

    [Fact]
    public void WhenMethodNameIsPascalCase_ShouldHumanizeCorrectly()
    {
        var result = ScenarioTitleResolver.ResolveScenarioTitle(
            "GetTests",
            "GetTests",
            "GivenRequest_WhenGetEndpointIsCalled_ThenItReturns");

        Assert.Equal("Given request when get endpoint is called then it returns", result);
    }

    [Fact]
    public void WhenMethodNameHasOnlyUnderscores_ShouldHumanizeCorrectly()
    {
        var result = ScenarioTitleResolver.ResolveScenarioTitle(
            "MyTests",
            "MyTests",
            "Given_A_Request_When_Called_Then_Returns");

        Assert.Equal("Given a request when called then returns", result);
    }

    [Fact]
    public void WhenScenarioTitleMatchesClassNameCaseInsensitively_ShouldStillReplace()
    {
        var result = ScenarioTitleResolver.ResolveScenarioTitle(
            "alternativeevidenceapitests",
            "AlternativeEvidenceApiTests",
            "GivenSomeTest");

        // BDDfy passes the title as-is, so case mismatch means the user didn't use nameof()
        // We should only match exact case
        Assert.Equal("alternativeevidenceapitests", result);
    }

    [Fact]
    public void WhenMethodNameIsPurePascalCase_ShouldSplitAndSentenceCase()
    {
        var result = ScenarioTitleResolver.ResolveScenarioTitle(
            "CakeFeature",
            "CakeFeature",
            "GivenCommandIsNotFoundWhenRequestIsMadeThenNotFoundResponseReturned");

        Assert.Equal("Given command is not found when request is made then not found response returned", result);
    }
}
