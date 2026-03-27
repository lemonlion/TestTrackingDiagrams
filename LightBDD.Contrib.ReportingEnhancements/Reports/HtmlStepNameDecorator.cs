using System.Net;
using System.Text;
using LightBDD.Core.Formatting.NameDecorators;
using LightBDD.Core.Metadata;

namespace LightBDD.Contrib.ReportingEnhancements.Reports
{
    public class HtmlStepNameDecorator : IStepNameDecorator
    {
        private Html _html;

        public HtmlStepNameDecorator(Html? html = null)
        {
            _html = html ?? new Html();
        }

        public string DecorateStepTypeName(IStepTypeNameInfo stepTypeName)
        {
            return stepTypeName.Name; // Html.Tag(Html5Tag.Span).Class("stepType").Content(stepTypeName.Name));
        }

        public string DecorateParameterValue(INameParameterInfo parameter)
        {
            return AsString(
                _html.Tag(Html5Tag.Span)
                    .Class($"inline-param {GetParameterValueClass(parameter)}")
                    .Content(parameter.FormattedValue));
        }

        private static string GetParameterValueClass(INameParameterInfo parameter)
        {
            return parameter.IsEvaluated
                ? parameter.VerificationStatus.ToString().ToLowerInvariant()
                : "unknown";
        }

        public string DecorateNameFormat(string nameFormat)
        {
            return WebUtility.HtmlEncode(nameFormat);
        }

        private static string AsString(IHtmlNode node)
        {
            var sb = new StringBuilder();
            using (var writer = new HtmlTextWriter(new StringWriter(sb)))
                node.Write(writer, "");
            return sb.ToString();
        }
    }
}