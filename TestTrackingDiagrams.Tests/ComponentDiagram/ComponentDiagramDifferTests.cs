using System.Net;
using TestTrackingDiagrams.ComponentDiagram;

namespace TestTrackingDiagrams.Tests.ComponentDiagram;

public class ComponentDiagramDifferTests
{
    private static ComponentRelationship Rel(string caller, string service, int calls = 1, int tests = 1) =>
        new(caller, service, "HTTP", new HashSet<string> { "GET" }, calls, tests);

    // ── New / Removed / Unchanged detection ──

    [Fact]
    public void CompareDiagrams_DetectsNewRelationships()
    {
        var baseline = new[] { Rel("A", "B") };
        var current = new[] { Rel("A", "B"), Rel("A", "C") };

        var diff = ComponentDiagramDiffer.Compare(baseline, current);

        Assert.Single(diff.Added);
        Assert.Equal("A", diff.Added[0].Caller);
        Assert.Equal("C", diff.Added[0].Service);
    }

    [Fact]
    public void CompareDiagrams_DetectsRemovedRelationships()
    {
        var baseline = new[] { Rel("A", "B"), Rel("A", "C") };
        var current = new[] { Rel("A", "B") };

        var diff = ComponentDiagramDiffer.Compare(baseline, current);

        Assert.Single(diff.Removed);
        Assert.Equal("A", diff.Removed[0].Caller);
        Assert.Equal("C", diff.Removed[0].Service);
    }

    [Fact]
    public void CompareDiagrams_DetectsUnchangedRelationships()
    {
        var baseline = new[] { Rel("A", "B") };
        var current = new[] { Rel("A", "B") };

        var diff = ComponentDiagramDiffer.Compare(baseline, current);

        Assert.Single(diff.Unchanged);
        Assert.Empty(diff.Added);
        Assert.Empty(diff.Removed);
    }

    [Fact]
    public void CompareDiagrams_DetectsNewServices()
    {
        var baseline = new[] { Rel("A", "B") };
        var current = new[] { Rel("A", "B"), Rel("B", "C") };

        var diff = ComponentDiagramDiffer.Compare(baseline, current);

        Assert.Contains("C", diff.NewServices);
        Assert.DoesNotContain("A", diff.NewServices);
    }

    [Fact]
    public void CompareDiagrams_DetectsRemovedServices()
    {
        var baseline = new[] { Rel("A", "B"), Rel("B", "C") };
        var current = new[] { Rel("A", "B") };

        var diff = ComponentDiagramDiffer.Compare(baseline, current);

        Assert.Contains("C", diff.RemovedServices);
    }

    // ── Stats comparison ──

    [Fact]
    public void CompareDiagrams_WithStats_DetectsRegression()
    {
        var rels = new[] { Rel("A", "B") };
        var baselineStats = new Dictionary<string, RelationshipStats>
        {
            ["iflow-rel-A-B"] = new(10, 5, 50, 50, 80, 90, 10, 100, 0,
                new Dictionary<HttpStatusCode, int>(), [], null, null, false)
        };
        var currentStats = new Dictionary<string, RelationshipStats>
        {
            ["iflow-rel-A-B"] = new(10, 5, 150, 150, 300, 400, 50, 500, 0,
                new Dictionary<HttpStatusCode, int>(), [], null, null, false)
        };

        var diff = ComponentDiagramDiffer.Compare(rels, rels, baselineStats, currentStats);

        Assert.Single(diff.Regressions);
        Assert.Equal("A", diff.Regressions[0].Relationship.Caller);
    }

    [Fact]
    public void CompareDiagrams_WithStats_DetectsImprovement()
    {
        var rels = new[] { Rel("A", "B") };
        var baselineStats = new Dictionary<string, RelationshipStats>
        {
            ["iflow-rel-A-B"] = new(10, 5, 150, 150, 300, 400, 50, 500, 0,
                new Dictionary<HttpStatusCode, int>(), [], null, null, false)
        };
        var currentStats = new Dictionary<string, RelationshipStats>
        {
            ["iflow-rel-A-B"] = new(10, 5, 50, 50, 80, 90, 10, 100, 0,
                new Dictionary<HttpStatusCode, int>(), [], null, null, false)
        };

        var diff = ComponentDiagramDiffer.Compare(rels, rels, baselineStats, currentStats);

        Assert.Single(diff.Improvements);
    }

    // ── Diff PlantUML generation ──

    [Fact]
    public void GenerateDiffPlantUml_MarksNewRelationshipsGreen()
    {
        var baseline = new[] { Rel("A", "B") };
        var current = new[] { Rel("A", "B"), Rel("A", "C") };

        var diff = ComponentDiagramDiffer.Compare(baseline, current);
        var puml = ComponentDiagramDiffer.GenerateDiffPlantUml(diff);

        Assert.Contains("@startuml", puml);
        Assert.Contains("#LimeGreen", puml);
        Assert.Contains("C", puml);
    }

    [Fact]
    public void GenerateDiffPlantUml_MarksRemovedRelationshipsRed()
    {
        var baseline = new[] { Rel("A", "B"), Rel("A", "C") };
        var current = new[] { Rel("A", "B") };

        var diff = ComponentDiagramDiffer.Compare(baseline, current);
        var puml = ComponentDiagramDiffer.GenerateDiffPlantUml(diff);

        Assert.Contains("#Red", puml);
        Assert.Contains("REMOVED", puml);
    }

    [Fact]
    public void GenerateDiffPlantUml_EmptyBaseline_AllNew()
    {
        var diff = ComponentDiagramDiffer.Compare([], new[] { Rel("A", "B") });
        var puml = ComponentDiagramDiffer.GenerateDiffPlantUml(diff);

        Assert.Contains("#LimeGreen", puml);
    }

    [Fact]
    public void GenerateDiffPlantUml_DoesNotUseRemoteIncludes()
    {
        var diff = ComponentDiagramDiffer.Compare([], new[] { Rel("A", "B") });
        var puml = ComponentDiagramDiffer.GenerateDiffPlantUml(diff);

        Assert.DoesNotContain("!include http", puml);
        Assert.Contains("!include <C4/", puml);
    }
}
