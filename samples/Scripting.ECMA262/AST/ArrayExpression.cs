using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace Scripting.ECMA262.AST
{
    public class ArrayExpression : INode
    {
        private readonly List<INode> items;

        public ArrayExpression(List<INode> items)
        {
            this.items = items;
        }

        public override string ToString() => "array(" + string.Join(",", items) + ")";
    }
}
