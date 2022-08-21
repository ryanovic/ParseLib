using System;
using System.Collections.Generic;
using System.Text;

namespace Scripting.ECMA262.AST
{
    public class BinaryExpression : INode
    {
        private readonly INode a;
        private readonly INode b;
        private readonly string op;

        public BinaryExpression(INode a, INode b, string op)
        {
            this.a = a;
            this.b = b;
            this.op = op;
        }

        public override string ToString() => $"({a}{op}{b})";
    }
}
