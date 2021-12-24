using ParseLib;
using System;

namespace Calculator
{
    public class Program
    {
        static void Main(string[] args)
        {
            var gramamr = CreateGrammar();
            var factory = gramamr.CreateStringParserFactory<ExpressionParser>("expr");
            var parser = factory("-(2*a + b*2) / 2 + 2 * 2");
            parser.Parse();
            var eval = parser.CreateDelegate();

            Console.WriteLine(eval(0, 0));    // Outputs: 4
            Console.WriteLine(eval(0, 1));    // Outputs: 3
            Console.WriteLine(eval(1, 0));    // Outputs: 3
            Console.WriteLine(eval(1, 1));    // Outputs: 2
            Console.WriteLine(eval(-1, -1));  // Outputs: 6
        }

        static Grammar CreateGrammar()
        {
            var grammar = new Grammar(new CustomConflictResolver());

            grammar.CreateTerminals("+", "-", "/", "*", "(", ")");
            grammar.CreateNonTerminals("expr");
            grammar.CreateWhitespace("ws", Rex.Char(' ').OneOrMore());

            grammar.CreateTerminal("expr:num", Rex.Char("0-9").OneOrMore());
            grammar.CreateTerminal("expr:a", Rex.Char('a'));
            grammar.CreateTerminal("expr:b", Rex.Char('b'));

            grammar.AddRule("expr:unary", "- expr");
            grammar.AddRule("expr:add", "expr + expr");
            grammar.AddRule("expr:sub", "expr - expr");
            grammar.AddRule("expr:mul", "expr * expr");
            grammar.AddRule("expr:div", "expr / expr");
            grammar.AddRule("expr:group", "( expr )");

            return grammar;
        }
    }
}
