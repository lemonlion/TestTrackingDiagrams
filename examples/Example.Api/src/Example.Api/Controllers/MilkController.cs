using Example.Api.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Example.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class MilkController : ControllerBase
{
    private readonly HttpClient _client;
    private readonly string _cowServiceBaseUrl;

    public MilkController(HttpClient client, IConfiguration configuration)
    {
        _client = client;
        _cowServiceBaseUrl = configuration["CowServiceBaseUrl"]!;
    }

    [HttpGet]
    public Task<MilkResponse> GetMilk() => _client.GetFromJsonAsync<MilkResponse>($"{_cowServiceBaseUrl}/milk")!;
}