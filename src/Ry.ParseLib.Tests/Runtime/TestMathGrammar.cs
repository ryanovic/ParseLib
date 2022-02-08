namespace Ry.ParseLib.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Ry.ParseLib.LALR;
    using Ry.ParseLib.Runtime;
    using Ry.ParseLib.Text;

    public class TestMathGrammar : Grammar
    {
        public const string Goal = "expr";
        public static Grammar Grammar = new TestMathGrammar();

        public TestMathGrammar()
            : base(new MathConflictResolver())
        {
            CreateTerminals("+", "-", "/", "*", "(", ")");
            CreateNonTerminals("expr");

            CreateTerminal("num", Rex.Char(@"0-9").OneOrMore());
            CreateWhitespace("ws", Rex.Char(' '));

            GetNonTerminal("expr").AddProduction("expr:num", "num");
            GetNonTerminal("expr").AddProduction("expr:unary", "- expr");
            GetNonTerminal("expr").AddProduction("expr:add", "expr + expr");
            GetNonTerminal("expr").AddProduction("expr:sub", "expr - expr");
            GetNonTerminal("expr").AddProduction("expr:mul", "expr * expr");
            GetNonTerminal("expr").AddProduction("expr:div", "expr / expr");
            GetNonTerminal("expr").AddProduction("expr:group", "( expr )");
        }

        public class MathConflictResolver : ConflictResolver
        {
            public override ParserAction ResolveShiftConflict(Symbol symbol, Production production)
            {
                switch (production.Name)
                {
                    case "expr:sub":
                    case "expr:add":
                        return symbol.Name == "+" || symbol.Name == "-"
                            ? ParserAction.Reduce
                            : ParserAction.Shift;
                    case "expr:div":
                    case "expr:mul":
                    case "expr:unary":
                        return ParserAction.Reduce;
                }

                return base.ResolveShiftConflict(symbol, production);
            }
        }
    }
}
