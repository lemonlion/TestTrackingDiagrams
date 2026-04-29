using TestTrackingDiagrams.Sql;

namespace TestTrackingDiagrams.Extensions.SqlClient;

public record SqlClientTrackingOptions : SqlTrackingOptionsBase
{
    public SqlClientTrackingOptions()
    {
        ServiceName = "SQL Server";
        DependencyCategory = "SqlServer";
        UriScheme = "sqlserver";
    }
}
