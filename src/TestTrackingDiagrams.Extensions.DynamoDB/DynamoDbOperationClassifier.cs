using System.Text.Json;
using System.Text.RegularExpressions;

namespace TestTrackingDiagrams.Extensions.DynamoDB;

/// <summary>
/// Classifies Amazon DynamoDB HTTP requests into specific operations based on URL patterns and HTTP methods.
/// </summary>
public static partial class DynamoDbOperationClassifier
{
    [GeneratedRegex(
        @"DynamoDB_\d+\.(?<operation>\w+)",
        RegexOptions.Compiled)]
    private static partial Regex TargetHeaderRegex();

    [GeneratedRegex(
        @"""TableName""\s*:\s*""(?<table>[^""]+)""",
        RegexOptions.Compiled)]
    private static partial Regex TableNameRegex();

    [GeneratedRegex(
        @"""Statement""\s*:\s*""(?<stmt>[^""]+)""",
        RegexOptions.Compiled)]
    private static partial Regex StatementRegex();

    public static DynamoDbOperationInfo Classify(HttpRequestMessage request, string? requestBody = null)
    {
        var targetHeader = request.Headers.TryGetValues("X-Amz-Target", out var values)
            ? values.FirstOrDefault() : null;

        if (targetHeader is null)
            return new DynamoDbOperationInfo(DynamoDbOperation.Other, null);

        var match = TargetHeaderRegex().Match(targetHeader);
        if (!match.Success)
            return new DynamoDbOperationInfo(DynamoDbOperation.Other, null);

        var operationName = match.Groups["operation"].Value;
        var operation = MapOperation(operationName);

        string? tableName;
        if (operation is DynamoDbOperation.BatchWriteItem or DynamoDbOperation.BatchGetItem)
        {
            tableName = ExtractBatchTableNames(requestBody);
        }
        else
        {
            tableName = ExtractTableName(requestBody);
        }

        var statement = operation is DynamoDbOperation.ExecuteStatement or
            DynamoDbOperation.BatchExecuteStatement or DynamoDbOperation.ExecuteTransaction
            ? ExtractStatement(requestBody) : null;

        return new DynamoDbOperationInfo(operation, tableName, statement);
    }

    public static string? GetDiagramLabel(DynamoDbOperationInfo op, DynamoDbTrackingVerbosity verbosity)
    {
        return verbosity switch
        {
            DynamoDbTrackingVerbosity.Summarised or DynamoDbTrackingVerbosity.Detailed
                => op.Operation.ToString(),
            _ => null // Raw: caller uses HTTP method + full target header
        };
    }

    private static DynamoDbOperation MapOperation(string operationName) => operationName switch
    {
        "PutItem" => DynamoDbOperation.PutItem,
        "GetItem" => DynamoDbOperation.GetItem,
        "UpdateItem" => DynamoDbOperation.UpdateItem,
        "DeleteItem" => DynamoDbOperation.DeleteItem,
        "Query" => DynamoDbOperation.Query,
        "Scan" => DynamoDbOperation.Scan,
        "BatchWriteItem" => DynamoDbOperation.BatchWriteItem,
        "BatchGetItem" => DynamoDbOperation.BatchGetItem,
        "TransactWriteItems" => DynamoDbOperation.TransactWriteItems,
        "TransactGetItems" => DynamoDbOperation.TransactGetItems,
        "CreateTable" => DynamoDbOperation.CreateTable,
        "DeleteTable" => DynamoDbOperation.DeleteTable,
        "DescribeTable" => DynamoDbOperation.DescribeTable,
        "ListTables" => DynamoDbOperation.ListTables,
        "UpdateTable" => DynamoDbOperation.UpdateTable,
        "ExecuteStatement" => DynamoDbOperation.ExecuteStatement,
        "BatchExecuteStatement" => DynamoDbOperation.BatchExecuteStatement,
        "ExecuteTransaction" => DynamoDbOperation.ExecuteTransaction,
        _ => DynamoDbOperation.Other
    };

    private static string? ExtractTableName(string? body)
    {
        if (string.IsNullOrEmpty(body)) return null;
        var match = TableNameRegex().Match(body);
        return match.Success ? match.Groups["table"].Value : null;
    }

    private static string? ExtractBatchTableNames(string? body)
    {
        if (string.IsNullOrEmpty(body)) return null;

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("RequestItems", out var requestItems) &&
                requestItems.ValueKind == JsonValueKind.Object)
            {
                var tables = requestItems.EnumerateObject().Select(p => p.Name).ToArray();
                if (tables.Length > 0)
                    return string.Join(", ", tables);
            }
        }
        catch (JsonException)
        {
            // Fall through to regex fallback
        }

        return ExtractTableName(body); // fallback
    }

    private static string? ExtractStatement(string? body)
    {
        if (string.IsNullOrEmpty(body)) return null;
        var match = StatementRegex().Match(body);
        return match.Success ? match.Groups["stmt"].Value : null;
    }
}