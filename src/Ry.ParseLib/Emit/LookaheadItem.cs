namespace Ry.ParseLib.Emit
{
    using System;
    using System.Collections.Generic;
    using System.Reflection.Emit;

    /// <summary>
    /// Represents storage for a lookahead stack item with associated operations.
    /// </summary>
    public sealed class LookaheadItem
    {
        public ICell Item { get; }

        public LookaheadItem(ICell item)
        {
            this.Item = CellGuard.Check(nameof(item), item, ReflectionInfo.LookaheadTuple);
        }

        public void LoadPosition(ILGenerator il)
        {
            Item.LoadAddress(il);
            il.Emit(OpCodes.Ldfld, ReflectionInfo.LookaheadTuple_Item1);
        }

        public void LoadState(ILGenerator il, bool success)
        {
            Item.LoadAddress(il);
            il.Emit(OpCodes.Ldfld, success ? ReflectionInfo.LookaheadTuple_Item3 : ReflectionInfo.LookaheadTuple_Item2);
        }
    }
}
