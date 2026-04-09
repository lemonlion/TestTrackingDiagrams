using System.Net;
using System.Net.Http.Json;
using Example.Api.Requests;
using Example.Api.Responses;
using Example.Api.Tests.CiPreview.AllFailing.Infrastructure;
using FluentAssertions;
using TestTrackingDiagrams.xUnit3;

namespace Example.Api.Tests.CiPreview.AllFailing.Scenarios;

// These tests deliberately assert the wrong status code to produce failures with diagrams.

[Endpoint("/cake")]
public class Cake_Failures : BaseFixture
{
    [Fact]
    public async Task Creating_a_cake_should_return_created()
    {
        var milk = (await Client.GetFromJsonAsync<MilkResponse>("milk"))!;
        var eggs = (await Client.GetFromJsonAsync<EggsResponse>("eggs"))!;
        var flour = (await Client.GetFromJsonAsync<FlourResponse>("flour"))!;
        var response = await Client.PostAsJsonAsync("cake", new CakeRequest { Milk = milk.Milk, Eggs = eggs.Eggs, Flour = flour.Flour });
        response.StatusCode.Should().Be(HttpStatusCode.Created, "we expect 201 Created but the API returns 200 OK");
    }

    [Fact]
    public async Task Creating_a_cake_without_eggs_should_return_not_found()
    {
        var milk = (await Client.GetFromJsonAsync<MilkResponse>("milk"))!;
        var flour = (await Client.GetFromJsonAsync<FlourResponse>("flour"))!;
        var response = await Client.PostAsJsonAsync("cake", new CakeRequest { Milk = milk.Milk, Flour = flour.Flour });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound, "deliberately wrong expectation");
    }

    [Fact]
    public async Task Creating_a_cake_without_milk_should_return_conflict()
    {
        var eggs = (await Client.GetFromJsonAsync<EggsResponse>("eggs"))!;
        var flour = (await Client.GetFromJsonAsync<FlourResponse>("flour"))!;
        var response = await Client.PostAsJsonAsync("cake", new CakeRequest { Eggs = eggs.Eggs, Flour = flour.Flour });
        response.StatusCode.Should().Be(HttpStatusCode.Conflict, "deliberately wrong expectation");
    }

    [Fact]
    public async Task Creating_a_cake_without_flour_should_return_unauthorized()
    {
        var milk = (await Client.GetFromJsonAsync<MilkResponse>("milk"))!;
        var eggs = (await Client.GetFromJsonAsync<EggsResponse>("eggs"))!;
        var response = await Client.PostAsJsonAsync("cake", new CakeRequest { Milk = milk.Milk, Eggs = eggs.Eggs });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "deliberately wrong expectation");
    }

    [Fact]
    public async Task Creating_a_cake_response_should_have_four_ingredients()
    {
        var milk = (await Client.GetFromJsonAsync<MilkResponse>("milk"))!;
        var eggs = (await Client.GetFromJsonAsync<EggsResponse>("eggs"))!;
        var flour = (await Client.GetFromJsonAsync<FlourResponse>("flour"))!;
        var response = await Client.PostAsJsonAsync("cake", new CakeRequest { Milk = milk.Milk, Eggs = eggs.Eggs, Flour = flour.Flour });
        var cake = await response.Content.ReadFromJsonAsync<CakeResponse>();
        cake!.Ingredients.Should().HaveCount(4, "deliberately wrong — only 3 ingredients exist");
    }

    [Fact]
    public async Task Creating_a_cake_response_should_contain_sugar()
    {
        var milk = (await Client.GetFromJsonAsync<MilkResponse>("milk"))!;
        var eggs = (await Client.GetFromJsonAsync<EggsResponse>("eggs"))!;
        var flour = (await Client.GetFromJsonAsync<FlourResponse>("flour"))!;
        var response = await Client.PostAsJsonAsync("cake", new CakeRequest { Milk = milk.Milk, Eggs = eggs.Eggs, Flour = flour.Flour });
        var cake = await response.Content.ReadFromJsonAsync<CakeResponse>();
        cake!.Ingredients.Should().Contain("Sugar", "deliberately wrong — sugar is not an ingredient");
    }
}

[Endpoint("/milk")]
public class Milk_Failures : BaseFixture
{
    [Fact]
    public async Task Getting_milk_should_return_not_found()
    {
        var response = await Client.GetAsync("milk");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound, "deliberately wrong expectation");
    }

    [Fact]
    public async Task Getting_milk_should_return_empty_value()
    {
        var milk = await Client.GetFromJsonAsync<MilkResponse>("milk");
        milk!.Milk.Should().BeNullOrEmpty("deliberately wrong — milk has a value");
    }
}

[Endpoint("/eggs")]
public class Eggs_Failures : BaseFixture
{
    [Fact]
    public async Task Getting_eggs_should_return_forbidden()
    {
        var response = await Client.GetAsync("eggs");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, "deliberately wrong expectation");
    }
}

[Endpoint("/flour")]
public class Flour_Failures : BaseFixture
{
    [Fact]
    public async Task Getting_flour_should_return_internal_server_error()
    {
        var response = await Client.GetAsync("flour");
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError, "deliberately wrong expectation");
    }
}
