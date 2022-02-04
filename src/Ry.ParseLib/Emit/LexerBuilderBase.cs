namespace Ry.ParseLib.Emit
{
    using System;
    using System.Collections.Generic;
    using System.Reflection.Emit;
    using Ry.ParseLib.Text;

    /// <summary>
    /// Implements the basic operations for generating a lexical analyzer.
    /// </summary>
    public abstract class LexerBuilderBase
    {
        private readonly Label selectStateLabel_CheckLowerBound;
        private readonly Label selectStateLabel;
        private readonly Label deadStateLabel;
        private readonly Label[] stateLabels;

        /// <summary>
        /// The IL generator.
        /// </summary>
        public ILGenerator IL { get; }

        /// <summary>
        /// The collection of lexer states;
        /// </summary>
        public ILexicalStates States { get; }

        /// <summary>
        /// Indicates whether a lexer contains lookaheads states.
        /// </summary>
        public bool HasLookaheads => States.HasLookaheads;

        /// <summary>
        /// The cell for a current character code.
        /// </summary>
        protected Cell<int> CharCode { get; }

        /// <summary>
        /// The cell for a current character category.
        /// </summary>
        protected Cell<int> UnicodeCategories { get; }

        /// <summary>
        /// Create an instance of the <see cref="LexerBuilderBase"/> class.
        /// </summary>
        /// <param name="il">The IL geneartor.</param>
        /// <param name="states">The collection of lexical analyzer states.</param>
        /// <param name="charCode">The cell in which a current character code is stored.</param>
        /// <param name="categories">The cell in which a current character category is stored.</param>
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

        /// <summary>
        /// Generates a lexical analyzer.
        /// </summary>
        public virtual void Build()
        {
            IL.MarkLabel(selectStateLabel_CheckLowerBound);
            CheckLowerBound();

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
                // If a dead state is reached during lookahead processing then
                // restore the position and continue execution from the state corresponding the negative lookahead condition.
                CheckIsLookahead(onFalse: label);
                PopLookaheadState(success: false);
                GoToSelectState(checkLowerBound: true);
                label = IL.MarkAndDefine(label);
            }

            // Check if an accepted state has been reached before the point.
            CheckHasAcceptedToken(onFalse: label);
            CompleteLastAcceptedToken();

            // No more characters can be accepted.
            IL.MarkLabel(label);
            CompleteSource();
        }

        protected virtual void HandleState(LexicalState current)
        {
            if (current.IsLookaheadFinal)
            {
                // Complete lookahead as soon as a final state is discovered.
                PopLookaheadState(success: true);
                GoToSelectState(checkLowerBound: true);
                return;
            }

            var label = IL.DefineLabel();

            CheckUpperBound(isValid: label);
            HandleStateTransition(current, next: null);

            // Handle the next character.
            IL.MarkLabel(label);
            UpdateCharCode(current);
            HandleStateTransitions(current);
        }

        protected virtual void HandleStateTransitions(LexicalState current)
        {
            // Perform a binary search over the the available intervals.
            (var intervals, var defaultLabel) = SearchInterval.CreateIntervals(IL, current.Ranges.Length);

            foreach (var interval in intervals)
            {
                // [left] ... [item] ... [right]
                var item = current.Ranges[interval.Middle];

                IL.MarkLabel(interval.Label);

                // The interval contains a single character code.
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
                        // Go left if the character code less than the lower bound of the current range. 
                        CharCode.Load(IL);
                        IL.Emit(OpCodes.Ldc_I4, item.Range.From);
                        IL.Emit(OpCodes.Blt, interval.Left);
                    }

                    if (!IsUpperBound(item.Range))
                    {
                        // Go right if the character code greater than the upper bound of the current range. 
                        CharCode.Load(IL);
                        IL.Emit(OpCodes.Ldc_I4, item.Range.To);
                        IL.Emit(OpCodes.Bgt, interval.Right);
                    }
                }

                HandleStateTransition(current, item.Default, item.Categories);
            }

            // No matches.
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
        /// Loads a current state of the lexer and puts its value onto the evaluation stack.
        /// </summary>
        protected abstract void LoadState();

        /// <summary>
        /// Moves the lexer to the <paramref name="next"/> state.
        /// </summary>
        protected abstract void MoveNext(LexicalState next);

        /// <summary>
        /// Loads a character code correspoinding the <paramref name="current"/> state of the lexer.
        /// </summary>
        protected abstract void LoadCharCode(LexicalState current);

        /// <summary>
        /// Loads a character category correspoinding the <paramref name="current"/> state of the lexer.
        /// </summary>
        /// <remarks>The <see cref="CharCode"/> property is expected to be updated with the recent value at this point.</remarks>
        protected virtual void LoadUnicodeCategory(LexicalState current)
        {
#if NETSTANDARD2_0
            if (current.IsLowSurrogate)
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
#else
            CharCode.Load(IL);
            IL.Emit(OpCodes.Call, ReflectionInfo.Char_GetCategoryByInt32);
#endif            
        }

        /// <summary>
        /// Ensures that the current position is equal to or greater than the buffer start.
        /// </summary>
        protected abstract void CheckLowerBound();

        /// <summary>
        /// Jumps to the <paramref name="isValid"/> label if the poisition is within the bounds of the buffer.
        /// </summary>
        protected abstract void CheckUpperBound(Label isValid);

        /// <summary>
        /// Jumps to the <paramref name="onFalse"/> label if a lookahead sub-expression is currently being evaluated.
        /// </summary>
        protected abstract void CheckIsLookahead(Label onFalse);

        /// <summary>
        /// Completes a lookahead sub-expression and resets the position and state according to the <paramref name="success"/> argument.
        /// </summary>
        protected abstract void PopLookaheadState(bool success);

        /// <summary>
        /// Saves the current state and position and begins evaluation of a lookahead sub-expression.
        /// </summary>
        protected abstract void PushLookaheadState(LexicalState current);

        /// <summary>
        /// Jumps to the <paramref name="onFalse"/> label if no tokens were recognized before the position.
        /// </summary>
        protected abstract void CheckHasAcceptedToken(Label onFalse);

        /// <summary>
        /// Saves the current possition as accepted for the <paramref name="tokenId"/>.
        /// </summary>
        protected abstract void SaveAcceptedToken(int tokenId);

        /// <summary>
        /// Restores the state and position to the last accepted token and completes it.
        /// </summary>
        protected abstract void CompleteLastAcceptedToken();

        /// <summary>
        /// Completes the token defined by <paramref name="tokenId"/> argument.
        /// </summary>
        protected abstract void CompleteToken(int tokenId);

        /// <summary>
        /// Completes the source evaluation.
        /// </summary>
        protected abstract void CompleteSource();

        /// <summary>
        /// Updates the <see cref="CharCode"/> cell with a character code corresponding to the current position.
        /// </summary>
        private void UpdateCharCode(LexicalState current)
        {
            CharCode.Update(IL, () => LoadCharCode(current));
        }

        /// <summary>
        /// Updates the <see cref="UnicodeCategories"/> cell with a Unicode category corresponding to the current position.
        /// </summary>
        /// <remarks>The <see cref="CharCode"/> property is expected to be updated with the recent value at this point.</remarks>
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
