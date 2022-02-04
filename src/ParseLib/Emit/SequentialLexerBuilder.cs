namespace ParseLib.Emit
{
    using System;
    using System.Collections.Generic;
    using System.Reflection.Emit;
    using ParseLib.Text;

    public sealed class SequentialLexerBuilder : LexerBuilderBase, ILexerSource
    {
        private readonly ILexerTarget target;
        private readonly LookaheadStack lhStack;
        private readonly LookaheadItem lhItem;
        private readonly Cell<int> state;
        private readonly Cell<int> position;
        private readonly Cell<int> index;
        private readonly Cell<int> acceptedPosition;
        private readonly Cell<int> acceptedTokenId;
        private readonly Cell<int> highSurrogate;

        public SequentialLexerBuilder(
            ILGenerator il,
            ILexicalStates states,
            ILexerTarget target,
            LookaheadStack lhStack,
            Cell<int> state,
            Cell<int> position,
            Cell<int> acceptedPosition,
            Cell<int> acceptedTokenId,
            Cell<int> highSurrogate)
            : base(il, states)
        {
            this.target = target ?? throw new ArgumentNullException(nameof(target));
            this.state = state;
            this.position = position;
            this.acceptedPosition = acceptedPosition;
            this.acceptedTokenId = acceptedTokenId;
            this.highSurrogate = highSurrogate;

            this.index = il.CreateCell<int>();

            if (HasLookaheads)
            {
                this.lhStack = lhStack ?? throw new ArgumentNullException(nameof(lhStack));
                this.lhItem = il.CreateLookaheadItem();
            }
        }

        public void LoadStartPosition()
        {
            IL.Emit(OpCodes.Ldarg_1);
        }

        public void LoadEndPosition()
        {
            LoadStartPosition();
            LoadLength();
            IL.Emit(OpCodes.Add);
        }

        public void LoadCharCode(Cell<int> bufferIndex)
        {
            IL.Emit(OpCodes.Ldarga_S, 2);
            bufferIndex.Load(IL);
            IL.Emit(OpCodes.Call, ReflectionInfo.CharSpan_Item_Get);
            IL.Emit(OpCodes.Ldind_U2);
        }

        public void LoadLength()
        {
            IL.Emit(OpCodes.Ldarga_S, 2);
            IL.Emit(OpCodes.Call, ReflectionInfo.CharSpan_Length_Get);
        }

        public void LoadIsFinal()
        {
            IL.Emit(OpCodes.Ldarg_3);
        }

        protected override void CheckLowerBound()
        {
            var label = IL.DefineLabel();

            index.Update(IL, LoadIndex);
            index.Load(IL);
            IL.Emit(OpCodes.Ldc_I4_0);
            IL.Emit(OpCodes.Bge_S, label);

            IL.LoadFalse();
            IL.Emit(OpCodes.Ret);

            IL.MarkLabel(label);
        }

        protected override void CheckUpperBound(Label isValid)
        {
            index.Load(IL);
            LoadLength();
            IL.Emit(OpCodes.Blt_S, isValid);
            CompleteChunk();
        }

        protected override void LoadCharCode(LexicalState current)
        {
            if (current.IsLowSurrogate)
            {
                highSurrogate.Load(IL);
            }

            LoadCharCode(index);

            if (current.IsLowSurrogate)
            {
                IL.Emit(OpCodes.Call, ReflectionInfo.Char_ToUtf32);
            }
        }

        protected override void LoadState()
        {
            state.Load(IL);
        }

        protected override void MoveNext(LexicalState next)
        {
            if (next.IsLowSurrogate)
            {
                highSurrogate.Update(IL, CharCode);
            }

            index.Increment(IL);
            state.Update(IL, next?.Id ?? -1);
            GoToState(next);
        }

        protected override void PushLookaheadState(LexicalState current)
        {
            lhStack.Push(IL, current, LoadPosition);
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

        protected override void SaveAcceptedToken(int tokenId)
        {
            acceptedPosition.Update(IL, LoadPosition);
            acceptedTokenId.Update(IL, tokenId);
        }

        protected override void CheckHasAcceptedToken(Label onFalse)
        {
            acceptedTokenId.Load(IL);
            IL.Emit(OpCodes.Ldc_I4_M1);
            IL.Emit(OpCodes.Beq_S, onFalse);
        }

        protected override void CompleteLastAcceptedToken()
        {
            position.Update(IL, acceptedPosition);
            target.CompleteToken(IL, acceptedTokenId);
            acceptedTokenId.Update(IL, -1);
            GoToSelectState(checkLowerBound: true);
        }

        protected override void CompleteToken(int tokenId)
        {
            position.Update(IL, LoadPosition);
            target.CompleteToken(IL, tokenId);
            acceptedTokenId.Update(IL, -1);
            GoToSelectState(checkLowerBound: false);
        }

        protected override void CompleteSource()
        {
            position.Update(IL, LoadPosition);
            target.CompleteSource(IL, this);
            IL.LoadTrue();
            IL.Emit(OpCodes.Ret);
        }

        private void CompleteChunk()
        {
            var label = IL.DefineLabel();

            LoadIsFinal();
            IL.Emit(OpCodes.Brtrue_S, label);

            position.Update(IL, LoadPosition);
            IL.LoadTrue();
            IL.Emit(OpCodes.Ret);

            IL.MarkLabel(label);
        }

        private void LoadIndex()
        {
            position.Load(IL);
            LoadStartPosition();
            IL.Emit(OpCodes.Sub);
        }

        private void LoadPosition()
        {
            LoadStartPosition();
            index.Load(IL);
            IL.Emit(OpCodes.Add);
        }
    }
}
