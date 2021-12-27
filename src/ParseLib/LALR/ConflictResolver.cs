namespace ParseLib.LALR
{
    using System;
    using System.Collections.Generic;

    public class ConflictResolver : IConflictResolver
    {
        public static ConflictResolver Default { get; } = new ConflictResolver();

        public virtual ParserAction ResolveShiftConflict(Symbol symbol, Production production)
        {
            if (production.ResolveOn != null && production.ResolveOn.TryGetValue(symbol, out var action))
            {
                return action;
            }

            throw new GrammarException(Errors.UnresolvedShift(), symbol, production);
        }

        public virtual Production ResolveReduceConflict(Symbol symbol, Production first, Production second)
        {
            throw new GrammarException(Errors.UnresolvedReduce(), symbol, first, second);
        }

        public virtual ParserItem[] ResolveCoreConflicts(Symbol symbol, ParserItem[] core)
        {
            return core;
        }
    }
}
