using Example.Api.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Example.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class EggsController : ControllerBase
{
    [HttpGet]
    public EggsResponse GetEggs() => new();
}