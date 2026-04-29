using Microsoft.AspNetCore.Http;
using TestTrackingDiagrams.Sql;

namespace TestTrackingDiagrams.Extensions.Npgsql;

public record NpgsqlTrackingOptions : SqlTrackingOptionsBase
{
    public NpgsqlTrackingOptions()
    {
        ServiceName = "PostgreSQL";
        DependencyCategory = "PostgreSQL";
        UriScheme = "postgresql";
    }
}
