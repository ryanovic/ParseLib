using System;
using System.Collections.Generic;
using System.Text;

namespace Scripting.ECMA262.AST
{
    public class ClassBody : INode
    {
        private readonly INode parent;
        private readonly List<INode> memebers;

        public ClassBody(INode parent, List<INode> memebers)
        {
            this.parent = parent;
            this.memebers = memebers;
        }

        public override string ToString()
        {
            if (memebers == null || memebers.Count == 0)
            {
                return parent == null ? "()" : $" extends {parent}()";
            }
            else
            {
                var buffer = new StringBuilder();
                buffer.Append(parent == null ? "(" : $" extends {parent}(");
                buffer.Append(string.Join(",", memebers));
                buffer.Append(")");
                return buffer.ToString();
            }
        }
    }
}
