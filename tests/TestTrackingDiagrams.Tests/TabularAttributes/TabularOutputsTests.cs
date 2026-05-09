using TestTrackingDiagrams.Reports;
using TestTrackingDiagrams.TabularAttributes;

namespace TestTrackingDiagrams.Tests.TabularAttributes;

public class TabularOutputsTests
{
    private class Greeting
    {
        public string Message { get; set; } = "";
    }

    private class MultiProp
    {
        public string Name { get; set; } = "";
        public int Score { get; set; }
    }

    [Fact]
    public void Indexer_returns_expected_item()
    {
        var expected = new[] { new Greeting { Message = "Hello" } };
        var outputs = new TabularOutputs<Greeting>(expected, ["Message"]);

        Assert.Equal("Hello", outputs[0].Message);
    }

    [Fact]
    public void Count_returns_expected_count()
    {
        var expected = new[]
        {
            new Greeting { Message = "Hello" },
            new Greeting { Message = "World" }
        };
        var outputs = new TabularOutputs<Greeting>(expected, ["Message"]);

        Assert.Equal(2, outputs.Count);
    }

    [Fact]
    public void Verify_passes_when_actual_matches_expected()
    {
        var expected = new[] { new Greeting { Message = "Hello" } };
        var outputs = new TabularOutputs<Greeting>(expected, ["Message"]);

        outputs.RecordActualResult(new Greeting { Message = "Hello" });
        outputs.Verify(); // Should not throw
    }

    [Fact]
    public void Verify_passes_with_multiple_matching_rows()
    {
        var expected = new[]
        {
            new Greeting { Message = "Hello" },
            new Greeting { Message = "World" }
        };
        var outputs = new TabularOutputs<Greeting>(expected, ["Message"]);

        outputs.RecordActualResult(new Greeting { Message = "Hello" });
        outputs.RecordActualResult(new Greeting { Message = "World" });
        outputs.Verify(); // Should not throw
    }

    [Fact]
    public void Verify_throws_when_value_mismatches()
    {
        var expected = new[] { new Greeting { Message = "Hello" } };
        var outputs = new TabularOutputs<Greeting>(expected, ["Message"]);

        outputs.RecordActualResult(new Greeting { Message = "Wrong" });

        var ex = Assert.Throws<TabularVerificationException>(() => outputs.Verify());
        Assert.Contains("Tabular output verification failed", ex.Message);
        Assert.Contains("Expected: Hello", ex.Message);
        Assert.Contains("Actual: Wrong", ex.Message);
    }

    [Fact]
    public void Verify_detects_surplus_rows()
    {
        var expected = new[] { new Greeting { Message = "Hello" } };
        var outputs = new TabularOutputs<Greeting>(expected, ["Message"]);

        outputs.RecordActualResult(new Greeting { Message = "Hello" });
        outputs.RecordActualResult(new Greeting { Message = "Extra" });

        var ex = Assert.Throws<TabularVerificationException>(() => outputs.Verify());
        Assert.Contains("Surplus", ex.Message);
    }

    [Fact]
    public void Verify_detects_missing_rows()
    {
        var expected = new[]
        {
            new Greeting { Message = "Hello" },
            new Greeting { Message = "World" }
        };
        var outputs = new TabularOutputs<Greeting>(expected, ["Message"]);

        outputs.RecordActualResult(new Greeting { Message = "Hello" });

        var ex = Assert.Throws<TabularVerificationException>(() => outputs.Verify());
        Assert.Contains("Missing", ex.Message);
    }

    [Fact]
    public void GetRows_before_verify_returns_unverified_with_NotProvided()
    {
        var expected = new[] { new Greeting { Message = "Hello" } };
        var outputs = new TabularOutputs<Greeting>(expected, ["Message"]);

        var rows = outputs.GetRows();

        Assert.Single(rows);
        Assert.Equal(TableRowType.Matching, rows[0].Type);
        Assert.Equal("Hello", rows[0].Values[0].Value);
        Assert.Null(rows[0].Values[0].Expectation);
        Assert.Equal(VerificationStatus.NotProvided, rows[0].Values[0].Status);
    }

    [Fact]
    public void GetRows_after_successful_verify_returns_Success_status()
    {
        var expected = new[] { new Greeting { Message = "Hello" } };
        var outputs = new TabularOutputs<Greeting>(expected, ["Message"]);

        outputs.RecordActualResult(new Greeting { Message = "Hello" });
        outputs.Verify();

        var rows = outputs.GetRows();

        Assert.Single(rows);
        Assert.Equal(TableRowType.Matching, rows[0].Type);
        Assert.Equal("Hello", rows[0].Values[0].Value); // actual value
        Assert.Equal("Hello", rows[0].Values[0].Expectation); // expected
        Assert.Equal(VerificationStatus.Success, rows[0].Values[0].Status);
    }

    [Fact]
    public void GetRows_after_failed_verify_returns_Failure_status()
    {
        var expected = new[] { new Greeting { Message = "Hello" } };
        var outputs = new TabularOutputs<Greeting>(expected, ["Message"]);

        outputs.RecordActualResult(new Greeting { Message = "Wrong" });

        Assert.Throws<TabularVerificationException>(() => outputs.Verify());

        var rows = outputs.GetRows();

        Assert.Single(rows);
        Assert.Equal("Wrong", rows[0].Values[0].Value);
        Assert.Equal("Hello", rows[0].Values[0].Expectation);
        Assert.Equal(VerificationStatus.Failure, rows[0].Values[0].Status);
    }

    [Fact]
    public void GetColumns_returns_column_definitions()
    {
        var outputs = new TabularOutputs<Greeting>([], ["Message"]);

        var columns = outputs.GetColumns();

        Assert.Single(columns);
        Assert.Equal("Message", columns[0].Name);
        Assert.False(columns[0].IsKey);
    }

    [Fact]
    public void Verify_with_multiple_columns_reports_each_failure()
    {
        var expected = new[] { new MultiProp { Name = "Alice", Score = 100 } };
        var outputs = new TabularOutputs<MultiProp>(expected, ["Name", "Score"]);

        outputs.RecordActualResult(new MultiProp { Name = "Bob", Score = 50 });

        var ex = Assert.Throws<TabularVerificationException>(() => outputs.Verify());
        Assert.Contains("Expected: Alice", ex.Message);
        Assert.Contains("Actual: Bob", ex.Message);
        Assert.Contains("Expected: 100", ex.Message);
        Assert.Contains("Actual: 50", ex.Message);
    }

    [Fact]
    public void Verify_with_partial_column_match()
    {
        var expected = new[] { new MultiProp { Name = "Alice", Score = 100 } };
        var outputs = new TabularOutputs<MultiProp>(expected, ["Name", "Score"]);

        outputs.RecordActualResult(new MultiProp { Name = "Alice", Score = 50 });

        var ex = Assert.Throws<TabularVerificationException>(() => outputs.Verify());
        // Name matches (Success), Score mismatches (Failure)
        Assert.Contains("Expected: 100", ex.Message);
        Assert.Contains("Actual: 50", ex.Message);
    }

    [Fact]
    public void Dispose_triggers_verify_when_actuals_recorded()
    {
        var expected = new[] { new Greeting { Message = "Hello" } };
        var outputs = new TabularOutputs<Greeting>(expected, ["Message"]);
        outputs.RecordActualResult(new Greeting { Message = "Hello" });

        outputs.Dispose(); // Should not throw — actuals match expected

        // GetRows should reflect verified state
        var rows = outputs.GetRows();
        Assert.Equal(VerificationStatus.Success, rows[0].Values[0].Status);
    }

    [Fact]
    public void Dispose_does_nothing_when_no_actuals()
    {
        var expected = new[] { new Greeting { Message = "Hello" } };
        var outputs = new TabularOutputs<Greeting>(expected, ["Message"]);

        outputs.Dispose(); // Should not throw — no actuals recorded

        // GetRows should remain unverified
        var rows = outputs.GetRows();
        Assert.Equal(VerificationStatus.NotProvided, rows[0].Values[0].Status);
    }

    [Fact]
    public void Dispose_does_nothing_when_already_verified()
    {
        var expected = new[] { new Greeting { Message = "Hello" } };
        var outputs = new TabularOutputs<Greeting>(expected, ["Message"]);
        outputs.RecordActualResult(new Greeting { Message = "Hello" });
        outputs.Verify();

        outputs.Dispose(); // Should not throw — already verified
    }

    [Fact]
    public void Dispose_throws_on_mismatch()
    {
        var expected = new[] { new Greeting { Message = "Hello" } };
        var outputs = new TabularOutputs<Greeting>(expected, ["Message"]);
        outputs.RecordActualResult(new Greeting { Message = "Wrong" });

        Assert.Throws<TabularVerificationException>(() => outputs.Dispose());
    }

    [Fact]
    public void Enumerator_iterates_expected_items()
    {
        var expected = new[]
        {
            new Greeting { Message = "Hello" },
            new Greeting { Message = "World" }
        };
        var outputs = new TabularOutputs<Greeting>(expected, ["Message"]);

        var messages = outputs.Select(g => g.Message).ToList();

        Assert.Equal(["Hello", "World"], messages);
    }
}
