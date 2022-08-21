using System;
using System.Collections.Generic;
using System.Text;

namespace Scripting.ECMA262.AST
{
    public class UnaryExpression : INode
    {
        private readonly INode expr;
        private readonly string op;
        private readonly bool postfix;

        public UnaryExpression(INode expr, string op, bool postfix = false)
        {
            this.expr = expr;
            this.op = op;
            this.postfix = postfix;
        }

        public override string ToString() => postfix ? $"({expr}{op})" : $"({op}{expr})";
    }
}
