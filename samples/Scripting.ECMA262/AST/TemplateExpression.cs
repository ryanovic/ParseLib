using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scripting.ECMA262.AST
{
    public class TemplateExpression : INode
    {
        private readonly List<INode> items;

        public TemplateExpression(List<INode> items)
        {
            this.items = items;
        }

        public TemplateExpression Tag(INode root)
        {
            var updated = new List<INode>(items.Count + 1);
            updated.Add(root);
            updated.AddRange(items);
            return new TemplateExpression(updated);
        }

        public override string ToString() => "template(" + string.Join(",", items.Select(ToString)) + ")";

        private static string ToString(INode item) => item is Leaf leaf && leaf.Kind == LeafType.Template ? leaf.Value : item.ToString();
    }
}
