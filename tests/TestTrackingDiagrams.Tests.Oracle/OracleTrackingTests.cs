using TestTrackingDiagrams.Extensions.Oracle;
using TestTrackingDiagrams.Sql;
using TestTrackingDiagrams.Tracking;
using Xunit;

namespace TestTrackingDiagrams.Tests.Oracle;

public class OracleTrackingOptionsTests
{
    [Fact]
    public void Default_options_use_oracle_values()
    {
        var options = new OracleTrackingOptions();
        Assert.Equal("Oracle", options.ServiceName);
        Assert.Equal("Oracle", options.DependencyCategory);
        Assert.Equal("oracle", options.UriScheme);
    }

    [Fact]
    public void Options_inherit_from_SqlTrackingOptionsBase()
    {
        var options = new OracleTrackingOptions();
        Assert.IsAssignableFrom<SqlTrackingOptionsBase>(options);
    }

    [Fact]
    public void Default_verbosity_is_Detailed()
    {
        var options = new OracleTrackingOptions();
        Assert.Equal(SqlTrackingVerbosityLevel.Detailed, options.Verbosity);
    }

    [Fact]
    public void ExcludedOperations_defaults_to_empty()
    {
        var options = new OracleTrackingOptions();
        Assert.Empty(options.ExcludedOperations);
    }

    [Fact]
    public void ExcludedOperations_can_be_configured()
    {
        var options = new OracleTrackingOptions
        {
            ExcludedOperations = [UnifiedSqlOperation.Select]
        };
        Assert.Contains(UnifiedSqlOperation.Select, options.ExcludedOperations);
    }
}

public class DependencyPaletteOracleTests
{
    [Fact]
    public void Oracle_category_resolves_to_Database()
    {
        var type = DependencyPalette.Resolve("Oracle");
        Assert.Equal(DependencyType.Database, type);
    }

    [Fact]
    public void PostgreSQL_category_resolves_to_Database()
    {
        var type = DependencyPalette.Resolve("PostgreSQL");
        Assert.Equal(DependencyType.Database, type);
    }

    [Fact]
    public void SqlServer_category_resolves_to_Database()
    {
        var type = DependencyPalette.Resolve("SqlServer");
        Assert.Equal(DependencyType.Database, type);
    }

    [Fact]
    public void MySQL_category_resolves_to_Database()
    {
        var type = DependencyPalette.Resolve("MySQL");
        Assert.Equal(DependencyType.Database, type);
    }

    [Fact]
    public void SQLite_category_resolves_to_Database()
    {
        var type = DependencyPalette.Resolve("SQLite");
        Assert.Equal(DependencyType.Database, type);
    }
}
