using LightBDD.Core.Results;
using LightBDD.Framework.Reporting.Formatters;

namespace LightBDD.Contrib.ReportingEnhancements.Reports;

/// <summary>
/// Formats feature results as HTML.
/// </summary>
public class CustomisableHtmlReportFormatter : IReportFormatter
{
    private readonly HtmlReportFormatterOptions _options = new();

    public HtmlReportAdvancedOptions? Options { get; set; }

    /// <summary>
    /// Formats provided feature results and writes to the <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">Stream to write formatted results to.</param>
    /// <param name="features">Feature results to format.</param>
    public void Format(Stream stream, params IFeatureResult[] features)
    {
        Options ??= new HtmlReportAdvancedOptions();
        var scenariosRun = features.SelectMany(x => x.GetScenarios()).ToList();

        if (Options.OnlyCreateReportOnFullySuccessfulTestRun)
        {
            if (scenariosRun.Any(x => x.Status == ExecutionStatus.Failed))
                return; 
        }

        if (Options.OnlyCreateReportOnFullTestRun)
        {
            var numberOfTestsInRun = scenariosRun.Count;
            var totalNumberOfTests = Options.TestAssembly.CountNumberOfTestsInAssembly();
            if (numberOfTestsInRun != totalNumberOfTests)
                return;
        }

        WithCustomCss(Stylesheets.GetHideStyleSheet());
        WithCustomCss(Stylesheets.GetPlantUmlStyleSheet());
        WithCustomCss(Stylesheets.GetFilterFreeTextStyleSheet());
        
        using var writer = new CustomisableHtmlResultTextWriter(stream, features)
        {
            Title = Options.Title,
            DiagramAsCode = (Options.ExampleDiagramsAsCode?.Invoke() ?? Enumerable.Empty<DiagramAsCode>()).ToArray(),
            DiagramsAsCodeCodeBehindTitle = Options.DiagramsAsCodeCodeBehindTitle,
            ShowHappyPathToggle = Options.ShowHappyPathToggle,
            ShowExampleDiagramsToggle = Options.ShowExampleDiagramsToggle,
            IncludeIgnoredTests = Options.IncludeIgnoredTests,
            IncludeDurations = Options.IncludeDurations,
            IncludeExecutionSummary = Options.IncludeExecutionSummary,
            IncludeFeatureSummary = Options.IncludeFeatureSummary,
            ShowStatusFilterToggles = Options.ShowStatusFilterToggles,
            WriteRuntimeIds = Options.WriteRuntimeIds,
            StepsHiddenInitially = Options.StepsHiddenInitially,
            FormatResult = Options.FormatResult,
            TreatScenariosAsPassed = Options.TreatScenariosAsPassed,
            LazyLoadDiagramImages = Options.LazyLoadDiagramImages
};
        writer.Write(_options);
    }

    /// <summary>
    /// Embeds <paramref name="cssContent"/> in the report HTML file, allowing to override default CSS styles.
    /// </summary>
    /// <param name="cssContent">CSS styles</param>
    public CustomisableHtmlReportFormatter WithCustomCss(string cssContent)
    {
        _options.CssContent += Environment.NewLine + cssContent;
        return this;
    }

    /// <summary>
    /// Embeds <paramref name="imageBody"/> image in <paramref name="mimeType"/> format into the report HTML file and uses it to override default LightBDD logo.
    /// </summary>
    /// <param name="mimeType">Image MIME type</param>
    /// <param name="imageBody">Image body</param>
    /// <returns></returns>
    public CustomisableHtmlReportFormatter WithCustomLogo(string mimeType, byte[] imageBody)
    {
        if (string.IsNullOrWhiteSpace(mimeType))
            throw new ArgumentException("MIME type needs to be specified", nameof(mimeType));
        if (imageBody == null)
            throw new ArgumentNullException(nameof(imageBody));

        _options.CustomLogo = Tuple.Create(mimeType, imageBody);
        return this;
    }

    /// <summary>
    /// Embeds <paramref name="imageBody"/> image in <paramref name="mimeType"/> format into the report HTML file and uses it to override default LightBDD favicon.
    /// </summary>
    /// <param name="mimeType">Favicon MIME type</param>
    /// <param name="imageBody">Favicon body</param>
    /// <returns></returns>
    public CustomisableHtmlReportFormatter WithCustomFavicon(string mimeType, byte[] imageBody)
    {
        if (string.IsNullOrWhiteSpace(mimeType))
            throw new ArgumentException("MIME type needs to be specified", nameof(mimeType));
        if (imageBody == null)
            throw new ArgumentNullException(nameof(imageBody));

        _options.CustomFavicon = Tuple.Create(mimeType, imageBody);
        return this;
    }
}