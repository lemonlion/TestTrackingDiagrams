using System.Reflection;
using System.Runtime.CompilerServices;
using LightBDD.Core.Configuration;
using LightBDD.Framework.Configuration;

namespace TestTrackingDiagrams.LightBDD.xUnit2;

/// <summary>
/// Provides xUnit v2-specific extension methods for configuring LightBDD report writers with test tracking diagram generation.
/// </summary>
public static class ReportWritersConfigurationExtensions
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ReportWritersConfiguration CreateStandardReportsWithDiagrams(
        this ReportWritersConfiguration configuration,
        ReportConfigurationOptions options)
    {
        return LightBDD.ReportWritersConfigurationExtensions.CreateStandardReportsWithDiagramsInternal(
            configuration, options, Assembly.GetCallingAssembly(),
            assembly => assembly.CountNumberOfTestsInAssembly());
    }
}