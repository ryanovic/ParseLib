namespace Ry.ParseLib.Emit
{
    using System;
    using System.Collections.Generic;
    using System.Reflection.Emit;
    using System.Text;

    internal abstract class LexerSourceBase : ILexerSource
    {
        protected ILGenerator IL { get; }

        public LexerSourceBase(ILGenerator il)
        {
            this.IL = il;
        }

        public abstract void LoadStartPosition();

        public abstract void LoadEndPosition();

        public virtual void LoadCharCode(Cell<int> index)
        {
            LoadBuffer();
            index.Load(IL);
#if NET6_0
            IL.Emit(OpCodes.Call, ReflectionInfo.ReadOnlyCharSpan_Item_Get);
#else
            // https://github.com/dotnet/runtime/issues/64799
            // I can't use the ReadOnlyCharSpan_Item_Get metadata for frameworks prior to .NET 6.
            // Luckily, this trick with CharSpan_Item_Get produces comparable IL,
            // so I still can keep the same interface for all versions.
            IL.Emit(OpCodes.Call, ReflectionInfo.CharSpan_Item_Get);
#endif
            IL.Emit(OpCodes.Ldind_U2);
        }

        public virtual void LoadCharCode(Cell<int> index, Cell<int> highSurrotate)
        {
            highSurrotate.Load(IL);
            LoadCharCode(index);
            IL.Emit(OpCodes.Call, ReflectionInfo.Char_ToUtf32);
        }

        public virtual void LoadLength()
        {
            LoadBuffer();
            IL.Emit(OpCodes.Call, ReflectionInfo.ReadOnlyCharSpan_Length_Get);
        }

        protected abstract void LoadBuffer();
    }
}
