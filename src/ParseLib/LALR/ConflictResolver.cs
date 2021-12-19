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
            if (production.ResolveOn != null && production.ResolveOn.TryGetValue(symbol, out var preferReduce))
            {
                return preferReduce ? ParserAction.Reduce : ParserAction.Shift;
            }

            throw new GrammarException("Grammar contains unresolved shift-reduce conflict.", symbol, production);
        }

        /// <inheritdoc/>
        public virtual Production ResolveReduceConflict(Symbol symbol, Production first, Production second)
        {
            throw new GrammarException("Grammar contains unresolved reduce-reduce conflict.", symbol, first, second);
        }

        /// <inheritdoc/>
        public virtual ParserItem[] ResolveCoreConflicts(Symbol symbol, ParserItem[] core)
        {
            return core;
        }
    }
}
