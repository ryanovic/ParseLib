namespace Html2Xml
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Xml;
    using Ry.ParseLib.Runtime;

    // Custom parser base defines the logic necessary to reduce source to desired output format.
    public abstract class HtmlParser : TextParser
    {
        private readonly Stack<XmlElement> elements;
        private XmlElement current;

        public XmlDocument Document { get; }

        public HtmlParser(TextReader reader) : base(reader)
        {
            elements = new Stack<XmlElement>();
            Document = new XmlDocument();
        }

        // Complete token handler with no return doesn't put any value on the data stack.
        [CompleteToken("comment")]
        protected void CreateComment() => AppendNode(Document.CreateComment(GetValue(4, Length - 7)));

        [CompleteToken("text")]
        protected void CreateText() => AppendNode(Document.CreateTextNode(WebUtility.HtmlDecode(GetValue())));

        [CompleteToken("%script%")]
        protected void CreateScript() => AppendNode(Document.CreateCDataSection(GetValue()));

        [CompleteToken("<script")]
        [CompleteToken("<tag")]
        protected void CreateElement() => current = Document.CreateElement(GetValue(1));

        [CompleteToken("</tag")]
        protected void VerifyElementTree()
        {
            var name = GetValue(2);

            while (elements.Count > 0 && !elements.Peek().Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                elements.Pop();
            }

            if (elements.Count == 0)
            {
                throw CreateParserException("unexpected close tag encountered.");
            }
        }

        // Put lexeme value on the data stack.
        [CompleteToken("attr-name")]
        protected string CreateAttributeName() => GetValue();

        [CompleteToken("attr-value-raw")]
        protected string CreateAttributeRawValue() => GetValue();

        [CompleteToken("attr-value-str")]
        protected string CreateAttributeStringValue() => GetValue(1);

        // Production hander. Reads attribute name from the stack.
        [Reduce("attr:single")]
        protected void AppendAttribute(string name) => current.SetAttribute(name, name);

        // Reads both name and value from the stack.        
        [Reduce("attr:value-raw")]
        [Reduce("attr:value-str")]
        protected void AppendAttribute(string name, string value) => current.SetAttribute(name, value);

        // Handles production prefix before it's reduced(without waiting the valid lookahead recognized).
        [Handle("<tag attrs />")]
        [Handle("<script attrs />")]
        [Handle("<script attrs > %script% </script >")]
        protected void AppendElement()
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

        [Handle("<tag attrs >")]
        protected void BeginElement()
        {
            AppendElement();

            if (!current.Name.Equals("br", StringComparison.OrdinalIgnoreCase))
            {
                // Begin element.
                elements.Push(current);
            }
        }

        [Handle("</tag >")]
        protected void CompleteElement()
        {
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
                Console.WriteLine($"token({name}): {GetValue()} [{from.Item1}:{from.Item2} - {to.Item1}:{to.Item2})");
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

        private void AppendNode(XmlNode node)
        {
            GetElement().AppendChild(node);
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
