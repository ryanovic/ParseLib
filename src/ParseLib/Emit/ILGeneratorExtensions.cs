namespace ParseLib.Emit
{
    using System;
    using System.Reflection.Emit;

    public static class ILGeneratorExtensions
    {
        public static ICell CreateCell(this ILGenerator il, Type type)
        {
            if (il == null) throw new ArgumentNullException(nameof(il));
            if (type == null) throw new ArgumentNullException(nameof(type));

            return new LocalCell(il.DeclareLocal(type));
        }

        public static Cell<T> CreateCell<T>(this ILGenerator il)
        {
            if (il == null) throw new ArgumentNullException(nameof(il));

            return new Cell<T>(il.CreateCell(typeof(T)));
        }

        public static LookaheadStack CreateLookaheadStack(this ILGenerator il)
        {
            if (il == null) throw new ArgumentNullException(nameof(il));

            return new LookaheadStack(il.CreateCell(ReflectionInfo.LookaheadStack));
        }

        public static LookaheadItem CreateLookaheadItem(this ILGenerator il)
        {
            if (il == null) throw new ArgumentNullException(nameof(il));

            return new LookaheadItem(il.CreateCell(ReflectionInfo.LookaheadTuple));
        }

        internal static void ThrowInvalidOperationException(this ILGenerator il, string message)
        {
            il.Emit(OpCodes.Ldstr, message);
            il.Emit(OpCodes.Newobj, ReflectionInfo.InvalidOperationException_Ctor);
            il.Emit(OpCodes.Throw);
        }

        internal static void ThrowArgumentNullException(this ILGenerator il, string paramName)
        {
            il.Emit(OpCodes.Ldstr, paramName);
            il.Emit(OpCodes.Newobj, ReflectionInfo.ArgumentNullException_Ctor);
            il.Emit(OpCodes.Throw);
        }

        internal static void ThrowArgumentOutOfRangeException(this ILGenerator il, string paramName, string message)
        {
            il.Emit(OpCodes.Ldstr, paramName);
            il.Emit(OpCodes.Ldstr, message);
            il.Emit(OpCodes.Newobj, ReflectionInfo.ArgumentOutOfRangeException_Ctor);
            il.Emit(OpCodes.Throw);
        }

        internal static Label MarkAndDefine(this ILGenerator il, Label label)
        {
            il.MarkLabel(label);
            return il.DefineLabel();
        }

        internal static Label[] DefineLabels(this ILGenerator il, int count)
        {
            var labels = new Label[count];

            for (int i = 0; i < count; i++)
            {
                labels[i] = il.DefineLabel();
            }

            return labels;
        }

        internal static void CreateArray(this ILGenerator il, ICell array, Action loadLength)
        {
            array.Update(il, () =>
            {
                loadLength();
                il.Emit(OpCodes.Newarr, array.CellType.GetElementType());
            });
        }

        internal static void GoTo(this ILGenerator il, Label label)
        {
            il.Emit(OpCodes.Br, label);
        }

        internal static void For(this ILGenerator il, ICell array, Cell<int> index, Action action)
        {
            var condition = il.DefineLabel();
            var iterate = il.DefineLabel();

            il.GoTo(condition);

            // Iterate.
            il.MarkLabel(iterate);
            action();
            index.Increment(il);

            // Check boundary.
            il.MarkLabel(condition);
            index.Load(il);
            array.Load(il);
            il.Emit(OpCodes.Ldlen);
            il.Emit(OpCodes.Conv_I4);
            il.Emit(OpCodes.Blt, iterate);
        }

        internal static void Map(this ILGenerator il, Label[] labels, Action<int> action)
        {
            for (int i = 0; i < labels.Length; i++)
            {
                il.MarkLabel(labels[i]);
                action(i);
            }
        }

        internal static void Increment(this ILGenerator il, Action loadValue)
        {
            loadValue();
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Add);
        }

        internal static void Decrement(this ILGenerator il, Action loadValue)
        {
            loadValue();
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Sub);
        }

        internal static void LoadTrue(this ILGenerator il)
        {
            il.Emit(OpCodes.Ldc_I4_1);
        }

        internal static void LoadFalse(this ILGenerator il)
        {
            il.Emit(OpCodes.Ldc_I4_0);
        }

        internal static void ConvertToObject(this ILGenerator il, Type type)
        {
            if (type.IsValueType)
            {
                il.Emit(OpCodes.Box, type);
            }
        }

        internal static void ConvertFromObject(this ILGenerator il, Type type)
        {
            if (type.IsValueType)
            {
                il.Emit(OpCodes.Unbox_Any, type);
            }
            else if (type != typeof(object))
            {
                il.Emit(OpCodes.Castclass, type);
            }
        }

        internal static void Debug(this ILGenerator il, string msg)
        {
            var mthd = typeof(System.Diagnostics.Debug).GetMethod("WriteLine", new[] { typeof(string) });
            il.Emit(OpCodes.Ldstr, msg);
            il.Emit(OpCodes.Call, mthd);
        }
    }
}
