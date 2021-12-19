using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using Xunit;

namespace ParseLib.Emit
{
    public class StringParserSourceTests
    {
        [Theory]
        [InlineData(0, "test", 0, 4)]
        [InlineData(2, "test", 2, 2)]
        [InlineData(4, "test", 0, 4)]
        public void CheckLowerBound_Emits_True_When_InBound(int position, string content, int offset, int length)
        {
            var func = CreateDelegate<bool>((il, pos) =>
            {
                var isValid = il.DefineLabel();
                var source = new StringParserSource();
                source.CheckLowerBound(il, pos, isValid);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ret);

                il.MarkLabel(isValid);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Ret);
            });

            Assert.True(func(position, content, offset, length));
        }

        [Theory]
        [InlineData(1, "test", 2, 2)]
        [InlineData(0, "test", 1, 3)]
        public void CheckLowerBound_Emits_False_When_OutOfBound(int position, string content, int offset, int length)
        {
            var func = CreateDelegate<bool>((il, pos) =>
            {
                var isValid = il.DefineLabel();
                var source = new StringParserSource();
                source.CheckLowerBound(il, pos, isValid);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ret);

                il.MarkLabel(isValid);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Ret);
            });

            Assert.False(func(position, content, offset, length));
        }

        [Theory]
        [InlineData(0, "test", 0, 4)]
        [InlineData(1, "test", 2, 2)]
        [InlineData(3, "test", 0, 4)]
        public void CheckUpperBound_Emits_True_When_InBound(int position, string content, int offset, int length)
        {
            var func = CreateDelegate<bool>((il, pos) =>
            {
                var isValid = il.DefineLabel();
                var source = new StringParserSource();
                source.CheckUpperBound(il, pos, isValid);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ret);

                il.MarkLabel(isValid);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Ret);
            });

            Assert.True(func(position, content, offset, length));
        }

        [Theory]
        [InlineData(2, "test", 2, 2)]
        [InlineData(5, "test", 0, 4)]
        public void CheckUpperBound_Emits_False_When_OutOfBound(int position, string content, int offset, int length)
        {
            var func = CreateDelegate<bool>((il, pos) =>
            {
                var isValid = il.DefineLabel();
                var source = new StringParserSource();
                source.CheckUpperBound(il, pos, isValid);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ret);

                il.MarkLabel(isValid);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Ret);
            });

            Assert.False(func(position, content, offset, length));
        }

        [Fact]
        public void CheckIsLastChunk_Emits_True()
        {
            var func = CreateDelegate<bool>((il, _) =>
            {
                var isLast = il.DefineLabel();
                var source = new StringParserSource();
                source.CheckIsLastChunk(il, isLast);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ret);

                il.MarkLabel(isLast);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Ret);
            });

            Assert.True(func(0, "content", 0, "content".Length));
        }

        [Theory]
        [InlineData(0, "test", 0, 4, 't')]
        [InlineData(1, "test", 2, 2, 't')]
        [InlineData(1, "test", 1, 3, 's')]
        public void LoadCharCode_Returns_CharCode(int position, string content, int offset, int length, char expected)
        {
            var func = CreateDelegate<char>((il, pos) =>
            {
                var source = new StringParserSource();
                source.LoadCharCode(il, pos);
                il.Emit(OpCodes.Ret);
            });

            Assert.Equal(expected, func(position, content, offset, length));
        }

        [Theory]
        [InlineData(0x10000)]
        [InlineData(0x10ffff)]
        public void LoadCharCode_Returns_CharCode_Surrogate(int utf32)
        {
            var content = Char.ConvertFromUtf32(utf32);

            var func = CreateDelegate<int>((il, pos) =>
            {
                var source = new StringParserSource();
                var highSurrogate = il.CreateCell<int>();

                highSurrogate.Update(il, content[0]);
                source.LoadCharCode(il, pos, highSurrogate);
                il.Emit(OpCodes.Ret);
            });

            Assert.Equal(utf32, func(1, content, 0, content.Length));
        }

        private static Func<int, string, int, int, TReturn> CreateDelegate<TReturn>(Action<ILGenerator, Cell<int>> generate)
        {
            var dm = new DynamicMethod("Test", typeof(TReturn), new[] { typeof(int), typeof(string), typeof(int), typeof(int) });
            var il = dm.GetILGenerator();
            var position = il.CreateCell<int>();
            position.Update(il, () => il.Emit(OpCodes.Ldarg_0));
            generate(il, position);
            return (Func<int, string, int, int, TReturn>)dm.CreateDelegate(typeof(Func<int, string, int, int, TReturn>));
        }
    }
}
