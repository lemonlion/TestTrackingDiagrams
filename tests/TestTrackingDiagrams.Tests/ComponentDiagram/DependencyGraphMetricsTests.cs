using TestTrackingDiagrams.ComponentDiagram;

namespace TestTrackingDiagrams.Tests.ComponentDiagram;

public class DependencyGraphMetricsTests
{
    private static ComponentRelationship MakeRel(string caller, string service, int callCount = 1, int testCount = 1)
    {
        return new ComponentRelationship(caller, service, "HTTP", ["GET"], callCount, testCount);
    }

    // ═══════════════════════════════════════════════════════════
    // 1.8 Dependency graph metrics (Feature #11)
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void ComputeGraphMetrics_empty_returns_empty()
    {
        var result = ComponentFlowSegmentBuilder.ComputeGraphMetrics([]);

        Assert.Empty(result.Services);
        Assert.Empty(result.CircularDependencies);
        Assert.Equal(0, result.LongestChainLength);
    }

    [Fact]
    public void ComputeGraphMetrics_fan_in_fan_out()
    {
        // A → B, A → C, D → B
        var rels = new[]
        {
            MakeRel("A", "B"),
            MakeRel("A", "C"),
            MakeRel("D", "B")
        };

        var result = ComponentFlowSegmentBuilder.ComputeGraphMetrics(rels);

        var serviceA = result.Services.Single(s => s.Name == "A");
        Assert.Equal(0, serviceA.FanIn);
        Assert.Equal(2, serviceA.FanOut);

        var serviceB = result.Services.Single(s => s.Name == "B");
        Assert.Equal(2, serviceB.FanIn);
        Assert.Equal(0, serviceB.FanOut);

        var serviceC = result.Services.Single(s => s.Name == "C");
        Assert.Equal(1, serviceC.FanIn);
        Assert.Equal(0, serviceC.FanOut);

        var serviceD = result.Services.Single(s => s.Name == "D");
        Assert.Equal(0, serviceD.FanIn);
        Assert.Equal(1, serviceD.FanOut);
    }

    [Fact]
    public void ComputeGraphMetrics_detects_circular_dependency()
    {
        // A → B → C → A (cycle)
        var rels = new[]
        {
            MakeRel("A", "B"),
            MakeRel("B", "C"),
            MakeRel("C", "A")
        };

        var result = ComponentFlowSegmentBuilder.ComputeGraphMetrics(rels);

        Assert.Single(result.CircularDependencies);
        var cycle = result.CircularDependencies[0];
        Assert.Contains("A", cycle);
        Assert.Contains("B", cycle);
        Assert.Contains("C", cycle);
    }

    [Fact]
    public void ComputeGraphMetrics_no_circular_when_acyclic()
    {
        var rels = new[]
        {
            MakeRel("A", "B"),
            MakeRel("B", "C")
        };

        var result = ComponentFlowSegmentBuilder.ComputeGraphMetrics(rels);

        Assert.Empty(result.CircularDependencies);
    }

    [Fact]
    public void ComputeGraphMetrics_longest_chain()
    {
        // A → B → C → D (chain of length 3)
        var rels = new[]
        {
            MakeRel("A", "B"),
            MakeRel("B", "C"),
            MakeRel("C", "D")
        };

        var result = ComponentFlowSegmentBuilder.ComputeGraphMetrics(rels);

        Assert.Equal(3, result.LongestChainLength);
        Assert.Equal(4, result.LongestChain.Length); // A, B, C, D
        Assert.Equal("A", result.LongestChain[0]);
        Assert.Equal("D", result.LongestChain[3]);
    }

    [Fact]
    public void ComputeGraphMetrics_fan_in_includes_inbound_list()
    {
        var rels = new[]
        {
            MakeRel("A", "B"),
            MakeRel("C", "B"),
            MakeRel("D", "B")
        };

        var result = ComponentFlowSegmentBuilder.ComputeGraphMetrics(rels);

        var serviceB = result.Services.Single(s => s.Name == "B");
        Assert.Equal(3, serviceB.FanIn);
        Assert.Contains("A", serviceB.InboundFrom);
        Assert.Contains("C", serviceB.InboundFrom);
        Assert.Contains("D", serviceB.InboundFrom);
    }

    [Fact]
    public void ComputeGraphMetrics_multiple_cycles_detected()
    {
        // A → B → A (cycle 1) and C → D → C (cycle 2)
        var rels = new[]
        {
            MakeRel("A", "B"),
            MakeRel("B", "A"),
            MakeRel("C", "D"),
            MakeRel("D", "C")
        };

        var result = ComponentFlowSegmentBuilder.ComputeGraphMetrics(rels);

        Assert.Equal(2, result.CircularDependencies.Length);
    }
}
