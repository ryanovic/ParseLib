namespace Ry.ParseLib.LALR
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Default implementation of the <see cref="IConflictResolver"/> interface.
    /// </summary>
    public class ConflictResolver : IConflictResolver
    {
        /// <summary>
        /// Gets the shared <seealso cref="ConflictResolver"/> instance.
        /// </summary>
        public static ConflictResolver Default { get; } = new ConflictResolver();

        public virtual ParserAction ResolveShiftConflict(Symbol symbol, Production production)
        {
            if (production.ReduceConflictActions != null && production.ReduceConflictActions.TryGetValue(symbol, out var action))
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
