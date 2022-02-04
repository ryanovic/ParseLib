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
            grammar.CreateNonTerminals("node", "nodes", "attr", "attrs");

            // List of plain terminals (keywords, punctuators, etc..).
            // Each item will serve as a name and an expression pattern simultaneously.
            grammar.CreateTerminals("=", "/>", ">", "<script", "</script");

            // Terminals defined as whitespace could appear at any position within production.
            grammar.CreateWhitespace("ws", ws.OneOrMore());

            // ':' defines a complex name in 'owner:name' format. In the example below it leads to two different actions are performed:
            // - First, terminal 'name' is created for provided regular expression.
            // - Second - implicit production is defined to reduce newly created terminal to 'owner' (which should be a valid non-terminal symbol)
            // Lazy parameter when set forces expression to be completed as soon as a final state is reached.
            grammar.CreateTerminal("node:comment", Rex.Text("<!--").Then(Rex.AnyText).Then("-->"), lazy: true);
            grammar.CreateTerminal("node:text", text_char.OneOrMore());

            // Symbol name could include any non-space characters (except ':').
            grammar.CreateTerminal("%script%", script_char.OneOrMore());

            // Since '<script' terminal is defined earlier in the grammar it would have priority over more generic '<tag' during processing. 
            grammar.CreateTerminal("<tag", Rex.Char('<').Then(name_char.OneOrMore()));
            grammar.CreateTerminal("</tag", Rex.Text("</").Then(name_char.OneOrMore()));
            grammar.CreateTerminal("attr-name", name_char.OneOrMore());
            grammar.CreateTerminal("attr-value-raw", name_char.OneOrMore());
            grammar.CreateTerminal("attr-value-str", Rex.Char('"').Then(Rex.AnyText).Then('"'), lazy: true);

            // Complex name (owner:name) is treated differently in a production rule definition -
            // 'owner' always defines a non-terminal symbol the rule is defined for and 'owner:name' will form a key, newly created production can be referenced by.
            // Meanwhile NO extra 'name' symbol is defined.
            grammar.AddRule("attr:single", "attr-name");
            grammar.AddRule("attr:value-raw", "attr-name = attr-value-raw");
            grammar.AddRule("attr:value-str", "attr-name = attr-value-str");

            // Each grammar conflict must be explicitly resolved, so in the example below for LALR state like:
            // 'attrs' -> . (reduce empty on 'attr-name')
            // 'attrs' -> . attr
            // 'attr'  -> . attr-name
            // ...
            // The shift action will be forced when 'attr-name' token encountered.
            grammar.AddRule("attrs:empty", "").ShiftOn("attr-name");
            grammar.AddRule("attrs:init", "attr");
            grammar.AddRule("attrs:append", "attrs attr");

            // 'node' -> '<script' 'attrs' '/>'
            grammar.AddRule("node:script-single", "<script attrs />");

            // 'node' -> '<script' 'attrs' '>' '%script%' '</script' '>'
            grammar.AddRule("node:script-inline", "<script attrs > %script% </script >");

            grammar.AddRule("node:tag-single", "<tag attrs />");
            grammar.AddRule("node:tag-open", "<tag attrs >");
            grammar.AddRule("node:tag-close", "</tag >");

            grammar.AddRule("nodes:init", "node");
            grammar.AddRule("nodes:append", "nodes node");

            return grammar;
        }
    }
}
