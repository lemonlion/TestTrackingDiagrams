using TestTrackingDiagrams.Extensions.AtlasDataApi;

namespace TestTrackingDiagrams.Tests.AtlasDataApi;

public class AtlasDataApiOperationClassifierTests
{
    // ── Classify: action → operation mapping ──

    [Theory]
    [InlineData("findOne", AtlasDataApiOperation.FindOne)]
    [InlineData("find", AtlasDataApiOperation.Find)]
    [InlineData("insertOne", AtlasDataApiOperation.InsertOne)]
    [InlineData("insertMany", AtlasDataApiOperation.InsertMany)]
    [InlineData("updateOne", AtlasDataApiOperation.UpdateOne)]
    [InlineData("updateMany", AtlasDataApiOperation.UpdateMany)]
    [InlineData("deleteOne", AtlasDataApiOperation.DeleteOne)]
    [InlineData("deleteMany", AtlasDataApiOperation.DeleteMany)]
    [InlineData("replaceOne", AtlasDataApiOperation.ReplaceOne)]
    [InlineData("aggregate", AtlasDataApiOperation.Aggregate)]
    public void Classify_ActionMapsToCorrectOperation(string action, AtlasDataApiOperation expected)
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"https://data.mongodb-api.com/app/myapp/endpoint/data/v1/action/{action}");

        var result = AtlasDataApiOperationClassifier.Classify(request);

        Assert.Equal(expected, result.Operation);
    }

    [Fact]
    public void Classify_UnknownAction_ReturnsOther()
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            "https://data.mongodb-api.com/app/myapp/endpoint/data/v1/action/unknownOp");

        var result = AtlasDataApiOperationClassifier.Classify(request);

        Assert.Equal(AtlasDataApiOperation.Other, result.Operation);
    }

    [Fact]
    public void Classify_NonActionPath_ReturnsOther()
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            "https://data.mongodb-api.com/app/myapp/endpoint/data/v1/healthcheck");

        var result = AtlasDataApiOperationClassifier.Classify(request);

        Assert.Equal(AtlasDataApiOperation.Other, result.Operation);
    }

    [Fact]
    public void Classify_CaseInsensitiveAction()
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            "https://data.mongodb-api.com/app/myapp/endpoint/data/v1/action/FINDONE");

        var result = AtlasDataApiOperationClassifier.Classify(request);

        Assert.Equal(AtlasDataApiOperation.FindOne, result.Operation);
    }

    // ── Classify: body extraction ──

    [Fact]
    public void Classify_ExtractsDataSourceFromBody()
    {
        var request = CreateActionRequest("find");
        var body = """{"dataSource":"Cluster0","database":"myDb","collection":"users"}""";

        var result = AtlasDataApiOperationClassifier.Classify(request, body);

        Assert.Equal("Cluster0", result.DataSource);
        Assert.Equal("myDb", result.DatabaseName);
        Assert.Equal("users", result.CollectionName);
    }

    [Fact]
    public void Classify_ExtractsFilterFromBody()
    {
        var request = CreateActionRequest("findOne");
        var body = """{"dataSource":"Cluster0","database":"myDb","collection":"users","filter":{"name":"Alice"}}""";

        var result = AtlasDataApiOperationClassifier.Classify(request, body);

        Assert.Equal("""{"name":"Alice"}""", result.FilterText);
    }

    [Fact]
    public void Classify_NullBody_NoMetadata()
    {
        var request = CreateActionRequest("insertOne");

        var result = AtlasDataApiOperationClassifier.Classify(request, null);

        Assert.Equal(AtlasDataApiOperation.InsertOne, result.Operation);
        Assert.Null(result.DataSource);
        Assert.Null(result.DatabaseName);
        Assert.Null(result.CollectionName);
        Assert.Null(result.FilterText);
    }

    [Fact]
    public void Classify_MalformedJson_NoMetadata()
    {
        var request = CreateActionRequest("find");

        var result = AtlasDataApiOperationClassifier.Classify(request, "not json at all");

        Assert.Equal(AtlasDataApiOperation.Find, result.Operation);
        Assert.Null(result.DataSource);
    }

    // ── Directional arrows ──

    [Theory]
    [InlineData(AtlasDataApiOperation.FindOne, "←")]
    [InlineData(AtlasDataApiOperation.Find, "←")]
    [InlineData(AtlasDataApiOperation.Aggregate, "←")]
    [InlineData(AtlasDataApiOperation.InsertOne, "→")]
    [InlineData(AtlasDataApiOperation.InsertMany, "→")]
    [InlineData(AtlasDataApiOperation.DeleteOne, "→")]
    [InlineData(AtlasDataApiOperation.DeleteMany, "→")]
    [InlineData(AtlasDataApiOperation.UpdateOne, "↔")]
    [InlineData(AtlasDataApiOperation.UpdateMany, "↔")]
    [InlineData(AtlasDataApiOperation.ReplaceOne, "↔")]
    [InlineData(AtlasDataApiOperation.Other, "→")]
    public void GetDirectionalArrow_ReturnsCorrectArrow(AtlasDataApiOperation op, string expected)
    {
        var result = AtlasDataApiOperationClassifier.GetDirectionalArrow(op);
        Assert.Equal(expected, result);
    }

    // ── GetDiagramLabel ──

    [Fact]
    public void GetDiagramLabel_Detailed_WithCollection_IncludesArrowAndCollection()
    {
        var op = new AtlasDataApiOperationInfo(AtlasDataApiOperation.Find, "Cluster0", "myDb", "users");

        var label = AtlasDataApiOperationClassifier.GetDiagramLabel(op, AtlasDataApiTrackingVerbosity.Detailed);

        Assert.Equal("Find ← users", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_NoCollection_JustOperation()
    {
        var op = new AtlasDataApiOperationInfo(AtlasDataApiOperation.Find, "Cluster0", "myDb", null);

        var label = AtlasDataApiOperationClassifier.GetDiagramLabel(op, AtlasDataApiTrackingVerbosity.Detailed);

        Assert.Equal("Find", label);
    }

    [Fact]
    public void GetDiagramLabel_Summarised_JustOperationName()
    {
        var op = new AtlasDataApiOperationInfo(AtlasDataApiOperation.InsertOne, "Cluster0", "myDb", "orders");

        var label = AtlasDataApiOperationClassifier.GetDiagramLabel(op, AtlasDataApiTrackingVerbosity.Summarised);

        Assert.Equal("InsertOne", label);
    }

    [Fact]
    public void GetDiagramLabel_Raw_JustOperationName()
    {
        var op = new AtlasDataApiOperationInfo(AtlasDataApiOperation.DeleteOne, "Cluster0", "myDb", "orders");

        var label = AtlasDataApiOperationClassifier.GetDiagramLabel(op, AtlasDataApiTrackingVerbosity.Raw);

        Assert.Equal("DeleteOne", label);
    }

    [Theory]
    [InlineData(AtlasDataApiOperation.InsertOne, "→")]
    [InlineData(AtlasDataApiOperation.UpdateOne, "↔")]
    [InlineData(AtlasDataApiOperation.Aggregate, "←")]
    public void GetDiagramLabel_Detailed_WritesAndUpdatesHaveCorrectArrows(
        AtlasDataApiOperation operation, string expectedArrow)
    {
        var op = new AtlasDataApiOperationInfo(operation, "Cluster0", "myDb", "items");

        var label = AtlasDataApiOperationClassifier.GetDiagramLabel(op, AtlasDataApiTrackingVerbosity.Detailed);

        Assert.Contains(expectedArrow, label);
        Assert.Contains("items", label);
    }

    // ── Helper ──

    private static HttpRequestMessage CreateActionRequest(string action) =>
        new(HttpMethod.Post,
            $"https://data.mongodb-api.com/app/myapp/endpoint/data/v1/action/{action}");
}
