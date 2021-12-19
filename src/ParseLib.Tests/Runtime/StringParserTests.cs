using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace ParseLib.Runtime
{
    public class StringParserTests
    {
        private readonly Func<string, StringParser> factory;

        public StringParserTests()
        {
            this.factory = TestMathGrammar.Grammar.CreateStringParserFactory<StringTestParser>(TestMathGrammar.Goal);
        }

        [Theory]
        [ClassData(typeof(TestMathData))]
        public void Evaluates_Math_Expression(string expression, int result)
        {
            var parser = factory(expression);
            parser.Parse();
            Assert.Equal(result, parser.GetResult());
        }

        public abstract class StringTestParser : StringParser
        {
            public StringTestParser(string content)
                : base(content)
            {
            }

            [CompleteToken("num")]
            public int CompleteNumber() => Int32.Parse(GetLexeme());

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
        }
    }
}
