using System;
using System.Collections.Generic;
using System.Text;

namespace Scripting.ECMA262.AST
{
    public class ImportExpression : INode
    {
        private readonly INode expr;

        public ImportExpression(INode expr)
        {
            this.expr = expr;
        }

        public override string ToString() => $"import({expr})";
    }
}
