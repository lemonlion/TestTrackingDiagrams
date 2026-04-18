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

    /// <summary>Produces verbose request headers to trigger note truncation in CI summary.</summary>
    [Fact]
    public async Task Creating_a_cake_with_tracing_headers_returns_success()
    {
        var milk = (await Client.GetFromJsonAsync<MilkResponse>("milk"))!;
        var eggs = (await Client.GetFromJsonAsync<EggsResponse>("eggs"))!;
        var flour = (await Client.GetFromJsonAsync<FlourResponse>("flour"))!;
        using var request = new HttpRequestMessage(HttpMethod.Post, "cake");
        request.Content = JsonContent.Create(new CakeRequest { Milk = milk.Milk, Eggs = eggs.Eggs, Flour = flour.Flour });
        request.Headers.Add("X-Correlation-Id", Guid.NewGuid().ToString());
        request.Headers.Add("X-Request-Id", Guid.NewGuid().ToString());
        request.Headers.Add("X-Trace-Id", Guid.NewGuid().ToString());
        request.Headers.Add("X-Session-Id", Guid.NewGuid().ToString());
        request.Headers.Add("X-Client-Version", "2.0.43-beta");
        request.Headers.Add("X-Feature-Flags", "dark-mode,new-checkout,beta-recipes,seasonal-menu");
        request.Headers.Add("X-Region", "eu-west-1");
        request.Headers.Add("X-Retry-Count", "0");
        request.Headers.Add("X-Idempotency-Key", Guid.NewGuid().ToString());
        request.Headers.Add("X-Source-Service", "order-orchestrator");
        request.Headers.Add("X-Priority", "high");
        request.Headers.Add("X-Tenant-Id", "dessert-factory-uk");
        var response = await Client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
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
