using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection.Emit;
using ParseLib;
using ParseLib.Runtime;

namespace Calculator
{
    public abstract class ExpressionParser : StringParser
    {
        private readonly DynamicMethod method;
        private readonly ILGenerator il;

        public ExpressionParser(string expr) : base(expr)
        {
            this.method = new DynamicMethod("Eval", typeof(int), new[] { typeof(int), typeof(int) });
            this.il = method.GetILGenerator();
        }

        public Func<int, int, int> CreateDelegate()
        {
            il.Emit(OpCodes.Ret);
            return (Func<int, int, int>)method.CreateDelegate(typeof(Func<int, int, int>));
        }

        [CompleteToken("a")]
        protected void CompleteArg_A() => il.Emit(OpCodes.Ldarg_0);

        [CompleteToken("b")]
        protected void CompleteArg_B() => il.Emit(OpCodes.Ldarg_1);

        [CompleteToken("num")]
        protected void CompleteNumber() => il.Emit(OpCodes.Ldc_I4, Int32.Parse(GetLexeme()));

        [Reduce("expr:add")]
        protected void Add() => il.Emit(OpCodes.Add);

        [Reduce("expr:sub")]
        protected void Sub() => il.Emit(OpCodes.Sub);

        [Reduce("expr:mul")]
        protected void Mul() => il.Emit(OpCodes.Mul);

        [Reduce("expr:div")]
        protected void Div() => il.Emit(OpCodes.Div);

        [Reduce("expr:unary")]
        protected void Neg() => il.Emit(OpCodes.Neg);
    }
}
