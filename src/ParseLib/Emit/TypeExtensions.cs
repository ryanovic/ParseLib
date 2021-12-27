namespace ParseLib.Emit
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;

    public static class TypeExtensions
    {
        public static ICell CreateCell(this TypeBuilder type, string field, Type fieldType)
        {
            return new FieldCell(type.DefineField(field, fieldType, FieldAttributes.Private));
        }

        public static Cell<T> CreateCell<T>(this TypeBuilder type, string field)
        {
            return new Cell<T>(type.CreateCell(field, typeof(T)));
        }

        public static LookaheadStack CreateLookaheadStack(this TypeBuilder type, string name)
        {
            return new LookaheadStack(type.CreateCell(name, ReflectionInfo.LookaheadStack));
        }

        public static LookaheadItem CreateLookaheadItem(this TypeBuilder type, string name)
        {
            return new LookaheadItem(type.CreateCell(name, ReflectionInfo.LookaheadTuple));
        }
    }
}
