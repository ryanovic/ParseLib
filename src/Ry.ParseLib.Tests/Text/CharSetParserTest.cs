namespace Ry.ParseLib.Text
{
    using System;
    using System.Collections.Generic;
    using Xunit;

    public class CharSetParserTest
    {
        private static IDictionary<string, UnicodeCategories> uc = UnicodeCategories.Mapping;

        [Theory]
        [InlineData("a-z", 'a', 'z')]
        [InlineData("0-9", '0', '9')]
        public void Parses_Char_Range(string pattern, int from, int to)
        {
            var charSet = CharSet.Parse(pattern);

            Assert.Equal(from, charSet.Ranges[0].From);
            Assert.Equal(to, charSet.Ranges[0].To);
        }

        [Fact]
        public void Parses_Multiple_Char_Ranges()
        {
            var charSet = CharSet.Parse(@"a-zA-Z0-9");

            Assert.Equal(3, charSet.Ranges.Length);
            Assert.Equal('0', charSet.Ranges[0].From);
            Assert.Equal('9', charSet.Ranges[0].To);
            Assert.Equal('A', charSet.Ranges[1].From);
            Assert.Equal('Z', charSet.Ranges[1].To);
            Assert.Equal('a', charSet.Ranges[2].From);
            Assert.Equal('z', charSet.Ranges[2].To);
        }

        [Fact]
        public void Parses_Multiple_Chars()
        {
            var charSet = CharSet.Parse(@"aA\-");

            Assert.Equal(3, charSet.Ranges.Length);
            Assert.Equal('-', charSet.Ranges[0].From);
            Assert.Equal('-', charSet.Ranges[0].To);
            Assert.Equal('A', charSet.Ranges[1].From);
            Assert.Equal('A', charSet.Ranges[1].To);
            Assert.Equal('a', charSet.Ranges[2].From);
            Assert.Equal('a', charSet.Ranges[2].To);
        }

        [Fact]
        public void Parses_Hex_Escape_Pattern()
        {
            var charSet = CharSet.Parse(@"\u{00-ff|FFFF}");

            Assert.Equal(0, charSet.Ranges[0].From);
            Assert.Equal(0xff, charSet.Ranges[0].To);
            Assert.Equal(0xffff, charSet.Ranges[1].From);
            Assert.Equal(0xffff, charSet.Ranges[1].To);
        }

        [Fact]
        public void Parses_Unicode_Categories_Escape_Pattern()
        {
            var charSet = CharSet.Parse(@"\p{Lu|Nd}");

            Assert.Equal(uc["Lu"].Union(uc["Nd"]), charSet.Categories);
        }

        [Fact]
        public void Parses_Except_CharSet()
        {
            var charSet = CharSet.Parse(@"\u{0-10FFFF}-[\p{Lu|Nd}]");

            Assert.Equal(UnicodeRanges.All, charSet.Ranges);
            Assert.Equal(uc["Lu"].Union(uc["Nd"]), charSet.Except.Categories);
        }

        [Fact]
        public void Parses_Nested_Except()
        {
            var charSet = CharSet.Parse(@"a-[b-[c]]");

            Assert.Equal('a', charSet.Ranges[0].From);
            Assert.Equal('b', charSet.Except.Ranges[0].From);
            Assert.Equal('c', charSet.Except.Except.Ranges[0].From);
        }

        [Theory]
        [InlineData(@"\a", '\a')]
        [InlineData(@"\b", '\b')]
        [InlineData(@"\t", '\t')]
        [InlineData(@"\r", '\r')]
        [InlineData(@"\n", '\n')]
        [InlineData(@"\v", '\v')]
        [InlineData(@"\f", '\f')]
        public void Parses_Escape_Chars(string pattern, int code)
        {
            var charSet = CharSet.Parse(pattern);

            Assert.Equal(code, charSet.Ranges[0].From);
            Assert.Equal(code, charSet.Ranges[0].To);
        }
    }
}
