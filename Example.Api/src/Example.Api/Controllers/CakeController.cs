using Example.Api.Requests;
using Example.Api.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Example.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class CakeController : ControllerBase
{
    [HttpPost]
    public CakeResponse MakeCake(CakeRequest request)
    {
        return new CakeResponse { Ingredients = new [] { request.Eggs!, request.Milk!, request.Flour! } };
    }
}