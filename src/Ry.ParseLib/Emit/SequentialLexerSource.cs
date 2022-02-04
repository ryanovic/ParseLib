namespace Ry.ParseLib.Emit
{
    using System;
    using System.Collections.Generic;
    using System.Reflection.Emit;
    using System.Text;

    internal sealed class SequentialLexerSource : LexerSourceBase
    {
        public SequentialLexerSource(ILGenerator il) : base(il)
        {
        }

        public override void LoadStartPosition()
        {
            IL.Emit(OpCodes.Ldarg_1);
        }

        public override void LoadEndPosition()
        {
            LoadStartPosition();
            LoadLength();
            IL.Emit(OpCodes.Add);
        }

        public void LoadIsFinal()
        {
            IL.Emit(OpCodes.Ldarg_3);
        }

        protected override void LoadBuffer()
        {
            IL.Emit(OpCodes.Ldarga_S, 2);
        }
    }
}
