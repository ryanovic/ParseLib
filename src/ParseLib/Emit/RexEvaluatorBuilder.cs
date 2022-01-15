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
            var method = new DynamicMethod("Read", typeof(int), new[] { typeof(string), typeof(int), typeof(int) });
            var il = method.GetILGenerator();
            var stateBuilder = new LexicalStatesBuilder(ignoreCase);
            var root = stateBuilder.CreateStates(0, expr, lazy);
            var builder = new RexEvaluatorBuilder(il, stateBuilder, il.CreateCell<int>(), il.CreateCell<int>());
            builder.Build(root);
            return (RexEvaluator)method.CreateDelegate(typeof(RexEvaluator));
        }

        public RexEvaluatorBuilder(ILGenerator il, LexicalStatesBuilder stateBuilder, Cell<int> charCode, Cell<int> categories) : base(il, stateBuilder, charCode, categories)
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
            var label = IL.DefineLabel();
            ValidateContentArgument(label);

            label = IL.MarkAndDefine(label);
            ValidateOffsetArgument(label);

            label = IL.MarkAndDefine(label);
            ValidateLengthArgument(label);

            IL.MarkLabel(label);
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

        protected override void CheckLoweBound()
        {
        }

        protected override void CheckUpperBound(Label isValid)
        {
            position.Load(IL);
            LoadLength();
            IL.Emit(OpCodes.Blt, isValid);
        }

        protected override void CheckEndOfSource()
        {
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

            LoadIndex();
            IL.Emit(OpCodes.Callvirt, ReflectionInfo.String_Get);

            if (state.IsLowSurrogate)
            {
                IL.Emit(OpCodes.Call, ReflectionInfo.Char_ToUtf32);
            }
        }

        protected override void LoadUnicodeCategory(LexicalState state)
        {
            if (ReflectionInfo.Char_GetCategoryByInt32 != null)
            {
                CharCode.Load(IL);
                IL.Emit(OpCodes.Call, ReflectionInfo.Char_GetCategoryByInt32);
            }
            else if (state.IsLowSurrogate)
            {
                IL.Decrement(() => LoadIndex());
                IL.Emit(OpCodes.Call, ReflectionInfo.Char_GetCategoryByStr);
            }
            else
            {
                CharCode.Load(IL);
                IL.Emit(OpCodes.Call, ReflectionInfo.Char_GetCategoryByChar);
            }
        }

        protected override void CheckIsLookahead(Label onFalse)
        {
            lhStack.CheckIsLookahead(IL, onFalse);
        }

        protected override void PopLookaheadState(bool success)
        {
            lhStack.Pop(IL, lhItem);
            lhItem.Restore(IL, position, state, success);
        }

        protected override void PushLookaheadState(LexicalState state)
        {
            lhStack.Push(IL, position, state);
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

        private void LoadIndex()
        {
            LoadContent();
            LoadOffset();
            position.Load(IL);
            IL.Emit(OpCodes.Add);
        }

        private void LoadContent()
        {
            IL.Emit(OpCodes.Ldarg_0);
        }

        private void LoadOffset()
        {
            IL.Emit(OpCodes.Ldarg_1);
        }

        private void LoadLength()
        {
            IL.Emit(OpCodes.Ldarg_2);
        }

        private void ValidateContentArgument(Label valid)
        {
            LoadContent();
            IL.Emit(OpCodes.Brtrue, valid);
            IL.ThrowArgumentNullException("content");
        }

        private void ValidateOffsetArgument(Label valid)
        {
            LoadOffset();
            IL.Emit(OpCodes.Ldc_I4_0);
            IL.Emit(OpCodes.Bge, valid);
            IL.ThrowArgumentOutOfRangeException("offset", Errors.OffsetOutOfRange());
        }

        private void ValidateLengthArgument(Label valid)
        {
            LoadOffset();
            LoadLength();
            IL.Emit(OpCodes.Add);
            LoadContent();
            IL.Emit(OpCodes.Callvirt, ReflectionInfo.String_Length_Get);
            IL.Emit(OpCodes.Ble, valid);
            IL.ThrowArgumentOutOfRangeException("length", Errors.LengthOutOfRange());
        }
    }
}
