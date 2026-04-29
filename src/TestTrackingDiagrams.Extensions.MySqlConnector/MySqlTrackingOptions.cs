using TestTrackingDiagrams.Sql;

namespace TestTrackingDiagrams.Extensions.MySqlConnector;

public record MySqlTrackingOptions : SqlTrackingOptionsBase
{
    public MySqlTrackingOptions()
    {
        ServiceName = "MySQL";
        DependencyCategory = "MySQL";
        UriScheme = "mysql";
    }
}
