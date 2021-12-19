namespace ParseLib.LALR
{
    /// <summary>
    /// Defines grammar conflicts resolver.
    /// </summary>
    public interface IConflictResolver
    {
        /// <summary>
        /// Resolves shift-reduce conflict.
        /// </summary>
        /// <returns>Action to be chosen.</returns>
        ParserAction ResolveShiftConflict(Symbol symbol, Production production);

        /// <summary>
        /// Resolves reduce-reduce conflict.
        /// </summary>
        /// <returns>Production to be chosen.</returns>
        Production ResolveReduceConflict(Symbol symbol, Production first, Production second);

        /// <summary>
        /// Allows to filter out core items for the state when necessary. 
        /// </summary>
        ParserItem[] ResolveCoreConflicts(Symbol symbol, ParserItem[] core);
    }
}
