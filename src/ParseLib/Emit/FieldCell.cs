namespace ParseLib.Emit
{
    using System;
    using System.Reflection.Emit;

    /// <summary>
    /// Implements a wrapper for a type field.
    /// </summary>
    internal sealed class FieldCell : ICell
    {
        private readonly FieldBuilder field;

        public FieldCell(FieldBuilder field)
        {
            this.field = field;
        }

        public Type CellType => field.FieldType;

        public void Load(ILGenerator il)
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, field);
        }

        public void LoadAddress(ILGenerator il)
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldflda, field);
        }

        public void Update(ILGenerator il, Action loadValue)
        {
            il.Emit(OpCodes.Ldarg_0);
            loadValue();
            il.Emit(OpCodes.Stfld, field);
        }
    }
}
