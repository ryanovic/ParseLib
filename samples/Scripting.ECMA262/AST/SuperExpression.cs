using System;
using System.Collections.Generic;
using System.Text;

namespace Scripting.ECMA262.AST
{
    public class SuperExpression : INode
    {
        private readonly List<INode> exprs;

        public SuperExpression(INode expr)
            : this(new List<INode> { expr })
        {
        }

        public SuperExpression(List<INode> exprs)
        {
            this.exprs = exprs;
        }

        public override string ToString() => $"super({string.Join(",", exprs)})";
    }
}
