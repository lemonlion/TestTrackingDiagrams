using System.Net;
using System.Net.Http.Json;
using Example.Api.Requests;
using Example.Api.Responses;
using Example.Api.Tests.Component.Shared;
using Example.Api.Tests.Component.NUnit.Infrastructure;
using FluentAssertions;

namespace Example.Api.Tests.Component.NUnit.Scenarios;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

[TestFixture]
public partial class Cake_Feature : BaseFixture
{
    private readonly CakeRequest _cakeRequest = new();
    private HttpResponseMessage? _cakeResponseMessage;
    private string? _cakeResponseString;
    private CakeResponse? _cakeResponse;

    private MilkResponse _milkResponse = new();
    private FlourResponse _flourResponse = new();
    private EggsResponse _eggsResponse = new();

    #region Given

    private async Task Given_a_valid_post_request_for_the_Cake_endpoint()
    {
        await A_valid_request_body();
    }

    private async Task A_valid_request_body()
    {
        await The_body_specifies_milk();
        await The_body_specifies_eggs();
        await The_body_specifies_flour();
    }

    private async Task The_body_specifies_milk()
    {
        await Milk_is_retrieved_from_the_get_milk_endpoint();
        await Retrieved_milk_is_set_on_the_body();
    }

    private async Task Milk_is_retrieved_from_the_get_milk_endpoint()
    {
        _milkResponse = (await Client.GetFromJsonAsync<MilkResponse>("milk"))!;
    }

    private async Task Retrieved_milk_is_set_on_the_body() => _cakeRequest.Milk = _milkResponse.Milk;

    private async Task The_body_specifies_eggs()
    {
        await Eggs_is_retrieved_from_the_get_eggs_endpoint();
        await Retrieved_eggs_is_set_on_the_body();
    }

    private async Task Eggs_is_retrieved_from_the_get_eggs_endpoint()
    {
        _eggsResponse = (await Client.GetFromJsonAsync<EggsResponse>("eggs"))!;
    }

    private async Task Retrieved_eggs_is_set_on_the_body() => _cakeRequest.Eggs = _eggsResponse.Eggs;

    private async Task The_body_specifies_flour()
    {
        await Flour_is_retrieved_from_the_get_flour_endpoint();
        await Retrieved_flour_is_set_on_the_body();
    }

    private async Task Flour_is_retrieved_from_the_get_flour_endpoint()
    {
        _flourResponse = (await Client.GetFromJsonAsync<FlourResponse>("flour"))!;
    }

    private async Task Retrieved_flour_is_set_on_the_body() => _cakeRequest.Flour = _flourResponse.Flour;

    private async Task But_the_request_body_is_missing_eggs() => _cakeRequest.Eggs = null;

    #endregion

    #region When
    private async Task When_the_request_is_sent_to_the_cake_post_endpoint()
    {
        _cakeResponseMessage = await Client.PostAsJsonAsync("cake", _cakeRequest);
    }
    #endregion

    #region Then
    private async Task Then_the_response_should_be_successful()
    {
        await The_response_http_status_should_be_ok();
        await The_response_should_be_valid_json();
        await The_response_ingredients_should_include_milk();
        await The_response_ingredients_should_include_eggs();
        await The_response_ingredients_should_include_flour();
    }

    private async Task The_response_http_status_should_be_ok() => _cakeResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

    private async Task The_response_should_be_valid_json()
    {
        _cakeResponseString = await _cakeResponseMessage!.Content.ReadAsStringAsync();
        Json.IsValid(_cakeResponseString).Should().BeTrue();
        
        _cakeResponse = Json.Deserialize<CakeResponse>(_cakeResponseString)!;
    }

    private async Task The_response_ingredients_should_include_milk() => _cakeResponse!.Ingredients.Should().Contain(_milkResponse.Milk);
    private async Task The_response_ingredients_should_include_eggs() => _cakeResponse!.Ingredients.Should().Contain(_flourResponse.Flour);
    private async Task The_response_ingredients_should_include_flour() => _cakeResponse!.Ingredients.Should().Contain(_eggsResponse.Eggs);
    
    private async Task Then_the_response_http_status_should_be_bad_request() => _cakeResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    #endregion
}