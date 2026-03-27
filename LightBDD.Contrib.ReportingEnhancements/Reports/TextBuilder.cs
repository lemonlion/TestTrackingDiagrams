namespace LightBDD.Contrib.ReportingEnhancements.Reports
{
    public class TextBuilder : IHtmlNode
    {
        private string _text;
        private bool _escape;
        private bool _skipEmpty;

        public TextBuilder(string text)
        {
            _text = text ?? string.Empty;
        }

        public TextBuilder Trim()
        {
            _text = _text.Trim();
            return this;
        }

        public TextBuilder Escape(bool escape = true)
        {
            _escape = escape;
            return this;
        }

        public IHtmlNode SkipEmpty(bool skipEmpty = true)
        {
            _skipEmpty = skipEmpty;
            return this;
        }

        public HtmlTextWriter Write(HtmlTextWriter writer, string indent)
        {
            if (IsEmpty())
                return writer;

            if (_escape)
                writer.WriteEncodedText(_text);
            else
                writer.Write(_text);
            return writer;
        }

        public bool IsEmpty() => _skipEmpty && string.IsNullOrEmpty(_text);
    }
}