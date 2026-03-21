using System.Net;
using System.Net.Http.Json;
using Example.Api.Requests;
using Example.Api.Responses;
using Example.Api.Tests.Component.Shared;
using FluentAssertions;
using Reqnroll;

namespace Example.Api.Tests.Component.ReqNRoll.StepDefinitions;

[Binding]
public class CakeStepDefinitions
{
    private readonly HttpClient _client;

    private readonly CakeRequest _cakeRequest = new();
    private HttpResponseMessage? _cakeResponseMessage;
    private string? _cakeResponseString;
    private CakeResponse? _cakeResponse;

    private MilkResponse _milkResponse = new();
    private FlourResponse _flourResponse = new();
    private EggsResponse _eggsResponse = new();

    public CakeStepDefinitions(HttpClient client)
    {
        _client = client;
    }

    [Given("a valid post request for the Cake endpoint")]
    public async Task GivenAValidPostRequestForTheCakeEndpoint()
    {
        _milkResponse = (await _client.GetFromJsonAsync<MilkResponse>("milk"))!;
        _cakeRequest.Milk = _milkResponse.Milk;

        _eggsResponse = (await _client.GetFromJsonAsync<EggsResponse>("eggs"))!;
        _cakeRequest.Eggs = _eggsResponse.Eggs;

        _flourResponse = (await _client.GetFromJsonAsync<FlourResponse>("flour"))!;
        _cakeRequest.Flour = _flourResponse.Flour;
    }

    [Given("the request body is missing eggs")]
    public void GivenTheRequestBodyIsMissingEggs()
    {
        _cakeRequest.Eggs = null;
    }

    [When("the request is sent to the cake post endpoint")]
    public async Task WhenTheRequestIsSentToTheCakePostEndpoint()
    {
        _cakeResponseMessage = await _client.PostAsJsonAsync("cake", _cakeRequest);
    }

    [Then("the response should be successful")]
    public async Task ThenTheResponseShouldBeSuccessful()
    {
        _cakeResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

        _cakeResponseString = await _cakeResponseMessage.Content.ReadAsStringAsync();
        Json.IsValid(_cakeResponseString).Should().BeTrue();

        _cakeResponse = Json.Deserialize<CakeResponse>(_cakeResponseString)!;

        _cakeResponse.Ingredients.Should().Contain(_milkResponse.Milk);
        _cakeResponse.Ingredients.Should().Contain(_eggsResponse.Eggs);
        _cakeResponse.Ingredients.Should().Contain(_flourResponse.Flour);
    }

    [Then("the response http status should be bad request")]
    public void ThenTheResponseHttpStatusShouldBeBadRequest()
    {
        _cakeResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
