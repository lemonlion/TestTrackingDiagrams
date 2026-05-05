using System.Reflection;
using System.Runtime.CompilerServices;
using LightBDD.Core.Configuration;
using LightBDD.Framework.Configuration;

namespace TestTrackingDiagrams.LightBDD.xUnit3;

/// <summary>
/// Provides xUnit v3-specific extension methods for configuring LightBDD report writers with test tracking diagram generation.
/// </summary>
public static class ReportWritersConfigurationExtensions
{
    /// <summary>
    /// Configures LightBDD to generate test tracking diagrams and reports.
    /// Call on <see cref="ReportWritersConfiguration"/> (legacy, does not register automatic argument capture).
    /// Prefer the <see cref="LightBddConfiguration"/> overload for automatic raw argument capture.
    /// </summary>
    [Obsolete("Use the LightBddConfiguration.CreateStandardReportsWithDiagrams() overload instead, which also registers automatic argument capture for rich rendering of complex parameters.")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ReportWritersConfiguration CreateStandardReportsWithDiagrams(
        this ReportWritersConfiguration configuration,
        ReportConfigurationOptions options)
    {
        return LightBDD.ReportWritersConfigurationExtensions.CreateStandardReportsWithDiagramsInternal(
            configuration, options, Assembly.GetCallingAssembly(),
            assembly => assembly.CountNumberOfTestsInAssembly());
    }

    /// <summary>
    /// Configures LightBDD to generate test tracking diagrams and reports with automatic raw argument capture.
    /// This overload registers an <see cref="LightBDD.Core.Extensibility.Execution.IScenarioDecorator"/> that captures
    /// raw test method arguments during scenario execution, enabling rich sub-table and expandable rendering
    /// for complex objects (records, lists, nested types) passed via framework-level attributes (MemberData,
    /// ClassData, etc.) — using the same processing pipeline as the non-LightBDD xUnit3 adapter.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static LightBddConfiguration CreateStandardReportsWithDiagrams(
        this LightBddConfiguration configuration,
        ReportConfigurationOptions options)
    {
        return LightBDD.ReportWritersConfigurationExtensions.CreateStandardReportsWithDiagramsInternal(
            configuration, options, Assembly.GetCallingAssembly(),
            assembly => assembly.CountNumberOfTestsInAssembly());
    }
}
