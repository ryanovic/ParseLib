namespace Html2Xml
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Xml;
    using Ry.ParseLib.Runtime;

    // A custom parser defines the logic necessary to reduce source to desired output format.
    public abstract class HtmlParser : TextParser
    {
        private readonly Stack<XmlElement> elements;

        public XmlDocument Document { get; }

        public HtmlParser(TextReader reader) : base(reader)
        {
            elements = new Stack<XmlElement>();
            Document = new XmlDocument();
        }

        // Creates a comment node and put it on the stack.
        [CompleteToken("comment")]
        protected XmlNode CreateComment() => Document.CreateComment(GetValue(4, Length - 7)); // Remove wrapping <!---->.

        [CompleteToken("text")]
        protected XmlNode CreateText() => Document.CreateTextNode(WebUtility.HtmlDecode(GetValue()));

        [CompleteToken("%script%")]
        protected XmlNode CreateScript() => Document.CreateCDataSection(GetValue());

        // Creates a new element and put it on the stack.
        [CompleteToken("<script")]
        [CompleteToken("<tag")]
        protected XmlElement CreateElement() => Document.CreateElement(GetValue(start: 1));

        [CompleteToken("</tag")]
        protected string CreateEndElementName() => GetValue(start: 2);

        [CompleteToken("attr-name")]
        protected string CreateAttributeName() => GetValue();

        [CompleteToken("attr-value-raw")]
        protected string CreateAttributeRawValue() => GetValue();

        [CompleteToken("attr-value-str")]
        protected string CreateAttributeStringValue() => GetValue(start: 1, count: Length - 2);

        // Gets a pending element and appends an attribute with a specified name.
        // Returns element back to the stack.
        [Reduce("attr:single")]
        protected XmlElement AppendAttribute(XmlElement current, string name)
        {
            current.SetAttribute(name, name);
            return current;
        }

        // Gets a pending element and appends an attribute with a specified name and value.
        // Returns element back to the stack.
        [Reduce("attr:value-raw")]
        [Reduce("attr:value-str")]
        protected XmlElement AppendAttribute(XmlElement current, string name, string value)
        {
            current.SetAttribute(name, value);
            return current;
        }

        // Appends a newly recognized open tag to the document and makes it a new root.
        [Reduce("node:tag-open")]
        protected void BeginElement(XmlElement current)
        {
            AppendElement(current);

            if (!current.Name.Equals("br", StringComparison.OrdinalIgnoreCase))
            {
                // Begin element.
                elements.Push(current);
            }
        }

        // Apends an element to the document if that's the root, or to the most recent open element.
        [Reduce("node:tag-single")]
        [Reduce("node:script-single")]
        protected void AppendElement(XmlElement current)
        {
            if (Document.DocumentElement == null)
            {
                Document.AppendChild(current);
            }
            else
            {
                AppendNode(current);
            }
        }

        // Handles a script tag.
        [Reduce("node:script-inline")]
        protected void AppenScript(XmlElement current, XmlNode script)
        {
            current.AppendChild(script);
            AppendElement(current);
        }

        // Adds a new node to the current root.
        [Reduce("node:comment")]
        [Reduce("node:text")]
        protected void AppendNode(XmlNode node)
        {
            GetCurrentRoot().AppendChild(node);
        }

        // Completes the current root.
        [Reduce("node:tag-close")]
        protected void CompleteElement(string name)
        {
            while (elements.Count > 0 && !elements.Peek().Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                // in case of a malformed HTML.
                elements.Pop();
            }

            if (elements.Count == 0)
            {
                throw CreateParserException("Unexpected close tag encountered.");
            }

            // Close element.
            elements.Pop();
        }

        // When method with such signature is defined on the parser it would be executed for every token recognized.
        protected void OnTokenCompleted(string name)
        {
            var from = GetLinePosition(StartPosition);
            var to = GetLinePosition(CurrentPosition);

            if (name != "ws")
            {
                Console.WriteLine($"token({name}) [{from.Item1}:{from.Item2} - {to.Item1}:{to.Item2}): {GetValue()}");
            }
            else
            {
                Console.WriteLine($"token({name}) [{from.Item1}:{from.Item2} - {to.Item1}:{to.Item2})");
            }
        }

        // When method with such signature is defined on the parser it would be executed for every production reduced.
        protected void OnProductionCompleted(string name)
        {
            Console.WriteLine($"production: {name}");
        }

        private XmlElement GetCurrentRoot()
        {
            if (elements.Count == 0)
            {
                throw CreateParserException("Root element is missing.");
            }

            return elements.Peek();
        }
    }
}
