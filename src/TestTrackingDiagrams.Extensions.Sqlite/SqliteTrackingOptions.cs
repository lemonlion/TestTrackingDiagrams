using TestTrackingDiagrams.Sql;

namespace TestTrackingDiagrams.Extensions.Sqlite;

public record SqliteTrackingOptions : SqlTrackingOptionsBase
{
    public SqliteTrackingOptions()
    {
        ServiceName = "SQLite";
        DependencyCategory = "SQLite";
        UriScheme = "sqlite";
    }
}
