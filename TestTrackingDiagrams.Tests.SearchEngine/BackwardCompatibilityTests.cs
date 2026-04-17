namespace TestTrackingDiagrams.Tests.SearchEngine;

/// <summary>
/// Verifies that isAdvancedSearch correctly distinguishes legacy inputs from advanced inputs.
/// </summary>
public class BackwardCompatibilityTests : JintTestBase
{
    #region Legacy inputs (isAdvancedSearch returns false)

    [Theory]
    [InlineData("chocolate")]
    [InlineData("chocolate cake")]
    [InlineData("\"chocolate cake\"")]
    [InlineData("\"chocolate cake\" vanilla")]
    [InlineData("@smoke")]
    [InlineData("@smoke and @regression")]
    [InlineData("@smoke or @regression")]
    [InlineData("not @slow")]
    [InlineData("(@smoke or @regression) and not @slow")]
    [InlineData("")]
    [InlineData("   ")]
    public void Legacy_inputs_are_not_advanced(string input)
    {
        Assert.False(CallIsAdvancedSearch(input));
    }

    #endregion

    #region Advanced inputs (isAdvancedSearch returns true)

    [Theory]
    [InlineData("a && b")]
    [InlineData("a || b")]
    [InlineData("!!a")]
    [InlineData("@smoke && @regression")]
    [InlineData("@smoke || @regression")]
    [InlineData("!!@slow")]
    [InlineData("login && $failed")]
    [InlineData("\"error message\" || timeout")]
    [InlineData("(a || b) && c")]
    public void Advanced_inputs_are_detected(string input)
    {
        Assert.True(CallIsAdvancedSearch(input));
    }

    #endregion

    #region Advanced search preserves basic text matching

    [Fact]
    public void Single_word_with_explicit_and_still_works()
    {
        // Explicit && behaves like implicit AND
        var result1 = CallMatch("chocolate && cake", "chocolate cake is tasty", [], "Passed");
        Assert.True(result1);

        var result2 = CallMatch("chocolate && cake", "vanilla cake is tasty", [], "Passed");
        Assert.False(result2);
    }

    [Fact]
    public void Quoted_phrase_with_or_works()
    {
        var result = CallMatch("\"chocolate cake\" || vanilla", "i like vanilla", [], "Passed");
        Assert.True(result);
    }

    [Fact]
    public void Tag_with_explicit_and_works_like_legacy_and()
    {
        var result1 = CallMatch("@smoke && @regression", "some text", ["smoke", "regression"], "Passed");
        Assert.True(result1);

        var result2 = CallMatch("@smoke && @regression", "some text", ["smoke"], "Passed");
        Assert.False(result2);
    }

    #endregion
}
