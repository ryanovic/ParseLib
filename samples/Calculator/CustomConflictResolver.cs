using System;
using System.Collections.Generic;
using System.Text;
using ParseLib;
using ParseLib.LALR;

namespace Calculator
{
    public class CustomConflictResolver : ConflictResolver
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
