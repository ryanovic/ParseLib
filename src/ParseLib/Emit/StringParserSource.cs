namespace ParseLib.Emit
{
    using System;
    using System.Collections.Generic;
    using System.Reflection.Emit;

    public sealed class StringParserSource : ILexerSource
    {
        public bool IsSequental => false;

        public void CheckLowerBound(ILGenerator il, Cell<int> position, Label isValid)
        {
            position.Load(il);
            LoadOffset(il);
            il.Emit(OpCodes.Bge, isValid);
        }

        public void CheckUpperBound(ILGenerator il, Cell<int> position, Label isValid)
        {
            position.Load(il);
            LoadLength(il);
            il.Emit(OpCodes.Blt, isValid);
        }

        public void CheckIsLastChunk(ILGenerator il, Label isLast)
        {
            il.GoTo(isLast);
        }

        public void LoadCharCode(ILGenerator il, Cell<int> position)
        {
            LoadContent(il);
            LoadOffset(il);
            position.Load(il);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Callvirt, ReflectionInfo.String_Get);
        }

        public void LoadCharCode(ILGenerator il, Cell<int> position, Cell<int> highSurrogate)
        {
            highSurrogate.Load(il);
            LoadCharCode(il, position);
            il.Emit(OpCodes.Call, ReflectionInfo.Char_ToUtf32);
        }

        private void LoadContent(ILGenerator il) => il.Emit(OpCodes.Ldarg_1);

        private void LoadOffset(ILGenerator il) => il.Emit(OpCodes.Ldarg_2);

        private void LoadLength(ILGenerator il) => il.Emit(OpCodes.Ldarg_3);
    }
}
