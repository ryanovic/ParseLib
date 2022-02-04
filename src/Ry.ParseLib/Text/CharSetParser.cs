namespace Ry.ParseLib.Text
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Generates a character set that matches to a specified pattern.
    /// </summary>
    /// <example>
    /// Supports:
    /// 
    /// @"abcde0123"         : distinct characters
    /// @"a-z0-9"            : ranges
    /// Unicode codes:
    /// @"\u{0|10-ff|ccc|10000-10FFFF}"
    /// Unicode categories:
    /// @"\p{Cc|Cf|Cn|Co|Cs|C|Ll|Lm|Lo|Lt|Lu|L|Mc|Me|Mn|M|Nd|Nl|No|N|Pc|Pd|Pe|Po|Ps|Pf|Pi|P|Sc|Sk|Sm|So|S|Zl|Zp|Zs|Z}"); 
    /// @"\a\b\t\r\n\v\f\\"  : escape sequences
    /// @"0-10ffff-[\p{L}]"  : subtract sub-set
    /// </example>
    internal sealed class CharSetParser
    {
        private const int StateDefault = 0;
        private const int StateRangeBegin = 1;
        private const int StateWaitingRangeEnd = 2;

        private readonly string pattern;
        private readonly CharSetBuilder builder;
        private readonly bool embedded;

        private int position;

        private int state;
        private int from;
        private CharSet except;


        public CharSetParser(string pattern)
            : this(pattern, 0)
        {
        }

        private CharSetParser(string pattern, int position)
        {
            if (pattern == null) throw new ArgumentNullException(nameof(pattern));

            this.pattern = pattern;
            this.builder = new CharSetBuilder();
            this.embedded = position > 0;
            this.position = position;
            this.state = StateDefault;
        }

        private char Lookahead => position < pattern.Length ? pattern[position] : (char)0;

        public CharSet Parse()
        {
            ParsePattern();
            return builder.CreateCharSet(except);
        }

        private char Read()
        {
            return position < pattern.Length ? pattern[position++] : (char)0;
        }

        private void ParsePattern()
        {
            while (true)
            {
                var ch = Read();

                if (ch == '\\')
                {
                    if (Lookahead == 'u')
                    {
                        CompleteRange();
                        position++;
                        ParseHexEscape();
                    }
                    else if (Lookahead == 'p')
                    {
                        CompleteRange();
                        position++;
                        ParseCategories();
                    }
                    else
                    {
                        ParseCharEscape();
                    }
                }
                else if (ch == '-')
                {
                    if (Lookahead == '[')
                    {
                        CompleteRange();
                        var embeddedParser = new CharSetParser(pattern, ++position);
                        except = embeddedParser.Parse();
                        position = embeddedParser.position;
                        CompletePattern();
                        return;
                    }
                    else if (state == StateRangeBegin)
                    {
                        state = StateWaitingRangeEnd;
                    }
                    else
                    {
                        HandleCode('-');
                    }
                }
                else if (ch == ']' && embedded)
                {
                    CompleteRange();
                    return;
                }
                else if (ch == (char)0)
                {
                    if (embedded)
                    {
                        throw CreateException(Errors.InvalidPattern());
                    }

                    CompleteRange();
                    return;
                }
                else
                {
                    HandleCode(ch);
                }
            }
        }

        private void CompletePattern()
        {
            if (embedded)
            {
                if (Read() != ']')
                {
                    throw CreateException(Errors.InvalidPattern());
                }
            }
            else
            {
                if (position != pattern.Length)
                {
                    throw CreateException(Errors.InvalidPattern());
                }
            }
        }

        private void ParseCharEscape()
        {
            switch (Read())
            {
                case (char)0: throw CreateException(Errors.InvalidPattern());
                case 'a':
                    HandleCode('\u0007');
                    break;
                case 'b':
                    HandleCode('\u0008');
                    break;
                case 't':
                    HandleCode('\u0009');
                    break;
                case 'r':
                    HandleCode('\u000D');
                    break;
                case 'v':
                    HandleCode('\u000B');
                    break;
                case 'f':
                    HandleCode('\u000C');
                    break;
                case 'n':
                    HandleCode('\u000A');
                    break;
                case char ch:
                    HandleCode(ch);
                    break;
            }
        }

        private void ParseHexEscape()
        {
            int hexCode = 0;
            var hasCode = false;
            char current;

            if (Read() != '{') throw CreateException(Errors.InvalidPattern());

            while ((current = Read()) != '}')
            {
                if (current >= '0' && current <= '9')
                {
                    hexCode = hexCode * 16 + (current - '0');
                    hasCode = true;
                }
                else if (current >= 'a' && current <= 'f')
                {
                    hexCode = hexCode * 16 + (current - 'a' + 10);
                    hasCode = true;
                }
                else if (current >= 'A' && current <= 'F')
                {
                    hexCode = hexCode * 16 + (current - 'A' + 10);
                    hasCode = true;
                }
                else if (current == '|')
                {
                    PutHexCode();
                    CompleteRange();
                }
                else if (current == '-')
                {
                    PutHexCode();
                    state = StateWaitingRangeEnd;
                }
                else
                {
                    throw CreateException(Errors.InvalidPattern());
                }
            }

            PutHexCode();
            CompleteRange();

            void PutHexCode()
            {
                if (!hasCode) CreateException(Errors.InvalidPattern());

                HandleCode(hexCode);
                hexCode = 0;
                hasCode = false;
            }
        }

        private void ParseCategories()
        {
            if (Read() != '{') CreateException(Errors.InvalidPattern());

            char current;
            var start = position;

            while ((current = Read()) != '}')
            {
                if (current == '|')
                {
                    PutCategory();
                    start = position;
                }
                else if (!Char.IsLetter(current))
                {
                    throw CreateException(Errors.InvalidPattern());
                }
            }

            PutCategory();

            void PutCategory()
            {
                var length = position - start - 1;
                if (length == 0) CreateException(Errors.InvalidPattern());

                var catName = pattern.Substring(start, length);

                if (!UnicodeCategories.Mapping.TryGetValue(catName, out var uc))
                {
                    CreateException(Errors.InvalidCategory(catName));
                }

                builder.Add(uc);
            }
        }

        private void CompleteRange()
        {
            if (state > StateDefault)
            {
                builder.Add(from);

                if (state == StateWaitingRangeEnd)
                {
                    builder.Add('-');
                }

                state = StateDefault;
            }
        }

        private void HandleCode(int code)
        {
            switch (state)
            {
                case StateDefault:
                    from = code;
                    state = StateRangeBegin;
                    break;
                case StateRangeBegin:
                    builder.Add(from);
                    from = code;
                    break;
                case StateWaitingRangeEnd:
                    builder.Add(from, code);
                    state = StateDefault;
                    break;
            }
        }

        private Exception CreateException(string message)
        {
            return new FormatException(message + Environment.NewLine
                + $"Pattern: {pattern}" + Environment.NewLine
                + $"Position: {position}");
        }
    }
}
