using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scripting.ECMA262.AST
{
    public class Binding : INode
    {
        private readonly List<INode> list;
        private readonly INode rest;

        public Binding(List<INode> list, INode rest)
        {
            this.list = list;
            this.rest = rest;
        }

        public override string ToString()
        {
            var buffer = new StringBuilder();
            buffer.Append("binding(");

            if (list != null && list.Any())
            {
                buffer.Append(string.Join(",", list));

                if (rest != null)
                {
                    buffer.Append($",... {rest}");
                }
            }
            else if (rest != null)
            {
                buffer.Append($"...{rest}");
            }

            buffer.Append(")");
            return buffer.ToString();
        }
    }
}
