namespace ParseLib.Emit
{
    using System;
    using System.Reflection.Emit;
    using System.Collections.Generic;

    /// <summary>
    /// Defines the interface to handle the tokens were recognized during the source processing.
    /// </summary>
    public interface ILexerTarget
    {
        /// <summary>
        /// Handles the specific token.
        /// </summary>
        void CompleteToken(ILGenerator il, int tokenId);

        /// <summary>
        /// Handles token with Id which can be accessed via <paramref name="tokenId"/> cell.
        /// </summary>
        void CompleteToken(ILGenerator il, Cell<int> tokenId);

        /// <summary>
        /// Handles the case when no more tokens can be recognized due to the entire source is processed or dead state reached.
        /// </summary>
        void CompleteSource(ILGenerator il, ILexerSource source);
    }
}
