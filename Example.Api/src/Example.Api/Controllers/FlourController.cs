using Example.Api.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Example.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class FlourController : ControllerBase
{
    [HttpGet]
    public FlourResponse GetFlour() => new();
}