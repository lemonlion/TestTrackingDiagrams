using System.Linq.Expressions;
using System.Net.Http.Json;
using System.Text.Json;

namespace TestTrackingDiagrams;

public static class HttpClientDiagramFocusExtensions
{
    public static FocusedHttpClient WithDiagramFocus(this HttpClient httpClient) => new(httpClient);
}

public sealed class FocusedHttpClient
{
    private readonly HttpClient _httpClient;
    private string[]? _requestFields;
    private string[]? _responseFields;

    internal FocusedHttpClient(HttpClient httpClient) => _httpClient = httpClient;

    public FocusedHttpClient OnRequest<T>(params Expression<Func<T, object?>>[] fields)
    {
        _requestFields = fields.Select(DiagramFocus.ExtractPropertyName).ToArray();
        return this;
    }

    public FocusedHttpClient OnRequest(params string[] fieldNames)
    {
        _requestFields = fieldNames;
        return this;
    }

    public FocusedHttpClient OnResponse<T>(params Expression<Func<T, object?>>[] fields)
    {
        _responseFields = fields.Select(DiagramFocus.ExtractPropertyName).ToArray();
        return this;
    }

    public FocusedHttpClient OnResponse(params string[] fieldNames)
    {
        _responseFields = fieldNames;
        return this;
    }

    // ─── Standard HttpClient methods ────────────────────────────

    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        ApplyFocus();
        return _httpClient.SendAsync(request, cancellationToken);
    }

    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken = default)
    {
        ApplyFocus();
        return _httpClient.SendAsync(request, completionOption, cancellationToken);
    }

    // ─── GET ────────────────────────────────────────────────────

    public Task<HttpResponseMessage> GetAsync(string? requestUri, CancellationToken cancellationToken = default)
    {
        ApplyFocus();
        return _httpClient.GetAsync(requestUri, cancellationToken);
    }

    public Task<HttpResponseMessage> GetAsync(Uri? requestUri, CancellationToken cancellationToken = default)
    {
        ApplyFocus();
        return _httpClient.GetAsync(requestUri, cancellationToken);
    }

    public Task<T?> GetFromJsonAsync<T>(string? requestUri, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        ApplyFocus();
        return _httpClient.GetFromJsonAsync<T>(requestUri, options, cancellationToken);
    }

    public Task<T?> GetFromJsonAsync<T>(Uri? requestUri, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        ApplyFocus();
        return _httpClient.GetFromJsonAsync<T>(requestUri, options, cancellationToken);
    }

    // ─── POST ───────────────────────────────────────────────────

    public Task<HttpResponseMessage> PostAsync(string? requestUri, HttpContent? content, CancellationToken cancellationToken = default)
    {
        ApplyFocus();
        return _httpClient.PostAsync(requestUri, content, cancellationToken);
    }

    public Task<HttpResponseMessage> PostAsync(Uri? requestUri, HttpContent? content, CancellationToken cancellationToken = default)
    {
        ApplyFocus();
        return _httpClient.PostAsync(requestUri, content, cancellationToken);
    }

    public Task<HttpResponseMessage> PostAsJsonAsync<TValue>(string? requestUri, TValue value, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        ApplyFocus();
        return _httpClient.PostAsJsonAsync(requestUri, value, options, cancellationToken);
    }

    public Task<HttpResponseMessage> PostAsJsonAsync<TValue>(Uri? requestUri, TValue value, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        ApplyFocus();
        return _httpClient.PostAsJsonAsync(requestUri, value, options, cancellationToken);
    }

    // ─── PUT ────────────────────────────────────────────────────

    public Task<HttpResponseMessage> PutAsync(string? requestUri, HttpContent? content, CancellationToken cancellationToken = default)
    {
        ApplyFocus();
        return _httpClient.PutAsync(requestUri, content, cancellationToken);
    }

    public Task<HttpResponseMessage> PutAsync(Uri? requestUri, HttpContent? content, CancellationToken cancellationToken = default)
    {
        ApplyFocus();
        return _httpClient.PutAsync(requestUri, content, cancellationToken);
    }

    public Task<HttpResponseMessage> PutAsJsonAsync<TValue>(string? requestUri, TValue value, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        ApplyFocus();
        return _httpClient.PutAsJsonAsync(requestUri, value, options, cancellationToken);
    }

    public Task<HttpResponseMessage> PutAsJsonAsync<TValue>(Uri? requestUri, TValue value, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        ApplyFocus();
        return _httpClient.PutAsJsonAsync(requestUri, value, options, cancellationToken);
    }

    // ─── PATCH ──────────────────────────────────────────────────

    public Task<HttpResponseMessage> PatchAsync(string? requestUri, HttpContent? content, CancellationToken cancellationToken = default)
    {
        ApplyFocus();
        return _httpClient.PatchAsync(requestUri, content, cancellationToken);
    }

    public Task<HttpResponseMessage> PatchAsync(Uri? requestUri, HttpContent? content, CancellationToken cancellationToken = default)
    {
        ApplyFocus();
        return _httpClient.PatchAsync(requestUri, content, cancellationToken);
    }

    public Task<HttpResponseMessage> PatchAsJsonAsync<TValue>(string? requestUri, TValue value, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        ApplyFocus();
        return _httpClient.PatchAsJsonAsync(requestUri, value, options, cancellationToken);
    }

    public Task<HttpResponseMessage> PatchAsJsonAsync<TValue>(Uri? requestUri, TValue value, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        ApplyFocus();
        return _httpClient.PatchAsJsonAsync(requestUri, value, options, cancellationToken);
    }

    // ─── DELETE ─────────────────────────────────────────────────

    public Task<HttpResponseMessage> DeleteAsync(string? requestUri, CancellationToken cancellationToken = default)
    {
        ApplyFocus();
        return _httpClient.DeleteAsync(requestUri, cancellationToken);
    }

    public Task<HttpResponseMessage> DeleteAsync(Uri? requestUri, CancellationToken cancellationToken = default)
    {
        ApplyFocus();
        return _httpClient.DeleteAsync(requestUri, cancellationToken);
    }

    // ─── Internals ──────────────────────────────────────────────

    private void ApplyFocus()
    {
        if (_requestFields is { Length: > 0 })
            DiagramFocus.Request(_requestFields);

        if (_responseFields is { Length: > 0 })
            DiagramFocus.Response(_responseFields);
    }
}
