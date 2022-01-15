namespace ParseLib.Emit
{
    using System;
    using System.Collections.Generic;
    using System.Reflection.Emit;
    using ParseLib.Runtime;

    /// <summary>
    /// Implements <see cref="ILexerSource"/> interface for a sequential parser.
    /// </summary>
    /// <remarks>
    /// Corresponds to a lexer method with the following  signature:
    /// <code>
    /// bool Read(int bufferPosition, char[] buffer, int offset, int length, bool endOfSource);
    /// </code>
    /// See <see cref="TextParser"/> for an example.
    /// </remarks>
    public sealed class SequentialParserSource : ILexerSource
    {
        public bool IsSequental => true;

        public void CheckLowerBound(ILGenerator il, Cell<int> position, Label isValid)
        {
            position.Load(il);
            LoadBufferPosition(il);
            il.Emit(OpCodes.Bge, isValid);
        }

        public void CheckUpperBound(ILGenerator il, Cell<int> position, Label isValid)
        {
            position.Load(il);
            LoadBufferPosition(il);
            il.Emit(OpCodes.Sub);
            LoadLength(il);
            il.Emit(OpCodes.Blt, isValid);
        }

        public void CheckIsLastChunk(ILGenerator il, Label isLast)
        {
            LoadIsEndOfSource(il);
            il.Emit(OpCodes.Brtrue, isLast);
        }

        public void LoadCharCode(ILGenerator il, Cell<int> position)
        {
            LoadBuffer(il);
            LoadOffset(il);
            position.Load(il);
            LoadBufferPosition(il);
            il.Emit(OpCodes.Sub);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Ldelem_U2);
        }

        public void LoadCharCode(ILGenerator il, Cell<int> position, Cell<int> highSurrogate)
        {
            highSurrogate.Load(il);
            LoadCharCode(il, position);
            il.Emit(OpCodes.Call, ReflectionInfo.Char_ToUtf32);
        }

        private void LoadBufferPosition(ILGenerator il) => il.Emit(OpCodes.Ldarg_1);

        private void LoadBuffer(ILGenerator il) => il.Emit(OpCodes.Ldarg_2);

        private void LoadOffset(ILGenerator il) => il.Emit(OpCodes.Ldarg_3);

        private void LoadLength(ILGenerator il) => il.Emit(OpCodes.Ldarg, 4);

        private void LoadIsEndOfSource(ILGenerator il) => il.Emit(OpCodes.Ldarg, 5);
    }
}
