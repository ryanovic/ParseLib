namespace ParseLib.Emit
{
    using System;
    using System.Reflection.Emit;

    /// <summary>
    /// Implements a wrapper for a local variable.
    /// </summary>
    internal sealed class LocalCell : ICell
    {
        private readonly LocalBuilder local;

        public LocalCell(LocalBuilder local)
        {
            this.local = local;
        }

        public Type CellType => local.LocalType;

        public void Load(ILGenerator il)
        {
            il.Emit(OpCodes.Ldloc, local);
        }

        public void LoadAddress(ILGenerator il)
        {
            il.Emit(OpCodes.Ldloca, local);
        }

        public void Update(ILGenerator il, Action loadValue)
        {
            loadValue();
            il.Emit(OpCodes.Stloc, local);
        }
    }
}
