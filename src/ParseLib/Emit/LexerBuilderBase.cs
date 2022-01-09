namespace ParseLib.Emit
{
    using System;
    using System.Collections.Generic;
    using System.Reflection.Emit;
    using ParseLib.Text;

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

        public LexerBuilderBase(ILGenerator il, ILexicalStates states, Cell<int> charCode, Cell<int> categories)
        {
            if (il == null) throw new ArgumentNullException(nameof(il));
            if (states == null) throw new ArgumentNullException(nameof(states));

            this.States = states;
            this.IL = il;

            this.selectStateLabel_CheckLowerBound = il.DefineLabel();
            this.selectStateLabel = il.DefineLabel();
            this.deadStateLabel = il.DefineLabel();
            this.stateLabels = il.DefineLabels(states.Count);

            this.CharCode = charCode;
            this.UnicodeCategories = categories;
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

        protected abstract void LoadState();
        protected abstract void MoveNext(LexicalState next);
        protected abstract void LoadCharCode(LexicalState current);

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

        protected abstract void CheckLoweBound();
        protected abstract void CheckUpperBound(Label isValid);
        protected abstract void CheckEndOfBuffer();

        protected abstract void CheckIsLookahead(Label onFalse);
        protected abstract void PopLookaheadState(bool success);
        protected abstract void PushLookaheadState(LexicalState current);

        protected abstract void CheckHasAcceptedToken(Label onFalse);
        protected abstract void SaveAcceptedToken(int tokenId);

        protected abstract void CompleteLastAcceptedToken();
        protected abstract void CompleteToken(int tokenId);
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
