using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using Xunit;

namespace ParseLib.Emit
{
    public class SequentialParserSourceTests
    {
        [Theory]
        [InlineData(0, 0, "test", 0, 4)]
        [InlineData(10, 10, "test", 0, 4)]
        [InlineData(30, 20, "test", 2, 2)]
        public void CheckLowerBound_Emits_True_When_InBound(int position, int bufferPosition, string content, int offset, int length)
        {
            var func = CreateDelegate<bool>((il, pos) =>
            {
                var isValid = il.DefineLabel();
                var source = new SequentialParserSource();
                source.CheckLowerBound(il, pos, isValid);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ret);

                il.MarkLabel(isValid);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Ret);
            });

            Assert.True(func(position, bufferPosition, content.ToCharArray(), offset, length, true));
        }

        [Theory]
        [InlineData(0, 10, "test", 2, 2)]
        [InlineData(9, 10, "test", 0, 4)]
        public void CheckLowerBound_Emits_False_When_OutOfBound(int position, int bufferPosition, string content, int offset, int length)
        {
            var func = CreateDelegate<bool>((il, pos) =>
            {
                var isValid = il.DefineLabel();
                var source = new SequentialParserSource();
                source.CheckLowerBound(il, pos, isValid);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ret);

                il.MarkLabel(isValid);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Ret);
            });

            Assert.False(func(position, bufferPosition, content.ToCharArray(), offset, length, true));
        }

        [Theory]
        [InlineData(0, 0, "test", 0, 4)]
        [InlineData(11, 10, "test", 2, 2)]
        [InlineData(13, 10, "test", 0, 4)]
        public void CheckUpperBound_Emits_True_When_InBound(int position, int bufferPosition, string content, int offset, int length)
        {
            var func = CreateDelegate<bool>((il, pos) =>
            {
                var isValid = il.DefineLabel();
                var source = new SequentialParserSource();
                source.CheckUpperBound(il, pos, isValid);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ret);

                il.MarkLabel(isValid);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Ret);
            });

            Assert.True(func(position, bufferPosition, content.ToCharArray(), offset, length, true));
        }

        [Theory]
        [InlineData(4, 0, "test", 0, 4)]
        [InlineData(12, 10, "test", 2, 2)]
        [InlineData(15, 10, "test", 0, 4)]
        public void CheckUpperBound_Emits_False_When_OutOfBound(int position, int bufferPosition, string content, int offset, int length)
        {
            var func = CreateDelegate<bool>((il, pos) =>
            {
                var isValid = il.DefineLabel();
                var source = new SequentialParserSource();
                source.CheckUpperBound(il, pos, isValid);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ret);

                il.MarkLabel(isValid);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Ret);
            });

            Assert.False(func(position, bufferPosition, content.ToCharArray(), offset, length, true));
        }

        [Fact]
        public void CheckIsLastChunk_Emits_True_When_Eos_IsTrue()
        {
            var func = CreateDelegate<bool>((il, _) =>
            {
                var isLast = il.DefineLabel();
                var source = new SequentialParserSource();
                source.CheckIsLastChunk(il, isLast);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ret);

                il.MarkLabel(isLast);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Ret);
            });

            Assert.True(func(0, 0, "content".ToCharArray(), 0, "content".Length, true));
        }

        [Fact]
        public void CheckIsLastChunk_Emits_False_When_Eos_IsFalse()
        {
            var func = CreateDelegate<bool>((il, _) =>
            {
                var isLast = il.DefineLabel();
                var source = new SequentialParserSource();
                source.CheckIsLastChunk(il, isLast);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ret);

                il.MarkLabel(isLast);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Ret);
            });

            Assert.False(func(0, 0, "content".ToCharArray(), 0, "content".Length, false));
        }

        [Theory]
        [InlineData(0, 0, "test", 0, 4, 't')]
        [InlineData(11, 10, "test", 0, 4, 'e')]
        [InlineData(15, 15, "test", 2, 2, 's')]
        [InlineData(16, 15, "test", 2, 2, 't')]
        public void LoadCharCode_Returns_CharCode(int position, int bufferPosition, string content, int offset, int length, char expected)
        {
            var func = CreateDelegate<char>((il, pos) =>
            {
                var source = new SequentialParserSource();
                source.LoadCharCode(il, pos);
                il.Emit(OpCodes.Ret);
            });

            Assert.Equal(expected, func(position, bufferPosition, content.ToCharArray(), offset, length, true));
        }

        [Theory]
        [InlineData(0x10000)]
        [InlineData(0x10ffff)]
        public void LoadCharCode_Returns_CharCode_Surrogate(int utf32)
        {
            var content = Char.ConvertFromUtf32(utf32);

            var func = CreateDelegate<int>((il, pos) =>
            {
                var source = new SequentialParserSource();
                var highSurrogate = il.CreateCell<int>();

                highSurrogate.Update(il, content[0]);
                source.LoadCharCode(il, pos, highSurrogate);
                il.Emit(OpCodes.Ret);
            });

            Assert.Equal(utf32, func(1, 0, content.ToCharArray(), 0, content.Length, true));
        }

        private static Func<int, int, char[], int, int, bool, TReturn> CreateDelegate<TReturn>(Action<ILGenerator, Cell<int>> generate)
        {
            var dm = new DynamicMethod("Test", typeof(TReturn), new[] { typeof(int), typeof(int), typeof(char[]), typeof(int), typeof(int), typeof(bool) });
            var il = dm.GetILGenerator();
            var position = il.CreateCell<int>();
            position.Update(il, () => il.Emit(OpCodes.Ldarg_0));
            generate(il, position);
            return (Func<int, int, char[], int, int, bool, TReturn>)dm.CreateDelegate(typeof(Func<int, int, char[], int, int, bool, TReturn>));
        }
    }
}
