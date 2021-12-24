using System;
using System.IO;
using System.Xml;
using ParseLib;

namespace Html2Xml
{
    class Program
    {
        static void Main(string[] args)
        {
            var grammar = CreateGrammar();
            var factory = grammar.CreateTextParserFactory<HtmlParser>("nodes");
            var parser = (HtmlParser)null;

            using (var reader = new StreamReader("input.htm"))
            {
                parser = factory(reader);
                parser.Parse();
            }

            var settings = new XmlWriterSettings { Indent = true };

            using (var writer = XmlWriter.Create(Console.Out, settings))
            {
                parser.Document.WriteContentTo(writer);
            }
        }

        static Grammar CreateGrammar()
        {
            var ws = Rex.Char(@" \t\r\n");
            var name_char = Rex.Char(@"a-z0-9-_");
            var script_char = Rex.IfNot("</script").Then(Rex.AnyChar);

            var text_char = Rex.IfNot(Rex.Or(
                    Rex.Text("<!--"),
                    Rex.Char('<').Then(name_char),
                    Rex.Text("</").Then(name_char)))
                .Then(Rex.AnyChar);

            var grammar = new Grammar(ignoreCase: true);

            grammar.CreateNonTerminals("node", "nodes", "attr", "attrs");
            grammar.CreateTerminals("=", "/>", ">", "<script", "</script");
            grammar.CreateWhitespace("ws", ws.OneOrMore());

            grammar.CreateTerminal("node:comment", Rex.Text("<!--").Then(Rex.AnyText).Then("-->"), lazy: true);
            grammar.CreateTerminal("node:text", text_char.OneOrMore());

            grammar.CreateTerminal("%script%", script_char.OneOrMore());
            grammar.CreateTerminal("<tag", Rex.Char('<').Then(name_char.OneOrMore()));
            grammar.CreateTerminal("</tag", Rex.Text("</").Then(name_char.OneOrMore()));
            grammar.CreateTerminal("attr-name", name_char.OneOrMore());
            grammar.CreateTerminal("attr-value-raw", name_char.OneOrMore());
            grammar.CreateTerminal("attr-value-str", Rex.Char('"').Then(Rex.AnyText).Then('"'), lazy: true);

            grammar.AddRule("attr:single", "attr-name");
            grammar.AddRule("attr:value-raw", "attr-name = attr-value-raw");
            grammar.AddRule("attr:value-str", "attr-name = attr-value-str");

            grammar.AddRule("attrs:empty", "").ShiftOn("attr-name");
            grammar.AddRule("attrs:init", "attr");
            grammar.AddRule("attrs:append", "attrs attr");

            grammar.AddRule("node:script-single", "<script attrs />");
            grammar.AddRule("node:script-inline", "<script attrs > %script% </script >");

            grammar.AddRule("node:tag-single", "<tag attrs />");
            grammar.AddRule("node:tag-open", "<tag attrs >");
            grammar.AddRule("node:tag-close", "</tag >");

            grammar.AddRule("nodes:init", "node");
            grammar.AddRule("nodes:apend", "nodes node");

            return grammar;
        }
    }
}
