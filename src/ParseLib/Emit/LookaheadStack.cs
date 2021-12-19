namespace ParseLib.Emit
{
    using System;
    using System.Collections.Generic;
    using System.Reflection.Emit;
    using ParseLib.Text;

    /// <summary>
    /// Defines operations to work with lookahed stack.
    /// </summary>
    public sealed class LookaheadStack
    {
        public ICell Stack { get; }

        public LookaheadStack(ICell stack)
        {
            this.Stack = CellGuard.Check(nameof(stack), stack, ReflectionInfo.LookaheadStack);
        }

        public void Initialize(ILGenerator il)
        {
            Stack.Update(il, () => il.Emit(OpCodes.Newobj, ReflectionInfo.LookaheadStack_Ctor));
        }

        public void CheckIsLookahead(ILGenerator il, Label onFalse)
        {
            Stack.Load(il);
            il.Emit(OpCodes.Callvirt, ReflectionInfo.LookaheadStack_Count_Get);
            il.Emit(OpCodes.Brfalse, onFalse);
        }

        public void Pop(ILGenerator il, LookaheadItem item)
        {
            item.Item.Update(il, () =>
            {
                Stack.Load(il);
                il.Emit(OpCodes.Callvirt, ReflectionInfo.LookaheadStack_Pop);
            });
        }

        public void Push(ILGenerator il, Cell<int> position, LexicalState state)
        {
            Stack.Load(il);
            position.Load(il);
            LoadState(il, state.OnFalse);
            LoadState(il, state.OnTrue);
            il.Emit(OpCodes.Newobj, ReflectionInfo.LookaheadTuple_Ctor);
            il.Emit(OpCodes.Callvirt, ReflectionInfo.LookaheadStack_Push);
        }

        private void LoadState(ILGenerator il, LexicalState state)
        {
            if (state != null)
            {
                il.Emit(OpCodes.Ldc_I4, state.Id);
            }
            else
            {
                il.Emit(OpCodes.Ldc_I4_M1);
            }
        }
    }
}
