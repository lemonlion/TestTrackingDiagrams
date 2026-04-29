using TestTrackingDiagrams.Sql;

namespace TestTrackingDiagrams.Extensions.Oracle;

public record OracleTrackingOptions : SqlTrackingOptionsBase
{
    public OracleTrackingOptions()
    {
        ServiceName = "Oracle";
        DependencyCategory = "Oracle";
        UriScheme = "oracle";
    }
}
