namespace ParseLib.Text
{
    using System;
    using System.Diagnostics;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public readonly struct UnicodeRange : IEquatable<UnicodeRange>
    {
        public const int SurrogateStart = 0xd800;
        public const int SurrogateLowStart = 0xdc00;
        public const int SurrogateEnd = 0xdfff;
        public const int Latin1Max = 0xff;
        public const int ExtendedStart = 0x10000;
        public const int Max = 0x10ffff;

        internal static bool IsValidRange(int code)
        {
            return code >= 0 && code <= Max;
        }

        public int From { get; }
        public int To { get; }

        public (int, int) Range => (From, To);

        public int Length => To - From + 1;

        public UnicodeRange(int code)
        {
            if (!IsValidRange(code)) throw new ArgumentOutOfRangeException(nameof(code));

            this.From = code;
            this.To = code;
        }

        public UnicodeRange(int from, int to)
        {
            if (!IsValidRange(from)) throw new ArgumentOutOfRangeException(nameof(from));
            if (!IsValidRange(to)) throw new ArgumentOutOfRangeException(nameof(to));

            if (from > to) throw new ArgumentException($"[{ToString(from)}-{ToString(to)}] range in reverse order.");

            this.From = from;
            this.To = to;
        }

        public bool Equals(UnicodeRange range)
        {
            return range.From == From && range.To == To;
        }

        public override bool Equals(object obj)
        {
            return obj is UnicodeRange range && Equals(range);
        }

        public override int GetHashCode()
        {
            var hashCode = -1781160927;
            hashCode = hashCode * -1521134295 + From;
            hashCode = hashCode * -1521134295 + To;
            return hashCode;
        }

        public override string ToString()
        {
            if (From == To)
            {
                return ToString(From);
            }

            if (IsInDisplayRange(From) && IsInDisplayRange(To))
            {
                return $"{(char)From}-{(char)To}";
            }

            return $"u{From:X}-u{To:X}";
        }

        private static string ToString(int code)
        {
            if (IsInDisplayRange(code))
            {
                return ((char)code).ToString();
            }

            return $"u{code:X}";
        }

        private static bool IsInDisplayRange(int code) => code >= 0x21 && code <= 0x7e;
    }
}
