namespace ParseLib.Emit
{
    using System;
    using System.Collections.Generic;
    using System.Reflection.Emit;

    public interface ILexerSource
    {
        bool IsSequental { get; }

        void CheckLowerBound(ILGenerator il, Cell<int> position, Label isValid);
        void CheckUpperBound(ILGenerator il, Cell<int> position, Label isValid);
        void CheckIsLastChunk(ILGenerator il, Label isLast);

        void LoadCharCode(ILGenerator il, Cell<int> position);
        void LoadCharCode(ILGenerator il, Cell<int> position, Cell<int> highSurrogate);
    }
}
