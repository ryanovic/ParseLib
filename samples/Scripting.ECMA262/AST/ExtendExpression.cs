using System;
using System.Collections.Generic;
using System.Text;

namespace Scripting.ECMA262.AST
{
    public class ExtendExpression : INode
    {
        private readonly INode expr;

        public ExtendExpression(INode expr) => this.expr = expr;

        public override string ToString() => "... " + expr.ToString();
    }
}
