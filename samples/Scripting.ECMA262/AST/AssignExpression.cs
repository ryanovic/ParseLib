using System;
using System.Collections.Generic;
using System.Text;

namespace Scripting.ECMA262.AST
{
    public class AssignExpression : INode
    {
        private readonly INode a;
        private readonly INode b;

        public AssignExpression(INode a, INode b)
        {
            this.a = a;
            this.b = b;
        }

        public override string ToString() => $"assing({a},{b})";
    }
}
