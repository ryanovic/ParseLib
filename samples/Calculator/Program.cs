namespace Calculator
{
    using System;
    using ParseLib;

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
            grammar.CreateNonTerminals("expr");

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
            grammar.CreateTerminal("expr:num", Rex.Char("0-9").OneOrMore());
            grammar.CreateTerminal("expr:a", Rex.Char('a'));
            grammar.CreateTerminal("expr:b", Rex.Char('b'));

            // Defines the production for each operation.
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
