namespace Ry.ParseLib
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection.Emit;
    using Ry.ParseLib.Emit;
    using Ry.ParseLib.LALR;
    using Ry.ParseLib.Runtime;
    using Ry.ParseLib.Text;

    public static class GrammarExtensions
    {
        /// <summary>
        /// Creates an empty lexical states builder. 
        /// </summary>
        public static ILexicalStates CreateLexicalStates(this Grammar grammar)
        {
            return new LexicalStatesBuilder(grammar.IgnoreCase);
        }

        /// <summary>
        /// Creates a set of parser states corresponding to a <c>goal</c> symbol.
        /// </summary>
        public static IParserStates CreateParserStates(this Grammar grammar, string goal)
        {
            var builder = new ParserStatesBuilder(grammar[goal], grammar.ConflictResolver);
            return builder.CreateStates();
        }

        /// <summary>
        /// Creates a parser metadata corresponding to a <c>goal</c> symbol. Contains computed parser and lexer states.
        /// </summary>
        public static ParserMetadata CreateParserMetadata(this Grammar grammar, string goal)
        {
            return ParserMetadata.Create(grammar, CreateLexicalStates(grammar), CreateParserStates(grammar, goal));
        }

        /// <summary>
        /// Creates a type that represents a string parser.
        /// </summary>
        public static Type CreateStringParser<T>(this Grammar grammar, string goal)
            where T : StringParser
        {
            return DynamicModule.CreateStringParser(typeof(T), grammar, goal);
        }

        /// <summary>
        /// Creates a type that represents a string parser and returns a corresponding factory method.
        /// </summary>
        public static Func<string, T> CreateStringParserFactory<T>(this Grammar grammar, string goal)
            where T : StringParser
        {
            var type = CreateStringParser<T>(grammar, goal);
            var ctor = type.GetConstructor(new[] { typeof(string) });

            if (ctor == null)
            {
                throw new InvalidOperationException(Errors.StringConstructorExpected(typeof(T).Name));
            }

            var facotry = new DynamicMethod("Factory", typeof(T), new[] { typeof(string) });
            var il = facotry.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Newobj, ctor);
            il.Emit(OpCodes.Ret);

            return (Func<string, T>)facotry.CreateDelegate(typeof(Func<string, T>));
        }

        /// <summary>
        /// Creates a type that represents a text parser.
        /// </summary>
        public static Type CreateTextParser<T>(this Grammar grammar, string goal)
            where T : TextParser
        {
            return DynamicModule.CreateTextParser(typeof(T), grammar, goal);
        }

        /// <summary>
        /// Creates a type that represents a text parser and returns a corresponding factory method.
        /// </summary>
        public static Func<TextReader, T> CreateTextParserFactory<T>(this Grammar grammar, string goal)
            where T : TextParser
        {
            var type = CreateTextParser<T>(grammar, goal);
            var ctor = type.GetConstructor(new[] { typeof(TextReader) });

            if (ctor == null)
            {
                throw new InvalidOperationException(Errors.TextReaderConstructorExpected(typeof(T).Name));
            }

            var facotry = new DynamicMethod("Factory", typeof(T), new[] { typeof(TextReader) });
            var il = facotry.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Newobj, ctor);
            il.Emit(OpCodes.Ret);

            return (Func<TextReader, T>)facotry.CreateDelegate(typeof(Func<TextReader, T>));
        }
    }
}
