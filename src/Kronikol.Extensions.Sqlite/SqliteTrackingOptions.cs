using Kronikol.Constants;
using Kronikol.Sql;

namespace Kronikol.Extensions.Sqlite;

/// <summary>
/// Configuration options for the Sqlite tracking extension.
/// </summary>
public record SqliteTrackingOptions : SqlTrackingOptionsBase
{
    public SqliteTrackingOptions()
    {
        ServiceName = "SQLite";
        DependencyCategory = DependencyCategories.SQLite;
        UriScheme = "sqlite";
    }
}
