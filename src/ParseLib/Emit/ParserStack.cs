namespace ParseLib.Emit
{
    using System;
    using System.Collections.Generic;
    using System.Reflection.Emit;

    /// <summary>
    /// Defines internal helper to work with parser's stack.
    /// </summary>
    internal sealed class ParserStack
    {
        private readonly ICell list;

        public ParserStack(ICell list)
        {
            this.list = list;
        }

        public void Initialize(ILGenerator il)
        {
            list.Update(il, () => il.Emit(OpCodes.Newobj, list.CellType.GetConstructor(Type.EmptyTypes)));
        }

        public void GetCount(ILGenerator il)
        {
            list.Load(il);
            il.Emit(OpCodes.Callvirt, list.CellType.GetProperty("Count").GetGetMethod());
        }

        public void GetElementAt(ILGenerator il, Cell<int> index)
        {
            GetElementAt(il, () => index.Load(il));
        }

        public void GetElementAt(ILGenerator il, Action loadIIndex)
        {
            list.Load(il);
            loadIIndex();
            il.Emit(OpCodes.Callvirt, list.CellType.GetProperty("Item").GetGetMethod());
        }

        public void Push(ILGenerator il, ICell item)
        {
            Push(il, () => item.Load(il));
        }

        public void Push(ILGenerator il, Action loadItem)
        {
            list.Load(il);
            loadItem();
            il.Emit(OpCodes.Callvirt, list.CellType.GetMethod("Add", list.CellType.GetGenericArguments()));
        }

        public void Peek(ILGenerator il, int offset)
        {
            list.Load(il);
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Callvirt, list.CellType.GetProperty("Count").GetGetMethod());
            il.Emit(OpCodes.Ldc_I4, offset + 1);
            il.Emit(OpCodes.Sub);
            il.Emit(OpCodes.Callvirt, list.CellType.GetProperty("Item").GetGetMethod());
        }

        public void ReplaceTop(ILGenerator il, int count, Action loadItem)
        {
            if (count == 0)
            {
                throw new InvalidOperationException("Can't replace zero sized sub-array.");
            }

            list.Load(il);
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Callvirt, list.CellType.GetProperty("Count").GetGetMethod());
            il.Emit(OpCodes.Ldc_I4, count);
            il.Emit(OpCodes.Sub);
            loadItem();
            il.Emit(OpCodes.Callvirt, list.CellType.GetProperty("Item").GetSetMethod());

            RemoveTop(il, count - 1);
        }

        public void RemoveTop(ILGenerator il, int count)
        {
            if (count > 0)
            {
                list.Load(il);
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Callvirt, list.CellType.GetProperty("Count").GetGetMethod());
                il.Emit(OpCodes.Ldc_I4, count);
                il.Emit(OpCodes.Sub);
                il.Emit(OpCodes.Ldc_I4, count);
                il.Emit(OpCodes.Callvirt, list.CellType.GetMethod("RemoveRange"));
            }
        }
    }
}
