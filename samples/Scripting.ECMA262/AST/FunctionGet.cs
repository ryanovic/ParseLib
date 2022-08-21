using System;
using System.Collections.Generic;
using System.Text;

namespace Scripting.ECMA262.AST
{
    public class FunctionGet : INode
    {
        private readonly INode name;
        private readonly INode body;

        public FunctionGet(INode name, INode body)
        {
            this.name = name;
            this.body = body;
        }

        public override string ToString()
        {
            var buffer = new StringBuilder("get ");
            if (name != null) buffer.Append(name);
            buffer.Append("()");
            buffer.Append(body.ToString());
            return buffer.ToString();
        }
    }
}
