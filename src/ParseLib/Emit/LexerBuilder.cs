﻿namespace ParseLib.Emit
{
    using System;
    using System.Reflection.Emit;
    using System.Collections.Generic;
    using ParseLib.Text;

    /// <summary>
    /// Implements a lexical analyzer that accepts <see cref="ILexerSource"/> as a source and <see cref="ILexerTarget"/> as a target interfaces.
    /// </summary>
    public sealed class LexerBuilder : LexerBuilderBase, ILexerSource
    {
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

        public void LoadStartPosition()
        {
            IL.Emit(OpCodes.Ldc_I4_0);
        }

        public void LoadEndPosition()
        {
            LoadLength();
        }

        public void LoadCharCode(Cell<int> index)
        {
            IL.Emit(OpCodes.Ldarga_S, 1);
            index.Load(IL);
#if NET6_0
            IL.Emit(OpCodes.Call, ReflectionInfo.ReadOnlyCharSpan_Item_Get);
#else
            // https://github.com/dotnet/runtime/issues/64799
            // I can't use the ReadOnlyCharSpan_Item_Get metadata for frameworks prior to .NET 6.
            // Luckily, this trick with CharSpan_Item_Get produces comparable IL,
            // so I still can keep the same interface for all versions.
            IL.Emit(OpCodes.Call, ReflectionInfo.CharSpan_Item_Get);
#endif
            IL.Emit(OpCodes.Ldind_U2);
        }

        public void LoadLength()
        {
            IL.Emit(OpCodes.Ldarga_S, 1);
            IL.Emit(OpCodes.Call, ReflectionInfo.ReadOnlyCharSpan_Length_Get);
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
            LoadLength();
            IL.Emit(OpCodes.Blt_S, isValid);
        }

        protected override void MoveNext(LexicalState next)
        {
            position.Increment(IL);
            state.Update(IL, next?.Id ?? -1);
            GoToState(next);
        }

        protected override void LoadCharCode(LexicalState current)
        {
            if (current.IsLowSurrogate)
            {
                CharCode.Load(IL);
            }

            LoadCharCode(position);

            if (current.IsLowSurrogate)
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
            target.CompleteSource(IL, this);
            IL.Emit(OpCodes.Ret);
        }
    }
}
