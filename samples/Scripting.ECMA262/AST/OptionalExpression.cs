using System;
using System.Collections.Generic;
using System.Text;

namespace Scripting.ECMA262.AST
{
    public class OptionalExpression : INode
    {
        private readonly INode expr;
        private readonly List<INode> chain;

        public OptionalExpression(INode expr, List<INode> chain)
        {
            this.expr = expr;
            this.chain = chain;
        }

        public override string ToString() => $"opt({expr},{string.Join("->", chain)})";
    }
}
