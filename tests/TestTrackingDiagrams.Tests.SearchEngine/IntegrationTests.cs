namespace TestTrackingDiagrams.Tests.SearchEngine;

/// <summary>
/// End-to-end tests: string input → advancedSearchMatch() → boolean result.
/// Tests a fixed dataset of 6 scenarios with known attributes.
/// </summary>
public class IntegrationTests : JintTestBase
{
    // S1: "user login success"          tags: smoke, happy-path      status: Passed
    // S2: "user login failure invalid password"  tags: smoke, regression  status: Failed
    // S3: "payment timeout error"       tags: critical, regression   status: Failed
    // S4: "order placed successfully"   tags: happy-path             status: Passed
    // S5: "admin dashboard loads"       tags: smoke                  status: Passed
    // S6: "refund processed"            tags: critical               status: Skipped

    private record Scenario(string SearchText, string[] Tags, string Status);

    private static readonly Scenario S1 = new("user login success", ["smoke", "happy-path"], "Passed");
    private static readonly Scenario S2 = new("user login failure invalid password", ["smoke", "regression"], "Failed");
    private static readonly Scenario S3 = new("payment timeout error", ["critical", "regression"], "Failed");
    private static readonly Scenario S4 = new("order placed successfully", ["happy-path"], "Passed");
    private static readonly Scenario S5 = new("admin dashboard loads", ["smoke"], "Passed");
    private static readonly Scenario S6 = new("refund processed", ["critical"], "Skipped");

    private static readonly Scenario[] AllScenarios = [S1, S2, S3, S4, S5, S6];

    private List<Scenario> Match(string input)
    {
        var matches = new List<Scenario>();
        foreach (var s in AllScenarios)
        {
            var result = CallMatch(input, s.SearchText, s.Tags, s.Status);
            if (result == true)
                matches.Add(s);
        }
        return matches;
    }

    [Fact]
    public void Login_matches_S1_and_S2()
    {
        var matches = Match("login");
        Assert.Equal([S1, S2], matches);
    }

    [Fact]
    public void Login_and_success_matches_S1_only()
    {
        var matches = Match("login && success");
        Assert.Equal([S1], matches);
    }

    [Fact]
    public void Login_or_payment_matches_S1_S2_S3()
    {
        var matches = Match("login || payment");
        Assert.Equal([S1, S2, S3], matches);
    }

    [Fact]
    public void Not_login_matches_S3_S4_S5_S6()
    {
        var matches = Match("!!login");
        Assert.Equal([S3, S4, S5, S6], matches);
    }

    [Fact]
    public void Login_and_not_success_matches_S2_only()
    {
        var matches = Match("login && !!success");
        Assert.Equal([S2], matches);
    }

    [Fact]
    public void Tag_smoke_matches_S1_S2_S5()
    {
        var matches = Match("@smoke");
        Assert.Equal([S1, S2, S5], matches);
    }

    [Fact]
    public void Tag_smoke_and_status_failed_matches_S2()
    {
        var matches = Match("@smoke && $failed");
        Assert.Equal([S2], matches);
    }

    [Fact]
    public void Tag_smoke_or_critical_matches_S1_S2_S3_S5_S6()
    {
        var matches = Match("@smoke || @critical");
        Assert.Equal([S1, S2, S3, S5, S6], matches);
    }

    [Fact]
    public void Grouped_tags_and_status_failed_matches_S2_S3()
    {
        var matches = Match("(@smoke || @critical) && $failed");
        Assert.Equal([S2, S3], matches);
    }

    [Fact]
    public void Quoted_phrase_invalid_password_matches_S2()
    {
        var matches = Match("\"invalid password\"");
        Assert.Equal([S2], matches);
    }

    [Fact]
    public void Phrase_or_word_matches_S2_S3()
    {
        var matches = Match("\"invalid password\" || timeout");
        Assert.Equal([S2, S3], matches);
    }

    [Fact]
    public void Not_tag_and_status_matches_S4()
    {
        var matches = Match("!!@smoke && $passed");
        Assert.Equal([S4], matches);
    }

    [Fact]
    public void Grouped_text_or_and_status_matches_S2_S3()
    {
        var matches = Match("(login || payment) && $failed");
        Assert.Equal([S2, S3], matches);
    }

    [Fact]
    public void Three_way_and_matches_S3()
    {
        var matches = Match("error && @critical && $failed");
        Assert.Equal([S3], matches);
    }

    [Fact]
    public void Happy_path_and_not_passed_matches_nothing()
    {
        var matches = Match("@happy-path && !!$passed");
        Assert.Empty(matches);
    }

    [Fact]
    public void Empty_string_returns_null_for_all()
    {
        foreach (var s in AllScenarios)
        {
            var result = CallMatch("", s.SearchText, s.Tags, s.Status);
            Assert.Null(result);
        }
    }

    [Fact]
    public void Whitespace_only_returns_null_for_all()
    {
        foreach (var s in AllScenarios)
        {
            var result = CallMatch("   ", s.SearchText, s.Tags, s.Status);
            Assert.Null(result);
        }
    }

    [Fact]
    public void Complex_nested_expression()
    {
        // (login && !!@regression) || ($skipped && @critical)
        // S1: login=yes, !regression=yes → true
        // S2: login=yes, !regression=no → false; skipped=no → false
        // S6: login=no; skipped=yes, critical=yes → true
        var matches = Match("(login && !!@regression) || ($skipped && @critical)");
        Assert.Equal([S1, S6], matches);
    }

    [Fact]
    public void Not_grouped_expression()
    {
        // !!(login || payment) → NOT(login OR payment) → S4, S5, S6
        var matches = Match("!!(login || payment)");
        Assert.Equal([S4, S5, S6], matches);
    }
}
