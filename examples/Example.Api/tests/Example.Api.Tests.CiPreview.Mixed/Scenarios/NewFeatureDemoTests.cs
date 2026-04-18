using System.Net;
using System.Net.Http.Json;
using Example.Api.Requests;
using Example.Api.Responses;
using Example.Api.Tests.CiPreview.Mixed.Infrastructure;
using FluentAssertions;
using TestTrackingDiagrams.xUnit3;

namespace Example.Api.Tests.CiPreview.Mixed.Scenarios;

// Scenarios that demonstrate failure clustering, error diff rendering, and timeline features.

/// <summary>
/// Tests that produce Assert.Equal-style errors to demonstrate error diff rendering.
/// </summary>
[Endpoint("/cake")]
public class Cake_Error_Diff_Feature : BaseFixture
{
    [Fact]
    public async Task Cake_ingredients_should_include_sugar_butter_vanilla()
    {
        var milk = (await Client.GetFromJsonAsync<MilkResponse>("milk"))!;
        var eggs = (await Client.GetFromJsonAsync<EggsResponse>("eggs"))!;
        var flour = (await Client.GetFromJsonAsync<FlourResponse>("flour"))!;
        var response = await Client.PostAsJsonAsync("cake", new CakeRequest { Milk = milk.Milk, Eggs = eggs.Eggs, Flour = flour.Flour });
        var cake = await response.Content.ReadFromJsonAsync<CakeResponse>();
        // This produces an xUnit Assert.Equal Expected/Actual diff
        var actual = string.Join(", ", cake!.Ingredients);
        Assert.Equal("Sugar, Butter, Vanilla", actual);
    }

    [Fact]
    public async Task Cake_batch_id_should_be_a_specific_value()
    {
        var milk = (await Client.GetFromJsonAsync<MilkResponse>("milk"))!;
        var eggs = (await Client.GetFromJsonAsync<EggsResponse>("eggs"))!;
        var flour = (await Client.GetFromJsonAsync<FlourResponse>("flour"))!;
        var response = await Client.PostAsJsonAsync("cake", new CakeRequest { Milk = milk.Milk, Eggs = eggs.Eggs, Flour = flour.Flour });
        var cake = await response.Content.ReadFromJsonAsync<CakeResponse>();
        // Another Expected/Actual diff — expected a specific GUID
        Assert.Equal("00000000-0000-0000-0000-000000000001", cake!.BatchId.ToString());
    }

    [Fact]
    public async Task Cake_ingredient_count_should_be_five()
    {
        var milk = (await Client.GetFromJsonAsync<MilkResponse>("milk"))!;
        var eggs = (await Client.GetFromJsonAsync<EggsResponse>("eggs"))!;
        var flour = (await Client.GetFromJsonAsync<FlourResponse>("flour"))!;
        var response = await Client.PostAsJsonAsync("cake", new CakeRequest { Milk = milk.Milk, Eggs = eggs.Eggs, Flour = flour.Flour });
        var cake = await response.Content.ReadFromJsonAsync<CakeResponse>();
        // Numeric Expected/Actual diff
        Assert.Equal(5, cake!.Ingredients.Length);
    }
}

/// <summary>
/// Multiple tests that produce the same FluentAssertions error to demonstrate failure clustering.
/// All these tests expect 201 Created but get 200 OK — same root cause.
/// </summary>
[Endpoint("/milk")]
public class Milk_Cluster_Feature : BaseFixture
{
    [Fact]
    public async Task Getting_milk_should_return_201_created()
    {
        var response = await Client.GetAsync("milk");
        response.StatusCode.Should().Be(HttpStatusCode.Created, "the API should return 201 for new resources");
    }

    [Fact]
    public async Task Getting_milk_with_accept_header_should_return_201_created()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "milk");
        request.Headers.Add("Accept", "application/json");
        var response = await Client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Created, "the API should return 201 for new resources");
    }

    [Fact]
    public async Task Getting_milk_with_correlation_id_should_return_201_created()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "milk");
        request.Headers.Add("X-Correlation-Id", Guid.NewGuid().ToString());
        var response = await Client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Created, "the API should return 201 for new resources");
    }
}

/// <summary>
/// Another cluster: multiple tests that all expect 404 Not Found but get 200 OK.
/// Demonstrates a second failure cluster with a different root cause.
/// </summary>
[Endpoint("/eggs")]
public class Eggs_Cluster_Feature : BaseFixture
{
    [Fact]
    public async Task Getting_eggs_should_return_404_not_found()
    {
        var response = await Client.GetAsync("eggs");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound, "we expect the eggs endpoint to return 404");
    }

    [Fact]
    public async Task Getting_eggs_twice_should_return_404()
    {
        await Client.GetAsync("eggs");
        var response = await Client.GetAsync("eggs");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound, "we expect the eggs endpoint to return 404");
    }
}

/// <summary>
/// A third cluster: tests expecting 500 Internal Server Error but getting 200 OK.
/// </summary>
[Endpoint("/flour")]
public class Flour_Cluster_Feature : BaseFixture
{
    [Fact]
    public async Task Getting_flour_should_return_500()
    {
        var response = await Client.GetAsync("flour");
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError, "deliberately wrong — expecting server error");
    }

    [Fact]
    public async Task Flour_endpoint_should_indicate_failure()
    {
        var response = await Client.GetAsync("flour");
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError, "deliberately wrong — expecting server error");
    }
}
