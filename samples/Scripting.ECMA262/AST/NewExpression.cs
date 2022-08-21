using System;
using System.Collections.Generic;
using System.Text;

namespace Scripting.ECMA262.AST
{
    public class NewExpression : INode
    {
        private readonly INode member;
        private readonly Parameters args;

        public NewExpression(INode member, Parameters args)
        {
            this.member = member;
            this.args = args;
        }

        public override string ToString() => args == null || args.IsEmpty ? $"new({member})" : $"new({member},{args})";
    }
}
