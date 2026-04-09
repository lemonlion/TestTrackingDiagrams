using System.Net;
using System.Net.Http.Json;
using Example.Api.Requests;
using Example.Api.Responses;
using Example.Api.Tests.CiPreview.AllPassing.Infrastructure;
using FluentAssertions;
using TestTrackingDiagrams.xUnit3;

namespace Example.Api.Tests.CiPreview.AllPassing.Scenarios;

[Endpoint("/cake")]
public class Cake_Feature : BaseFixture
{
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
    public async Task Creating_a_cake_without_milk_returns_bad_request()
    {
        var eggs = (await Client.GetFromJsonAsync<EggsResponse>("eggs"))!;
        var flour = (await Client.GetFromJsonAsync<FlourResponse>("flour"))!;
        var response = await Client.PostAsJsonAsync("cake", new CakeRequest { Eggs = eggs.Eggs, Flour = flour.Flour });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Creating_a_cake_without_flour_returns_bad_request()
    {
        var milk = (await Client.GetFromJsonAsync<MilkResponse>("milk"))!;
        var eggs = (await Client.GetFromJsonAsync<EggsResponse>("eggs"))!;
        var response = await Client.PostAsJsonAsync("cake", new CakeRequest { Milk = milk.Milk, Eggs = eggs.Eggs });
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
}

[Endpoint("/milk")]
public class Milk_Feature : BaseFixture
{
    [Fact]
    [HappyPath]
    public async Task Getting_milk_returns_success()
    {
        var response = await Client.GetAsync("milk");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Getting_milk_returns_milk_value()
    {
        var milk = await Client.GetFromJsonAsync<MilkResponse>("milk");
        milk!.Milk.Should().NotBeNullOrEmpty();
    }
}

[Endpoint("/eggs")]
public class Eggs_Feature : BaseFixture
{
    [Fact]
    [HappyPath]
    public async Task Getting_eggs_returns_success()
    {
        var response = await Client.GetAsync("eggs");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

[Endpoint("/flour")]
public class Flour_Feature : BaseFixture
{
    [Fact]
    [HappyPath]
    public async Task Getting_flour_returns_success()
    {
        var response = await Client.GetAsync("flour");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Getting_flour_returns_flour_value()
    {
        var flour = await Client.GetFromJsonAsync<FlourResponse>("flour");
        flour!.Flour.Should().NotBeNullOrEmpty();
    }
}
