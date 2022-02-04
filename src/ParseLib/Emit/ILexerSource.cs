namespace ParseLib.Emit
{
    using System;
    using System.Collections.Generic;
    using System.Reflection.Emit;

    /// <summary>
    /// Represents an input for a lexical analyzer.
    /// </summary>
    public interface ILexerSource
    {
        void LoadStartPosition();
        void LoadEndPosition();
        void LoadLength();
        void LoadCharCode(Cell<int> index);
    }
}
