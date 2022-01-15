namespace ParseLib.Emit
{
    using System;
    using System.Collections.Generic;
    using System.Reflection.Emit;
    using ParseLib.Runtime;

    /// <summary>
    /// Implements <see cref="ILexerSource"/> interface for a string parser.
    /// </summary>
    /// <remarks>
    /// Corresponds to a lexer method with the following  signature:
    /// <code>
    /// void Read(string content, int offset, int length);
    /// </code>
    /// See <see cref="StringParser"/> for an example.
    /// </remarks>
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
