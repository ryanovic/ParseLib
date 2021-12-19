using Xunit;

namespace ParseLib.LALR
{
    public class ParserStatesBuilderTests
    {
        [Theory]
        [InlineData("a", "b", ParserAction.Reduce)]
        [InlineData("A", "b", ParserAction.Shift)]
        [InlineData("A b", "$EOS", ParserAction.Reduce)]
        [InlineData("A B", "$EOS", ParserAction.Reduce)]
        public void Creates_Actions_For_Simple_Grammar(string prefix, string lookahead, ParserAction expected)
        {
            var grammar = new Grammar();

            grammar.CreateTerminals("a", "b");
            grammar.CreateNonTerminals("S", "A", "B");

            grammar.AddRule("S", "A B");
            grammar.AddRule("A", "a");
            grammar.AddRule("B", "b");

            var states = grammar.CreateParserStates("S");
            var state = states[0].GetState(grammar.ParseSymbols(prefix));

            Assert.NotNull(state);
            Assert.Equal(expected, state.Actions[grammar[lookahead]]);
        }

        [Theory]
        [InlineData("a", "b", ParserAction.Reduce)]
        [InlineData("a", "c", ParserAction.Reduce)]
        [InlineData("A", "b", ParserAction.Shift)]
        [InlineData("A", "c", ParserAction.Reduce)]
        [InlineData("A B c", "$EOS", ParserAction.Reduce)]
        [InlineData("A B C", "$EOS", ParserAction.Reduce)]
        public void Creates_Actions_For_Grammar_With_Optional_Production(string prefix, string lookahead, ParserAction expected)
        {
            var grammar = new Grammar();

            grammar.CreateTerminals("a", "b", "c");
            grammar.CreateNonTerminals("S", "A", "B", "C");

            grammar.AddRule("S", "A B C");
            grammar.AddRule("A", "a");
            grammar.AddRule("B:0", "b");
            grammar.AddRule("B:1", "");
            grammar.AddRule("C", "c");

            var states = grammar.CreateParserStates("S");
            var state = states[0].GetState(grammar.ParseSymbols(prefix));

            Assert.NotNull(state);
            Assert.Equal(expected, state.Actions[grammar[lookahead]]);
        }

        [Theory]
        [InlineData("L", "=", ParserAction.Shift)]
        [InlineData("L", "$EOS", ParserAction.Reduce)]
        public void Creates_Actions_For_LR_Grammar(string prefix, string lookahead, ParserAction expected)
        {
            var grammar = new Grammar();

            grammar.CreateTerminals("id", "*", "=");
            grammar.CreateNonTerminals("S", "L", "R");

            grammar.AddRule("S:0", "L = R");
            grammar.AddRule("S:1", "R");
            grammar.AddRule("L:0", "id");
            grammar.AddRule("L:1", "* R");
            grammar.AddRule("R", "L");

            var states = grammar.CreateParserStates("S");
            var state = states[0].GetState(grammar.ParseSymbols(prefix));

            Assert.NotNull(state);
            Assert.Equal(expected, state.Actions[grammar[lookahead]]);
        }

        [Theory]
        [InlineData("A", "c", ParserAction.Shift)]
        [InlineData("A", "b", ParserAction.Shift)]
        [InlineData("A [LB]", "c", ParserAction.Shift)]
        [InlineData("[LB]", "c", ParserAction.Reduce)]
        [InlineData("a [LB]", "c", ParserAction.Reduce)]
        public void Creates_Actions_For_LineBreak_Sensitive_Grammar(string prefix, string lookahead, ParserAction expected)
        {
            var grammar = new Grammar();

            grammar.CreateTerminals("a", "b", "c");
            grammar.CreateNonTerminals("S", "A", "B", "C");

            grammar.AddRule("S:0", "A [NoLB] B");
            grammar.AddRule("S:1", "A C");
            grammar.AddRule("A:0", "a [LB]");
            grammar.AddRule("A:1", "[LB]");
            grammar.AddRule("B", "b");
            grammar.AddRule("C", "c");

            var states = grammar.CreateParserStates("S");
            var state = states[0].GetState(grammar.ParseSymbols(prefix));

            Assert.NotNull(state);
            Assert.Equal(expected, state.Actions[grammar[lookahead]]);
        }

        [Theory]
        [InlineData("A [LB]", "b")]
        [InlineData("[LB]", "b")]
        [InlineData("a", "c")]
        [InlineData("a [LB]", "b")]
        public void Does_Not_Create_Actions_For_LineBreak_Sensitive_Grammar(string prefix, string lookahead)
        {
            var grammar = new Grammar();

            grammar.CreateTerminals("a", "b", "c");
            grammar.CreateNonTerminals("S", "A", "B", "C");

            grammar.AddRule("S:0", "A [NoLB] B");
            grammar.AddRule("S:1", "A C");
            grammar.AddRule("A:0", "a [LB]");
            grammar.AddRule("A:1", "[LB]");
            grammar.AddRule("B", "b");
            grammar.AddRule("C", "c");

            var states = grammar.CreateParserStates("S");
            var state = states[0].GetState(grammar.ParseSymbols(prefix));

            Assert.NotNull(state);
            Assert.False(state.Actions.ContainsKey(grammar[lookahead]));
        }

        [Theory]
        [InlineData("a", "b", ParserAction.Reduce)]
        [InlineData("a [LB]", "c", ParserAction.Reduce)]
        public void Creates_Actions_For_Grammar_With_LookaheadsOverride(string prefix, string lookahead, ParserAction expected)
        {
            var grammar = new Grammar();

            grammar.CreateTerminals("a", "b", "c");
            grammar.CreateNonTerminals("S", "A", "B", "C");

            grammar.AddRule("S:0", "A [NoLB] B");
            grammar.AddRule("S:1", "A C");
            grammar.AddRule("A", "a").OverrideLookaheads(Symbol.LineBreak, "b");
            grammar.AddRule("B", "b");
            grammar.AddRule("C", "c");

            var states = grammar.CreateParserStates("S");
            var state = states[0].GetState(grammar.ParseSymbols(prefix));

            Assert.NotNull(state);
            Assert.Equal(expected, state.Actions[grammar[lookahead]]);
        }

        [Theory]
        [InlineData("a", "c")]
        [InlineData("a [LB]", "b")]
        public void Does_Not_Create_Actions_For_Grammar_With_LookaheadsOverride(string prefix, string lookahead)
        {
            var grammar = new Grammar();

            grammar.CreateTerminals("a", "b", "c");
            grammar.CreateNonTerminals("S", "A", "B", "C");

            grammar.AddRule("S:0", "A [NoLB] B");
            grammar.AddRule("S:1", "A C");
            grammar.AddRule("A", "a").OverrideLookaheads(Symbol.LineBreak, "b");
            grammar.AddRule("B", "b");
            grammar.AddRule("C", "c");

            var states = grammar.CreateParserStates("S");
            var state = states[0].GetState(grammar.ParseSymbols(prefix));

            Assert.NotNull(state);
            Assert.False(state.Actions.ContainsKey(grammar[lookahead]));
        }

        [Fact]
        public void Throws_Error_For_Grammar_With_Dangling_Else_Ambiguity()
        {
            var grammar = new Grammar();

            grammar.CreateTerminals("expr", "if", "then", "else");
            grammar.CreateNonTerminals("stmnt");

            grammar.AddRule("stmnt:if", "if expr then stmnt");
            grammar.AddRule("stmnt:if-else", "if expr then stmnt else stmnt");

            var ex = Assert.Throws<GrammarException>(() =>
            {
                var states = grammar.CreateParserStates("stmnt");

            });

            Assert.Equal("stmnt:if", ex.Productions[0]);
        }

        [Fact]
        public void Throws_Error_For_LR_1_Grammar()
        {
            var grammar = new Grammar();

            grammar.CreateTerminals("a", "b", "c", "d", "e");
            grammar.CreateNonTerminals("S", "A", "B");

            grammar.AddRule("S:0", "a A d");
            grammar.AddRule("S:1", "b B d");
            grammar.AddRule("S:2", "a B e");
            grammar.AddRule("S:3", "b A e");
            grammar.AddRule("A", "c");
            grammar.AddRule("B", "c");

            Assert.Throws<GrammarException>(() =>
            {
                var states = grammar.CreateParserStates("S");

            });
        }

        [Fact]
        public void Creates_Actions_For_Grammar_With_Dangling_Else_Ambiguity_Solved()
        {
            var grammar = new Grammar();

            grammar.CreateTerminals("expr", "if", "then", "else");
            grammar.CreateNonTerminals("stmnt");

            grammar.AddRule("stmnt:if", "if expr then stmnt").ShiftOn("else");
            grammar.AddRule("stmnt:if-else", "if expr then stmnt else stmnt");

            var states = grammar.CreateParserStates("stmnt");
            var actions = states[0].GetState(grammar.GetSymbols("if", "expr", "then", "stmnt")).Actions;

            Assert.Equal(ParserAction.Shift, actions[grammar["else"]]);
        }

        [Theory]
        [ClassData(typeof(ExpressionData))]
        public void Creates_States_For_Math_Grammar_With_Inline_Resolver(string prefix, string lookahead, ParserAction expected)
        {
            var grammar = new Grammar();

            grammar.CreateTerminals("num", "+", "-", "/", "*", "(", ")");
            grammar.CreateNonTerminals("expr");

            grammar.AddRule("expr:num", "num");

            grammar.AddRule("expr:unary", "- expr")
                .ReduceOn("*", "/", "+", "-");

            grammar.AddRule("expr:add", "expr + expr")
                .ReduceOn("+", "-")
                .ShiftOn("*", "/");

            grammar.AddRule("expr:sub", "expr - expr")
                .ReduceOn("+", "-")
                .ShiftOn("*", "/");

            grammar.AddRule("expr:mul", "expr * expr")
                .ReduceOn("*", "/", "+", "-");

            grammar.AddRule("expr:div", "expr / expr")
                .ReduceOn("*", "/", "+", "-");

            grammar.AddRule("expr:group", "( expr )");

            var states = grammar.CreateParserStates("expr");
            var state = states[0].GetState(grammar.ParseSymbols(prefix));

            Assert.NotNull(state);
            Assert.Equal(expected, state.Actions[grammar[lookahead]]);
        }

        [Theory]
        [ClassData(typeof(ExpressionData))]
        public void Creates_States_For_Math_Grammar_With_Custom_Resolver(string prefix, string lookahead, ParserAction expected)
        {
            var grammar = new Grammar(new TestExprResolver());

            grammar.CreateTerminals("num", "+", "-", "/", "*", "(", ")");
            grammar.CreateNonTerminals("expr");

            grammar.AddRule("expr:num", "num");
            grammar.AddRule("expr:unary", "- expr");
            grammar.AddRule("expr:add", "expr + expr");
            grammar.AddRule("expr:sub", "expr - expr");
            grammar.AddRule("expr:mul", "expr * expr");
            grammar.AddRule("expr:div", "expr / expr");
            grammar.AddRule("expr:group", "( expr )");

            var states = grammar.CreateParserStates("expr");
            var state = states[0].GetState(grammar.ParseSymbols(prefix));

            Assert.NotNull(state);
            Assert.Equal(expected, state.Actions[grammar[lookahead]]);
        }

        [Theory]
        [InlineData("{", "stmnt:block")]
        [InlineData("id = {", "expr:obj")]
        public void Creates_States_For_Grammar_With_Core_Conflict(string prefix, string expected)
        {
            var grammar = new Grammar(new TestCoreResolver());

            grammar.CreateTerminals("id", ":", ";", "{", "}", "=");
            grammar.CreateNonTerminals("stmnt", "expr");

            grammar.AddRule("expr:obj", "{ id : id }");
            grammar.AddRule("expr:assign", "id = expr");
            grammar.AddRule("stmnt:expr", "expr ;");
            grammar.AddRule("stmnt:block", "{ stmnt }");

            var states = grammar.CreateParserStates("stmnt");
            var state = states[0].GetState(grammar.ParseSymbols(prefix));

            Assert.Single(state.Core);
            Assert.Equal(expected, state.Core[0].Production.Name);
        }

        private class TestExprResolver : ConflictResolver
        {
            public override ParserAction ResolveShiftConflict(Symbol symbol, Production production)
            {
                switch (production.Name)
                {
                    case "expr:sub":
                    case "expr:add":
                        return symbol.Name == "+" || symbol.Name == "-"
                            ? ParserAction.Reduce
                            : ParserAction.Shift;
                    case "expr:div":
                    case "expr:mul":
                    case "expr:unary":
                        return ParserAction.Reduce;
                }

                return base.ResolveShiftConflict(symbol, production);
            }
        }

        private class TestCoreResolver : ConflictResolver
        {
            public override ParserItem[] ResolveCoreConflicts(Symbol symbol, ParserItem[] core)
            {
                if (symbol.Name == "{" && core.Length == 2)
                {
                    return core[0].Production.Name == "stmnt:block"
                        ? new[] { core[0] }
                        : new[] { core[1] };
                }

                return base.ResolveCoreConflicts(symbol, core);
            }
        }

        private class ExpressionData : TheoryData<string, string, ParserAction>
        {
            public ExpressionData()
            {
                Add("expr + expr", "+", ParserAction.Reduce);
                Add("expr + expr", "$EOS", ParserAction.Reduce);
                Add("expr + expr", "*", ParserAction.Shift);
                Add("expr / expr", "*", ParserAction.Reduce);
                Add("expr - expr", "/", ParserAction.Shift);
                Add("expr * expr", "/", ParserAction.Reduce);
                Add("expr * expr", "$EOS", ParserAction.Reduce);
                Add("- expr", "-", ParserAction.Reduce);
                Add("( expr )", "$EOS", ParserAction.Reduce);
            }
        }
    }
}
