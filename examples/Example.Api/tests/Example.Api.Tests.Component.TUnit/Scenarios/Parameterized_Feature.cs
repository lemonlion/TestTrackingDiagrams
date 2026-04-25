using TestTrackingDiagrams.TUnit;
using TUnit.Core;

namespace Example.Api.Tests.Component.TUnit.Scenarios;

// Records for parameterized test data
public record OrderScenario(string Region, int Amount, string Currency);
public record ShippingAddress(string Street, string City, string PostCode);
public record CustomerOrder(string Name, int Age, string Email, ShippingAddress Address);

/// <summary>
/// Parameterized tests exercising TUnit's [Arguments] and [MethodDataSource] attributes
/// to verify the full pipeline: C# attribute → TUnit adapter → ExampleValues/ExampleRawValues → HTML report rendering.
/// Covers R1 (scalar columns), R2 (flattened object), R3 (sub-table), R4 (expandable).
/// </summary>
[Endpoint("/parameterized")]
public class Parameterized_Feature : DiagrammedComponentTest
{
    // R1: Multiple scalar parameters → ScalarColumns rendering
    [Test]
    [Arguments("UK", 100, true)]
    [Arguments("US", 200, false)]
    [Arguments("DE", 300, true)]
    public async Task Process_order_in_region(string region, int amount, bool expedited)
    {
        await Task.CompletedTask;
    }

    // R2: Single record parameter → FlattenedObject rendering (reflection-based via ExampleRawValues)
    [Test]
    [MethodDataSource(nameof(GetOrderScenarios))]
    public async Task Validate_order_scenario(OrderScenario scenario)
    {
        await Task.CompletedTask;
    }

    public static IEnumerable<OrderScenario> GetOrderScenarios()
    {
        yield return new OrderScenario("UK", 100, "GBP");
        yield return new OrderScenario("US", 200, "USD");
        yield return new OrderScenario("JP", 400, "JPY");
    }

    // R3: Scalar + small complex object → SubTable for the complex param cell
    [Test]
    [MethodDataSource(nameof(GetShippingScenarios))]
    public async Task Ship_to_address(int orderId, ShippingAddress address)
    {
        await Task.CompletedTask;
    }

    public static IEnumerable<(int OrderId, ShippingAddress Address)> GetShippingScenarios()
    {
        yield return (101, new ShippingAddress("10 High St", "Manchester", "M1 1AA"));
        yield return (102, new ShippingAddress("20 Broad St", "Bristol", "BS1 2AB"));
    }

    // R4: Scalar + deeply nested object → Expandable rendering for the complex param cell
    [Test]
    [MethodDataSource(nameof(GetCustomerScenarios))]
    public async Task Enroll_customer(string tier, CustomerOrder customer)
    {
        await Task.CompletedTask;
    }

    public static IEnumerable<(string Tier, CustomerOrder Customer)> GetCustomerScenarios()
    {
        yield return ("Gold", new CustomerOrder("Alice", 30, "alice@test.com", new ShippingAddress("1 Main St", "London", "SW1A")));
        yield return ("Silver", new CustomerOrder("Bob", 25, "bob@test.com", new ShippingAddress("2 Oak Ave", "Paris", "75001")));
    }
}
