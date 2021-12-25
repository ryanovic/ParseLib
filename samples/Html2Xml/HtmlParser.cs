namespace Html2Xml
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Xml;
    using ParseLib.Runtime;

    // Custom parser base defines the logic necessary to reduce source to desired output format.
    public abstract class HtmlParser : TextParser
    {
        private readonly Stack<XmlElement> elements;
        public XmlDocument Document { get; }

        public HtmlParser(TextReader reader) : base(reader)
        {
            elements = new Stack<XmlElement>();
            Document = new XmlDocument();
        }

        // Maps current lexeme to some value and put it on the stack.
        // Every token handler must be parameterless and relies on the parser state to proceed.
        // Lexemes for tokens whith no reducers will be skipped.
        [CompleteToken("comment")]
        protected XmlNode CreateComment() => Document.CreateComment(GetLexeme(trimLeft: 4, trimRight: 3));

        [CompleteToken("text")]
        protected XmlNode CreateText() => Document.CreateTextNode(WebUtility.HtmlDecode(GetLexeme()));

        [CompleteToken("%script%")]
        protected XmlNode CreateScript() => Document.CreateCDataSection(GetLexeme());

        // Multiple definitions.
        [CompleteToken("<script")]
        [CompleteToken("<tag")]
        protected XmlElement CreateElement() => Document.CreateElement(GetLexeme(trimLeft: 1, trimRight: 0));

        [CompleteToken("</tag")]
        protected string CreateCloseElement() => GetLexeme(trimLeft: 2, trimRight: 0);

        [CompleteToken("attr-name")]
        protected string CreateAttributeName() => GetLexeme();

        [CompleteToken("attr-value-raw")]
        protected string CreateAttributeRawValue() => GetLexeme();

        [CompleteToken("attr-value-str")]
        protected string CreateAttributeStringValue() => GetLexeme(trim: 1);

        // Production reducer for the attribute.
        // Reads XmlElement and attribute name from top of the stack.
        // Puts element back after.
        [Reduce("attr:single")]
        protected XmlElement AppendAttribute(XmlElement element, string name)
        {
            element.SetAttribute(name, name);
            return element;
        }

        // Expects both name and value on the stack.
        // Note that no compile time validation on parameter type and count is performed. 
        // If not defined correctly - runtime exeption will be thrown.
        [Reduce("attr:value-raw")]
        [Reduce("attr:value-str")]
        protected XmlElement AppendAttribute(XmlElement element, string name, string value)
        {
            element.SetAttribute(name, value);
            return element;
        }

        // void return means both element and script nodes are popped out from the stack after the call.
        [Reduce("node:script-inline")]
        protected void AppendScript(XmlElement element, XmlNode script)
        {
            element.AppendChild(script);
            AppendElement(element);
        }

        [Reduce("node:script-single")]
        [Reduce("node:tag-single")]
        protected void AppendElement(XmlElement element)
        {
            GetElement().AppendChild(element);
        }

        // Any grammar production is reduced when correct lookeahead is encountered.
        // Meaning the following token sequence '<tag' '>' 'text' '</tag'  is processed in the following order:
        // 1. CompleteToken '<tag' then '>', state now: 'tag' -> '<tag' 'attrs'(empty) '>' . <- here
        // 2. CompleteToken 'text'
        // 3. Reduce node:tag-open
        // 4. Put 'text' value on the stack
        // 4. CompleteToken '</tag'
        // 5. Reduce node:text ...
        // So it would be incorrect to append node in step #2 for example ('text' complete handler).
        // Of course it's not the issue if you would append element in #1 step as well, so it's up to design of a reducer. 
        [Reduce("node:comment")]
        [Reduce("node:text")]
        protected void AppendNode(XmlNode node)
        {
            GetElement().AppendChild(node);
        }

        [Reduce("node:tag-open")]
        protected void BeginElement(XmlElement element)
        {
            if (Document.DocumentElement == null)
            {
                Document.AppendChild(element);
            }
            else
            {
                AppendElement(element);
            }

            if (!element.Name.Equals("br", StringComparison.OrdinalIgnoreCase))
            {
                elements.Push(element);
            }
        }

        // Note: name is taken from the stack.
        // Lexer position is updated after token is completed, so original lexeme is not available for a production reducer.
        [Reduce("node:tag-close")]
        protected void CompleteElement(string name)
        {
            while (elements.Count > 0 && !elements.Peek().Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                elements.Pop();
            }

            if (elements.Count == 0)
            {
                throw CreateParserException("unexpected close tag encountered.");
            }

            elements.Pop();
        }

        private XmlElement GetElement()
        {
            if (elements.Count == 0)
            {
                throw new InvalidOperationException("Root element is missing.");
            }

            return elements.Peek();
        }
    }
}
