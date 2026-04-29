namespace TestTrackingDiagrams.Extensions.AtlasDataApi;

/// <summary>
/// Classified AtlasDataApi operation types.
/// </summary>
public enum AtlasDataApiOperation
{
    FindOne,
    Find,
    InsertOne,
    InsertMany,
    UpdateOne,
    UpdateMany,
    DeleteOne,
    DeleteMany,
    ReplaceOne,
    Aggregate,
    Other
}
