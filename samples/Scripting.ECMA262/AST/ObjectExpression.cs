using System;
using System.Collections.Generic;
using System.Text;

namespace Scripting.ECMA262.AST
{
    public class ObjectExpression : INode
    {
        public readonly List<INode> properties;

        public ObjectExpression(List<INode> properties)
        {
            this.properties = properties;
        }

        public override string ToString()
        {
            if (properties.Count == 0)
            {
                return "{}";
            }

            var buffer = new StringBuilder();
            buffer.AppendLine("{");

            foreach (var prop in properties)
            {
                buffer.AppendLine(prop.ToString());
            }

            buffer.AppendLine("}");
            return buffer.ToString();
        }
    }
}
