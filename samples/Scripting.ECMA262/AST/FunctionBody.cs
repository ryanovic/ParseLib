using System;
using System.Collections.Generic;
using System.Text;

namespace Scripting.ECMA262.AST
{
    public class FunctionBody : INode
    {
        private readonly INode body;

        public FunctionBody(INode body)
        {
            this.body = body;
        }

        public override string ToString()
        {
            if (body == null) return "{}";

            var buffer = new StringBuilder();
            buffer.Append("{");
            buffer.Append(body.ToString());
            buffer.Append("}");
            return buffer.ToString();
        }

    }
}
