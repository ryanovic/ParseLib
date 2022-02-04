namespace ParseLib.Emit
{
    using System;
    using System.Collections.Generic;
    using System.Reflection.Emit;
    using ParseLib.Text;

    /// <summary>
    /// Implements a dynamic method to match a specified regular expression.
    /// </summary>
    internal sealed class RexEvaluatorBuilder : LexerBuilderBase
    {
        private readonly LookaheadStack lhStack;
        private readonly LookaheadItem lhItem;

        private readonly Cell<int> position;
        private readonly Cell<int> state;
        private readonly Cell<int> acceptedPosition;

        public static RexEvaluator CreateDelegate(RexNode expr, bool lazy, bool ignoreCase)
        {
            var method = new DynamicMethod("Read", typeof(int), new[] { typeof(ReadOnlySpan<char>) });
            var il = method.GetILGenerator();
            var stateBuilder = new LexicalStatesBuilder(ignoreCase);
            var root = stateBuilder.CreateStates(0, expr, lazy);
            var builder = new RexEvaluatorBuilder(il, stateBuilder);
            builder.Build(root);
            return (RexEvaluator)method.CreateDelegate(typeof(RexEvaluator));
        }

        public RexEvaluatorBuilder(ILGenerator il, LexicalStatesBuilder stateBuilder) : base(il, stateBuilder)
        {
            this.position = il.CreateCell<int>();
            this.state = il.CreateCell<int>();
            this.acceptedPosition = il.CreateCell<int>();

            if (HasLookaheads)
            {
                this.lhStack = il.CreateLookaheadStack();
                this.lhItem = il.CreateLookaheadItem();
            }
        }

        public void Build(LexicalState root)
        {
            state.Update(IL, root.Id);
            position.Update(IL, 0);
            acceptedPosition.Update(IL, -1);
            lhStack?.Initialize(IL);

            Build();
        }

        protected override void LoadState()
        {
            state.Load(IL);
        }

        protected override void CheckLowerBound()
        {
        }

        protected override void CheckUpperBound(Label isValid)
        {
            position.Load(IL);
            LoadLength();
            IL.Emit(OpCodes.Blt_S, isValid);
        }

        protected override void MoveNext(LexicalState next)
        {
            position.Increment(IL);
            GoToState(next);
        }

        protected override void LoadCharCode(LexicalState state)
        {
            if (state.IsLowSurrogate)
            {
                CharCode.Load(IL);
            }

            IL.Emit(OpCodes.Ldarga_S, 0);
            position.Load(IL);
            IL.Emit(OpCodes.Call, ReflectionInfo.ReadOnlyCharSpan_Item_Get);
            IL.Emit(OpCodes.Ldind_U2);

            if (state.IsLowSurrogate)
            {
                IL.Emit(OpCodes.Call, ReflectionInfo.Char_ToUtf32);
            }
        }

        protected override void CheckIsLookahead(Label onFalse)
        {
            lhStack.CheckIsLookahead(IL, onFalse);
        }

        protected override void PopLookaheadState(bool success)
        {
            lhStack.Pop(IL, lhItem);
            position.Update(IL, () => lhItem.LoadPosition(IL));
            state.Update(IL, () => lhItem.LoadState(IL, success));
        }

        protected override void PushLookaheadState(LexicalState state)
        {
            lhStack.Push(IL, state, position);
        }

        protected override void CheckHasAcceptedToken(Label onFalse)
        {
            acceptedPosition.Load(IL);
            IL.Emit(OpCodes.Ldc_I4_M1);
            IL.Emit(OpCodes.Beq, onFalse);
        }

        protected override void SaveAcceptedToken(int tokenId)
        {
            acceptedPosition.Update(IL, position);
        }

        protected override void CompleteLastAcceptedToken()
        {
            acceptedPosition.Load(IL);
            IL.Emit(OpCodes.Ret);
        }

        protected override void CompleteToken(int tokenId)
        {
            position.Load(IL);
            IL.Emit(OpCodes.Ret);
        }

        protected override void CompleteSource()
        {
            IL.Emit(OpCodes.Ldc_I4_M1);
            IL.Emit(OpCodes.Ret);
        }


        private void LoadLength()
        {
            IL.Emit(OpCodes.Ldarga_S, 0);
            IL.Emit(OpCodes.Call, ReflectionInfo.ReadOnlyCharSpan_Length_Get);
        }
    }
}
