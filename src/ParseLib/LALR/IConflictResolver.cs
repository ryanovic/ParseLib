namespace ParseLib.LALR
{
    public interface IConflictResolver
    {
        ParserAction ResolveShiftConflict(Symbol symbol, Production production);

        Production ResolveReduceConflict(Symbol symbol, Production first, Production second);

        ParserItem[] ResolveCoreConflicts(Symbol symbol, ParserItem[] core);
    }
}
