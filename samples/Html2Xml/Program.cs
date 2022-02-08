namespace Html2Xml
{
    using System;
    using System.IO;
    using System.Xml;
    using Ry.ParseLib;

    public class Program
    {
        static void Main(string[] args)
        {
            // Program reads html-like input(only limited language's rule set is supported here for demo purposes)
            // and restores it as XML docment.

            var grammar = CreateGrammar();
            var factory = grammar.CreateTextParserFactory<HtmlParser>("nodes");
            var parser = (HtmlParser)null;

            Console.WriteLine("\r\n **** Parser Output ****\r\n");

            using (var reader = new StreamReader("input.htm"))
            {
                // Creates new parser instance.
                parser = factory(reader);
                parser.Parse();
            }

            Console.WriteLine("\r\n **** XML Document ****\r\n");
            var settings = new XmlWriterSettings { Indent = true };

            // Output resulting Xml Document.
            using (var writer = XmlWriter.Create(Console.Out, settings))
            {
                parser.Document.WriteContentTo(writer);
            }
        }

        static Grammar CreateGrammar()
        {
            // Basic character sets going to be used in the grammar:
            // - space, tab and new line are defined as a whitespace characters. 
            // - Names and raw values are represented by numbes, english alphabet characters, '-' and '_' symbols.  
            // - Script content could contain any character unless '</script' char sequence is recognized.
            // - Text node could contain any unicode character including '<' as well, unless it defines a start for more complex html constructs. 

            var ws = Rex.Char(@" \t\r\n");
            var name_char = Rex.Char(@"a-z0-9-_");
            var script_char = Rex.IfNot("</script").Then(Rex.AnyChar);

            var text_char = Rex.IfNot(Rex.Or(
                    Rex.Text("<!--"),
                    Rex.Char('<').Then(name_char),
                    Rex.Text("</").Then(name_char)))
                .Then(Rex.AnyChar);

            var grammar = new Grammar(ignoreCase: true);

            // Any non-terminal should be defined before it's referenced.
            var node = grammar.CreateNonTerminal("node");
            var nodes = grammar.CreateNonTerminal("nodes");
            var attr = grammar.CreateNonTerminal("attr");
            var attrs = grammar.CreateNonTerminal("attrs");

            // List of plain terminals (keywords, punctuators, etc..).
            // Each item will serve as a name and an expression pattern simultaneously.
            grammar.CreateTerminals("=", "/>", ">", "<script", "</script");

            // Terminals defined as whitespace could appear at any position within production.
            grammar.CreateWhitespace("ws", ws.OneOrMore());

            // Lazy parameter when set forces expression to be completed as soon as a final state is reached.
            node.AddProduction("node:comment", grammar.CreateTerminal("comment", Rex.Text("<!--").Then(Rex.AnyText).Then("-->"), lazy: true));
            node.AddProduction("node:text", grammar.CreateTerminal("text", text_char.OneOrMore()));

            grammar.CreateTerminal("%script%", script_char.OneOrMore());

            // Since '<script' terminal is defined earlier in the grammar it would have priority over more generic '<tag' during processing. 
            grammar.CreateTerminal("<tag", Rex.Char('<').Then(name_char.OneOrMore()));
            grammar.CreateTerminal("</tag", Rex.Text("</").Then(name_char.OneOrMore()));
            grammar.CreateTerminal("attr-name", name_char.OneOrMore());
            grammar.CreateTerminal("attr-value-raw", name_char.OneOrMore());
            grammar.CreateTerminal("attr-value-str", Rex.Char('"').Then(Rex.AnyText).Then('"'), lazy: true);

            attr.AddProduction("attr:single", "attr-name");
            attr.AddProduction("attr:value-raw", "attr-name = attr-value-raw");
            attr.AddProduction("attr:value-str", "attr-name = attr-value-str");

            // Each grammar conflict must be explicitly resolved, so in the example below for LALR state like:
            // 'attrs' -> . (reduce empty on 'attr-name')
            // 'attrs' -> . attr
            // 'attr'  -> . attr-name
            // ...
            // The shift action will be forced when 'attr-name' token encountered.
            attrs.AddProduction("attrs:empty", "").ShiftOn("attr-name");
            attrs.AddProduction("attrs:init", "attr");
            attrs.AddProduction("attrs:append", "attrs attr");

            // 'node' -> '<script' 'attrs' '/>'
            node.AddProduction("node:script-single", "<script attrs />");

            // 'node' -> '<script' 'attrs' '>' '%script%' '</script' '>'
            node.AddProduction("node:script-inline", "<script attrs > %script% </script >");

            node.AddProduction("node:tag-single", "<tag attrs />");
            node.AddProduction("node:tag-open", "<tag attrs >");
            node.AddProduction("node:tag-close", "</tag >");

            nodes.AddProduction("nodes:init", "node");
            nodes.AddProduction("nodes:append", "nodes node");

            return grammar;
        }
    }
}
