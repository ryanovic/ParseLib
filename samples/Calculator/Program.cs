namespace Calculator
{
    using System;
    using Ry.ParseLib;

    public class Program
    {
        static void Main(string[] args)
        {
            // Simple expression evaluator with basic math operation support.
            // Generates (int a, int b) -> int delegated based on template provided as an input.

            var gramamr = CreateGrammar();
            var factory = gramamr.CreateStringParserFactory<ExpressionParser>("expr");

            Console.WriteLine("\r\n **** Parser Output ****\r\n");

            const string expression = "-(2*a + b*2) / 2 + 2 * 2";
            var parser = factory(expression);
            parser.Parse();
            var eval = parser.CreateDelegate();

            Console.WriteLine($"\r\n **** Evaluates: {expression}\r\n");

            Console.WriteLine($"a = {0}, b = {0}, result: {eval(0, 0)}");        // Outputs: 4
            Console.WriteLine($"a = {1}, b = {0}, result: {eval(1, 0)}");        // Outputs: 3
            Console.WriteLine($"a = {1}, b = {1}, result: {eval(1, 1)}");        // Outputs: 2
            Console.WriteLine($"a = {-1}, b = {-1}, result: {eval(-1, -1)}");    // Outputs: 6
        }

        static Grammar CreateGrammar()
        {
            // Custom conflict resolver helps to setup operator's priorities and associativity
            // keeping grammar definition compact and nice.
            var grammar = new Grammar(new CustomConflictResolver());

            // Group definition for 'plain' terminals ("name" -> Rex.Text("name")).
            grammar.CreateTerminals("+", "-", "/", "*", "(", ")");

            // Root non-terminal.
            var expr = grammar.CreateNonTerminal("expr");

            // Define whitespace so digits and operators could be separated in the source. 
            grammar.CreateWhitespace("ws", Rex.Char(' ').OneOrMore());

            // Equals to:            
            // num = [0-9]+
            // a = 'a'
            // b = 'b'
            // With implicitly created productions:
            // expr -> num
            // expr -> a
            // expr -> b
            expr.AddProduction("expr:num", grammar.CreateTerminal("num", Rex.Char("0-9").OneOrMore()));
            expr.AddProduction("expr:a", grammar.CreateTerminal("a", Rex.Char('a')));
            expr.AddProduction("expr:b", grammar.CreateTerminal("b", Rex.Char('b')));

            // Define production for each operation.
            expr.AddProduction("expr:unary", "- expr");
            expr.AddProduction("expr:add", "expr + expr");
            expr.AddProduction("expr:sub", "expr - expr");
            expr.AddProduction("expr:mul", "expr * expr");
            expr.AddProduction("expr:div", "expr / expr");
            expr.AddProduction("expr:group", "( expr )");

            return grammar;
        }
    }
}
