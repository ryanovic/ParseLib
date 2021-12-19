namespace ParseLib.Text
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using Xunit;

    public class StateBuilderTest
    {
        public LexicalState CreateStates(RexNode expression, bool ignoreCase = false, bool lazy = false)
        {
            var builder = new LexicalStatesBuilder(ignoreCase);
            return builder.CreateStates(0, expression, lazy);
        }

        public LexicalState GetTransition(UnicodeCategory? uc, CategoryTransition[] categories, LexicalState defaultState)
        {
            if (uc.HasValue)
            {
                for (int i = 0; i < categories.Length; i++)
                {
                    if (categories[i].Category.Contains(uc.Value))
                    {
                        return categories[i].State;
                    }
                }
            }

            return defaultState;
        }

        public LexicalState GetTransition(LexicalState state, int code, UnicodeCategory? uc)
        {
            foreach (var transition in state.Ranges)
            {
                if (code >= transition.Range.From && code <= transition.Range.To)
                {
                    return GetTransition(uc, transition.Categories, transition.Default);
                }
            }

            return GetTransition(uc, state.Categories, state.Default);
        }

        public LexicalState GetTransition(LexicalState state, char ch)
        {
            return GetTransition(state, ch, Char.GetUnicodeCategory(ch));
        }

        [Fact]
        public void Creates_States_With_Disjoint_Ranges_And_No_Default_Transition()
        {
            var state = CreateStates(Rex.Or(
                Rex.Char('a'),
                Rex.Char('b'),
                Rex.Char("a-z")));

            for (int i = 1; i < state.Ranges.Length; i++)
            {
                Assert.True(state.Ranges[i].Range.From > state.Ranges[i - 1].Range.To);
            }

            Assert.Null(state.Default);
        }

        [Fact]
        public void Creates_States_With_Disjoint_Ranges_And_Default_Transition()
        {
            var state = CreateStates(Rex.Or(
                Rex.Char('a'),
                Rex.Char('b'),
                Rex.Char("a-z"),
                Rex.Char(CharSet.Any)));

            for (int i = 1; i < state.Ranges.Length; i++)
            {
                Assert.True(state.Ranges[i].Range.From > state.Ranges[i - 1].Range.To);
            }

            Assert.NotNull(state.Default);
        }

        [Theory]
        [InlineData(@"a-z")]
        [InlineData(@"\u{0-10ffff}")]
        public void Creates_States_With_NO_Transitions_By_Category(string pattern)
        {
            var state = CreateStates(Rex.Char(pattern));

            Assert.All(state.Ranges, x => Assert.Empty(x.Categories));
            Assert.Empty(state.Categories);
        }

        [Fact]
        public void Creates_States_With_Transitions_By_Category()
        {
            var state = CreateStates(Rex.Char(@"\p{N}"));

            Assert.Equal(UnicodeCategories.Mapping["N"], state.Categories.Single().Category);
        }

        [Theory]
        [InlineData(@"\u{0-10ffff}")]
        [InlineData(@"\u{10000-10ffff}")]
        [InlineData(@"\p{N}")]
        public void Creates_States_With_Surrogate_Transitions(string pattern)
        {
            var state = CreateStates(Rex.Char(pattern));
            Assert.NotNull(GetTransition(state, UnicodeRange.SurrogateStart, null));
        }

        [Fact]
        public void Creates_Ranges_For_Surrogate_State()
        {
            var state = CreateStates(Rex.Char(@"\u{10-1000ff}"));
            var surrogate = GetTransition(state, UnicodeRange.SurrogateStart, null);

            Assert.Equal(new UnicodeRange(0x10000, 0x1000ff), surrogate.Ranges.Single().Range);
        }

        [Fact]
        public void Creates_Categories_For_Surrogate_State()
        {
            var state = CreateStates(Rex.Char(@"\p{N}"));
            var surrogate = GetTransition(state, UnicodeRange.SurrogateStart, null);

            Assert.Equal(UnicodeCategories.Mapping["N"], surrogate.Categories.Single().Category);
        }

        [Fact]
        public void Creates_States_For_Char_Expression()
        {
            var cs = CharSet.Parse("a");
            var state = CreateStates(Rex.Char(cs));

            Assert.Equal(cs.Ranges, state.Ranges.Select(x => x.Range));
            Assert.False(state.IsFinal);
            Assert.True(GetTransition(state, 'a').IsFinal);
        }

        [Fact]
        public void Creates_States_For_Category_Expression()
        {
            var cs = CharSet.Parse(@"\p{N}");
            var state = CreateStates(Rex.Char(cs));
            var surrogate = GetTransition(state, UnicodeRange.SurrogateStart, null);

            Assert.True(GetTransition(surrogate, '0').IsFinal);
            Assert.True(GetTransition(state, '0').IsFinal);
        }

        [Fact]
        public void Creates_States_For_NonOrMore_Expression()
        {
            var cs = CharSet.Parse("a");
            var state = CreateStates(Rex.Char(cs).NoneOrMore());

            Assert.Equal(cs.Ranges, state.Ranges.Select(x => x.Range));
            Assert.True(state.IsFinal);
            Assert.Equal(state, GetTransition(state, 'a'));
        }

        [Fact]
        public void Creates_States_For_OneOrMore_Expression()
        {
            var cs = CharSet.Parse("a");
            var state = CreateStates(Rex.Char(cs).OneOrMore());
            var final = GetTransition(state, 'a');

            Assert.Equal(cs.Ranges, state.Ranges.Select(x => x.Range));
            Assert.False(state.IsFinal);
            Assert.True(final.IsFinal);
            Assert.Equal(final, GetTransition(final, 'a'));
        }

        [Fact]
        public void Create_States_For_Positive_Lookahead()
        {
            var state = CreateStates(Rex.If('a'));

            Assert.Null(state.OnFalse);
            Assert.True(state.OnTrue.IsFinal);
        }

        [Fact]
        public void Create_States_For_Negative_Lookahead()
        {
            var state = CreateStates(Rex.IfNot('a'));

            Assert.True(state.OnFalse.IsFinal);
            Assert.Null(state.OnTrue);
        }

        [Fact]
        public void Removes_Final_Transitions_For_Lazy_Expression()
        {
            var state = CreateStates(Rex.Char('a').NoneOrMore(), lazy: true);

            Assert.True(state.IsFinal);
            Assert.Null(GetTransition(state, 'a'));
        }

        [Theory]
        [InlineData(@"a", 'A')]
        [InlineData(@"A", 'a')]
        [InlineData(@"a-c", 'B')]
        public void Expands_Range_Transitions_When_IgnoreCase(string pattern, char expected)
        {
            var state = CreateStates(Rex.Char(pattern), ignoreCase: true);

            Assert.NotNull(GetTransition(state, expected));
        }

        [Fact]
        public void Creates_States_For_Except_Char_Expression()
        {
            var state = CreateStates(Rex.Except('a'));

            Assert.Null(GetTransition(state, 'a'));
            Assert.NotNull(state.Default);
        }

        [Fact]
        public void Creates_States_For_Except_Category_Expression()
        {
            var state = CreateStates(Rex.Except(@"\p{N}"));

            Assert.Null(GetTransition(state, '0'));
            Assert.NotNull(state.Default);
        }
    }
}
