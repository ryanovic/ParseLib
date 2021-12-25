namespace Html2Xml
{
    using System;
    using System.IO;
    using System.Xml;
    using ParseLib;

    public class Program
    {
        static void Main(string[] args)
        {
            // Program reads html-like input(only limited language's rule set is supported here for demo purposes)
            // and restores it as XML docment.

            var grammar = CreateGrammar();
            var factory = grammar.CreateTextParserFactory<HtmlParser>("nodes");
            var parser = (HtmlParser)null;

            using (var reader = new StreamReader("input.htm"))
            {
                // Creates new parser instance.
                parser = factory(reader);
                parser.Parse();
            }

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

            // List of plain terminals (keywords, punctuators, etc..) can be defined in a single call.
            // For each entry the following mapping will be generated:
            // '=' -> Rex.Text("=")
            // '/>' -> Rex.Text("/>")
            // ...
            grammar.CreateTerminals("=", "/>", ">", "<script", "</script");

            // Defines whitespace terminal which could appear anywhere in a production.
            grammar.CreateWhitespace("ws", ws.OneOrMore());

            // ':' has special meaning in the terminal definition, and when it's used the following applies:
            // 1. terminal 'comment' ('text') is defined with regular expression specified.
            // 2. node -> comment (node -> text) production rule is added for 'node' non-terminal
            // Lazy parameter when set forces expression to be handled as soon as a final state is reached.
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

            // ':' is treated differently in the rule declaration, in the example below only the following rule is created:
            // 'attr' -> 'attr-name' 
            // Meaning no 'single' extra non-terminal is created.
            // Rule name('attr:single') will be used to reference this production later in the reducer.
            grammar.AddRule("attr:single", "attr-name");
            grammar.AddRule("attr:value-raw", "attr-name = attr-value-raw");
            grammar.AddRule("attr:value-str", "attr-name = attr-value-str");

            // Each grammar conflict must be explicitly resolved, so in the example below for LALR state like:
            // 'attrs' -> . (reduce empty on 'attr-name')
            // 'attrs' -> . attr
            // 'attr'  -> . attr-name
            // ...
            // the shift action will be forced when 'attr-name' token encountered.
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
            grammar.AddRule("nodes:apend", "nodes node");

            return grammar;
        }
    }
}
