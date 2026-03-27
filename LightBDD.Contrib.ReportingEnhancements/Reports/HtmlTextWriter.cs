using System.Net;

namespace LightBDD.Contrib.ReportingEnhancements.Reports
{
    public class HtmlTextWriter(TextWriter writer) : IDisposable
    {
        private static readonly string[] VoidElements = ["area", "base", "br", "col", "hr", "img", "input", "link", "meta", "param", "command", "keygen", "source", "img"];
        private static readonly string[] NoClosingSlashVoidElements = ["br", "img"];

        public void Dispose()
        {
            writer.Dispose();
        }

        public void OpenTag(string tag, IEnumerable<KeyValuePair<string, string>> attributes)
        {
            WriteTag(tag, attributes, false);
        }

        public void WriteTag(string tag, IEnumerable<KeyValuePair<string, string>> attributes)
        {
            WriteTag(tag, attributes, true);
        }

        private void WriteTag(string tag, IEnumerable<KeyValuePair<string, string>> attributes, bool closed)
        {
            Write("<");
            Write(tag);
            foreach (var attribute in attributes)
                WriteAttribute(attribute.Key, attribute.Value);

            if (closed)
            {
                if (NoClosingSlashVoidElements.Contains(tag))
                    Write(">");
                else if (VoidElements.Contains(tag))
                    Write("/>");
                else
                {
                    Write(">");
                    CloseTag(tag);
                }
            }
            else
                Write(">");
        }

        public void CloseTag(string tag)
        {
            Write("</"); Write(tag); Write(">");
        }

        private void WriteAttribute(string attribute, string value)
        {
            Write(" ");
            Write(attribute);
            if (value is null)
                return;
            Write("=\"");
            WriteEncodedText(value);
            Write("\"");
        }

        public void Write(string text)
        {
            writer.Write(text);
        }

        public void WriteEncodedText(string text)
        {
            writer.Write(WebUtility.HtmlEncode(text));
        }

        internal HtmlTextWriter WriteTag(IHtmlNode node)
        {
            node.Write(this, "");
            return this;
        }
    }
}