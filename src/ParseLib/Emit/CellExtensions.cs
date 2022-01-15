namespace ParseLib.Emit
{
    using System;
    using System.Collections.Generic;
    using System.Reflection.Emit;

    public static class CellExtensions
    {
        /// <summary>
        /// Updates the <paramref name="cell"/> with the value form the <paramref name="other"/>.
        /// </summary>
        public static void Update<T>(this Cell<T> cell, ILGenerator il, Cell<T> other)
        {
            if (il == null) throw new ArgumentNullException(nameof(il));

            if (!cell.Equals(other))
            {
                cell.Update(il, () => other.Load(il));
            }
        }

        /// <summary>
        /// Updates the <paramref name="cell"/> with the <paramref name="value"/>.
        /// </summary>
        public static void Update(this Cell<int> cell, ILGenerator il, int value)
        {
            if (il == null) throw new ArgumentNullException(nameof(il));

            cell.Update(il, () =>
            {
                if (value == 0)
                {
                    il.Emit(OpCodes.Ldc_I4_0);
                }
                else if (value == -1)
                {
                    il.Emit(OpCodes.Ldc_I4_M1);
                }
                else
                {
                    il.Emit(OpCodes.Ldc_I4, value);
                }
            });
        }

        /// <summary>
        /// Updates the <paramref name="cell"/> with the <paramref name="value"/>.
        /// </summary>
        public static void Update(this Cell<bool> cell, ILGenerator il, bool value)
        {
            if (il == null) throw new ArgumentNullException(nameof(il));

            cell.Update(il, () => il.Emit(value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
        }

        /// <summary>
        /// Increments the <paramref name="cell"/> value.
        /// </summary>
        public static void Increment(this Cell<int> cell, ILGenerator il)
        {
            if (il == null) throw new ArgumentNullException(nameof(il));

            cell.Update(il, () =>
            {
                cell.Load(il);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Add);
            });
        }

        /// <summary>
        /// Decrements the <paramref name="cell"/> value.
        /// </summary>
        public static void Decrement(this Cell<int> cell, ILGenerator il)
        {
            if (il == null) throw new ArgumentNullException(nameof(il));

            cell.Update(il, () =>
            {
                cell.Load(il);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Sub);
            });
        }
    }
}
