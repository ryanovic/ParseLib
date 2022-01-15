namespace ParseLib.Emit
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;

    /// <summary>
    /// Implements <see cref="ParserBuilder"/> class for a <see cref="StringParserSource">string</see> input source.
    /// </summary>
    public sealed class StringParserBuilder : ParserBuilder
    {
        public StringParserBuilder(TypeBuilder target, IParserReducer reducer, Grammar grammar, string goal)
            : this(target, reducer, grammar.CreateParserMetadata(goal))
        {
        }

        public StringParserBuilder(TypeBuilder target, IParserReducer reducer, ParserMetadata metadata)
            : base(target, reducer, metadata)
        {
        }

        protected override void BuildLexer()
        {
            var method = Target.DefineMethod("Read",
                MethodAttributes.Family | MethodAttributes.Virtual,
                typeof(void), new[] { typeof(string), typeof(int), typeof(int) });

            // String source reader method is never breaks, so we can safely store the parser's state in local variables.
            var il = method.GetILGenerator();
            var charCode = il.CreateCell<int>();
            var categories = il.CreateCell<int>();
            var acceptedPosition = il.CreateCell<int>();
            var acceptedTokenId = il.CreateCell<int>();

            var lhStack = LexicalStates.HasLookaheads
                ? il.CreateLookaheadStack()
                : null;

            // Same reason we can use charCode also as a high surrogate storage, since it's guaranteed that it always has previous charcode value.
            var source = CreateSource();
            var lexer = new LexerBuilder(
                il, LexicalStates, source, this, lhStack, charCode, categories, LexerState, CurrentPosition, acceptedPosition, acceptedTokenId, charCode);

            acceptedTokenId.Update(il, -1);
            lhStack?.Initialize(il);
            lexer.Build();
        }

        private ILexerSource CreateSource() => new StringParserSource();
    }
}
