namespace LightBDD.Contrib.ReportingEnhancements.Reports;

public class HtmlReportFormatterOptions
{
    public string CssContent { get; set; }
    public Tuple<string, byte[]> CustomLogo { get; set; }
    public Tuple<string, byte[]> CustomFavicon { get; set; }
}