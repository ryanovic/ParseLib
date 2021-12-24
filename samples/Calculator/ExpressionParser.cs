namespace Calculator
{
    using System;
    using System.Reflection.Emit;
    using ParseLib.Runtime;

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

        // Gets num int value and puts it on the stack.
        [CompleteToken("num")]
        protected int CompleteNumber() => Int32.Parse(GetLexeme());

        // Reduces 'expr -> a' production. 
        [Reduce("expr:a")]
        protected void LoadArg_A() => il.Emit(OpCodes.Ldarg_0);

        // In this particular parser same action can be performed in CompleteToken('b') handler with same result generated.
        [Reduce("expr:b")]
        protected void LoadArg_B() => il.Emit(OpCodes.Ldarg_1);

        // Gets num value from the top and puts it on evaluation stack.
        [Reduce("expr:num")]
        protected void LoadNum(int num) => il.Emit(OpCodes.Ldc_I4, num);

        // Maps rule to appropriate IL instruction.
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
