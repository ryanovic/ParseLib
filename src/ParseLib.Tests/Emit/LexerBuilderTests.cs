namespace ParseLib.Emit
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Text;
    using Xunit;
    using ParseLib.Text;

    public class LexerBuilderTests
    {
        private readonly List<Terminal> terminals;
        private readonly LexicalStatesBuilder lexStateBuilder;
        private readonly LexicalState lexState;
        private readonly Type stringLexerType;
        private readonly Type sequentialLexerType;

        public LexerBuilderTests()
        {
            var assemblyName = new AssemblyName($"ParseLib.Dynamic.Tests_{Guid.NewGuid()}");
            var assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var module = assembly.DefineDynamicModule(assemblyName.Name);

            terminals = new List<Terminal>();

            var cmnt_char = Rex.IfNot("-->").Then(Rex.AnyChar);
            var cmnt_expr = Rex.Text("<!--").Then(cmnt_char.NoneOrMore()).Then("-->");

            AddTerminal("ws", Rex.Char(' ').OneOrMore());
            AddTerminal("kw", Rex.Text("keyword"));
            AddTerminal("comment", cmnt_expr);
            AddTerminal("num", Rex.Char(@"\p{N}").OneOrMore());
            AddTerminal("id", Rex.Char(@"\p{L}").OneOrMore());
            AddTerminal(".", Rex.Char('.'));
            AddTerminal("...", Rex.Char('.').Repeat(3));

            lexStateBuilder = new LexicalStatesBuilder();
            lexState = lexStateBuilder.CreateStates(terminals);
            stringLexerType = TestLexerBuilder.CreateStringTestLexer(module, lexStateBuilder, lexState);
            sequentialLexerType = TestLexerBuilder.CreateSequentalTestLexer(module, lexStateBuilder, lexState);
        }

        [Theory]
        [ClassData(typeof(LexerData))]
        public void Creates_String_Lexer(string input, string expected)
        {
            var lexer = (StringTestLexer)Activator.CreateInstance(stringLexerType);
            lexer.Parse(input);
            Assert.Equal(expected, ToString(lexer.Tokens));
        }

        [Theory]
        [ClassData(typeof(LexerData))]
        public void Creates_Sequential_Lexer(string input, string expected)
        {
            var lexer = (SequentialTestLexer)Activator.CreateInstance(sequentialLexerType);
            lexer.Parse(input);
            Assert.Equal(expected, ToString(lexer.Tokens));
        }

        public class LexerData : TheoryData<string, string>
        {
            public LexerData()
            {
                Add("a1", "id:1 num:1");
                Add("aa12", "id:2 num:2");
                Add("a 1 .", "id:1 ws:1 num:1 ws:1 .:1");

                Add("keywor", "id:6");
                Add("keyword", "kw:7");
                Add("keywordd", "id:8");

                Add("𝒳𝒴𝒵", "id:6");

                Add(".", ".:1");
                Add("..", ".:1 .:1");
                Add("...", "...:3");

                Add("<!--56 -->!--", "comment:10");
                Add("<!--56 --><-->", "comment:10 ~1");
                Add("<!--56 -- xxx yy --><!-->", "comment:20 ~5");
            }
        }

        private void AddTerminal(string name, RexNode expr)
        {
            terminals.Add(new Terminal(name, expr, terminals.Count));
        }

        private string ToString(IEnumerable<TokenMatch> tokens)
        {
            var position = 0;
            var buffer = new StringBuilder();

            foreach (var token in tokens)
            {
                if (token.id == -1)
                {
                    if (token.position != position)
                    {
                        Append($"~{token.position - position}");
                    }

                    return buffer.ToString();
                }
                else
                {
                    Append($"{terminals[token.id].Name}:{token.position - position}");
                    position = token.position;
                }
            }

            return "no-eos";

            void Append(string text)
            {
                if (position > 0)
                {
                    buffer.Append(' ');
                }

                buffer.Append(text);
            }
        }

        public class TokenMatch
        {
            public int id;
            public int position;

            public TokenMatch(int id, int position)
            {
                this.id = id;
                this.position = position;
            }
        }

        public abstract class TestLexer
        {
            public bool Completed { get; private set; }
            public abstract int Position { get; }
            public readonly List<TokenMatch> Tokens = new List<TokenMatch>();

            public void Match(int tokenId)
            {
                if (tokenId == -1)
                {
                    Completed = true;
                }

                Tokens.Add(new TokenMatch(tokenId, Position));
            }

            public abstract void Parse(string content);
        }

        public abstract class StringTestLexer : TestLexer
        {
            public abstract void Read(string content, int offset, int length);

            public override void Parse(string content)
            {
                Read(content, 0, content.Length);
            }
        }

        public abstract class SequentialTestLexer : TestLexer
        {
            public abstract bool Read(int bufferPosition, char[] buffer, int offset, int length, bool endOfSource);

            public override void Parse(string content)
            {
                var buffer = content.ToCharArray();
                var bufferPosition = 0;

                while (bufferPosition <= content.Length)
                {
                    var eos = bufferPosition == content.Length;

                    if (Read(bufferPosition, buffer, bufferPosition, eos ? 0 : 1, eos))
                    {
                        bufferPosition++;
                    }
                    else
                    {
                        bufferPosition--;
                    }

                    if (Completed)
                    {
                        return;
                    }
                }
            }
        }

        private class TestLexerBuilder : ILexerTarget
        {
            private readonly LexicalState lexState;
            private readonly Cell<int> state;

            public static Type CreateStringTestLexer(ModuleBuilder module, LexicalStatesBuilder lexStateBuilder, LexicalState lexState)
            {
                var target = module.DefineType("String_Lexer", TypeAttributes.Public, typeof(StringTestLexer));
                var state = target.CreateCell<int>("state");
                var position = target.CreateCell<int>("position");

                BuildConstructor(target, ctor_il =>
                {
                    state.Update(ctor_il, lexState.Id);
                    position.Update(ctor_il, 0);
                });

                BuildPositionProperty(target, position);

                var builder = new TestLexerBuilder(lexState, state);

                var method = target.DefineMethod("Read",
                    MethodAttributes.Public | MethodAttributes.Virtual,
                    typeof(void),
                    new[] { typeof(string), typeof(int), typeof(int) });

                var il = method.GetILGenerator();

                var lhStack = lexStateBuilder.HasLookaheads
                    ? il.CreateLookaheadStack()
                    : null;

                var acceptedPosition = il.CreateCell<int>();
                var acceptedId = il.CreateCell<int>();

                acceptedId.Update(il, -1);
                lhStack?.Initialize(il);

                var lexer = new LexerBuilder(
                    il, lexStateBuilder, new StringParserSource(), builder, lhStack, state, position, acceptedPosition, acceptedId, null);
                lexer.Build();

                return target.CreateType();
            }

            public static Type CreateSequentalTestLexer(ModuleBuilder module, LexicalStatesBuilder lexStateBuilder, LexicalState lexState)
            {
                var target = module.DefineType("Sequential_Lexer", TypeAttributes.Public, typeof(SequentialTestLexer));

                var state = target.CreateCell<int>("state");
                var position = target.CreateCell<int>("position");
                var acceptedPosition = target.CreateCell<int>("acceptedPosition");
                var acceptedId = target.CreateCell<int>("acceptedId");
                var highSurrogate = target.CreateCell<int>("highSurrogate");

                var lhStack = lexStateBuilder.HasLookaheads
                    ? target.CreateLookaheadStack("lhStack")
                    : null;

                BuildConstructor(target, il =>
                {
                    state.Update(il, lexState.Id);
                    position.Update(il, 0);
                    acceptedId.Update(il, -1);
                    lhStack?.Initialize(il);
                });

                BuildPositionProperty(target, position);

                var builder = new TestLexerBuilder(lexState, state);
                var source = new SequentialParserSource();

                var method = target.DefineMethod("Read",
                    MethodAttributes.Public | MethodAttributes.Virtual,
                    typeof(bool),
                    new[] { typeof(int), typeof(char[]), typeof(int), typeof(int), typeof(bool) });

                var lexer = new LexerBuilder(
                    method.GetILGenerator(), lexStateBuilder, source, builder, lhStack, state, position, acceptedPosition, acceptedId, highSurrogate);
                lexer.Build();

                return target.CreateType();
            }

            private static void BuildPositionProperty(TypeBuilder target, Cell<int> position)
            {
                var mthd = target.DefineMethod(
                    "get_Position", MethodAttributes.Public | MethodAttributes.Virtual, typeof(int), Type.EmptyTypes);

                var il = mthd.GetILGenerator();
                position.Load(il);
                il.Emit(OpCodes.Ret);
            }

            private static void BuildConstructor(TypeBuilder target, Action<ILGenerator> initialize)
            {
                var ctor = target.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
                var il = ctor.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, target.BaseType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null));
                initialize(il);
                il.Emit(OpCodes.Ret);
            }

            public TestLexerBuilder(LexicalState lexState, Cell<int> state)
            {
                this.lexState = lexState;
                this.state = state;
            }

            public void CompleteSource(ILGenerator il, ILexerSource _)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldc_I4_M1);
                il.Emit(OpCodes.Callvirt, typeof(TestLexer).GetMethod("Match"));
            }

            public void CompleteToken(ILGenerator il, int tokenId)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldc_I4, tokenId);
                il.Emit(OpCodes.Callvirt, typeof(TestLexer).GetMethod("Match"));
                state.Update(il, lexState.Id);
            }

            public void CompleteToken(ILGenerator il, Cell<int> tokenId)
            {
                il.Emit(OpCodes.Ldarg_0);
                tokenId.Load(il);
                il.Emit(OpCodes.Callvirt, typeof(TestLexer).GetMethod("Match"));
                state.Update(il, lexState.Id);
            }
        }
    }
}
