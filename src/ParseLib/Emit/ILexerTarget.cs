namespace ParseLib.Emit
{
    using System;
    using System.Reflection.Emit;
    using System.Collections.Generic;

    public interface ILexerTarget
    {
        void CompleteToken(ILGenerator il, int tokenId);
        void CompleteToken(ILGenerator il, Cell<int> tokenId);
        void CompleteSource(ILGenerator il, ILexerSource source);
    }
}
