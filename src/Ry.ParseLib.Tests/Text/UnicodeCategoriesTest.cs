namespace Ry.ParseLib.Text
{
    using System;
    using System.Linq;
    using System.Globalization;
    using System.Collections.Generic;
    using Xunit;

    public class UnicodeCategoriesTest
    {
        public static IDictionary<string, UnicodeCategories> Mapping => UnicodeCategories.Mapping;
        public static UnicodeCategories Parse(string pattern) => CharSet.Parse(pattern).Categories;


        [Theory]
        [InlineData(UnicodeCategory.UppercaseLetter, "Lu")]
        [InlineData(UnicodeCategory.Control, "Cc")]
        public void Creates_Set_From_Signle_Category(UnicodeCategory uc, string category)
        {
            var set = UnicodeCategories.Create(uc);

            Assert.Equal(set, Mapping[category]);
        }

        [Fact]
        public void Creates_Set_From_Multiple_Categories()
        {
            var set = UnicodeCategories.Create(
                UnicodeCategory.UppercaseLetter,
                UnicodeCategory.Control,
                UnicodeCategory.CurrencySymbol,
                UnicodeCategory.OtherSymbol);

            var expected = Parse(@"\p{Lu|Cc|Sc|So}");

            Assert.Equal(expected, set);
        }

        [Fact]
        public void Negate_Produces_Expected_Result()
        {
            var uc = UnicodeCategory.UppercaseLetter;
            var set = UnicodeCategories.Create(UnicodeCategory.UppercaseLetter).Negate();

            foreach (var other in Enum.GetValues(typeof(UnicodeCategory)).OfType<UnicodeCategory>().Where(x => x != uc))
            {
                Assert.True(set.Contains(other));
            }

            Assert.False(set.Contains(uc));
        }

        [Theory]
        [InlineData(@"\p{Lu}", @"\p{Cc}", @"\p{Lu|Cc}")]
        [InlineData(@"\p{Sc|So}", @"\p{Lu|So}", @"\p{Lu|Sc|So}")]
        public void Union_Produces_Expected_Result(string x, string y, string z)
        {
            var a = Parse(x);
            var b = Parse(y);
            var c = Parse(z);

            Assert.Equal(c, a.Union(b));
        }

        [Theory]
        [InlineData(@"\p{Lu|Cc}", @"\p{Sc|Cc}", @"\p{Cc}")]
        [InlineData(@"\p{Lu|Cc}", @"\p{Sc|So}", @"")]
        public void Intersect_Produces_Expected_Result(string x, string y, string z)
        {
            var a = Parse(x);
            var b = Parse(y);
            var c = Parse(z);

            Assert.Equal(c, a.Intersect(b));
        }

        [Theory]
        [InlineData(@"\p{Lu|Cc}", @"\p{Sc|Cc}", @"\p{Lu}")]
        [InlineData(@"\p{Lu|Cc}", @"\p{Sc|So}", @"\p{Lu|Cc}")]
        public void Subtract_Produces_Expected_Result(string x, string y, string z)
        {
            var a = Parse(x);
            var b = Parse(y);
            var c = Parse(z);

            Assert.Equal(c, a.Subtract(b));
        }

        [Theory]
        [InlineData(@"\p{Lu|Cc}", @"\p{Ll|Lt|Lu|Cc}")]
        public void ToAnyCase_Produces_Expected_Result(string x, string y)
        {
            var a = Parse(x);
            var b = Parse(y);

            Assert.Equal(b, a.ToAnyCase());
        }
    }
}
