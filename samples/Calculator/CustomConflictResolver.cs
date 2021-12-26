namespace Calculator
{
    using ParseLib;
    using ParseLib.LALR;

    public class CustomConflictResolver : ConflictResolver
    {
        // Operator's mutual priority can be defined as a shift-reduce grammar conflict, like:
        // expr -> expr  + expr . (reduce on '*')
        // expr -> expr .* expr 
        // Similar for operator associativity we would have state like:
        // expr -> expr  + expr . (reduce on '+')
        // expr -> expr .+ expr
        // Hence, by specifying a correct action for every (production, symbol) pair we can configure both. 
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
