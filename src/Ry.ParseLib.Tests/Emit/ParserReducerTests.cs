using Xunit;
using Ry.ParseLib.Runtime;

namespace Ry.ParseLib.Emit
{
    public class ParserReducerTests
    {
        private readonly ParserReducer reducer;
        private readonly Grammar grammar;

        public ParserReducerTests()
        {
            grammar = new Grammar();

            grammar.CreateTerminals("a", "b", "c");
            grammar.CreateNonTerminals("A", "B", "C", "S");

            grammar.AddRule("S:0", "A B C");
            grammar.AddRule("A:0", "a");
            grammar.AddRule("B:0", "b");
            grammar.AddRule("C:0", "c");

            reducer = ParserReducer.CreateReducer(typeof(TestReducer), grammar);
        }

        [Fact]
        public void Map_Token_Reducer_To_Instance_Method()
        {
            Assert.NotNull(reducer.GetTokenReducer("a"));
        }

        [Fact]
        public void Map_Token_Reducer_To_Instance_Overrided_Method()
        {
            Assert.NotNull(reducer.GetTokenReducer("b"));
        }

        [Fact]
        public void Map_Token_Reducer_To_Parent_Instance_Method()
        {
            Assert.NotNull(reducer.GetTokenReducer("c"));
        }

        [Fact]
        public void Map_Multiple_Rule_Reducer_To_Instance_Method()
        {
            Assert.NotNull(reducer.GetProductionReducer("A:0"));
            Assert.NotNull(reducer.GetProductionReducer("B:0"));
            Assert.NotNull(reducer.GetProductionReducer("C:0"));
        }

        [Fact]
        public void Map_Rule_Reducer_To_Static_Mehtod()
        {
            Assert.NotNull(reducer.GetProductionReducer("S:0"));
        }

        [Fact]
        public void Map_Handler_To_Instance_Method()
        {
            Assert.NotNull(reducer.GetPrefixHandler(new[] { grammar["A"], grammar["B"] }));
        }

        [Fact]
        public void Return_Null_When_Not_Specified()
        {
            Assert.Null(reducer.GetTokenReducer("x"));
            Assert.Null(reducer.GetProductionReducer("X:0"));
            Assert.Null(reducer.GetPrefixHandler(new[] { grammar["A"] }));
        }

        private class TestReducerBase
        {
            [CompleteToken("b")]
            protected virtual void CompleteTokenB()
            {
            }


            [CompleteToken("c")]
            protected void CompleteTokenC()
            {
            }
        }

        private class TestReducer : TestReducerBase
        {
            [CompleteToken("a")]
            protected void CompleteTokenA()
            {
            }

            [Reduce("S:0")]
            public static void ReduceProductionA()
            {
            }

            [Reduce("A:0")]
            [Reduce("B:0")]
            [Reduce("C:0")]
            protected void ReduceProductionS()
            {
            }


            [Handle("A B")]
            public void HandlePrefix()
            {
            }

            protected override void CompleteTokenB()
            {
            }
        }
    }
}
