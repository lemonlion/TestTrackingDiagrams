using TestTrackingDiagrams.Extensions.DynamoDB;

namespace TestTrackingDiagrams.Tests.DynamoDB;

public class DynamoDbOperationClassifierTests
{
    private static HttpRequestMessage MakeDynamoRequest(string operationName, string? body = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://dynamodb.us-east-1.amazonaws.com/");
        request.Headers.Add("X-Amz-Target", $"DynamoDB_20120810.{operationName}");
        if (body is not null)
            request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/x-amz-json-1.0");
        return request;
    }

    // ══════════════════════════════════════════════════════════
    //  Header-based classification (all 18 operations)
    // ══════════════════════════════════════════════════════════

    [Theory]
    [InlineData("PutItem", DynamoDbOperation.PutItem)]
    [InlineData("GetItem", DynamoDbOperation.GetItem)]
    [InlineData("UpdateItem", DynamoDbOperation.UpdateItem)]
    [InlineData("DeleteItem", DynamoDbOperation.DeleteItem)]
    [InlineData("Query", DynamoDbOperation.Query)]
    [InlineData("Scan", DynamoDbOperation.Scan)]
    [InlineData("BatchWriteItem", DynamoDbOperation.BatchWriteItem)]
    [InlineData("BatchGetItem", DynamoDbOperation.BatchGetItem)]
    [InlineData("TransactWriteItems", DynamoDbOperation.TransactWriteItems)]
    [InlineData("TransactGetItems", DynamoDbOperation.TransactGetItems)]
    [InlineData("CreateTable", DynamoDbOperation.CreateTable)]
    [InlineData("DeleteTable", DynamoDbOperation.DeleteTable)]
    [InlineData("DescribeTable", DynamoDbOperation.DescribeTable)]
    [InlineData("ListTables", DynamoDbOperation.ListTables)]
    [InlineData("UpdateTable", DynamoDbOperation.UpdateTable)]
    [InlineData("ExecuteStatement", DynamoDbOperation.ExecuteStatement)]
    [InlineData("BatchExecuteStatement", DynamoDbOperation.BatchExecuteStatement)]
    [InlineData("ExecuteTransaction", DynamoDbOperation.ExecuteTransaction)]
    public void Classify_AllOperations_ReturnsCorrectEnum(string operationName, DynamoDbOperation expected)
    {
        var request = MakeDynamoRequest(operationName);

        var result = DynamoDbOperationClassifier.Classify(request);

        Assert.Equal(expected, result.Operation);
    }

    // ─── Missing / malformed header ───────────────────────────

    [Fact]
    public void Classify_NoTargetHeader_ReturnsOther()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://dynamodb.us-east-1.amazonaws.com/");

        var result = DynamoDbOperationClassifier.Classify(request);

        Assert.Equal(DynamoDbOperation.Other, result.Operation);
    }

    [Fact]
    public void Classify_MalformedTargetHeader_ReturnsOther()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://dynamodb.us-east-1.amazonaws.com/");
        request.Headers.Add("X-Amz-Target", "garbage-value");

        var result = DynamoDbOperationClassifier.Classify(request);

        Assert.Equal(DynamoDbOperation.Other, result.Operation);
    }

    [Fact]
    public void Classify_UnknownOperation_ReturnsOther()
    {
        var request = MakeDynamoRequest("SomeNewOperation");

        var result = DynamoDbOperationClassifier.Classify(request);

        Assert.Equal(DynamoDbOperation.Other, result.Operation);
    }

    // ══════════════════════════════════════════════════════════
    //  Table name extraction
    // ══════════════════════════════════════════════════════════

    [Fact]
    public void Classify_PutItem_ExtractsTableName()
    {
        var body = """{"TableName": "Users", "Item": {"id": {"S": "123"}}}""";
        var request = MakeDynamoRequest("PutItem", body);

        var result = DynamoDbOperationClassifier.Classify(request, body);

        Assert.Equal("Users", result.TableName);
    }

    [Fact]
    public void Classify_Query_ExtractsTableName()
    {
        var body = """{"TableName": "Orders", "KeyConditionExpression": "pk = :pk"}""";
        var request = MakeDynamoRequest("Query", body);

        var result = DynamoDbOperationClassifier.Classify(request, body);

        Assert.Equal("Orders", result.TableName);
    }

    [Fact]
    public void Classify_NullBody_ReturnsNullTableName()
    {
        var request = MakeDynamoRequest("PutItem");

        var result = DynamoDbOperationClassifier.Classify(request);

        Assert.Null(result.TableName);
    }

    [Fact]
    public void Classify_EmptyBody_ReturnsNullTableName()
    {
        var request = MakeDynamoRequest("GetItem", "");

        var result = DynamoDbOperationClassifier.Classify(request, "");

        Assert.Null(result.TableName);
    }

    [Fact]
    public void Classify_TableNameWithSpecialChars_ExtractsCorrectly()
    {
        var body = """{"TableName": "my-table.v2_prod", "Key": {}}""";
        var request = MakeDynamoRequest("GetItem", body);

        var result = DynamoDbOperationClassifier.Classify(request, body);

        Assert.Equal("my-table.v2_prod", result.TableName);
    }

    // ─── Batch operations with multiple tables ────────────────

    [Fact]
    public void Classify_BatchWriteItem_ExtractsMultipleTableNames()
    {
        var body = """{"RequestItems": {"Users": [{"PutRequest": {}}], "Orders": [{"PutRequest": {}}]}}""";
        var request = MakeDynamoRequest("BatchWriteItem", body);

        var result = DynamoDbOperationClassifier.Classify(request, body);

        Assert.Contains("Users", result.TableName!);
        Assert.Contains("Orders", result.TableName!);
    }

    [Fact]
    public void Classify_BatchGetItem_ExtractsMultipleTableNames()
    {
        var body = """{"RequestItems": {"Products": {"Keys": []}, "Categories": {"Keys": []}}}""";
        var request = MakeDynamoRequest("BatchGetItem", body);

        var result = DynamoDbOperationClassifier.Classify(request, body);

        Assert.NotNull(result.TableName);
    }

    // ─── PartiQL statement extraction ─────────────────────────

    [Fact]
    public void Classify_ExecuteStatement_ExtractsStatement()
    {
        var body = """{"Statement": "SELECT * FROM Users WHERE id = ?", "Parameters": []}""";
        var request = MakeDynamoRequest("ExecuteStatement", body);

        var result = DynamoDbOperationClassifier.Classify(request, body);

        Assert.Equal(DynamoDbOperation.ExecuteStatement, result.Operation);
        Assert.Equal("SELECT * FROM Users WHERE id = ?", result.StatementText);
    }

    [Fact]
    public void Classify_ExecuteStatement_NullBody_NullStatement()
    {
        var request = MakeDynamoRequest("ExecuteStatement");

        var result = DynamoDbOperationClassifier.Classify(request);

        Assert.Null(result.StatementText);
    }

    [Fact]
    public void Classify_PutItem_NoStatementExtracted()
    {
        var body = """{"TableName": "Users", "Item": {}}""";
        var request = MakeDynamoRequest("PutItem", body);

        var result = DynamoDbOperationClassifier.Classify(request, body);

        Assert.Null(result.StatementText);
    }

    // ─── ListTables has no table name ─────────────────────────

    [Fact]
    public void Classify_ListTables_NullTableName()
    {
        var body = """{}""";
        var request = MakeDynamoRequest("ListTables", body);

        var result = DynamoDbOperationClassifier.Classify(request, body);

        Assert.Equal(DynamoDbOperation.ListTables, result.Operation);
        Assert.Null(result.TableName);
    }

    // ══════════════════════════════════════════════════════════
    //  GetDiagramLabel tests
    // ══════════════════════════════════════════════════════════

    [Theory]
    [InlineData(DynamoDbOperation.PutItem, "PutItem")]
    [InlineData(DynamoDbOperation.GetItem, "GetItem")]
    [InlineData(DynamoDbOperation.UpdateItem, "UpdateItem")]
    [InlineData(DynamoDbOperation.DeleteItem, "DeleteItem")]
    [InlineData(DynamoDbOperation.Query, "Query")]
    [InlineData(DynamoDbOperation.Scan, "Scan")]
    [InlineData(DynamoDbOperation.BatchWriteItem, "BatchWriteItem")]
    [InlineData(DynamoDbOperation.BatchGetItem, "BatchGetItem")]
    [InlineData(DynamoDbOperation.TransactWriteItems, "TransactWriteItems")]
    [InlineData(DynamoDbOperation.TransactGetItems, "TransactGetItems")]
    [InlineData(DynamoDbOperation.CreateTable, "CreateTable")]
    [InlineData(DynamoDbOperation.DeleteTable, "DeleteTable")]
    [InlineData(DynamoDbOperation.DescribeTable, "DescribeTable")]
    [InlineData(DynamoDbOperation.ListTables, "ListTables")]
    [InlineData(DynamoDbOperation.UpdateTable, "UpdateTable")]
    [InlineData(DynamoDbOperation.ExecuteStatement, "ExecuteStatement")]
    [InlineData(DynamoDbOperation.BatchExecuteStatement, "BatchExecuteStatement")]
    [InlineData(DynamoDbOperation.ExecuteTransaction, "ExecuteTransaction")]
    [InlineData(DynamoDbOperation.Other, "Other")]
    public void Summarised_ReturnsOperationName(DynamoDbOperation operation, string expected)
    {
        var info = new DynamoDbOperationInfo(operation, "Users");

        var label = DynamoDbOperationClassifier.GetDiagramLabel(info, DynamoDbTrackingVerbosity.Summarised);

        Assert.Equal(expected, label);
    }

    [Theory]
    [InlineData(DynamoDbOperation.PutItem, "PutItem")]
    [InlineData(DynamoDbOperation.GetItem, "GetItem")]
    [InlineData(DynamoDbOperation.Query, "Query")]
    [InlineData(DynamoDbOperation.Scan, "Scan")]
    [InlineData(DynamoDbOperation.Other, "Other")]
    public void Detailed_ReturnsOperationName(DynamoDbOperation operation, string expected)
    {
        var info = new DynamoDbOperationInfo(operation, "Users");

        var label = DynamoDbOperationClassifier.GetDiagramLabel(info, DynamoDbTrackingVerbosity.Detailed);

        Assert.Equal(expected, label);
    }

    [Fact]
    public void Raw_ReturnsNull()
    {
        var info = new DynamoDbOperationInfo(DynamoDbOperation.PutItem, "Users");

        var label = DynamoDbOperationClassifier.GetDiagramLabel(info, DynamoDbTrackingVerbosity.Raw);

        Assert.Null(label);
    }
}
