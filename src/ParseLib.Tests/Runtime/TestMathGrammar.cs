namespace ParseLib.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using ParseLib.LALR;
    using ParseLib.Runtime;
    using ParseLib.Text;

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

            AddRule("expr:num", "num");
            AddRule("expr:unary", "- expr");
            AddRule("expr:add", "expr + expr");
            AddRule("expr:sub", "expr - expr");
            AddRule("expr:mul", "expr * expr");
            AddRule("expr:div", "expr / expr");
            AddRule("expr:group", "( expr )");
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
