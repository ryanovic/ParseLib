using System;
using System.Collections.Generic;
using System.Text;

namespace Scripting.ECMA262.AST
{
    public class MemberExpression : INode
    {
        private readonly INode member;
        private readonly List<INode> exprs;

        public MemberExpression(INode member, INode expr)
            : this(member, new List<INode> { expr })
        {
        }

        public MemberExpression(INode member, List<INode> exprs)
        {
            this.member = member;
            this.exprs = exprs;
        }

        public override string ToString() => $"member({member},{string.Join(",", exprs)})";
    }
}
