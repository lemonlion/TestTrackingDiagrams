using MongoDB.Bson;
using TestTrackingDiagrams.Extensions.MongoDB;

namespace TestTrackingDiagrams.Tests.MongoDB;

public class MongoDbOperationClassifierTests
{
    // ─── Command name mapping ────────────────────────────────

    [Theory]
    [InlineData("find", MongoDbOperation.Find)]
    [InlineData("insert", MongoDbOperation.Insert)]
    [InlineData("update", MongoDbOperation.Update)]
    [InlineData("delete", MongoDbOperation.Delete)]
    [InlineData("aggregate", MongoDbOperation.Aggregate)]
    [InlineData("count", MongoDbOperation.Count)]
    [InlineData("countDocuments", MongoDbOperation.Count)]
    [InlineData("findAndModify", MongoDbOperation.FindAndModify)]
    [InlineData("distinct", MongoDbOperation.Distinct)]
    [InlineData("bulkWrite", MongoDbOperation.BulkWrite)]
    [InlineData("createIndexes", MongoDbOperation.CreateIndex)]
    [InlineData("dropIndexes", MongoDbOperation.DropIndex)]
    [InlineData("create", MongoDbOperation.CreateCollection)]
    [InlineData("drop", MongoDbOperation.DropCollection)]
    [InlineData("listCollections", MongoDbOperation.ListCollections)]
    [InlineData("listDatabases", MongoDbOperation.ListDatabases)]
    [InlineData("getMore", MongoDbOperation.GetMore)]
    public void Classify_MapsCommandNameToCorrectOperation(string commandName, MongoDbOperation expected)
    {
        var command = new BsonDocument(commandName, "testcollection");

        var result = MongoDbOperationClassifier.Classify(commandName, "testdb", command);

        Assert.Equal(expected, result.Operation);
    }

    [Fact]
    public void Classify_UnknownCommand_ReturnsOther()
    {
        var result = MongoDbOperationClassifier.Classify("unknownCommand", "testdb", null);

        Assert.Equal(MongoDbOperation.Other, result.Operation);
    }

    [Theory]
    [InlineData("Find")]
    [InlineData("FIND")]
    [InlineData("fInD")]
    public void Classify_CaseInsensitiveCommandName(string commandName)
    {
        var command = new BsonDocument(commandName, "users");

        var result = MongoDbOperationClassifier.Classify(commandName, "testdb", command);

        Assert.Equal(MongoDbOperation.Find, result.Operation);
    }

    // ─── Collection name extraction ──────────────────────────

    [Fact]
    public void Classify_ExtractsCollectionNameFromFindCommand()
    {
        var command = new BsonDocument { { "find", "users" }, { "filter", new BsonDocument() } };

        var result = MongoDbOperationClassifier.Classify("find", "mydb", command);

        Assert.Equal("users", result.CollectionName);
    }

    [Fact]
    public void Classify_ExtractsCollectionNameFromInsertCommand()
    {
        var command = new BsonDocument { { "insert", "orders" }, { "documents", new BsonArray() } };

        var result = MongoDbOperationClassifier.Classify("insert", "mydb", command);

        Assert.Equal("orders", result.CollectionName);
    }

    [Fact]
    public void Classify_ExtractsCollectionNameFromAggregateCommand()
    {
        var command = new BsonDocument { { "aggregate", "products" }, { "pipeline", new BsonArray() } };

        var result = MongoDbOperationClassifier.Classify("aggregate", "mydb", command);

        Assert.Equal("products", result.CollectionName);
    }

    [Fact]
    public void Classify_DatabaseLevelAggregate_CollectionIsNull()
    {
        var command = new BsonDocument { { "aggregate", 1 }, { "pipeline", new BsonArray() } };

        var result = MongoDbOperationClassifier.Classify("aggregate", "mydb", command);

        Assert.Null(result.CollectionName);
    }

    [Fact]
    public void Classify_NullCommand_CollectionIsNull()
    {
        var result = MongoDbOperationClassifier.Classify("find", "mydb", null);

        Assert.Null(result.CollectionName);
    }

    [Fact]
    public void Classify_IncludesDatabaseName()
    {
        var result = MongoDbOperationClassifier.Classify("find", "myapp", new BsonDocument("find", "users"));

        Assert.Equal("myapp", result.DatabaseName);
    }

    // ─── Filter extraction ───────────────────────────────────

    [Fact]
    public void Classify_ExtractsFilterFromCommand()
    {
        var filter = new BsonDocument("age", new BsonDocument("$gt", 25));
        var command = new BsonDocument { { "find", "users" }, { "filter", filter } };

        var result = MongoDbOperationClassifier.Classify("find", "mydb", command);

        Assert.NotNull(result.FilterText);
        Assert.Contains("age", result.FilterText);
    }

    [Fact]
    public void Classify_NoFilter_FilterTextIsNull()
    {
        var command = new BsonDocument { { "insert", "orders" }, { "documents", new BsonArray() } };

        var result = MongoDbOperationClassifier.Classify("insert", "mydb", command);

        Assert.Null(result.FilterText);
    }

    [Fact]
    public void Classify_EmptyFilter_FilterTextNotNull()
    {
        var command = new BsonDocument { { "find", "users" }, { "filter", new BsonDocument() } };

        var result = MongoDbOperationClassifier.Classify("find", "mydb", command);

        Assert.NotNull(result.FilterText);
    }

    // ─── Diagram labels ─────────────────────────────────────

    [Fact]
    public void GetDiagramLabel_Raw_IncludesDatabaseAndCollectionAndFilter()
    {
        var info = new MongoDbOperationInfo(MongoDbOperation.Find, "mydb", "users", "{ age: { $gt: 25 } }");

        var label = MongoDbOperationClassifier.GetDiagramLabel(info, MongoDbTrackingVerbosity.Raw);

        Assert.Contains("Find", label);
        Assert.Contains("mydb", label);
        Assert.Contains("users", label);
        Assert.Contains("filter=", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_ShowsOperationAndCollection()
    {
        var info = new MongoDbOperationInfo(MongoDbOperation.Find, "mydb", "users");

        var label = MongoDbOperationClassifier.GetDiagramLabel(info, MongoDbTrackingVerbosity.Detailed);

        Assert.Equal("Find → users", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_NoCollection_ShowsOperationOnly()
    {
        var info = new MongoDbOperationInfo(MongoDbOperation.ListDatabases, "mydb", null);

        var label = MongoDbOperationClassifier.GetDiagramLabel(info, MongoDbTrackingVerbosity.Detailed);

        Assert.Equal("ListDatabases", label);
    }

    [Fact]
    public void GetDiagramLabel_Summarised_ShowsOperationOnly()
    {
        var info = new MongoDbOperationInfo(MongoDbOperation.Insert, "mydb", "orders");

        var label = MongoDbOperationClassifier.GetDiagramLabel(info, MongoDbTrackingVerbosity.Summarised);

        Assert.Equal("Insert", label);
    }

    [Fact]
    public void GetDiagramLabel_Raw_NoFilter_OmitsFilterPrefix()
    {
        var info = new MongoDbOperationInfo(MongoDbOperation.Insert, "mydb", "orders");

        var label = MongoDbOperationClassifier.GetDiagramLabel(info, MongoDbTrackingVerbosity.Raw);

        Assert.DoesNotContain("filter=", label);
    }
}
