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

    // ── AppendTestParameters ──

    [Fact]
    public void AppendTestParameters_WhenDisplayNameHasParameters_ShouldAppendInBrackets()
    {
        var result = ScenarioTitleResolver.AppendTestParameters(
            "Given bad xss like values for request",
            "Namespace.Class.GivenBadXssLikeValuesForRequest(clientId: \"<script>\", sessionId: \"abc\")");

        Assert.Equal("Given bad xss like values for request [clientId: \"<script>\", sessionId: \"abc\"]", result);
    }

    [Fact]
    public void AppendTestParameters_WhenDisplayNameHasNoParameters_ShouldReturnOriginalTitle()
    {
        var result = ScenarioTitleResolver.AppendTestParameters(
            "Some scenario title",
            "Namespace.Class.SomeMethodName");

        Assert.Equal("Some scenario title", result);
    }

    [Fact]
    public void AppendTestParameters_WhenDisplayNameIsNull_ShouldReturnOriginalTitle()
    {
        var result = ScenarioTitleResolver.AppendTestParameters(
            "Some scenario title",
            null);

        Assert.Equal("Some scenario title", result);
    }

    [Fact]
    public void AppendTestParameters_WhenDisplayNameHasEmptyParentheses_ShouldReturnOriginalTitle()
    {
        var result = ScenarioTitleResolver.AppendTestParameters(
            "Some scenario title",
            "Namespace.Class.SomeMethodName()");

        Assert.Equal("Some scenario title", result);
    }

    [Fact]
    public void AppendTestParameters_WhenDisplayNameHasMultipleInlineDataValues_ShouldAppendAll()
    {
        var result = ScenarioTitleResolver.AppendTestParameters(
            "Given bad xss like values",
            "Ns.C.Method(clientId: \"val1\", journeyId: \"val2\", appId: \"val3\", mobile: \"000\")");

        Assert.Equal("Given bad xss like values [clientId: \"val1\", journeyId: \"val2\", appId: \"val3\", mobile: \"000\"]", result);
    }

    [Fact]
    public void AppendTestParameters_WhenParametersExceedMaxLength_ShouldTruncate()
    {
        var longValue = new string('x', 300);
        var result = ScenarioTitleResolver.AppendTestParameters(
            "Some title",
            $"Ns.C.Method(account: \"{longValue}\")");

        Assert.EndsWith("...]", result);
        Assert.StartsWith("Some title [", result);
        Assert.True(result.Length < 300, "Result should be significantly shorter than the raw parameter content");
    }

    [Fact]
    public void AppendTestParameters_WhenParametersAreExactlyAtMaxLength_ShouldNotTruncate()
    {
        // A short parameter string should be kept as-is
        var result = ScenarioTitleResolver.AppendTestParameters(
            "Some title",
            "Ns.C.Method(x: \"short\")");

        Assert.Equal("Some title [x: \"short\"]", result);
        Assert.DoesNotContain("...", result);
    }
}
