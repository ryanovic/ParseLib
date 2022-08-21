using System;
using System.Collections.Generic;
using System.Text;

namespace Scripting.ECMA262.AST
{
    public class ClassMethod : INode
    {
        private readonly INode mthd;
        private readonly bool isStatic;

        public ClassMethod(INode mthd, bool isStatic = false)
        {
            this.mthd = mthd;
            this.isStatic = isStatic;
        }

        public override string ToString() => isStatic ? $"static {mthd}" : mthd.ToString();
    }
}
