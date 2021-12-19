namespace ParseLib.Emit
{
    using System;
    using System.Collections.Generic;
    using System.Reflection.Emit;
    using ParseLib.Text;

    /// <summary>
    /// Defines the base lexical analyzer builder.
    /// </summary>
    public abstract class LexerBuilderBase
    {
        private readonly Label selectStateLabel_CheckLowerBound;
        private readonly Label selectStateLabel;
        private readonly Label deadStateLabel;
        private readonly Label[] stateLabels;

        public ILGenerator IL { get; }
        public ILexicalStates States { get; }
        public bool HasLookaheads => States.HasLookaheads;

        protected Cell<int> CharCode { get; }
        protected Cell<int> UnicodeCategories { get; }

        public LexerBuilderBase(ILGenerator il, ILexicalStates states)
        {
            if (il == null) throw new ArgumentNullException(nameof(il));
            if (states == null) throw new ArgumentNullException(nameof(states));

            this.States = states;
            this.IL = il;

            this.selectStateLabel_CheckLowerBound = il.DefineLabel();
            this.selectStateLabel = il.DefineLabel();
            this.deadStateLabel = il.DefineLabel();
            this.stateLabels = il.DefineLabels(states.Count);

            this.CharCode = il.CreateCell<int>();
            this.UnicodeCategories = il.CreateCell<int>();
        }

        public virtual void Build()
        {
            IL.MarkLabel(selectStateLabel_CheckLowerBound);
            CheckLoweBound();

            IL.MarkLabel(selectStateLabel);
            LoadState();
            IL.Emit(OpCodes.Switch, stateLabels);

            IL.MarkLabel(deadStateLabel);
            HandleDeadState();

            for (int i = 0; i < stateLabels.Length; i++)
            {
                IL.MarkLabel(stateLabels[i]);
                HandleState(States[i]);
            }
        }

        protected virtual void HandleDeadState()
        {
            var label = IL.DefineLabel();

            if (HasLookaheads)
            {
                CheckIsLookahead(onFalse: label);
                PopLookaheadState(success: false);
                GoToSelectState(checkLowerBound: true);
                label = IL.MarkAndDefine(label);
            }

            CheckHasAcceptedToken(onFalse: label);
            CompleteLastAcceptedToken();

            IL.MarkLabel(label);
            CompleteSource();
        }

        protected virtual void HandleState(LexicalState current)
        {
            if (current.IsLookaheadFinal)
            {
                PopLookaheadState(success: true);
                GoToSelectState(checkLowerBound: true);
                return;
            }

            var label = IL.DefineLabel();

            CheckUpperBound(isValid: label);
            CheckEndOfBuffer();
            HandleStateTransition(current, next: null);

            IL.MarkLabel(label);
            UpdateCharCode(current);
            HandleStateTransitions(current);
        }

        protected virtual void HandleStateTransitions(LexicalState current)
        {
            (var intervals, var defaultLabel) = SearchInterval.CreateIntervals(IL, current.Ranges.Length);

            foreach (var interval in intervals)
            {
                var item = current.Ranges[interval.Middle];

                IL.MarkLabel(interval.Label);

                if (interval.IsSingle && item.Range.Length == 1)
                {
                    CharCode.Load(IL);
                    IL.Emit(OpCodes.Ldc_I4, item.Range.From);
                    IL.Emit(OpCodes.Bne_Un, defaultLabel);
                }
                else
                {
                    if (!IsLowerBound(item.Range))
                    {
                        CharCode.Load(IL);
                        IL.Emit(OpCodes.Ldc_I4, item.Range.From);
                        IL.Emit(OpCodes.Blt, interval.Left);
                    }

                    if (!IsUpperBound(item.Range))
                    {
                        CharCode.Load(IL);
                        IL.Emit(OpCodes.Ldc_I4, item.Range.To);
                        IL.Emit(OpCodes.Bgt, interval.Right);
                    }
                }

                HandleStateTransition(current, item.Default, item.Categories);
            }

            IL.MarkLabel(defaultLabel);
            HandleStateTransition(current, current.Default, current.Categories);
        }

        protected virtual void HandleStateTransition(LexicalState current, LexicalState nextDefault, CategoryTransition[] nextByCategories)
        {
            var label = IL.DefineLabel();

            if (nextByCategories.Length > 0)
            {
                UpdateUnicodeCategories(current);
            }

            for (int i = 0; i < nextByCategories.Length; i++)
            {
                label = IL.MarkAndDefine(label);
                var next = nextByCategories[i];

                UnicodeCategories.Load(IL);
                IL.Emit(OpCodes.Ldc_I4, next.Category.Set);
                IL.Emit(OpCodes.And);
                IL.Emit(OpCodes.Brfalse, label);

                HandleStateTransition(current, next.State);
            }

            IL.MarkLabel(label);
            HandleStateTransition(current, nextDefault);
        }

        protected virtual void HandleStateTransition(LexicalState current, LexicalState next)
        {
            if (current.IsLookaheadStart)
            {
                if (next == null)
                {
                    GoToState(current.OnFalse);
                    return;
                }
                if (next.IsLookaheadFinal)
                {
                    GoToState(current.OnTrue);
                    return;
                }
                else
                {
                    PushLookaheadState(current);
                }
            }
            else if (current.IsFinal)
            {
                if (next == null)
                {
                    CompleteToken(current.AcceptId);
                    return;
                }
                else if (!next.IsFinal)
                {
                    SaveAcceptedToken(current.AcceptId);
                }
            }
            else if (next == null)
            {
                GoToDeadState();
                return;
            }

            MoveNext(next);
        }

        protected virtual void GoToState(LexicalState next)
        {
            if (next != null)
            {
                IL.GoTo(stateLabels[next.Id]);
            }
            else
            {
                IL.GoTo(deadStateLabel);
            }
        }

        protected virtual void GoToSelectState(bool checkLowerBound)
        {
            IL.GoTo(checkLowerBound ? selectStateLabel_CheckLowerBound : selectStateLabel);
        }

        protected virtual void GoToDeadState()
        {
            IL.GoTo(deadStateLabel);
        }

        /// <summary>
        /// Loads current state and puts it on the stack.
        /// </summary>
        protected abstract void LoadState();

        /// <summary>
        /// Performs transistion to the next state.
        /// </summary>
        /// <param name="next"></param>
        protected abstract void MoveNext(LexicalState next);

        /// <summary>
        /// Loads current character code and puts it on the stack.
        /// </summary>
        protected abstract void LoadCharCode(LexicalState current);

        /// <summary>
        /// Loads an uncicode category corresponded to the current char and puts it on the stack.
        /// </summary>
        protected virtual void LoadUnicodeCategory(LexicalState current)
        {
            if (ReflectionInfo.Char_GetCategoryByInt32 != null)
            {
                CharCode.Load(IL);
                IL.Emit(OpCodes.Call, ReflectionInfo.Char_GetCategoryByInt32);
            }
            else if (current.IsLowSurrogate)
            {
                CharCode.Load(IL);
                IL.Emit(OpCodes.Call, ReflectionInfo.Char_FromUtf32);
                IL.Emit(OpCodes.Ldc_I4_0);
                IL.Emit(OpCodes.Call, ReflectionInfo.Char_GetCategoryByStr);
            }
            else
            {
                CharCode.Load(IL);
                IL.Emit(OpCodes.Call, ReflectionInfo.Char_GetCategoryByChar);
            }
        }

        /// <summary>
        /// Verifies lower bound of the current buffer.
        /// </summary>
        protected abstract void CheckLoweBound();

        /// <summary>
        /// Verifies upper bound of the current buffer.
        /// </summary>
        protected abstract void CheckUpperBound(Label isValid);

        /// <summary>
        /// Verifies current chunk is completed.
        /// </summary>
        protected abstract void CheckEndOfBuffer();

        /// <summary>
        /// Checks if lookahead sub-expression is currently processed.
        /// </summary>
        protected abstract void CheckIsLookahead(Label onFalse);

        /// <summary>
        /// Restores state and position are corresponded to a lookahead sub-expression outcome.
        /// </summary>
        protected abstract void PopLookaheadState(bool success);

        /// <summary>
        /// Saves lookahead sub-expession return info when processing is started. 
        /// </summary>
        protected abstract void PushLookaheadState(LexicalState current);

        /// <summary>
        /// Checks if any token has been accepted.  
        /// </summary>
        /// <param name="onFalse"></param>
        protected abstract void CheckHasAcceptedToken(Label onFalse);

        /// <summary>
        /// Saves specified token as accepted.
        /// </summary>
        protected abstract void SaveAcceptedToken(int tokenId);

        /// <summary>
        /// Completes token was accepted.
        /// </summary>
        protected abstract void CompleteLastAcceptedToken();

        /// <summary>
        /// Completes token specified.
        /// </summary>
        protected abstract void CompleteToken(int tokenId);

        /// <summary>
        /// Completes source processing.
        /// </summary>
        protected abstract void CompleteSource();

        private void UpdateCharCode(LexicalState current)
        {
            CharCode.Update(IL, () => LoadCharCode(current));
        }

        private void UpdateUnicodeCategories(LexicalState current)
        {
            UnicodeCategories.Update(IL, () =>
            {
                IL.Emit(OpCodes.Ldc_I4_1);
                LoadUnicodeCategory(current);
                IL.Emit(OpCodes.Shl);
            });
        }

        private static bool IsLowerBound(UnicodeRange range)
        {
            return range.From == 0 || range.From == UnicodeRange.ExtendedStart;
        }

        private static bool IsUpperBound(UnicodeRange range)
        {
            return range.To == UnicodeRange.ExtendedStart - 1 || range.To == UnicodeRange.Max;
        }
    }
}
