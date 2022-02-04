namespace Ry.ParseLib.Emit
{
    using System;
    using System.Globalization;
    using System.Reflection;
    using System.Collections.Generic;
    using Ry.ParseLib.Runtime;

    /// <summary>
    /// Stores shortcuts for reflection types.
    /// </summary>
    internal static class ReflectionInfo
    {
        public static Type LookaheadTuple => typeof(ValueTuple<int, int, int>);

        public static ConstructorInfo LookaheadTuple_Ctor => LookaheadTuple.GetConstructor(new[] { typeof(int), typeof(int), typeof(int) });

        public static FieldInfo LookaheadTuple_Item1 => LookaheadTuple.GetField("Item1");

        public static FieldInfo LookaheadTuple_Item2 => LookaheadTuple.GetField("Item2");

        public static FieldInfo LookaheadTuple_Item3 => LookaheadTuple.GetField("Item3");

        public static Type LookaheadStack => typeof(Stack<ValueTuple<int, int, int>>);

        public static ConstructorInfo LookaheadStack_Ctor => LookaheadStack.GetConstructor(Type.EmptyTypes);

        public static PropertyInfo LookaheadStack_Count => LookaheadStack.GetProperty("Count");

        public static MethodInfo LookaheadStack_Count_Get => LookaheadStack_Count.GetGetMethod();

        public static MethodInfo LookaheadStack_Pop => LookaheadStack.GetMethod("Pop");

        public static MethodInfo LookaheadStack_Push => LookaheadStack.GetMethod("Push", new[] { LookaheadTuple });

        public static MethodInfo CharSpan_Length_Get => typeof(Span<char>).GetMethod("get_Length");

        public static MethodInfo CharSpan_Item_Get => typeof(Span<char>).GetMethod("get_Item");

        public static MethodInfo ReadOnlyCharSpan_Length_Get => typeof(ReadOnlySpan<char>).GetMethod("get_Length");

        public static MethodInfo ReadOnlyCharSpan_Item_Get => typeof(ReadOnlySpan<char>).GetMethod("get_Item");

        public static PropertyInfo String_Length => typeof(string).GetProperty("Length");

        public static MethodInfo String_Length_Get => String_Length.GetGetMethod();

        public static MethodInfo String_Get => typeof(string).GetMethod("get_Chars", new[] { typeof(int) });

        public static MethodInfo String_Substring => typeof(string).GetMethod("Substring", new[] { typeof(int), typeof(int) });

        public static MethodInfo String_Join => typeof(string).GetMethod("Join", new[] { typeof(string), typeof(string[]) });

        public static MethodInfo String_Format => typeof(string).GetMethod("Format", new[] { typeof(string), typeof(object) });

        public static MethodInfo Char_ToUtf32 => typeof(Char).GetMethod("ConvertToUtf32", new[] { typeof(char), typeof(char) });

        public static MethodInfo Char_FromUtf32 => typeof(Char).GetMethod("ConvertFromUtf32", new[] { typeof(int) });

        public static MethodInfo Char_GetCategoryByChar => typeof(CharUnicodeInfo).GetMethod("GetUnicodeCategory", new[] { typeof(char) });

        public static MethodInfo Char_GetCategoryByInt32 => typeof(CharUnicodeInfo).GetMethod("GetUnicodeCategory", new[] { typeof(int) });

        public static MethodInfo Char_GetCategoryByStr => typeof(CharUnicodeInfo).GetMethod("GetUnicodeCategory", new[] { typeof(string), typeof(int) });

        public static Type ParserBase => typeof(ParserBase);

        public static MethodInfo ParserBase_CreateParserExceptionByMessage =>
            ParserBase.GetMethod("CreateParserException", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(string) }, null);

        public static MethodInfo ParserBase_CreateParserExceptionByException =>
            ParserBase.GetMethod("CreateParserException", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(Exception) }, null);

        public static MethodInfo SyntaxException_Lookahead_Set => typeof(ParserException).GetMethod("set_Lookahead", new[] { typeof(char) });

        public static MethodInfo ParserException_State_Set => typeof(ParserException).GetMethod("set_State", new[] { typeof(char) });

        public static ConstructorInfo ArgumentNullException_Ctor => typeof(ArgumentNullException).GetConstructor(new[] { typeof(string) });

        public static ConstructorInfo ArgumentOutOfRangeException_Ctor => typeof(ArgumentOutOfRangeException).GetConstructor(new[] { typeof(string), typeof(string) });

        public static ConstructorInfo InvalidOperationException_Ctor => typeof(InvalidOperationException).GetConstructor(new[] { typeof(string) });
    }
}
