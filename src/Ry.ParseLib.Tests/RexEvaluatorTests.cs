﻿namespace Ry.ParseLib
{
    using System;
    using System.Collections.Generic;
    using Xunit;
    using Ry.ParseLib.Text;

    public class RexEvaluatorTests
    {
        [Theory]
        [InlineData("aaadaaa", 0)]
        [InlineData("aaaaaa", 1)]
        [InlineData("aaadaaa", 4)]
        public void Matches_Expression_From_Position_Specified(string input, int offset)
        {
            var eval = Rex.Compile(Rex.Text("aaa"));

            Assert.Equal("aaa".Length, eval(input.AsSpan(offset)));
        }

        [Theory]
        [InlineData("aaaaa", 3)]
        [InlineData("aaaaa", 4)]
        [InlineData("aadaa", 0)]
        public void Does_Not_Match_Expression_From_Position_Specified(string input, int offset)
        {
            var eval = Rex.Compile(Rex.Text("aaa"));

            Assert.Equal(-1, eval(input.AsSpan(offset)));
        }

        [Theory]
        [InlineData("word", "word")]
        [InlineData("word", "words")]
        public void Matches_Text_Expression(string text, string input)
        {
            var eval = Rex.Compile(Rex.Text(text));

            Assert.Equal("word".Length, eval(input.AsSpan()));
        }

        [Theory]
        [InlineData("word", "wo")]
        [InlineData("word", "world")]
        public void Does_Not_Match_Text_Expression(string text, string input)
        {
            var eval = Rex.Compile(Rex.Text(text));

            Assert.Equal(-1, eval(input.AsSpan()));
        }

        [Fact]
        public void Matches_Char_Code_Expression()
        {
            var eval = Rex.Compile(Rex.Char('a'));

            Assert.Equal(1, eval("a".AsSpan()));
        }

        [Fact]
        public void Does_Not_Match_Char_Code_Expression()
        {
            var eval = Rex.Compile(Rex.Char('a'));

            Assert.Equal(-1, eval("b".AsSpan()));
        }

        [Theory]
        [InlineData(@"a-z", "x")]
        [InlineData(@"\p{L}", "x")]
        [InlineData(@"\p{Lu}", "𝑿")]
        public void Matches_Char_Template_Expression(string pattern, string input)
        {
            var eval = Rex.Compile(Rex.Char(pattern));

            Assert.Equal(input.Length, eval(input.AsSpan()));
        }

        [Theory]
        [InlineData(@"a-z", "A")]
        [InlineData(@"\p{L}", "0")]
        [InlineData(@"\p{Ll}", "𐓑")]
        public void Does_Not_Match_Char_Template_Expression(string pattern, string input)
        {
            var eval = Rex.Compile(Rex.Char(pattern));

            Assert.Equal(-1, eval(input.AsSpan()));
        }

        [Fact]
        public void Matches_Except_Code_Expression()
        {
            var eval = Rex.Compile(Rex.Except('a'));

            Assert.Equal(1, eval("b".AsSpan()));
        }

        [Fact]
        public void Does_Not_Match_Except_Code_Expression()
        {
            var eval = Rex.Compile(Rex.Except('a'));

            Assert.Equal(-1, eval("a".AsSpan()));
        }

        [Theory]
        [InlineData(@"a-z", "A")]
        [InlineData(@"\p{L}", "0")]
        [InlineData(@"\p{Ll}", "𝑿")]
        public void Matches_Except_Template_Expression(string pattern, string input)
        {
            var eval = Rex.Compile(Rex.Except(pattern));

            Assert.Equal(input.Length, eval(input.AsSpan()));
        }

        [Theory]
        [InlineData(@"a-z", "x")]
        [InlineData(@"\p{L}", "x")]
        [InlineData(@"\p{Lu}", "𝑿")]
        public void Does_Not_Match_Except_Template_Expression(string pattern, string input)
        {
            var eval = Rex.Compile(Rex.Except(pattern));

            Assert.Equal(-1, eval(input.AsSpan()));
        }

        [Fact]
        public void Matches_Or_Codes_Expression()
        {
            var eval = Rex.Compile(Rex.Or('a', 'b'));

            Assert.Equal(1, eval("b".AsSpan()));
        }

        [Fact]
        public void Matches_Or_Words_Expression()
        {
            var eval = Rex.Compile(Rex.Or("word1", "word2"));

            Assert.Equal("word2".Length, eval("word2".AsSpan()));
        }

        [Theory]
        [InlineData(@"a")]
        [InlineData(@"1")]
        public void Matches_Or_Nodes_Expression(string input)
        {
            var eval = Rex.Compile(Rex.Or(Rex.Char(@"a-z"), Rex.Char(@"0-9")));

            Assert.Equal(input.Length, eval(input.AsSpan()));
        }

        [Theory]
        [InlineData(@"!")]
        [InlineData(@" ")]
        public void Does_Not_Match_Or_Nodes_Expression(string input)
        {
            var eval = Rex.Compile(Rex.Or(Rex.Char(@"a-z"), Rex.Char(@"0-9")));

            Assert.Equal(-1, eval(input.AsSpan()));
        }

        [Fact]
        public void Matches_And_Codes_Expression()
        {
            var eval = Rex.Compile(Rex.Concat('a', 'b'));
            var input = "ab";

            Assert.Equal(input.Length, eval(input.AsSpan()));
        }

        [Fact]
        public void Matches_Concat_Words_Expression()
        {
            var eval = Rex.Compile(Rex.Concat("ab", "cd"));

            Assert.Equal("abcd".Length, eval("abcd".AsSpan()));
        }

        [Theory]
        [InlineData(@"a1")]
        public void Matches_Concat_Expression(string input)
        {
            var eval = Rex.Compile(Rex.Concat(Rex.Char(@"a-z"), Rex.Char(@"0-9")));

            Assert.Equal(input.Length, eval(input.AsSpan()));
        }

        [Theory]
        [InlineData(@"1a")]
        [InlineData(@"b!")]
        public void Does_Not_Match_Concat_Expression(string input)
        {
            var eval = Rex.Compile(Rex.Concat(Rex.Char(@"a-z"), Rex.Char(@"0-9")));

            Assert.Equal(-1, eval(input.AsSpan()));
        }

        [Theory]
        [InlineData("abb")]
        [InlineData("ababb")]
        [InlineData("abbabb")]
        public void Matches_Non_Or_More_Expression(string input)
        {
            var eval = Rex.Compile(Rex.Or('a', 'b').NoneOrMore().Then("abb"));

            Assert.Equal(input.Length, eval(input.AsSpan()));
        }

        [Theory]
        [InlineData("aBb")]
        [InlineData("abAbb")]
        [InlineData("AbbAbb")]
        public void Matches_Non_Or_More_Expression_IgnoreCase(string input)
        {
            var eval = Rex.Compile(Rex.Or('a', 'b').NoneOrMore().Then("abb"), ignoreCase: true);

            Assert.Equal(input.Length, eval(input.AsSpan()));
        }

        [Fact]
        public void Matches_Non_Or_More_Expression_Lazy()
        {
            var eval = Rex.Compile(Rex.Or('a', 'b').NoneOrMore().Then("abb"), lazy: true);

            Assert.Equal("abb".Length, eval("abbabb".AsSpan()));
        }

        [Theory]
        [InlineData("0")]
        [InlineData("10")]
        [InlineData("𝑿𝟡")]
        public void Matches_One_Or_More_Expression(string input)
        {
            var eval = Rex.Compile(Rex.Char(@"\p{L|N}").OneOrMore());

            Assert.Equal(input.Length, eval(input.AsSpan()));
        }

        [Theory]
        [InlineData("ab")]
        [InlineData("b")]
        public void Matches_Optional_Expression(string input)
        {
            var eval = Rex.Compile(Rex.Char('a').Optional().Then('b'));

            Assert.Equal(input.Length, eval(input.AsSpan()));
        }

        [Theory]
        [InlineData("-->")]
        [InlineData("xxxx -->")]
        public void Matches_IfNot_Expression(string input)
        {
            var eval = Rex.Compile(Rex.IfNot("-->").Then(Rex.AnyChar).NoneOrMore());

            Assert.Equal(input.Length - 3, eval(input.AsSpan()));
        }

        [Theory]
        [InlineData("test")]
        [InlineData("testst")]
        [InlineData("teststol")]
        public void Matches_Lookahead_Expression(string input)
        {
            var expr = Rex.Text("test").NotFollowedBy(Rex.Text("st").FollowedBy("op"));
            var eval = Rex.Compile(expr);

            Assert.Equal("test".Length, eval(input.AsSpan()));
        }

        [Theory]
        [InlineData("tes")]
        [InlineData("teststop")]
        [InlineData("teststopp")]
        public void Does_Not_Match_Lookahead_Expression(string input)
        {
            var expr = Rex.Text("test").NotFollowedBy(Rex.Text("st").FollowedBy("op"));
            var eval = Rex.Compile(expr);

            Assert.Equal(-1, eval(input.AsSpan()));
        }

        [Theory]
        [InlineData("test")]
        [InlineData("testz")]
        public void Matches_Multiple_Lookahead_Expression(string input)
        {
            var expr = Rex.Text("test")
                .NotFollowedBy("x")
                .NotFollowedBy("y");
            var eval = Rex.Compile(expr);

            Assert.Equal("test".Length, eval(input.AsSpan()));
        }

        [Theory]
        [InlineData("testx")]
        [InlineData("testy")]
        public void Does_Not_Match_Multiple_Lookahead_Expression(string input)
        {
            var expr = Rex.Text("test")
                .NotFollowedBy("x")
                .NotFollowedBy("y");
            var eval = Rex.Compile(expr);

            Assert.Equal(-1, eval(input.AsSpan()));
        }

        [Theory]
        [InlineData("a")]
        public void Matches_NOT_Surrogate_Letter_Expression(string input)
        {
            var expr = Rex.Char(CharSet.Parse(@"\p{L}-[\u{10000-10ffff}]"));
            var eval = Rex.Compile(expr);

            Assert.Equal(input.Length, eval(input.AsSpan()));
        }

        [Theory]
        [InlineData("𐓑")]
        public void Does_Not_Match_Surrogate_Letter_Expression(string input)
        {
            var expr = Rex.Char(CharSet.Parse(@"\p{L}-[\u{10000-10ffff}]"));
            var eval = Rex.Compile(expr);

            Assert.Equal(-1, eval(input.AsSpan()));
        }

        [Theory]
        [InlineData(@"''")]
        [InlineData(@"'𐓑'")]
        [InlineData(@"'\''")]
        [InlineData(@"'\\'")]
        public void Matches_String_With_Escape_Sequence_Expression(string input)
        {
            var str_char = Rex.Except('\'').Or(Rex.Char('\\').Then(Rex.AnyChar));
            var str = Rex.Char('\'').Then(str_char.NoneOrMore()).Then('\'');
            var eval = Rex.Compile(str);

            Assert.Equal(input.Length, eval(input.AsSpan()));
        }
    }
}
