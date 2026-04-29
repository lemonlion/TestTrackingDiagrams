using System.Reflection;
using System.Runtime.CompilerServices;
using LightBDD.Core.Configuration;
using LightBDD.Framework.Configuration;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.LightBDD
{
    /// <summary>
    /// Provides extension methods for configuring LightBDD report writers to generate test tracking diagrams and reports.
    /// </summary>
    public static class ReportWritersConfigurationExtensions
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ReportWritersConfiguration CreateStandardReportsWithDiagrams(
            this ReportWritersConfiguration configuration,
            ReportConfigurationOptions options,
            Func<Assembly, int> testCountResolver)
        {
            return CreateStandardReportsWithDiagramsInternal(configuration, options, Assembly.GetCallingAssembly(), testCountResolver);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ReportWritersConfiguration CreateStandardReportsWithDiagramsInternal(
            ReportWritersConfiguration configuration, ReportConfigurationOptions options, Assembly testAssembly, Func<Assembly, int> testCountResolver)
        {
            // Disable LightBDD's built-in report writers and register a single formatter
            // that delegates to the standard ReportGenerator pipeline — the same pipeline
            // used by every other framework adapter (xUnit, NUnit, MSTest, TUnit, BDDfy, ReqNRoll).
            var reportsFilePath = options.ReportsFolderPath.Trim().TrimEnd('/');

            return configuration
                .Clear()
                .AddFileWriter<StandardPipelineFormatter>($"{reportsFilePath}/.generation-complete",
                formatter =>
                {
                    options.ExpectedTestCount = () => testCountResolver(testAssembly);
                    formatter.Options = options;
                });
        }
    }
}