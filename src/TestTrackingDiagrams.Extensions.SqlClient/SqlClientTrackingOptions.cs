using TestTrackingDiagrams.Constants;
using TestTrackingDiagrams.Sql;

namespace TestTrackingDiagrams.Extensions.SqlClient;

/// <summary>
/// Configuration options for the SqlClient tracking extension.
/// </summary>
public record SqlClientTrackingOptions : SqlTrackingOptionsBase
{
    public SqlClientTrackingOptions()
    {
        ServiceName = "SQL Server";
        DependencyCategory = DependencyCategories.SqlServer;
        UriScheme = "sqlserver";
    }
}
