using TestTrackingDiagrams;

namespace TestTrackingDiagrams.Tests;

// ReSharper disable once ClassNeverInstantiated.Global
public record SampleRequest(string Name, int Age, bool IsActive, string Email);

// ReSharper disable once ClassNeverInstantiated.Global
public record SampleResponse(string Id, string Status, decimal Amount);

public class DiagramFocusTests : IDisposable
{
    public void Dispose()
    {
        DiagramFocus.ClearAll();
    }

    // ─── Request focus ──────────────────────────────────────────

    [Fact]
    public void Request_extracts_single_property_name()
    {
        DiagramFocus.Request<SampleRequest>(x => x.Name);

        var fields = DiagramFocus.ConsumePendingRequestFocus();

        Assert.NotNull(fields);
        Assert.Single(fields);
        Assert.Equal("Name", fields[0]);
    }

    [Fact]
    public void Request_extracts_multiple_property_names()
    {
        DiagramFocus.Request<SampleRequest>(x => x.Name, x => x.Email);

        var fields = DiagramFocus.ConsumePendingRequestFocus();

        Assert.NotNull(fields);
        Assert.Equal(2, fields.Length);
        Assert.Contains("Name", fields);
        Assert.Contains("Email", fields);
    }

    [Fact]
    public void Request_extracts_value_type_property_via_boxing()
    {
        DiagramFocus.Request<SampleRequest>(x => x.Age);

        var fields = DiagramFocus.ConsumePendingRequestFocus();

        Assert.NotNull(fields);
        Assert.Single(fields);
        Assert.Equal("Age", fields[0]);
    }

    [Fact]
    public void Request_extracts_bool_property_via_boxing()
    {
        DiagramFocus.Request<SampleRequest>(x => x.IsActive);

        var fields = DiagramFocus.ConsumePendingRequestFocus();

        Assert.NotNull(fields);
        Assert.Single(fields);
        Assert.Equal("IsActive", fields[0]);
    }

    [Fact]
    public void Request_extracts_all_types_together()
    {
        DiagramFocus.Request<SampleRequest>(x => x.Name, x => x.Age, x => x.IsActive, x => x.Email);

        var fields = DiagramFocus.ConsumePendingRequestFocus();

        Assert.NotNull(fields);
        Assert.Equal(4, fields.Length);
        Assert.Contains("Name", fields);
        Assert.Contains("Age", fields);
        Assert.Contains("IsActive", fields);
        Assert.Contains("Email", fields);
    }

    // ─── Response focus ─────────────────────────────────────────

    [Fact]
    public void Response_extracts_single_property_name()
    {
        DiagramFocus.Response<SampleResponse>(x => x.Id);

        var fields = DiagramFocus.ConsumePendingResponseFocus();

        Assert.NotNull(fields);
        Assert.Single(fields);
        Assert.Equal("Id", fields[0]);
    }

    [Fact]
    public void Response_extracts_multiple_property_names()
    {
        DiagramFocus.Response<SampleResponse>(x => x.Status, x => x.Amount);

        var fields = DiagramFocus.ConsumePendingResponseFocus();

        Assert.NotNull(fields);
        Assert.Equal(2, fields.Length);
        Assert.Contains("Status", fields);
        Assert.Contains("Amount", fields);
    }

    // ─── Consumption clears pending ─────────────────────────────

    [Fact]
    public void ConsumePendingRequestFocus_clears_after_consumption()
    {
        DiagramFocus.Request<SampleRequest>(x => x.Name);

        var first = DiagramFocus.ConsumePendingRequestFocus();
        var second = DiagramFocus.ConsumePendingRequestFocus();

        Assert.NotNull(first);
        Assert.Null(second);
    }

    [Fact]
    public void ConsumePendingResponseFocus_clears_after_consumption()
    {
        DiagramFocus.Response<SampleResponse>(x => x.Id);

        var first = DiagramFocus.ConsumePendingResponseFocus();
        var second = DiagramFocus.ConsumePendingResponseFocus();

        Assert.NotNull(first);
        Assert.Null(second);
    }

    [Fact]
    public void ConsumePendingRequestFocus_returns_null_when_not_set()
    {
        var fields = DiagramFocus.ConsumePendingRequestFocus();

        Assert.Null(fields);
    }

    [Fact]
    public void ConsumePendingResponseFocus_returns_null_when_not_set()
    {
        var fields = DiagramFocus.ConsumePendingResponseFocus();

        Assert.Null(fields);
    }

    // ─── Request and response are independent ───────────────────

    [Fact]
    public void Request_and_response_focus_are_independent()
    {
        DiagramFocus.Request<SampleRequest>(x => x.Name);
        DiagramFocus.Response<SampleResponse>(x => x.Id);

        var requestFields = DiagramFocus.ConsumePendingRequestFocus();
        var responseFields = DiagramFocus.ConsumePendingResponseFocus();

        Assert.NotNull(requestFields);
        Assert.Single(requestFields);
        Assert.Equal("Name", requestFields[0]);

        Assert.NotNull(responseFields);
        Assert.Single(responseFields);
        Assert.Equal("Id", responseFields[0]);
    }

    [Fact]
    public void Consuming_request_does_not_affect_response()
    {
        DiagramFocus.Request<SampleRequest>(x => x.Name);
        DiagramFocus.Response<SampleResponse>(x => x.Id);

        _ = DiagramFocus.ConsumePendingRequestFocus();

        var responseFields = DiagramFocus.ConsumePendingResponseFocus();
        Assert.NotNull(responseFields);
        Assert.Equal("Id", responseFields[0]);
    }

    // ─── ClearAll ───────────────────────────────────────────────

    [Fact]
    public void ClearAll_clears_both_request_and_response()
    {
        DiagramFocus.Request<SampleRequest>(x => x.Name);
        DiagramFocus.Response<SampleResponse>(x => x.Id);

        DiagramFocus.ClearAll();

        Assert.Null(DiagramFocus.ConsumePendingRequestFocus());
        Assert.Null(DiagramFocus.ConsumePendingResponseFocus());
    }
}
