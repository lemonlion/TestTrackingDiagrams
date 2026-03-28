namespace LightBDD.Contrib.ReportingEnhancements.Reports
{
    public class Html
    {
        private readonly bool _format;

        public Html(bool format = false)
        {
            _format = format;
        }

        public TagBuilder Checkbox()
        {
            return Tag(Html5Tag.Input).Attribute(Html5Attribute.Type, "checkbox");
        }

        public TagBuilder Radio()
        {
            return Tag(Html5Tag.Input).Attribute(Html5Attribute.Type, "radio");
        }

        public TagBuilder Tag(Html5Tag tag)
        {
            return new TagBuilder(tag, _format);
        }

        public static TextBuilder Text(string text)
        {
            return new TextBuilder(text);
        }

        public static IHtmlNode Br()
        {
            return Text("<br>");
        }

        public static IHtmlNode Nothing()
        {
            return new Html().Tag(Html5Tag.Div).SkipEmpty();
        }
    }
}