﻿namespace ParseLib.Emit
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;

    /// <summary>
    /// Implements <see cref="ParserBuilder"/> class for a <see cref="SequentialParserSource">sequential</see> input source.
    /// </summary>
    public sealed class SequentialParserBuilder : ParserBuilder
    {
        private readonly Cell<int> acceptedPosition;
        private readonly Cell<int> acceptedTokenId;
        private readonly Cell<int> highSurrogate;
        private readonly LookaheadStack lhStack;

        public SequentialParserBuilder(TypeBuilder target, IParserReducer reducer, Grammar grammar, string goal)
            : this(target, reducer, grammar.CreateParserMetadata(goal))
        {
        }

        public SequentialParserBuilder(TypeBuilder target, IParserReducer reducer, ParserMetadata metadata)
            : base(target, reducer, metadata)
        {
            // Save the state in fields so that it's never lost when the method breaks.
            this.acceptedPosition = target.CreateCell<int>("acceptedPosition");
            this.acceptedTokenId = target.CreateCell<int>("acceptedTokenId");
            this.highSurrogate = target.CreateCell<int>("highSurrogate");

            this.lhStack = LexicalStates.HasLookaheads
                ? target.CreateLookaheadStack("lhStack")
                : null;
        }

        protected override void InitializeParser(ILGenerator il)
        {
            base.InitializeParser(il);
            acceptedTokenId.Update(il, -1);
            lhStack?.Initialize(il);
        }

        protected override void BuildLexer()
        {
            var method = Target.DefineMethod("Read",
                MethodAttributes.Family | MethodAttributes.Virtual,
                typeof(bool),
                new[] { typeof(int), typeof(char[]), typeof(int), typeof(int), typeof(bool) });

            var il = method.GetILGenerator();
            var charCode = il.CreateCell<int>();
            var categories = il.CreateCell<int>();

            var source = CreateSource();
            var lexer = new LexerBuilder(
                il, LexicalStates, source, this, lhStack, charCode, categories, LexerState, CurrentPosition, acceptedPosition, acceptedTokenId, highSurrogate);

            lexer.Build();
        }

        private ILexerSource CreateSource() => new SequentialParserSource();
    }
}
