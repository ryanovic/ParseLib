namespace Ry.ParseLib.Emit
{
    using System;
    using System.Collections.Generic;
    using System.Reflection.Emit;
    using System.Text;

    internal sealed class LexerSource : LexerSourceBase
    {
        public LexerSource(ILGenerator il) : base(il)
        {
        }

        public override void LoadStartPosition()
        {
            IL.Emit(OpCodes.Ldc_I4_0);
        }

        public override void LoadEndPosition()
        {
            LoadLength();
        }

        protected override void LoadBuffer()
        {
            IL.Emit(OpCodes.Ldarga_S, 1);
        }
    }
}
