namespace ParseLib.Text
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    public class UnicodeRangesTest
    {
        public static UnicodeRange[] Parse(string pattern) => CharSet.Parse(pattern).Ranges;

        [Theory]
        [InlineData(@"\u{10-20|30-40}", @"\u{15-35}", @"\u{10-14|36-40}")]
        [InlineData(@"\u{50-100|150-200}", @"\u{10-20|61-7f|91-120}", @"\u{50-60|80-90|150-200}")]
        [InlineData(@"\u{50-100|150-200}", @"\u{10-200}", @"")]
        [InlineData(@"\u{0-10ffff}", @"\u{0-d7ff|e000-10ffff}", @"")]
        [InlineData(@"\u{0-10ffff}", @"\u{0-d7ff}", @"\u{e000-10ffff}")]
        [InlineData(@"\u{0-10ffff}", @"\u{e000-10ffff}", @"\u{0-d7ff}")]
        public void Substruct_Produces_Expected_Result(string x, string y, string z)
        {
            var a = Parse(x);
            var b = Parse(y);
            var c = Parse(z);

            Assert.Equal(c, UnicodeRanges.Subtract(a, b));
        }

        [Theory]
        [InlineData(@"\u{10-20|30-40}", @"\u{15-35}", @"\u{10-40}")]
        [InlineData(@"\u{50-100|150-200}", @"\u{10-20|61-7f|91-120}", @"\u{10-20|50-120|150-200}")]
        [InlineData(@"\u{50-100|150-200}", @"\u{10-200}", @"\u{10-200}")]
        public void Union_Produces_Expected_Result(string x, string y, string z)
        {
            var a = Parse(x);
            var b = Parse(y);
            var c = Parse(z);

            Assert.Equal(c, UnicodeRanges.Union(a, b));
        }

        [Theory]
        [InlineData(@"\u{10-20|30-40}", @"\u{15-35}", @"\u{15-20|30-35}")]
        [InlineData(@"\u{50-100|150-200}", @"\u{10-20|60-80|90-120}", @"\u{60-80|90-100}")]
        [InlineData(@"\u{50-100|150-200}", @"\u{10-200}", @"\u{50-100|150-200}")]
        public void Intersect_Produces_Expected_Result(string x, string y, string z)
        {
            var a = Parse(x);
            var b = Parse(y);
            var c = Parse(z);

            Assert.Equal(c, UnicodeRanges.Intersect(a, b));
        }

        [Theory]
        [InlineData(@"\u{0-10ffff}")]
        [InlineData(@"\u{0-d7ff|e000-10ffff}")]
        public void Negate_Produces_Empty_Ranges(string x)
        {
            var a = Parse(x);

            Assert.Equal(UnicodeRanges.Empty, UnicodeRanges.Negate(a));
        }

        [Fact]
        public void Negate_Produces_All_Ranges_When_Empty_Input()
        {
            Assert.Equal(UnicodeRanges.All, UnicodeRanges.Negate(UnicodeRanges.Empty));
        }

        [Fact]
        public void Negate_Produces_Low_Surrogate_Range()
        {
            Assert.Equal(Parse(@"\u{e000-10ffff}"), UnicodeRanges.Negate(Parse(@"\u{0-d7ff}")));
        }

        [Fact]
        public void Negate_Produces_High_Surrogate_Range()
        {
            Assert.Equal(Parse(@"\u{0-d7ff}"), UnicodeRanges.Negate(Parse(@"\u{e000-10ffff}")));
        }

        [Theory]
        [InlineData(@"a-zA-Z0-9")]
        public void Negate_Produces_Expected_Result(string x)
        {
            var a = Parse(x);

            Assert.Equal(UnicodeRanges.Subtract(UnicodeRanges.All, a), UnicodeRanges.Negate(a));
        }

        [Theory]
        [InlineData(@"\u{0-10ffff}", @"\u{0-10ffff}")]
        [InlineData(@"aQw1~!", @"aAqQwW1~!")]
        [InlineData(@"a-z0-9", @"a-zA-Z0-9")]
        [InlineData(@"b-lC-O", @"b-oB-O")]
        [InlineData(@"B-x", @"A-z")]
        [InlineData(@"\u{110-120}", @"\u{110-120|111-121}")]
        [InlineData(@"\u{111-121}", @"\u{110-120|111-121}")]
        [InlineData(@"\u{139-147}", @"\u{139-148}")]
        [InlineData(@"\u{13a-148}", @"\u{139-148}")]
        [InlineData(@"\u{114|11a}", @"\u{114-115|11a-11b}")]
        [InlineData(@"\u{115|11b}", @"\u{114-115|11a-11b}")]
        [InlineData(@"\u{13b|147}", @"\u{13b-13c|147-148}")]
        [InlineData(@"\u{13c|148}", @"\u{13b-13c|147-148}")]
        public void ToAnyCase_Produces_Expected_Result(string x, string y)
        {
            var a = Parse(x);
            var b = Parse(y);

            Assert.Equal(b, UnicodeRanges.ToAnyCase(a));
        }
    }
}
