namespace LightBDD.Contrib.ReportingEnhancements.Reports
{
    public interface IHtmlNode
    {
        HtmlTextWriter Write(HtmlTextWriter writer, string indent);
        bool IsEmpty();
    }
}