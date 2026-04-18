using Example.Api.Events;
using Example.Api.Requests;
using Example.Api.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Example.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class CakeController(IEventPublisher eventPublisher) : ControllerBase
{
    [HttpPost]
    public async Task<CakeResponse> MakeCake(CakeRequest request)
    {
        var response = new CakeResponse { Ingredients = new [] { request.Eggs!, request.Milk!, request.Flour! } };

        await eventPublisher.PublishAsync(new CakeCreatedEvent(response.BatchId, response.Ingredients));

        return response;
    }
}