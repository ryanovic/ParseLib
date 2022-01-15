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
        /// <summary>
        /// Indicates whether a source is represented by a single piece or divided into chunks.
        /// </summary>
        bool IsSequental { get; }

        /// <summary>
        /// Jumps to the specified label if the specified position is equal or higher the buffer start.
        /// </summary>
        void CheckLowerBound(ILGenerator il, Cell<int> position, Label isValid);

        /// <summary>
        /// Jumps to the specified label if the specified position is less than the buffer end.
        /// </summary>
        void CheckUpperBound(ILGenerator il, Cell<int> position, Label isValid);

        /// <summary>
        /// Jumps to the specified label if the source end is reached.
        /// </summary>
        void CheckIsLastChunk(ILGenerator il, Label isLast);

        /// <summary>
        /// Reads a UTF-16 character code at the specified position and puts it onto the evaluation stack.
        /// </summary>
        void LoadCharCode(ILGenerator il, Cell<int> position);

        /// <summary>
        /// Reads a low surrogate at the specified position and puts a final UTF-32 Unicode code-point onto the evaluation stack.
        /// </summary>
        void LoadCharCode(ILGenerator il, Cell<int> position, Cell<int> highSurrogate);
    }
}
