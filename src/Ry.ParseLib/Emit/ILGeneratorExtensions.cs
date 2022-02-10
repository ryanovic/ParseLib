namespace Ry.ParseLib.Emit
{
    using System;
    using System.Reflection;
    using System.Reflection.Emit;

    public static class ILGeneratorExtensions
    {
        /// <summary>
        /// Creates a local cell of the specified <paramref name="type"/>.
        /// </summary>
        public static ICell CreateCell(this ILGenerator il, Type type)
        {
            if (il == null) throw new ArgumentNullException(nameof(il));
            if (type == null) throw new ArgumentNullException(nameof(type));

            return new LocalCell(il.DeclareLocal(type));
        }

        /// <summary>
        /// Creates a local cell of the <typeparamref name="T"/> type.
        /// </summary>
        public static Cell<T> CreateCell<T>(this ILGenerator il)
        {
            if (il == null) throw new ArgumentNullException(nameof(il));

            return new Cell<T>(il.CreateCell(typeof(T)));
        }

        /// <summary>
        /// Creates a local cell for an lookahead stack instance.
        /// </summary>
        public static LookaheadStack CreateLookaheadStack(this ILGenerator il)
        {
            if (il == null) throw new ArgumentNullException(nameof(il));

            return new LookaheadStack(il.CreateCell(ReflectionInfo.LookaheadStack));
        }

        /// <summary>
        /// Creates a local cell for an lookahead stack item instance.
        /// </summary>
        public static LookaheadItem CreateLookaheadItem(this ILGenerator il)
        {
            if (il == null) throw new ArgumentNullException(nameof(il));

            return new LookaheadItem(il.CreateCell(ReflectionInfo.LookaheadTuple));
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> exception with the specified <paramref name="message"/>.
        /// </summary>
        internal static void ThrowInvalidOperationException(this ILGenerator il, string message)
        {
            il.Emit(OpCodes.Ldstr, message);
            il.Emit(OpCodes.Newobj, ReflectionInfo.InvalidOperationException_Ctor);
            il.Emit(OpCodes.Throw);
        }

        /// <summary>
        /// Throws an <see cref="ArgumentNullException"/> exception.
        /// </summary>
        internal static void ThrowArgumentNullException(this ILGenerator il, string paramName)
        {
            il.Emit(OpCodes.Ldstr, paramName);
            il.Emit(OpCodes.Newobj, ReflectionInfo.ArgumentNullException_Ctor);
            il.Emit(OpCodes.Throw);
        }

        /// <summary>
        /// Throws an <see cref="ArgumentOutOfRangeException"/> exception.
        /// </summary>
        internal static void ThrowArgumentOutOfRangeException(this ILGenerator il, string paramName, string message)
        {
            il.Emit(OpCodes.Ldstr, paramName);
            il.Emit(OpCodes.Ldstr, message);
            il.Emit(OpCodes.Newobj, ReflectionInfo.ArgumentOutOfRangeException_Ctor);
            il.Emit(OpCodes.Throw);
        }

        /// <summary>
        /// Marks the <paramref name="label"/> and defines another one.
        /// </summary>
        internal static Label MarkAndDefine(this ILGenerator il, Label label)
        {
            il.MarkLabel(label);
            return il.DefineLabel();
        }

        /// <summary>
        /// Generates an array with <paramref name="count"/> labels.
        /// </summary>
        internal static Label[] DefineLabels(this ILGenerator il, int count)
        {
            var labels = new Label[count];

            for (int i = 0; i < count; i++)
            {
                labels[i] = il.DefineLabel();
            }

            return labels;
        }

        /// <summary>
        /// Updates the <paramref name="array"/> cell with a new array instance.
        /// </summary>
        /// <param name="array">The target cell that expected to be defined with an array type.</param>
        /// <param name="loadLength">The action that loads a desired array length onto the evaluation stack.</param>
        internal static void CreateArray(this ILGenerator il, ICell array, Action loadLength)
        {
            array.Update(il, () =>
            {
                loadLength();
                il.Emit(OpCodes.Newarr, array.CellType.GetElementType());
            });
        }

        /// <summary>
        /// Jumps to the <paramref name="label"/>.
        /// </summary>
        internal static void GoTo(this ILGenerator il, Label label)
        {
            il.Emit(OpCodes.Br, label);
        }

        /// <summary>
        /// Generates logic to iterate through the <paramref name="array"/> value.
        /// </summary>
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

        /// <summary>
        /// Marks the <paramref name="labels"/> and executes an associated action for each item.
        /// </summary>
        internal static void Mark(this ILGenerator il, Label[] labels, Action<int> action)
        {
            for (int i = 0; i < labels.Length; i++)
            {
                il.MarkLabel(labels[i]);
                action(i);
            }
        }

        /// <summary>
        /// Increments the value generated by the <paramref name="loadValue"/> action.
        /// </summary>
        internal static void Increment(this ILGenerator il, Action loadValue)
        {
            loadValue();
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Add);
        }

        /// <summary>
        /// Decrements the value generated by the <paramref name="loadValue"/> action.
        /// </summary>
        internal static void Decrement(this ILGenerator il, Action loadValue)
        {
            loadValue();
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Sub);
        }

        /// <summary>
        /// Puts <c>true</c> onto the evaluation stack.
        /// </summary>
        internal static void LoadTrue(this ILGenerator il)
        {
            il.Emit(OpCodes.Ldc_I4_1);
        }

        /// <summary>
        /// Puts <c>false</c> onto the evaluation stack.
        /// </summary>
        internal static void LoadFalse(this ILGenerator il)
        {
            il.Emit(OpCodes.Ldc_I4_0);
        }

        /// <summary>
        /// Executes a specified method with no parameters.
        /// </summary>
        internal static void Execute(this ILGenerator il, MethodInfo method)
        {
            Execute(il, method, () => { });
        }

        /// <summary>
        /// Executes a specified method.
        /// </summary>
        internal static void Execute(this ILGenerator il, MethodInfo method, Action loadArgs)
        {
            if (method.IsStatic)
            {
                loadArgs();
                il.Emit(OpCodes.Call, method);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                loadArgs();
                il.Emit(OpCodes.Callvirt, method);
            }
        }

        /// <summary>
        /// Emits <see cref="OpCodes.Box"/> instruction when the <paramref name="type"/> represents a value type.
        /// </summary>
        internal static void ConvertToObject(this ILGenerator il, Type type)
        {
            if (type.IsValueType)
            {
                il.Emit(OpCodes.Box, type);
            }
        }

        /// <summary>
        /// Converts <see cref="object"/> on the top of the evaluation stack to the specifed <paramref name="type"/>.
        /// </summary>
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

        /// <summary>
        /// Emits <see cref="System.Diagnostics.Debug.WriteLine(string)" /> call.
        /// </summary>
        internal static void Debug(this ILGenerator il, string msg)
        {
            var mthd = typeof(System.Diagnostics.Debug).GetMethod("WriteLine", new[] { typeof(string) });
            il.Emit(OpCodes.Ldstr, msg);
            il.Emit(OpCodes.Call, mthd);
        }
    }
}
