using FluentAssertions;

namespace TestTrackingDiagrams.Tests.AssertionRewriter;

public class AssertionWrappingRewriterTests
{
    [Fact]
    public void Simple_Should_Be_Is_Wrapped()
    {
        var source = """
            using TestTrackingDiagrams.Tracking;
            class Test
            {
                void Method()
                {
                    x.Should().Be(1);
                }
            }
            """;

        var result = RewriterTestHelper.Rewrite(source);

        result.Should().Contain("Track.That(() => x.Should().Be(1))");
    }

    [Fact]
    public void Already_Wrapped_Is_Unchanged()
    {
        var source = """
            using TestTrackingDiagrams.Tracking;
            class Test
            {
                void Method()
                {
                    Track.That(() => x.Should().Be(1));
                }
            }
            """;

        var result = RewriterTestHelper.Rewrite(source);

        result.Should().Contain("Track.That(() => x.Should().Be(1))");
        RewriterTestHelper.GetChangeCount(source).Should().Be(0);
    }

    [Fact]
    public void Await_Should_ThrowAsync_Is_Wrapped()
    {
        var source = """
            using TestTrackingDiagrams.Tracking;
            class Test
            {
                async Task Method()
                {
                    await act.Should().ThrowAsync<Exception>();
                }
            }
            """;

        var result = RewriterTestHelper.Rewrite(source);

        result.Should().Contain("await Track.ThatAsync(async () => await act.Should().ThrowAsync<Exception>())");
    }

    [Fact]
    public void Expression_Bodied_Is_Wrapped()
    {
        var source = """
            using TestTrackingDiagrams.Tracking;
            class Test
            {
                void Method() => x.Should().Be(1);
            }
            """;

        var result = RewriterTestHelper.Rewrite(source);

        result.Should().Contain("Track.That(() => x.Should().Be(1))");
    }

    [Fact]
    public void Fluent_And_Chain_Is_Single_Wrap()
    {
        var source = """
            using TestTrackingDiagrams.Tracking;
            class Test
            {
                void Method()
                {
                    x.Should().BeGreaterThan(0).And.BeLessThan(10);
                }
            }
            """;

        var result = RewriterTestHelper.Rewrite(source);

        result.Should().Contain("Track.That(() => x.Should().BeGreaterThan(0).And.BeLessThan(10))");
        RewriterTestHelper.GetChangeCount(source).Should().Be(1);
    }

    [Fact]
    public void No_Should_Is_Unchanged()
    {
        var source = """
            using TestTrackingDiagrams.Tracking;
            class Test
            {
                void Method()
                {
                    Assert.True(x);
                }
            }
            """;

        var result = RewriterTestHelper.Rewrite(source);

        result.Should().Contain("Assert.True(x)");
        RewriterTestHelper.GetChangeCount(source).Should().Be(0);
    }

    [Fact]
    public void String_Literal_Not_Matched()
    {
        var source = """
            using TestTrackingDiagrams.Tracking;
            class Test
            {
                void Method()
                {
                    var s = "x.Should().Be(1)";
                }
            }
            """;

        var result = RewriterTestHelper.Rewrite(source);

        result.Should().Contain("var s = \"x.Should().Be(1)\"");
        RewriterTestHelper.GetChangeCount(source).Should().Be(0);
    }

    [Fact]
    public void Pragma_Disable_Trailing_Skips_Statement()
    {
        var source = """
            using TestTrackingDiagrams.Tracking;
            class Test
            {
                void Method()
                {
                    x.Should().Be(1); // pragma:TrackAssertions:disable
                }
            }
            """;

        var result = RewriterTestHelper.Rewrite(source);

        result.Should().NotContain("Track.That");
        RewriterTestHelper.GetChangeCount(source).Should().Be(0);
    }

    [Fact]
    public void Pragma_Range_Disables_Multiple()
    {
        var source = """
            using TestTrackingDiagrams.Tracking;
            class Test
            {
                void Method()
                {
                    // pragma:TrackAssertions:disable
                    x.Should().Be(1);
                    y.Should().Be(2);
                    // pragma:TrackAssertions:enable
                }
            }
            """;

        var result = RewriterTestHelper.Rewrite(source);

        result.Should().NotContain("Track.That");
        RewriterTestHelper.GetChangeCount(source).Should().Be(0);
    }

    [Fact]
    public void Pragma_Enable_Resumes_Wrapping()
    {
        var source = """
            using TestTrackingDiagrams.Tracking;
            class Test
            {
                void Method()
                {
                    // pragma:TrackAssertions:disable
                    x.Should().Be(1);
                    // pragma:TrackAssertions:enable
                    y.Should().Be(2);
                }
            }
            """;

        var result = RewriterTestHelper.Rewrite(source);

        result.Should().NotContain("Track.That(() => x.Should().Be(1))");
        result.Should().Contain("Track.That(() => y.Should().Be(2))");
    }

    [Fact]
    public void SuppressAttribute_On_Method_Skips()
    {
        var source = """
            using TestTrackingDiagrams.Tracking;
            class Test
            {
                [SuppressAssertionTracking]
                void Method()
                {
                    x.Should().Be(1);
                }
            }
            """;

        var result = RewriterTestHelper.Rewrite(source);

        result.Should().NotContain("Track.That");
        RewriterTestHelper.GetChangeCount(source).Should().Be(0);
    }

    [Fact]
    public void SuppressAttribute_On_Class_Skips()
    {
        var source = """
            using TestTrackingDiagrams.Tracking;

            [SuppressAssertionTracking]
            class Test
            {
                void Method()
                {
                    x.Should().Be(1);
                }
            }
            """;

        var result = RewriterTestHelper.Rewrite(source);

        result.Should().NotContain("Track.That");
        RewriterTestHelper.GetChangeCount(source).Should().Be(0);
    }

    [Fact]
    public void Using_Directive_Added_When_Missing()
    {
        var source = """
            class Test
            {
                void Method()
                {
                    x.Should().Be(1);
                }
            }
            """;

        var result = RewriterTestHelper.Rewrite(source);

        result.Should().Contain("using TestTrackingDiagrams.Tracking;");
    }

    [Fact]
    public void Using_Not_Duplicated()
    {
        var source = """
            using TestTrackingDiagrams.Tracking;
            class Test
            {
                void Method()
                {
                    x.Should().Be(1);
                }
            }
            """;

        var result = RewriterTestHelper.Rewrite(source);

        var count = result.Split("using TestTrackingDiagrams.Tracking;").Length - 1;
        count.Should().Be(1);
    }

    [Fact]
    public void Multiple_Assertions_Each_Wrapped()
    {
        var source = """
            using TestTrackingDiagrams.Tracking;
            class Test
            {
                void Method()
                {
                    x.Should().Be(1);
                    y.Should().Be(2);
                    z.Should().Be(3);
                }
            }
            """;

        RewriterTestHelper.GetChangeCount(source).Should().Be(3);
    }

    [Fact]
    public void Multiline_Fluent_Chain_Preserved()
    {
        var source = """
            using TestTrackingDiagrams.Tracking;
            class Test
            {
                void Method()
                {
                    x.Should()
                        .Be(1);
                }
            }
            """;

        var result = RewriterTestHelper.Rewrite(source);

        result.Should().Contain("Track.That(() => x.Should()");
        RewriterTestHelper.GetChangeCount(source).Should().Be(1);
    }

    [Fact]
    public void Assignment_Not_Wrapped()
    {
        var source = """
            using TestTrackingDiagrams.Tracking;
            class Test
            {
                void Method()
                {
                    var result = x.Should().Be(1);
                }
            }
            """;

        var result = RewriterTestHelper.Rewrite(source);

        result.Should().NotContain("Track.That");
        RewriterTestHelper.GetChangeCount(source).Should().Be(0);
    }

    [Fact]
    public void With_OriginalFilePath_Includes_CallerFilePath_Argument()
    {
        var source = """
            using TestTrackingDiagrams.Tracking;
            class Test
            {
                void Method()
                {
                    x.Should().Be(1);
                }
            }
            """;

        var result = RewriterTestHelper.Rewrite(source, @"C:\src\MyTests.cs");

        result.Should().Contain("callerFilePath:");
        result.Should().Contain(@"C:\src\MyTests.cs");
    }

    [Fact]
    public void With_OriginalFilePath_Includes_CallerLineNumber_Argument()
    {
        var source = """
            using TestTrackingDiagrams.Tracking;
            class Test
            {
                void Method()
                {
                    x.Should().Be(1);
                }
            }
            """;

        var result = RewriterTestHelper.Rewrite(source, @"C:\src\MyTests.cs");

        result.Should().Contain("callerLineNumber:");
    }

    [Fact]
    public void Without_OriginalFilePath_Does_Not_Include_CallerInfo()
    {
        var source = """
            using TestTrackingDiagrams.Tracking;
            class Test
            {
                void Method()
                {
                    x.Should().Be(1);
                }
            }
            """;

        var result = RewriterTestHelper.Rewrite(source);

        result.Should().NotContain("callerFilePath:");
        result.Should().NotContain("callerLineNumber:");
    }

    [Fact]
    public void Async_With_OriginalFilePath_Includes_CallerInfo()
    {
        var source = """
            using TestTrackingDiagrams.Tracking;
            class Test
            {
                async Task Method()
                {
                    await act.Should().ThrowAsync<Exception>();
                }
            }
            """;

        var result = RewriterTestHelper.Rewrite(source, @"C:\src\AsyncTests.cs");

        result.Should().Contain("callerFilePath:");
        result.Should().Contain(@"C:\src\AsyncTests.cs");
        result.Should().Contain("callerLineNumber:");
    }
}
