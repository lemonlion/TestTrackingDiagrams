using Kronikol.Constants;
using Microsoft.AspNetCore.Http;
using Kronikol.Sql;

namespace Kronikol.Extensions.Npgsql;

/// <summary>
/// Configuration options for the Npgsql tracking extension.
/// </summary>
public record NpgsqlTrackingOptions : SqlTrackingOptionsBase
{
    public NpgsqlTrackingOptions()
    {
        ServiceName = "PostgreSQL";
        DependencyCategory = DependencyCategories.PostgreSQL;
        UriScheme = "postgresql";
    }
}
