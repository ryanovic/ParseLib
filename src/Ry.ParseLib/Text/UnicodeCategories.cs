namespace Ry.ParseLib.Text
{
    using System;
    using System.Linq;
    using System.Globalization;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Represents a set of Unicode categories.
    /// </summary>
    public readonly struct UnicodeCategories : IEquatable<UnicodeCategories>
    {
        public const int Count = 30;
        private const int FullMask = 0x3fffffff;

        public static UnicodeCategories Empty = new UnicodeCategories(0);
        public static UnicodeCategories All = new UnicodeCategories(FullMask);

        public static Dictionary<string, UnicodeCategories> Mapping = new Dictionary<string, UnicodeCategories> {
            { "Cc", new UnicodeCategories(0x00004000)},     // Control 
            { "Cf", new UnicodeCategories(0x00008000)},     // Format 
            { "Cn", new UnicodeCategories(0x20000000)},     // OtherNotAssigned 
            { "Co", new UnicodeCategories(0x00020000)},     // PrivateUse 
            { "Cs", new UnicodeCategories(0x00010000)},     // Surrogate 
            { "C",  new UnicodeCategories(0x2003c000)},

            { "Ll", new UnicodeCategories(0x00000002)},     // LowercaseLetter 
            { "Lm", new UnicodeCategories(0x00000008)},     // ModifierLetter 
            { "Lo", new UnicodeCategories(0x00000010)},     // OtherLetter 
            { "Lt", new UnicodeCategories(0x00000004)},     // TitlecaseLetter 
            { "Lu", new UnicodeCategories(0x00000001)},     // UppercaseLetter 
            { "L",  new UnicodeCategories(0x0000001f)},

            { "Mc", new UnicodeCategories(0x00000040)},     // SpacingCombiningMark 
            { "Me", new UnicodeCategories(0x00000080)},     // EnclosingMark 
            { "Mn", new UnicodeCategories(0x00000020)},     // NonSpacingMark 
            { "M",  new UnicodeCategories(0x000000e0)},

            { "Nd", new UnicodeCategories(0x00000100)},     // DecimalDigitNumber 
            { "Nl", new UnicodeCategories(0x00000200)},     // LetterNumber 
            { "No", new UnicodeCategories(0x00000400)},     // OtherNumber 
            { "N",  new UnicodeCategories(0x00000700)},

            { "Pc", new UnicodeCategories(0x00040000)},     // ConnectorPunctuation 
            { "Pd", new UnicodeCategories(0x00080000)},     // DashPunctuation 
            { "Pe", new UnicodeCategories(0x00200000)},     // ClosePunctuation 
            { "Po", new UnicodeCategories(0x01000000)},     // OtherPunctuation 
            { "Ps", new UnicodeCategories(0x00100000)},     // OpenPunctuation 
            { "Pf", new UnicodeCategories(0x00800000)},     // FinalQuotePunctuation 
            { "Pi", new UnicodeCategories(0x00400000)},     // InitialQuotePunctuation 
            { "P",  new UnicodeCategories(0x01fc0000)},

            { "Sc", new UnicodeCategories(0x04000000)},     // CurrencySymbol 
            { "Sk", new UnicodeCategories(0x08000000)},     // ModifierSymbol 
            { "Sm", new UnicodeCategories(0x02000000)},     // MathSymbol 
            { "So", new UnicodeCategories(0x10000000)},     // OtherSymbol 
            { "S",  new UnicodeCategories(0x1e000000)},

            { "Zl", new UnicodeCategories(0x00001000)},     // LineSeparator 
            { "Zp", new UnicodeCategories(0x00002000)},     // ParagraphSeparator 
            { "Zs", new UnicodeCategories(0x00000800)},     // SpaceSeparator 
            { "Z",  new UnicodeCategories(0x00003800)},
        };

        private static int letterAnyCase = Mapping["Ll"].Set | Mapping["Lt"].Set | Mapping["Lu"].Set;

        public int Set { get; }

        public static UnicodeCategories Create(UnicodeCategory uc)
        {
            return new UnicodeCategories(1 << ((int)uc));
        }

        public static UnicodeCategories Create(params UnicodeCategory[] ucs)
        {
            int set = 0;

            for (int i = 0; i < ucs.Length; i++)
            {
                set |= 1 << ((int)ucs[i]);
            }

            return new UnicodeCategories(set);
        }

        internal UnicodeCategories(int set)
        {
            this.Set = set;
        }

        public bool IsEmpty => Set == 0;
        public bool IsFull => Set == FullMask;

        public UnicodeCategories ToAnyCase()
        {
            return (Set & letterAnyCase) > 0 ? new UnicodeCategories(Set | letterAnyCase) : this;
        }

        public UnicodeCategories Negate()
        {
            return new UnicodeCategories(~Set & FullMask);
        }

        public UnicodeCategories Union(UnicodeCategories ucs)
        {
            return new UnicodeCategories(Set | ucs.Set);
        }

        public UnicodeCategories Union(IEnumerable<UnicodeCategories> list) => list.Aggregate((a, b) => a.Union(b));

        public UnicodeCategories Intersect(UnicodeCategories ucs) => new UnicodeCategories(Set & ucs.Set);

        public UnicodeCategories Subtract(UnicodeCategories ucs) => new UnicodeCategories(Set & ~ucs.Set & FullMask);

        public bool Contains(UnicodeCategories ucs)
        {
            return (Set & ucs.Set) == ucs.Set;
        }

        public bool Contains(UnicodeCategory uc)
        {
            int tmp = 1 << ((int)uc);
            return (Set & tmp) == tmp;
        }

        public bool Equals(UnicodeCategories other)
        {
            return other.Set == Set;
        }

        public override bool Equals(object obj)
        {
            return obj is UnicodeCategories uc && uc.Set == Set;
        }

        public override int GetHashCode()
        {
            return Set;
        }

        public override string ToString()
        {
            var buffer = new StringBuilder();
            var set = this.Set;

            if (!AppendIfContains("C"))
            {
                AppendIfContains("Cc");
                AppendIfContains("Cf");
                AppendIfContains("Cn");
                AppendIfContains("Co");
                AppendIfContains("Cs");
            }

            if (!AppendIfContains("L"))
            {
                AppendIfContains("Ll");
                AppendIfContains("Lm");
                AppendIfContains("Lo");
                AppendIfContains("Lt");
                AppendIfContains("Lu");
            }

            if (!AppendIfContains("M"))
            {
                AppendIfContains("Mc");
                AppendIfContains("Me");
                AppendIfContains("Mn");
            }

            if (!AppendIfContains("N"))
            {
                AppendIfContains("Nd");
                AppendIfContains("Nl");
                AppendIfContains("No");
            }

            if (!AppendIfContains("P"))
            {
                AppendIfContains("Pc");
                AppendIfContains("Pd");
                AppendIfContains("Pe");
                AppendIfContains("Po");
                AppendIfContains("Ps");
                AppendIfContains("Pf");
                AppendIfContains("Pi");
            }

            if (!AppendIfContains("S"))
            {
                AppendIfContains("Sc");
                AppendIfContains("Sk");
                AppendIfContains("Sm");
                AppendIfContains("So");
            }

            if (!AppendIfContains("Z"))
            {
                AppendIfContains("Zl");
                AppendIfContains("Zp");
                AppendIfContains("Zs");
            }

            buffer.Replace("C|L|M|N|P|S|Z", "Full");
            return buffer.ToString();

            bool AppendIfContains(string name)
            {
                var uc = Mapping[name];

                if ((uc.Set & set) != uc.Set)
                {
                    return false;
                }

                if (buffer.Length > 0)
                {
                    buffer.Append('|');
                }

                buffer.Append(name);
                return true;
            }
        }
    }
}
