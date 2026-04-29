using Microsoft.AspNetCore.Http;
using TestTrackingDiagrams.Sql;

namespace TestTrackingDiagrams.Extensions.Npgsql;

/// <summary>
/// Configuration options for the Npgsql tracking extension.
/// </summary>
public record NpgsqlTrackingOptions : SqlTrackingOptionsBase
{
    public NpgsqlTrackingOptions()
    {
        ServiceName = "PostgreSQL";
        DependencyCategory = "PostgreSQL";
        UriScheme = "postgresql";
    }
}
