using System.Net;
using System.Net.Http.Json;
using Example.Api.Requests;
using Example.Api.Responses;
using Example.Api.Tests.Component.BDDfy.xUnit3.Infrastructure;
using Example.Api.Tests.Component.Shared;
using FluentAssertions;
using TestStack.BDDfy;
using Xunit;

namespace Example.Api.Tests.Component.BDDfy.xUnit3.Scenarios;

[Story(
    AsA = "dessert provider",
    IWant = "to create cakes from ingredients",
    SoThat = "customers can enjoy delicious cakes")]
public class CakeFeature : BaseFixture
{
    private readonly CakeRequest _cakeRequest = new();
    private HttpResponseMessage? _cakeResponseMessage;
    private string? _cakeResponseString;
    private CakeResponse? _cakeResponse;

    private MilkResponse _milkResponse = new();
    private FlourResponse _flourResponse = new();
    private EggsResponse _eggsResponse = new();

    [Fact]
    public void Calling_Create_Cake_Endpoint_Returns_Cake()
    {
        this.Given(x => x.GivenAValidPostRequestForTheCakeEndpoint())
            .When(x => x.WhenTheRequestIsSentToTheCakePostEndpoint())
            .Then(x => x.ThenTheResponseShouldBeSuccessful())
            .WithTags("happy-path", "endpoint:/cake")
            .BDDfy();
    }

    [Fact]
    public void Calling_Create_Cake_Endpoint_Without_Eggs_Returns_Bad_Request()
    {
        this.Given(x => x.GivenAValidPostRequestForTheCakeEndpoint())
            .And(x => x.AndTheRequestBodyIsMissingEggs())
            .When(x => x.WhenTheRequestIsSentToTheCakePostEndpoint())
            .Then(x => x.ThenTheResponseHttpStatusShouldBeBadRequest())
            .WithTags("endpoint:/cake")
            .BDDfy();
    }

    #region Steps

    private async Task GivenAValidPostRequestForTheCakeEndpoint()
    {
        _milkResponse = await Client.GetFromJsonAsync<MilkResponse>("milk");
        _cakeRequest.Milk = _milkResponse!.Milk;

        _eggsResponse = await Client.GetFromJsonAsync<EggsResponse>("eggs");
        _cakeRequest.Eggs = _eggsResponse!.Eggs;

        _flourResponse = await Client.GetFromJsonAsync<FlourResponse>("flour");
        _cakeRequest.Flour = _flourResponse!.Flour;
    }

    private void AndTheRequestBodyIsMissingEggs()
    {
        _cakeRequest.Eggs = null;
    }

    private async Task WhenTheRequestIsSentToTheCakePostEndpoint()
    {
        _cakeResponseMessage = await Client.PostAsJsonAsync("cake", _cakeRequest);
    }

    private async Task ThenTheResponseShouldBeSuccessful()
    {
        _cakeResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

        _cakeResponseString = await _cakeResponseMessage.Content.ReadAsStringAsync();
        Json.IsValid(_cakeResponseString).Should().BeTrue();

        _cakeResponse = Json.Deserialize<CakeResponse>(_cakeResponseString);

        _cakeResponse!.Ingredients.Should().Contain(_milkResponse!.Milk);
        _cakeResponse.Ingredients.Should().Contain(_eggsResponse!.Eggs);
        _cakeResponse.Ingredients.Should().Contain(_flourResponse!.Flour);
    }

    private void ThenTheResponseHttpStatusShouldBeBadRequest()
    {
        _cakeResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion
}
