using System.Net;
using System.Net.Http.Json;
using Example.Api.Requests;
using Example.Api.Responses;
using Example.Api.Tests.CiPreview.Mixed.Infrastructure;
using FluentAssertions;
using TestTrackingDiagrams.xUnit3;

namespace Example.Api.Tests.CiPreview.Mixed.Scenarios;

// 5 passing + 5 failing tests to demonstrate mixed CI summary output.

[Endpoint("/cake")]
public class Cake_Feature : BaseFixture
{
    // --- PASSING ---

    [Fact]
    [HappyPath]
    public async Task Creating_a_cake_returns_success()
    {
        var milk = (await Client.GetFromJsonAsync<MilkResponse>("milk"))!;
        var eggs = (await Client.GetFromJsonAsync<EggsResponse>("eggs"))!;
        var flour = (await Client.GetFromJsonAsync<FlourResponse>("flour"))!;
        var response = await Client.PostAsJsonAsync("cake", new CakeRequest { Milk = milk.Milk, Eggs = eggs.Eggs, Flour = flour.Flour });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Creating_a_cake_without_eggs_returns_bad_request()
    {
        var milk = (await Client.GetFromJsonAsync<MilkResponse>("milk"))!;
        var flour = (await Client.GetFromJsonAsync<FlourResponse>("flour"))!;
        var response = await Client.PostAsJsonAsync("cake", new CakeRequest { Milk = milk.Milk, Flour = flour.Flour });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [HappyPath]
    public async Task Creating_a_cake_response_contains_all_ingredients()
    {
        var milk = (await Client.GetFromJsonAsync<MilkResponse>("milk"))!;
        var eggs = (await Client.GetFromJsonAsync<EggsResponse>("eggs"))!;
        var flour = (await Client.GetFromJsonAsync<FlourResponse>("flour"))!;
        var response = await Client.PostAsJsonAsync("cake", new CakeRequest { Milk = milk.Milk, Eggs = eggs.Eggs, Flour = flour.Flour });
        var cake = await response.Content.ReadFromJsonAsync<CakeResponse>();
        cake!.Ingredients.Should().HaveCount(3);
    }

    // --- FAILING ---

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
public class Milk_Feature : BaseFixture
{
    // --- PASSING ---

    [Fact]
    [HappyPath]
    public async Task Getting_milk_returns_success()
    {
        var response = await Client.GetAsync("milk");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // --- FAILING ---

    [Fact]
    public async Task Getting_milk_should_return_not_found()
    {
        var response = await Client.GetAsync("milk");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound, "deliberately wrong expectation");
    }
}

[Endpoint("/flour")]
public class Flour_Feature : BaseFixture
{
    // --- PASSING ---

    [Fact]
    [HappyPath]
    public async Task Getting_flour_returns_success()
    {
        var response = await Client.GetAsync("flour");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // --- FAILING ---

    [Fact]
    public async Task Getting_flour_should_return_internal_server_error()
    {
        var response = await Client.GetAsync("flour");
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError, "deliberately wrong expectation");
    }
}
