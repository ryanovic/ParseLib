namespace ParseLib.Emit
{
    using System;
    using System.Collections.Generic;
    using System.Reflection.Emit;

    /// <summary>
    /// Defines the interface to access the source which represents one or more chunks of UTF-16 char code sequences.
    /// </summary>
    public interface ILexerSource
    {
        /// <summary>
        /// Gets the value indicating if the source is represented by a single unceasing buffer or a set of chunks which are read sequentially.  
        /// </summary>
        bool IsSequental { get; }

        /// <summary>
        /// Checks if the <paramref name="position"/> is greater than or equal of the lower bound of the current chunk.
        /// If positive - execution will continue from the <paramref name="isValid"/> label.
        /// </summary>
        void CheckLowerBound(ILGenerator il, Cell<int> position, Label isValid);

        /// <summary>
        /// Checks if the <paramref name="position"/> is less than or equal of the upper bound of the current chunk.
        /// If positive - execution will continue from the <paramref name="isValid"/> label.
        /// </summary>
        void CheckUpperBound(ILGenerator il, Cell<int> position, Label isValid);

        /// <summary>
        /// Checks if the current chunk is the last in the source.
        /// If positive - execution will continue from the <paramref name="isLast"/> label.
        /// </summary>
        void CheckIsLastChunk(ILGenerator il, Label isLast);

        /// <summary>
        /// Loads the UTF-16 char code and puts it on the stack. <paramref name="position"/> is supposed to be in bound of the current chunk.
        /// </summary>
        void LoadCharCode(ILGenerator il, Cell<int> position);

        /// <summary>
        /// Loads the UTF-32 char code and puts it on the stack. <paramref name="position"/> is supposed to be in bound of the current chunk and pointing to the low surrogate character code.
        /// </summary>
        void LoadCharCode(ILGenerator il, Cell<int> position, Cell<int> highSurrogate);
    }
}
