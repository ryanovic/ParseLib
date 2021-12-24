using ParseLib.Runtime;
using ParseLib.Text;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Xunit;

namespace ParseLib.Emit
{
    public class ParserBuilderTests
    {
        private readonly Grammar grammar;
        private readonly Type strParserType;
        private readonly Type seqParserType;

        public Type CreateStringTestParser(ModuleBuilder module, IParserReducer reducer, ParserMetadata metadata)
        {
            var target = module.DefineType("String_Parser", TypeAttributes.Public, typeof(StringTestParser));
            var builder = new StringParserBuilder(target, reducer, metadata);
            return builder.Build();
        }

        public Type CreateSequentialTestParser(ModuleBuilder module, IParserReducer reducer, ParserMetadata metadata)
        {
            var target = module.DefineType("Sequential_Lexer", TypeAttributes.Public, typeof(SequentialTestParser));
            var builder = new SequentialParserBuilder(target, reducer, metadata);
            return builder.Build();
        }

        public ParserBuilderTests()
        {
            var assemblyName = new AssemblyName($"ParseLib.Dynamic.Tests_{Guid.NewGuid()}");
            var assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var module = assembly.DefineDynamicModule(assemblyName.Name);

            var cmnt_char = Rex.Except('\n');
            var cmnt_char_lb = Rex.AnyChar;

            grammar = new Grammar();
            grammar.CreateNonTerminals("S", "A", "B", "C", "D", "CC");

            grammar.CreateWhitespace("ws", Rex.Char(' '));
            grammar.CreateWhitespace("lb", Rex.Char('\n'), isLineBreak: true);

            grammar.CreateWhitespace("cmnt", Rex.Text("/*").Then(cmnt_char.NoneOrMore()).Then("*/"), lazy: true);
            grammar.CreateWhitespace("cmnt_lb", Rex.Text("/*").Then(cmnt_char_lb.NoneOrMore()).Then("*/"), isLineBreak: true, lazy: true);

            grammar.CreateTerminal("A:a", Rex.Char('a'));
            grammar.CreateTerminal("A:aaa", Rex.Text("aaa"));
            grammar.CreateTerminal("B:b", Rex.Char('b'));
            grammar.CreateTerminal("C:c", Rex.Char('c'));
            grammar.CreateTerminal("D:d", Rex.Char('d'));

            grammar.AddRule("CC", "C C");
            grammar.AddRule("S:ABC", "A B C");
            grammar.AddRule("S:AA", "A A");
            grammar.AddRule("S:AC", "A [LB] C");
            grammar.AddRule("S:AD", "A [NoLB] D");
            grammar.AddRule("S:CC", "CC");

            var reducer = ParserReducer.CreateReducer(typeof(TestParser), grammar);
            var metadata = grammar.CreateParserMetadata("S");

            strParserType = CreateStringTestParser(module, reducer, metadata);
            seqParserType = CreateSequentialTestParser(module, reducer, metadata);
        }

        [Theory]
        [ClassData(typeof(ValidData))]
        public void String_Parser_Reduces_To_Target_When_Input_Is_Valid(string input)
        {
            var parser = (StringTestParser)Activator.CreateInstance(strParserType, new object[] { input });
            parser.Parse();
            Assert.Equal("S", parser.GetResult());
        }

        [Theory]
        [ClassData(typeof(InvalidData))]
        public void String_Parser_Throws_Error_When_Input_Is_NOT_Valud(string input, string expectedState)
        {
            var parser = (StringTestParser)Activator.CreateInstance(strParserType, new object[] { input });
            var ex = Assert.Throws<ParserException>(() => parser.Parse());

            Assert.Equal(expectedState, ex.ParserState);
        }

        [Theory]
        [ClassData(typeof(ValidData))]
        public void Sequential_Parser_Reduces_To_Target_When_Input_Is_Valid(string input)
        {
            var parser = (SequentialTestParser)Activator.CreateInstance(seqParserType, new object[] { input });
            parser.Parse();
            Assert.Equal("S", parser.GetResult());
        }

        [Theory]
        [ClassData(typeof(InvalidData))]
        public void Sequential_Parser_Throws_Error_When_Input_Is_NOT_Valud(string input, string expectedState)
        {
            var parser = (SequentialTestParser)Activator.CreateInstance(seqParserType, new object[] { input });
            var ex = Assert.Throws<ParserException>(() => parser.Parse());

            Assert.Equal(expectedState, ex.ParserState);
        }

        public class InvalidData : TheoryData<string, string>
        {
            public InvalidData()
            {
                Add("a b d", "A b");
                Add("a \n d", "A");
                Add("a /*** linebreak \n comment ***/ d", "A");
                Add("a c", "A");
                Add("a /*** comment ***/ c", "A");
                Add("a x", "a");
                Add("c c c", "C C");
                Add("c", "C");
                Add("a", "A");
                Add("a b", "A b");
                Add("aaaaa", "A A");
            }
        }

        public class ValidData : TheoryData<string>
        {
            public ValidData()
            {
                Add("abc");
                Add("aa");
                Add("aaa d");
                Add("aaaa");
                Add("aaaaaa");
                Add("a d");
                Add("a /*** comment ***/ d");
                Add("a \n b c");
                Add("a /*** linebreak \n comment ***/ b c");
                Add("a \n c");
                Add("a /*** linebreak \n comment ***/ c");
                Add("c \n c");
            }
        }

        public abstract class SequentialTestParser : TestParser
        {
            private readonly char[] buffer;

            public SequentialTestParser(string content)
            {
                this.buffer = content.ToCharArray();
            }

            public override void Parse()
            {
                Read(0, buffer, 0, buffer.Length, true);
            }

            protected abstract bool Read(int bufferPosition, char[] buffer, int offset, int length, bool isEndOfSource);

            protected override string GetLexeme()
            {
                return CurrentPosition > StartPosition
                    ? new String(buffer, StartPosition, CurrentPosition - StartPosition)
                    : null;
            }
        }

        public abstract class StringTestParser : TestParser
        {
            private readonly string content;

            public StringTestParser(string content)
            {
                this.content = content;
            }

            public override void Parse()
            {
                Read(content, 0, content.Length);
            }

            protected abstract void Read(string content, int offset, int length);

            protected override string GetLexeme()
            {
                return CurrentPosition > StartPosition
                    ? content.Substring(StartPosition, CurrentPosition - StartPosition)
                    : null;
            }
        }

        public abstract class TestParser : ParserBase
        {
            public Stack<string> Expected { get; } = new Stack<string>();

            [CompleteToken("ws")]
            public string CompleteToken_ws()
            {
                Assert.Equal(' ', GetChar());
                return "ignore";
            }

            [CompleteToken("lb")]
            public void CompleteToken_lb()
            {
                Assert.Equal('\n', GetChar());
            }

            [CompleteToken("cmnt")]
            public void CompleteToken_cmnt()
            {
                Assert.Equal("/*** comment ***/", GetLexeme());
            }

            [CompleteToken("cmnt_lb")]
            public void CompleteToken_cmnt_lb()
            {
                Assert.Equal("/*** linebreak \n comment ***/", GetLexeme());
            }

            [CompleteToken("a")]
            public string CompleteToken_a()
            {
                Assert.Equal('a', GetChar());
                return "a";
            }

            [CompleteToken("c")]
            public string CompleteToken_c()
            {
                Assert.Equal('c', GetChar());
                return "c";
            }

            [CompleteToken("d")]
            public void CompleteToken_d()
            {
                Assert.Equal('d', GetChar());
            }

            [Reduce("A:a")]
            public string Reduce_A(string a)
            {
                Assert.Equal("a", a);
                Assert.Equal("a", Expected.Pop());
                return "A";
            }

            [Reduce("A:aaa")]
            public string Reduce_A_2()
            {
                return "A";
            }

            [Reduce("B:b")]
            public static string Reduce_B()
            {
                return "B";
            }

            [Reduce("C:c")]
            public static string Reduce_C(string c)
            {
                Assert.Equal("c", c);
                return "C";
            }

            [Reduce("CC")]
            public static string Reduce_CC(string C1, string C2)
            {
                Assert.Equal("C", C1);
                Assert.Equal("C", C2);
                return "CC";
            }

            [Reduce("S:ABC")]
            public string Reduce_S_ABC(string A, string B, string C)
            {
                Assert.Equal("A", A);
                Assert.Equal("B", B);
                Assert.Equal("C", C);
                Assert.Equal("C", Expected.Pop());
                Assert.Equal("B", Expected.Pop());
                Assert.Equal("A", Expected.Pop());
                return "S";
            }

            [Reduce("S:AC")]
            public static string Reduce_S_AC(string A, string C)
            {
                Assert.Equal("A", A);
                Assert.Equal("C", C);
                return "S";
            }

            [Reduce("S:AD")]
            public string Reduce_S_AD(string A)
            {
                Assert.Equal("A", A);
                Assert.Equal("D", Expected.Pop());
                Assert.Equal("A", Expected.Pop());
                return "S";
            }

            [Reduce("S:CC")]
            public static string Reduce_S_CC(string CC)
            {
                Assert.Equal("CC", CC);
                return "S";
            }

            [Reduce("S:AA")]
            public static string Reduce_S_AA(string A1, string A2)
            {
                Assert.Equal("A", A1);
                Assert.Equal("A", A2);
                return "S";
            }


            [Handle("a")]
            public void Handle_a(string a)
            {
                Assert.Equal("a", a);
                Expected.Push("a");
            }

            [Handle("A")]
            public void Handle_A(string A)
            {
                Assert.Equal("A", A);
                Expected.Push("A");
            }

            [Handle("A B")]
            public void Handle_AB()
            {
                Expected.Push("B");
            }

            [Handle("A B C")]
            public string Handle_ABC(string A, string B, string C)
            {
                Assert.Equal("A", A);
                Assert.Equal("B", B);
                Assert.Equal("C", C);
                Expected.Push("C");
                return "ignore";
            }

            [Handle("A D")]
            public void Handle_D(string A)
            {
                Assert.Equal("A", A);
                Expected.Push("D");
            }

            protected char GetChar()
            {
                var lexem = GetLexeme();
                Assert.NotNull(lexem);
                Assert.Equal(1, lexem.Length);
                return lexem[0];
            }
        }
    }
}
