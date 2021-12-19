namespace ParseLib.Text
{
    using System;
    using System.Collections.Generic;
    using System.Text;

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
                        throw new InvalidOperationException("Invalid embedded pattern.");
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
                    throw new InvalidOperationException("Invalid embedded pattern.");
                }
            }
            else
            {
                if (position != pattern.Length)
                {
                    throw new InvalidOperationException("Invalid pattern.");
                }
            }
        }

        private void ParseCharEscape()
        {
            switch (Read())
            {
                case (char)0: throw new InvalidOperationException("Invalid escape character.");
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

            if (Read() != '{') throw new InvalidOperationException("Invalid hex escape pattern.");

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
                    throw new InvalidOperationException("Invalid character code value.");
                }
            }

            PutHexCode();
            CompleteRange();

            void PutHexCode()
            {
                if (!hasCode) throw new InvalidOperationException("Character code expected.");

                HandleCode(hexCode);
                hexCode = 0;
                hasCode = false;
            }
        }

        private void ParseCategories()
        {
            if (Read() != '{') throw new InvalidOperationException("Invalid unicode categories pattern.");

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
                    throw new InvalidOperationException($"Invalid unicode category name.");
                }
            }

            PutCategory();

            void PutCategory()
            {
                var length = position - start - 1;
                if (length == 0) throw new InvalidOperationException("Empty category name.");

                if (!UnicodeCategories.Mapping.TryGetValue(pattern.Substring(start, length), out var uc))
                {
                    throw new InvalidOperationException($"Invalid unicode category name.");
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
    }
}
