namespace Ry.ParseLib.Emit
{
    using System;
    using System.Collections.Generic;
    using System.Reflection.Emit;

    /// <summary>
    /// Implements basic operations on the <see cref="List{T}"/> allowing it to be used as a state/data stack at runtime.
    /// </summary>
    internal sealed class ParserStack
    {
        private readonly ICell list;

        public ParserStack(ICell list)
        {
            this.list = list;
        }

        /// <summary>
        /// Initializes the cell with a new <see cref="List{T}"/> instance.
        /// </summary>
        public void Initialize(ILGenerator il)
        {
            list.Update(il, () => il.Emit(OpCodes.Newobj, list.CellType.GetConstructor(Type.EmptyTypes)));
        }

        /// <summary>
        /// Gets a number of elements and puts it onto the evaluation stack.
        /// </summary>
        public void GetCount(ILGenerator il)
        {
            list.Load(il);
            il.Emit(OpCodes.Callvirt, list.CellType.GetProperty("Count").GetGetMethod());
        }

        /// <summary>
        /// Gets an element at the specified <paramref name="index"/> and puts it onto the evaluation stack.
        /// </summary>
        public void GetElementAt(ILGenerator il, Cell<int> index)
        {
            GetElementAt(il, () => index.Load(il));
        }

        /// <summary>
        /// Gets an element at the index defined by the <paramref name="loadIIndex"/>() and puts it onto the evaluation stack.
        /// </summary>
        public void GetElementAt(ILGenerator il, Action loadIIndex)
        {
            list.Load(il);
            loadIIndex();
            il.Emit(OpCodes.Callvirt, list.CellType.GetProperty("Item").GetGetMethod());
        }

        /// <summary>
        /// Loads a value from the <paramref name="item"/> and appends it to the list.
        /// </summary>
        public void Push(ILGenerator il, ICell item)
        {
            Push(il, () => item.Load(il));
        }

        /// <summary>
        /// Loads a value from the <paramref name="loadItem"/>() output and appends it to the list.
        /// </summary>
        public void Push(ILGenerator il, Action loadItem)
        {
            list.Load(il);
            loadItem();
            il.Emit(OpCodes.Callvirt, list.CellType.GetMethod("Add", list.CellType.GetGenericArguments()));
        }

        /// <summary>
        /// Gets the nth element from the top of the list and puts it onto the evaluation stack.
        /// </summary>
        public void Peek(ILGenerator il, int offset)
        {
            list.Load(il);
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Callvirt, list.CellType.GetProperty("Count").GetGetMethod());
            il.Emit(OpCodes.Ldc_I4, offset + 1);
            il.Emit(OpCodes.Sub);
            il.Emit(OpCodes.Callvirt, list.CellType.GetProperty("Item").GetGetMethod());
        }

        /// <summary>
        /// Replaces the specified numbers of elements on the top with the <paramref name="loadItem"/>() output.
        /// </summary>
        public void ReplaceTop(ILGenerator il, int count, Action loadItem)
        {
            if (count == 0)
            {
                throw new InvalidOperationException(Errors.ZeroSubArray());
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

        /// <summary>
        /// Removes the specified numbers of elements from the top of the list.
        /// </summary>
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
