namespace ParseLib.LALR
{
    using System;
    using System.Collections.Generic;

    /// <inheritdoc/>
    public class ConflictResolver : IConflictResolver
    {
        /// <summary>
        /// Gets default resolver based on a production level rules defined.
        /// </summary>
        public static ConflictResolver Default { get; } = new ConflictResolver();

        /// <inheritdoc/>
        public virtual ParserAction ResolveShiftConflict(Symbol symbol, Production production)
        {
            if (production.ResolveOn != null && production.ResolveOn.TryGetValue(symbol, out var action))
            {
                return action;
            }

            throw new GrammarException(Errors.UnresolvedShift(), symbol, production);
        }

        /// <inheritdoc/>
        public virtual Production ResolveReduceConflict(Symbol symbol, Production first, Production second)
        {
            throw new GrammarException(Errors.UnresolvedReduce(), symbol, first, second);
        }

        /// <inheritdoc/>
        public virtual ParserItem[] ResolveCoreConflicts(Symbol symbol, ParserItem[] core)
        {
            return core;
        }
    }
}
