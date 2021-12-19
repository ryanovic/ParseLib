namespace ParseLib.Emit
{
    using System;
    using System.Reflection.Emit;
    using System.Collections.Generic;
    using ParseLib.Text;

    /// <summary>
    /// Creates the lexical analyzer for the source and target specified.
    /// </summary>
    public sealed class LexerBuilder : LexerBuilderBase
    {
        public ILexerSource Source { get; }
        public ILexerTarget Target { get; }

        private readonly Cell<int> state;
        private readonly Cell<int> position;
        private readonly Cell<int> highSurrogate;
        private readonly Cell<int> acceptedPosition;
        private readonly Cell<int> acceptedTokenId;

        private readonly LookaheadStack lookaheadStack;
        private readonly LookaheadItem lookaheadItem;

        /// <remarks>
        /// When <paramref name="highSurrogate"/> is NULL charCode cell will be used instead(applies when a whole source is read in a single chunk).
        /// </remarks>
        public LexerBuilder(
            ILGenerator il,
            ILexicalStates states,
            ILexerSource source,
            ILexerTarget target,
            LookaheadStack lhStack,
            Cell<int> state,
            Cell<int> position,
            Cell<int> acceptedPosition,
            Cell<int> acceptedTokenId,
            Cell<int>? highSurrogate)
            : base(il, states)
        {
            this.Source = source ?? throw new ArgumentNullException(nameof(source));
            this.Target = target ?? throw new ArgumentNullException(nameof(target));

            this.state = state;
            this.position = position;
            this.acceptedPosition = acceptedPosition;
            this.acceptedTokenId = acceptedTokenId;
            this.highSurrogate = highSurrogate ?? CharCode;

            if (HasLookaheads)
            {
                this.lookaheadStack = lhStack ?? throw new ArgumentNullException(nameof(lhStack));
                this.lookaheadItem = il.CreateLookaheadItem();
            }
        }

        protected override void LoadState()
        {
            state.Load(IL);
        }

        protected override void CheckLoweBound()
        {
            if (Source.IsSequental)
            {
                var label = IL.DefineLabel();

                Source.CheckLowerBound(IL, position, label);
                IL.Emit(OpCodes.Ldc_I4_0);
                IL.Emit(OpCodes.Ret);

                IL.MarkLabel(label);
            }
        }

        protected override void CheckUpperBound(Label isValid)
        {
            Source.CheckUpperBound(IL, position, isValid);
        }

        protected override void CheckEndOfBuffer()
        {
            if (Source.IsSequental)
            {
                var label = IL.DefineLabel();

                Source.CheckIsLastChunk(IL, isLast: label);
                IL.Emit(OpCodes.Ldc_I4_1);
                IL.Emit(OpCodes.Ret);

                IL.MarkLabel(label);
            }
        }

        protected override void MoveNext(LexicalState next)
        {
            if (next.IsLowSurrogate)
            {
                UpdateHighSurrogate();
            }

            IncrementPosition();
            UpdateState(next);
            GoToState(next);
        }

        protected override void LoadCharCode(LexicalState current)
        {
            if (current.IsLowSurrogate)
            {
                Source.LoadCharCode(IL, position, highSurrogate);
            }
            else
            {
                Source.LoadCharCode(IL, position);
            }
        }

        protected override void CheckIsLookahead(Label onFalse)
        {
            lookaheadStack.CheckIsLookahead(IL, onFalse);
        }

        protected override void PopLookaheadState(bool success)
        {
            lookaheadStack.Pop(IL, lookaheadItem);
            lookaheadItem.Restore(IL, position, state, success);
        }

        protected override void PushLookaheadState(LexicalState current)
        {
            lookaheadStack.Push(IL, position, current);
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
            Target.CompleteToken(IL, acceptedTokenId);
            acceptedTokenId.Update(IL, -1);
            GoToSelectState(checkLowerBound: true);
        }

        protected override void CompleteToken(int tokenId)
        {
            Target.CompleteToken(IL, tokenId);
            acceptedTokenId.Update(IL, -1);
            GoToSelectState(checkLowerBound: false);
        }

        protected override void CompleteSource()
        {
            Target.CompleteSource(IL, Source);

            if (Source.IsSequental)
            {
                IL.LoadTrue();
            }

            IL.Emit(OpCodes.Ret);
        }

        private void IncrementPosition()
        {
            position.Increment(IL);
        }

        private void UpdateHighSurrogate()
        {
            highSurrogate.Update(IL, CharCode);
        }

        private void UpdateState(LexicalState next)
        {
            state.Update(IL, next?.Id ?? -1);
        }
    }
}
