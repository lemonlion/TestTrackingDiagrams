using global::MongoDB.Bson;

namespace TestTrackingDiagrams.Extensions.MongoDB;

public static class MongoDbOperationClassifier
{
    public static MongoDbOperationInfo Classify(
        string commandName, string? databaseName, BsonDocument? command)
    {
        var operation = commandName.ToLowerInvariant() switch
        {
            "find" => MongoDbOperation.Find,
            "insert" => MongoDbOperation.Insert,
            "update" => MongoDbOperation.Update,
            "delete" => MongoDbOperation.Delete,
            "aggregate" => MongoDbOperation.Aggregate,
            "count" or "countdocuments" => MongoDbOperation.Count,
            "findandmodify" => MongoDbOperation.FindAndModify,
            "distinct" => MongoDbOperation.Distinct,
            "bulkwrite" => MongoDbOperation.BulkWrite,
            "createindexes" => MongoDbOperation.CreateIndex,
            "dropindexes" => MongoDbOperation.DropIndex,
            "create" => MongoDbOperation.CreateCollection,
            "drop" => MongoDbOperation.DropCollection,
            "listcollections" => MongoDbOperation.ListCollections,
            "listdatabases" => MongoDbOperation.ListDatabases,
            "getmore" => MongoDbOperation.GetMore,
            _ => MongoDbOperation.Other
        };

        var collectionName = ExtractCollectionName(commandName, command);
        var filterText = ExtractFilter(command);

        return new MongoDbOperationInfo(operation, databaseName, collectionName, filterText);
    }

    public static string GetDiagramLabel(MongoDbOperationInfo op, MongoDbTrackingVerbosity verbosity)
    {
        return verbosity switch
        {
            MongoDbTrackingVerbosity.Raw =>
                $"{op.Operation} {op.DatabaseName}.{op.CollectionName}" +
                (op.FilterText != null ? $" filter={op.FilterText}" : ""),
            MongoDbTrackingVerbosity.Detailed =>
                op.CollectionName != null
                    ? $"{op.Operation} → {op.CollectionName}"
                    : op.Operation.ToString(),
            MongoDbTrackingVerbosity.Summarised =>
                op.Operation.ToString(),
            _ => op.Operation.ToString()
        };
    }

    private static string? ExtractCollectionName(string commandName, BsonDocument? command)
    {
        if (command is null) return null;

        // The collection name is the value of the command element itself
        // e.g., { "find": "users", "filter": {...} }
        var key = commandName.ToLowerInvariant();
        foreach (var element in command)
        {
            if (element.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase) &&
                element.Value.IsString)
            {
                return element.Value.AsString;
            }
        }

        return null;
    }

    private static string? ExtractFilter(BsonDocument? command)
    {
        if (command is null) return null;

        return command.TryGetValue("filter", out var filter)
            ? filter.ToString()!
            : null;
    }
}
