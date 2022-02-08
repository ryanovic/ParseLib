using Xunit;

namespace Ry.ParseLib.LALR
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

            grammar.GetNonTerminal("S").AddProduction("S", "A B");
            grammar.GetNonTerminal("A").AddProduction("A", "a");
            grammar.GetNonTerminal("B").AddProduction("B", "b");

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

            grammar.GetNonTerminal("S").AddProduction("S", "A B C");
            grammar.GetNonTerminal("A").AddProduction("A", "a");
            grammar.GetNonTerminal("B").AddProduction("B:0", "b");
            grammar.GetNonTerminal("B").AddProduction("B:1", "");
            grammar.GetNonTerminal("C").AddProduction("C", "c");

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

            grammar.GetNonTerminal("S").AddProduction("S:0", "L = R");
            grammar.GetNonTerminal("S").AddProduction("S:1", "R");
            grammar.GetNonTerminal("L").AddProduction("L:0", "id");
            grammar.GetNonTerminal("L").AddProduction("L:1", "* R");
            grammar.GetNonTerminal("R").AddProduction("R", "L");

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

            grammar.GetNonTerminal("S").AddProduction("S:0", "A [NoLB] B");
            grammar.GetNonTerminal("S").AddProduction("S:1", "A C");
            grammar.GetNonTerminal("A").AddProduction("A:0", "a [LB]");
            grammar.GetNonTerminal("A").AddProduction("A:1", "[LB]");
            grammar.GetNonTerminal("B").AddProduction("B", "b");
            grammar.GetNonTerminal("C").AddProduction("C", "c");

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

            grammar.GetNonTerminal("S").AddProduction("S:0", "A [NoLB] B");
            grammar.GetNonTerminal("S").AddProduction("S:1", "A C");
            grammar.GetNonTerminal("A").AddProduction("A:0", "a [LB]");
            grammar.GetNonTerminal("A").AddProduction("A:1", "[LB]");
            grammar.GetNonTerminal("B").AddProduction("B", "b");
            grammar.GetNonTerminal("C").AddProduction("C", "c");

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

            grammar.GetNonTerminal("S").AddProduction("S:0", "A [NoLB] B");
            grammar.GetNonTerminal("S").AddProduction("S:1", "A C");
            grammar.GetNonTerminal("A").AddProduction("A", "a").OverrideLookaheads(Symbol.LineBreak, "b");
            grammar.GetNonTerminal("B").AddProduction("B", "b");
            grammar.GetNonTerminal("C").AddProduction("C", "c");

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

            grammar.GetNonTerminal("S").AddProduction("S:0", "A [NoLB] B");
            grammar.GetNonTerminal("S").AddProduction("S:1", "A C");
            grammar.GetNonTerminal("A").AddProduction("A", "a").OverrideLookaheads(Symbol.LineBreak, "b");
            grammar.GetNonTerminal("B").AddProduction("B", "b");
            grammar.GetNonTerminal("C").AddProduction("C", "c");

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

            grammar.GetNonTerminal("stmnt").AddProduction("stmnt:if", "if expr then stmnt");
            grammar.GetNonTerminal("stmnt").AddProduction("stmnt:if-else", "if expr then stmnt else stmnt");

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

            grammar.GetNonTerminal("S").AddProduction("S:0", "a A d");
            grammar.GetNonTerminal("S").AddProduction("S:1", "b B d");
            grammar.GetNonTerminal("S").AddProduction("S:2", "a B e");
            grammar.GetNonTerminal("S").AddProduction("S:3", "b A e");
            grammar.GetNonTerminal("A").AddProduction("A", "c");
            grammar.GetNonTerminal("B").AddProduction("B", "c");

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

            grammar.GetNonTerminal("stmnt").AddProduction("stmnt:if", "if expr then stmnt").ShiftOn("else");
            grammar.GetNonTerminal("stmnt").AddProduction("stmnt:if-else", "if expr then stmnt else stmnt");

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
            var expr = grammar.CreateNonTerminal("expr");

            expr.AddProduction("expr:num", "num");

            expr.AddProduction("expr:unary", "- expr")
                .ReduceOn("*", "/", "+", "-");

            expr.AddProduction("expr:add", "expr + expr")
                .ReduceOn("+", "-")
                .ShiftOn("*", "/");

            expr.AddProduction("expr:sub", "expr - expr")
                .ReduceOn("+", "-")
                .ShiftOn("*", "/");

            expr.AddProduction("expr:mul", "expr * expr")
                .ReduceOn("*", "/", "+", "-");

            expr.AddProduction("expr:div", "expr / expr")
                .ReduceOn("*", "/", "+", "-");

            expr.AddProduction("expr:group", "( expr )");

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
            var expr = grammar.CreateNonTerminal("expr");

            expr.AddProduction("expr:num", "num");
            expr.AddProduction("expr:unary", "- expr");
            expr.AddProduction("expr:add", "expr + expr");
            expr.AddProduction("expr:sub", "expr - expr");
            expr.AddProduction("expr:mul", "expr * expr");
            expr.AddProduction("expr:div", "expr / expr");
            expr.AddProduction("expr:group", "( expr )");

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

            var expr = grammar.CreateNonTerminal("expr");
            var stmnt = grammar.CreateNonTerminal("stmnt");

            expr.AddProduction("expr:obj", "{ id : id }");
            expr.AddProduction("expr:assign", "id = expr");
            stmnt.AddProduction("stmnt:expr", "expr ;");
            stmnt.AddProduction("stmnt:block", "{ stmnt }");

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
