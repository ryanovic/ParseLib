namespace Ry.ParseLib
{
    using System;

    internal static class SymbolGuard
    {
        private const int SymbolTypeSize = 7;

        public static void Verify(Symbol symbol, params SymbolType[] allowed)
        {
            if (symbol == null)
            {
                throw new NullReferenceException(nameof(symbol));
            }

            for (int i = 0; i < allowed.Length; i++)
            {
                if (symbol.Type == allowed[i])
                {
                    return;
                }
            }

            throw new InvalidOperationException(Errors.SymbolNotAllowed(symbol.Name));
        }

        public static void Verify(Symbol[] symbols, params SymbolType[] allowed)
        {
            if (symbols == null)
            {
                throw new NullReferenceException(nameof(symbols));
            }

            Span<bool> mask = stackalloc bool[SymbolTypeSize];

            for (int i = 0; i < allowed.Length; i++)
            {
                mask[(int)allowed[i]] = true;
            }

            foreach (var symbol in symbols)
            {
                if (!mask[(int)symbol.Type])
                {
                    throw new InvalidOperationException(Errors.SymbolNotAllowed(symbol.Name));
                }
            }
        }
    }
}
