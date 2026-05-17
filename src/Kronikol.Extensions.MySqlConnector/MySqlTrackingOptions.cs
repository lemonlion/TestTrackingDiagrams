using Kronikol.Constants;
using Kronikol.Sql;

namespace Kronikol.Extensions.MySqlConnector;

/// <summary>
/// Configuration options for the MySqlConnector tracking extension.
/// </summary>
public record MySqlTrackingOptions : SqlTrackingOptionsBase
{
    public MySqlTrackingOptions()
    {
        ServiceName = "MySQL";
        DependencyCategory = DependencyCategories.MySQL;
        UriScheme = "mysql";
    }
}
