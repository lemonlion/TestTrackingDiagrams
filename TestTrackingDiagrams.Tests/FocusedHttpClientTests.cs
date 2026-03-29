using TestTrackingDiagrams;

namespace TestTrackingDiagrams.Tests;

public class FocusedHttpClientTests : IDisposable
{
    private readonly HttpClient _httpClient = new(new NoOpHandler());

    public void Dispose()
    {
        DiagramFocus.ClearAll();
        _httpClient.Dispose();
    }

    // ─── OnRequest with expressions ─────────────────────────────

    [Fact]
    public async Task OnRequest_sets_request_focus_via_terminal_method()
    {
        await _httpClient.WithDiagramFocus()
            .OnRequest<SampleRequest>(x => x.Name, x => x.Age)
            .PostAsync("http://localhost/test", null);

        // NoOpHandler doesn't consume focus, so it should still be pending
        var fields = DiagramFocus.ConsumePendingRequestFocus();
        Assert.NotNull(fields);
        Assert.Equal(2, fields.Length);
        Assert.Contains("Name", fields);
        Assert.Contains("Age", fields);
    }

    [Fact]
    public void OnRequest_expression_extracts_property_names()
    {
        var focused = _httpClient.WithDiagramFocus()
            .OnRequest<SampleRequest>(x => x.Name, x => x.Email);

        // Trigger ApplyFocus indirectly — just call a terminal method
        // But first, let's verify the builder stores fields correctly
        // by calling a fire-and-forget method
        _ = focused.GetAsync("http://localhost/test");

        // The static DiagramFocus was set by ApplyFocus, then consumed atomically
        // Since NoOpHandler doesn't consume focus, it should still be pending
        var fields = DiagramFocus.ConsumePendingRequestFocus();
        Assert.NotNull(fields);
        Assert.Equal(2, fields.Length);
        Assert.Contains("Name", fields);
        Assert.Contains("Email", fields);
    }

    [Fact]
    public void OnRequest_string_overload_sets_fields()
    {
        _ = _httpClient.WithDiagramFocus()
            .OnRequest("Name", "Email")
            .GetAsync("http://localhost/test");

        var fields = DiagramFocus.ConsumePendingRequestFocus();
        Assert.NotNull(fields);
        Assert.Contains("Name", fields);
        Assert.Contains("Email", fields);
    }

    // ─── OnResponse with expressions ────────────────────────────

    [Fact]
    public void OnResponse_expression_extracts_property_names()
    {
        _ = _httpClient.WithDiagramFocus()
            .OnResponse<SampleResponse>(x => x.Status, x => x.Amount)
            .PostAsync("http://localhost/test", null);

        var fields = DiagramFocus.ConsumePendingResponseFocus();
        Assert.NotNull(fields);
        Assert.Equal(2, fields.Length);
        Assert.Contains("Status", fields);
        Assert.Contains("Amount", fields);
    }

    [Fact]
    public void OnResponse_string_overload_sets_fields()
    {
        _ = _httpClient.WithDiagramFocus()
            .OnResponse("Status", "Amount")
            .PostAsync("http://localhost/test", null);

        var fields = DiagramFocus.ConsumePendingResponseFocus();
        Assert.NotNull(fields);
        Assert.Contains("Status", fields);
        Assert.Contains("Amount", fields);
    }

    // ─── Combined request and response focus ────────────────────

    [Fact]
    public void OnRequest_and_OnResponse_sets_both()
    {
        _ = _httpClient.WithDiagramFocus()
            .OnRequest<SampleRequest>(x => x.Name)
            .OnResponse<SampleResponse>(x => x.Status)
            .PostAsync("http://localhost/test", null);

        var requestFields = DiagramFocus.ConsumePendingRequestFocus();
        var responseFields = DiagramFocus.ConsumePendingResponseFocus();

        Assert.NotNull(requestFields);
        Assert.Single(requestFields);
        Assert.Equal("Name", requestFields[0]);

        Assert.NotNull(responseFields);
        Assert.Single(responseFields);
        Assert.Equal("Status", responseFields[0]);
    }

    [Fact]
    public void OnResponse_then_OnRequest_order_does_not_matter()
    {
        _ = _httpClient.WithDiagramFocus()
            .OnResponse<SampleResponse>(x => x.Id)
            .OnRequest<SampleRequest>(x => x.Age)
            .PutAsync("http://localhost/test", null);

        var requestFields = DiagramFocus.ConsumePendingRequestFocus();
        var responseFields = DiagramFocus.ConsumePendingResponseFocus();

        Assert.NotNull(requestFields);
        Assert.Equal("Age", requestFields[0]);

        Assert.NotNull(responseFields);
        Assert.Equal("Id", responseFields[0]);
    }

    // ─── Only request or only response ──────────────────────────

    [Fact]
    public void OnRequest_only_does_not_set_response_focus()
    {
        _ = _httpClient.WithDiagramFocus()
            .OnRequest<SampleRequest>(x => x.Name)
            .GetAsync("http://localhost/test");

        var responseFields = DiagramFocus.ConsumePendingResponseFocus();
        Assert.Null(responseFields);
    }

    [Fact]
    public void OnResponse_only_does_not_set_request_focus()
    {
        _ = _httpClient.WithDiagramFocus()
            .OnResponse<SampleResponse>(x => x.Status)
            .GetAsync("http://localhost/test");

        var requestFields = DiagramFocus.ConsumePendingRequestFocus();
        Assert.Null(requestFields);
    }

    // ─── All HTTP verbs trigger focus ───────────────────────────

    [Fact]
    public void GetAsync_applies_focus()
    {
        _ = _httpClient.WithDiagramFocus()
            .OnResponse<SampleResponse>(x => x.Id)
            .GetAsync("http://localhost/test");

        Assert.NotNull(DiagramFocus.ConsumePendingResponseFocus());
    }

    [Fact]
    public void PostAsync_applies_focus()
    {
        _ = _httpClient.WithDiagramFocus()
            .OnRequest<SampleRequest>(x => x.Name)
            .PostAsync("http://localhost/test", null);

        Assert.NotNull(DiagramFocus.ConsumePendingRequestFocus());
    }

    [Fact]
    public void PutAsync_applies_focus()
    {
        _ = _httpClient.WithDiagramFocus()
            .OnRequest<SampleRequest>(x => x.Name)
            .PutAsync("http://localhost/test", null);

        Assert.NotNull(DiagramFocus.ConsumePendingRequestFocus());
    }

    [Fact]
    public void PatchAsync_applies_focus()
    {
        _ = _httpClient.WithDiagramFocus()
            .OnRequest<SampleRequest>(x => x.Name)
            .PatchAsync("http://localhost/test", null);

        Assert.NotNull(DiagramFocus.ConsumePendingRequestFocus());
    }

    [Fact]
    public void DeleteAsync_applies_focus()
    {
        _ = _httpClient.WithDiagramFocus()
            .OnResponse<SampleResponse>(x => x.Status)
            .DeleteAsync("http://localhost/test");

        Assert.NotNull(DiagramFocus.ConsumePendingResponseFocus());
    }

    [Fact]
    public void SendAsync_applies_focus()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/test");

        _ = _httpClient.WithDiagramFocus()
            .OnResponse<SampleResponse>(x => x.Id)
            .SendAsync(request);

        Assert.NotNull(DiagramFocus.ConsumePendingResponseFocus());
    }

    // ─── Value type property extraction ─────────────────────────

    [Fact]
    public void OnRequest_handles_value_type_properties()
    {
        _ = _httpClient.WithDiagramFocus()
            .OnRequest<SampleRequest>(x => x.Age, x => x.IsActive)
            .GetAsync("http://localhost/test");

        var fields = DiagramFocus.ConsumePendingRequestFocus();
        Assert.NotNull(fields);
        Assert.Contains("Age", fields);
        Assert.Contains("IsActive", fields);
    }

    [Fact]
    public void OnResponse_handles_value_type_properties()
    {
        _ = _httpClient.WithDiagramFocus()
            .OnResponse<SampleResponse>(x => x.Amount)
            .GetAsync("http://localhost/test");

        var fields = DiagramFocus.ConsumePendingResponseFocus();
        Assert.NotNull(fields);
        Assert.Equal("Amount", fields[0]);
    }

    // ─── Handler that doesn't consume DiagramFocus ──────────────

    private sealed class NoOpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
    }
}
