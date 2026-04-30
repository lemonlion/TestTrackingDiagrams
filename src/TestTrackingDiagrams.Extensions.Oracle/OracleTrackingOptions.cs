using TestTrackingDiagrams.Constants;
using TestTrackingDiagrams.Sql;

namespace TestTrackingDiagrams.Extensions.Oracle;

/// <summary>
/// Configuration options for the Oracle tracking extension.
/// </summary>
public record OracleTrackingOptions : SqlTrackingOptionsBase
{
    public OracleTrackingOptions()
    {
        ServiceName = "Oracle";
        DependencyCategory = DependencyCategories.Oracle;
        UriScheme = "oracle";
    }
}
