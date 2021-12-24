using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;
using ParseLib.Runtime;

namespace Html2Xml
{
    public abstract class HtmlParser : TextParser
    {
        private readonly Stack<XmlElement> elements;
        public XmlDocument Document { get; }

        public HtmlParser(TextReader reader) : base(reader)
        {
            elements = new Stack<XmlElement>();
            Document = new XmlDocument();
        }

        [CompleteToken("comment")]
        protected XmlNode CreateComment() => Document.CreateComment(GetLexeme(trimLeft: 4, trimRight: 3));

        [CompleteToken("text")]
        protected XmlNode CreateText() => Document.CreateTextNode(WebUtility.HtmlDecode(GetLexeme()));

        [CompleteToken("%script%")]
        protected XmlNode CreateScript() => Document.CreateCDataSection(GetLexeme());

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

        [Reduce("attr:single")]
        protected XmlElement AppendAttribute(XmlElement element, string name)
        {
            element.SetAttribute(name, name);
            return element;
        }

        [Reduce("attr:value-raw")]
        [Reduce("attr:value-str")]
        protected XmlElement AppendAttribute(XmlElement element, string name, string value)
        {
            element.SetAttribute(name, value);
            return element;
        }

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
