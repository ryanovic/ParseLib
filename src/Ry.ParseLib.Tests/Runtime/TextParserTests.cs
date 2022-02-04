namespace Ry.ParseLib.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Xunit;

    public class TextParserTests
    {
        private readonly Func<TextReader, TextParser> factory;

        public TextParserTests()
        {
            this.factory = TestMathGrammar.Grammar.CreateTextParserFactory<TextTestParser>(TestMathGrammar.Goal);
        }

        [Theory]
        [ClassData(typeof(TestMathData))]
        public void Evaluates_Math_Expression(string expression, int result)
        {
            var parser = factory(new StringReader(expression));
            parser.Parse();
            Assert.Equal(result, parser.GetResult());
        }

        [Theory]
        [ClassData(typeof(TestMathData))]
        public async Task Evaluates_Math_Expression_Async(string expression, int result)
        {
            var parser = factory(new StringReader(expression));
            await parser.ParseAsync();
            Assert.Equal(result, parser.GetResult());
        }

        public abstract class TextTestParser : TextParser
        {
            public TextTestParser(TextReader reader)
                : base(reader, bufferSize: 1)
            {
            }

            [CompleteToken("num")]
            public int CompleteNumber() => Int32.Parse(GetValue());

            [Reduce("expr:add")]
            public int Add(int x, int y) => x + y;

            [Reduce("expr:sub")]
            public int Sub(int x, int y) => x - y;

            [Reduce("expr:mul")]
            public int Mul(int x, int y) => x * y;

            [Reduce("expr:div")]
            public int Div(int x, int y) => x / y;

            [Reduce("expr:unary")]
            public int Neg(int x) => -x;

            protected void OnTokenCompleted(string token)
            {
                Assert.True(TestMathGrammar.Grammar.ContainsTerminal(token));
            }

            protected void OnProductionCompleted(string production)
            {
                Assert.True(TestMathGrammar.Grammar.ContainsRule(production));
            }
        }
    }
}
