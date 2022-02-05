namespace Ry.ParseLib.Emit
{
    using System;
    using System.Reflection.Emit;
    using System.Collections.Generic;
    using Ry.ParseLib.Text;

    /// <summary>
    /// Represents a lexical analyzer.
    /// </summary>
    public sealed class LexerBuilder : LexerBuilderBase
    {
        private readonly LexerSource source;
        private readonly ILexerTarget target;
        private readonly LookaheadStack lhStack;
        private readonly LookaheadItem lhItem;
        private readonly Cell<int> state;
        private readonly Cell<int> position;
        private readonly Cell<int> acceptedPosition;
        private readonly Cell<int> acceptedTokenId;

        public LexerBuilder(
            ILGenerator il,
            ILexicalStates states,
            ILexerTarget target,
            Cell<int> state,
            Cell<int> position)
            : base(il, states)
        {
            this.source = new LexerSource(il);
            this.target = target ?? throw new ArgumentNullException(nameof(target));
            this.state = state;
            this.position = position;
            this.acceptedPosition = il.CreateCell<int>();
            this.acceptedTokenId = il.CreateCell<int>();

            if (HasLookaheads)
            {
                this.lhStack = il.CreateLookaheadStack();
                this.lhItem = il.CreateLookaheadItem();
            }
        }

        public override void Build()
        {
            acceptedTokenId.Update(IL, -1);
            lhStack?.Initialize(IL);

            base.Build();
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
            source.LoadLength();
            IL.Emit(OpCodes.Blt_S, isValid);
        }

        protected override void MoveNext(LexicalState next)
        {
            position.Increment(IL);
            state.Update(IL, next.Id);
            GoToState(next);
        }

        protected override void LoadCharCode(LexicalState current)
        {
            if (current.IsLowSurrogate)
            {
                source.LoadCharCode(position, CharCode);
            }
            else
            {
                source.LoadCharCode(position);
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

        protected override void PushLookaheadState(LexicalState current)
        {
            lhStack.Push(IL, current, position);
        }

        protected override void CheckHasAcceptedToken(Label onFalse)
        {
            acceptedTokenId.Load(IL);
            IL.Emit(OpCodes.Ldc_I4_M1);
            IL.Emit(OpCodes.Beq, onFalse);
        }

        protected override void SaveAcceptedToken(int tokenId)
        {
            acceptedPosition.Update(IL, position);
            acceptedTokenId.Update(IL, tokenId);
        }

        protected override void CompleteLastAcceptedToken()
        {
            position.Update(IL, acceptedPosition);
            target.CompleteToken(IL, acceptedTokenId);
            acceptedTokenId.Update(IL, -1);
            GoToSelectState(checkLowerBound: false);
        }

        protected override void CompleteToken(int tokenId)
        {
            target.CompleteToken(IL, tokenId);
            acceptedTokenId.Update(IL, -1);
            GoToSelectState(checkLowerBound: false);
        }

        protected override void CompleteSource()
        {
            target.CompleteSource(IL, source);
            IL.Emit(OpCodes.Ret);
        }
    }
}
