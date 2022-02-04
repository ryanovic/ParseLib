namespace ParseLib.Emit
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;

    /// <summary>
    /// Implements <see cref="ParserBuilderBase"/> class for a <see cref="StringParserSource">string</see> input source.
    /// </summary>
    public sealed class ParserBuilder : ParserBuilderBase
    {
        public ParserBuilder(TypeBuilder target, IParserReducer reducer, Grammar grammar, string goal)
            : this(target, reducer, grammar.CreateParserMetadata(goal))
        {
        }

        public ParserBuilder(TypeBuilder target, IParserReducer reducer, ParserMetadata metadata)
            : base(target, reducer, metadata)
        {
        }

        protected override void BuildLexer()
        {
            var method = Target.DefineMethod("Read",
                MethodAttributes.Family | MethodAttributes.Virtual,
                typeof(void), new[] { typeof(ReadOnlySpan<char>) });

            var il = method.GetILGenerator();

            var lexer = new LexerBuilder(
                il, LexicalStates, this, LexerState, CurrentPosition);

            lexer.Build();
        }
    }
}
