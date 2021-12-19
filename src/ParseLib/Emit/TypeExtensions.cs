namespace ParseLib.Emit
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;

    public static class TypeExtensions
    {
        /// <summary>
        /// Creates a fiel cell of the specified type.
        /// </summary>
        public static ICell CreateCell(this TypeBuilder type, string field, Type fieldType)
        {
            return new FieldCell(type.DefineField(field, fieldType, FieldAttributes.Private));
        }

        /// <summary>
        /// Creates a strong typed field cell.
        /// </summary>
        public static Cell<T> CreateCell<T>(this TypeBuilder type, string field)
        {
            return new Cell<T>(type.CreateCell(field, typeof(T)));
        }

        /// <summary>
        /// Creates a field cell to store a Lookahead stack instance.
        /// </summary>
        public static LookaheadStack CreateLookaheadStack(this TypeBuilder type, string name)
        {
            return new LookaheadStack(type.CreateCell(name, ReflectionInfo.LookaheadStack));
        }

        /// <summary>
        /// Creates a field cell to store a Lookahead stack item instance.
        /// </summary>
        public static LookaheadItem CreateLookaheadItem(this TypeBuilder type, string name)
        {
            return new LookaheadItem(type.CreateCell(name, ReflectionInfo.LookaheadTuple));
        }
    }
}
