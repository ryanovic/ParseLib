namespace ParseLib.Emit
{
    using System;
    using System.Collections.Generic;
    using System.Reflection.Emit;

    public sealed class LookaheadItem
    {
        public ICell Item { get; }

        public LookaheadItem(ICell item)
        {
            this.Item = CellGuard.Check(nameof(item), item, ReflectionInfo.LookaheadTuple);
        }

        public void UpdatePosition(ILGenerator il, Cell<int> position)
        {
            position.Update(il, () =>
            {
                Item.LoadAddress(il);
                il.Emit(OpCodes.Ldfld, ReflectionInfo.LookaheadTuple_Item1);
            });
        }

        public void UpdateStateOnFailure(ILGenerator il, Cell<int> state)
        {
            state.Update(il, () =>
            {
                Item.LoadAddress(il);
                il.Emit(OpCodes.Ldfld, ReflectionInfo.LookaheadTuple_Item2);
            });
        }

        public void UpdateStateOnSuccess(ILGenerator il, Cell<int> state)
        {
            state.Update(il, () =>
            {
                Item.LoadAddress(il);
                il.Emit(OpCodes.Ldfld, ReflectionInfo.LookaheadTuple_Item3);
            });
        }

        public void Restore(ILGenerator il, Cell<int> position, Cell<int> state, bool success)
        {
            UpdatePosition(il, position);

            if (success)
            {
                UpdateStateOnSuccess(il, state);
            }
            else
            {
                UpdateStateOnFailure(il, state);
            }
        }
    }
}
