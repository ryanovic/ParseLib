using System;
using System.Collections.Generic;
using System.Text;

namespace Scripting.ECMA262.AST
{
    public class ExpressionOrParameters : INode
    {
        private readonly List<INode> items;

        public ExpressionOrParameters(List<INode> items)
        {
            this.items = items;
        }

        public override string ToString() => items == null || items.Count == 0 ? "()" : "(" + string.Join(",", items) + ")";
    }
}
