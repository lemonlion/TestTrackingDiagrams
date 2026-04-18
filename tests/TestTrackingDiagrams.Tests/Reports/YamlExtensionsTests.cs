using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

public class YamlExtensionsTests
{
    // ── Existing behavior ──

    [Fact]
    public void Replaces_square_brackets()
    {
        Assert.Equal("<1, 2, 3>", "[1, 2, 3]".SanitiseForYml());
    }

    [Fact]
    public void Replaces_colon_space()
    {
        Assert.Equal("key = value", "key: value".SanitiseForYml());
    }

    [Fact]
    public void Plain_text_unchanged()
    {
        Assert.Equal("Simple test name", "Simple test name".SanitiseForYml());
    }

    // ── Characters that would break YAML if not sanitized ──

    [Fact]
    public void Sanitises_hash_comment()
    {
        // # starts a YAML comment — everything after it would be lost
        var result = "Order total is $50 # after discount".SanitiseForYml();
        Assert.DoesNotContain("#", result);
    }

    [Fact]
    public void Sanitises_ampersand_anchor()
    {
        // & starts a YAML anchor reference
        var result = "Tom & Jerry".SanitiseForYml();
        Assert.DoesNotContain("&", result);
    }

    [Fact]
    public void Sanitises_asterisk_alias()
    {
        // * starts a YAML alias reference
        var result = "2 * 3 = 6".SanitiseForYml();
        Assert.DoesNotContain("*", result);
    }

    [Fact]
    public void Sanitises_curly_braces()
    {
        // {} starts a YAML flow mapping
        var result = "Config {retries=3}".SanitiseForYml();
        Assert.DoesNotContain("{", result);
        Assert.DoesNotContain("}", result);
    }

    [Fact]
    public void Sanitises_exclamation_tag()
    {
        // ! starts a YAML tag directive
        var result = "Alert! Something happened".SanitiseForYml();
        Assert.DoesNotContain("!", result);
    }

    [Fact]
    public void Sanitises_percent_directive()
    {
        // % starts a YAML directive
        var result = "50% off".SanitiseForYml();
        Assert.DoesNotContain("%", result);
    }

    [Fact]
    public void Sanitises_at_sign()
    {
        // @ is reserved in YAML
        var result = "user@email.com".SanitiseForYml();
        Assert.DoesNotContain("@", result);
    }

    [Fact]
    public void Sanitises_backtick()
    {
        // ` is reserved in YAML
        var result = "Use `code` here".SanitiseForYml();
        Assert.DoesNotContain("`", result);
    }

    [Fact]
    public void Sanitises_pipe_literal()
    {
        // | starts a YAML literal block scalar
        var result = "A | B".SanitiseForYml();
        Assert.DoesNotContain("|", result);
    }

    [Fact]
    public void Combined_special_characters()
    {
        var input = "Test [data]: {config} & *alias # comment !tag 50% @user `code` | pipe";
        var result = input.SanitiseForYml();

        Assert.DoesNotContain("#", result);
        Assert.DoesNotContain("&", result);
        Assert.DoesNotContain("*", result);
        Assert.DoesNotContain("{", result);
        Assert.DoesNotContain("}", result);
        Assert.DoesNotContain("!", result);
        Assert.DoesNotContain("%", result);
        Assert.DoesNotContain("@", result);
        Assert.DoesNotContain("`", result);
        Assert.DoesNotContain("|", result);
        Assert.DoesNotContain("[", result);
        Assert.DoesNotContain("]", result);
    }
}
