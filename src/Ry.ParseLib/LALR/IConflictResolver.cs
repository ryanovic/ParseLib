namespace Ry.ParseLib.LALR
{
    /// <summary>
    /// Defines an interface for resolving grammar conflicts.
    /// </summary>
    public interface IConflictResolver
    {
        /// <summary>
        /// Resolves a shift-reduce conflict for the specified production and lookahead symbol.
        /// </summary>
        /// <returns>The preferred grammar action.</returns>
        ParserAction ResolveShiftConflict(Symbol symbol, Production production);

        /// <summary>
        /// Resolves a reduce-reduce conflict for the specified productions and lookahead symbol.
        /// </summary>
        /// <returns>The preferred production for reduction.</returns>
        Production ResolveReduceConflict(Symbol symbol, Production first, Production second);

        /// <summary>
        /// Resolves parser state core conflicts if any. The updated core will be used to create a state.
        /// </summary>
        /// <param name="symbol">The input symbol for a state.</param>
        /// <param name="core">The set of core items.</param>
        /// <returns>The final set of core items.</returns>
        /// <example>
        /// Consider a sample set of core items for the '{' input symbol:
        /// ... -- '{' --> [block --> { *stmnts } ], [obj_expr --> { *obj_initializer }]
        /// So if you would like to exclude second production from the core
        /// <c>ResolveCoreConflicts</c> is the right place for this.
        /// </example>
        ParserItem[] ResolveCoreConflicts(Symbol symbol, ParserItem[] core);
    }
}
