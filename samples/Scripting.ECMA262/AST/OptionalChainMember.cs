using System;
using System.Collections.Generic;
using System.Text;

namespace Scripting.ECMA262.AST
{
    public class OptionalChainMember : INode
    {
        private readonly List<INode> exprs;

        public OptionalChainMember(INode expr)
            : this(new List<INode> { expr })
        {
        }

        public OptionalChainMember(List<INode> exprs)
        {
            this.exprs = exprs;
        }

        public override string ToString() => $"member({string.Join(",", exprs)})";
    }
}
