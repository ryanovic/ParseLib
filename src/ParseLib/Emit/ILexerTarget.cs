namespace ParseLib.Emit
{
    using System;
    using System.Reflection.Emit;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a lexical analyzer output handler. 
    /// </summary>
    public interface ILexerTarget
    {
        void CompleteToken(ILGenerator il, int tokenId);
        void CompleteToken(ILGenerator il, Cell<int> tokenId);
        void CompleteSource(ILGenerator il, ILexerSource source);
    }
}
