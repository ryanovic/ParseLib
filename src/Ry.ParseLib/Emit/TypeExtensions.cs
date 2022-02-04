namespace Ry.ParseLib.Emit
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;

    public static class TypeExtensions
    {
        /// <summary>
        /// Creates a filed cell of the specified type and name.
        /// </summary>
        public static ICell CreateCell(this TypeBuilder type, string field, Type fieldType)
        {
            return new FieldCell(type.DefineField(field, fieldType, FieldAttributes.Private));
        }

        /// <summary>
        /// Creates a field cell of the <typeparamref name="T"/> type.
        /// </summary>
        public static Cell<T> CreateCell<T>(this TypeBuilder type, string field)
        {
            return new Cell<T>(type.CreateCell(field, typeof(T)));
        }

        /// <summary>
        /// Creates a field cell for an lookahead stack instance.
        /// </summary>
        public static LookaheadStack CreateLookaheadStack(this TypeBuilder type, string name)
        {
            return new LookaheadStack(type.CreateCell(name, ReflectionInfo.LookaheadStack));
        }

        /// <summary>
        /// Creates a field cell for an lookahead stack item instance.
        /// </summary>
        public static LookaheadItem CreateLookaheadItem(this TypeBuilder type, string name)
        {
            return new LookaheadItem(type.CreateCell(name, ReflectionInfo.LookaheadTuple));
        }
    }
}
