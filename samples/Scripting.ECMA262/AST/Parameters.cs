using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scripting.ECMA262.AST
{
    public class Parameters : INode
    {
        private readonly List<INode> list;

        public bool IsEmpty => list == null || list.Count == 0;

        public Parameters(List<INode> list)
        {
            this.list = list;
        }

        public override string ToString() => "(" + InnerToString() + ")";

        public string InnerToString() => IsEmpty ? "" : string.Join(",", list);
    }
}
