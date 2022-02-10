namespace Ry.ParseLib.Emit
{
    using System;
    using System.Collections.Generic;
    using System.Reflection.Emit;
    using Ry.ParseLib.LALR;

    /// <summary>
    /// Represents an interface for executing handlers for recognized tokens.
    /// </summary>
    public interface IParserReducer
    {
        /// <summary>
        /// Initializes internal fields of the reducer.
        /// </summary>
        void Initialize(ILGenerator il);

        /// <summary>
        /// Handles a completed terminal.
        /// </summary>
        void HandleTerminal(ILGenerator il, Terminal terminal);

        /// <summary>
        /// Handles a completed production.
        /// </summary>
        void HandleProduction(ILGenerator il, Production production);

        /// <summary>
        /// Handles new state when some terminal or non-terminal is consumed.
        /// </summary>
        void HandleState(ILGenerator il, ParserState state);

        /// <summary>
        /// Loads a result value.
        /// </summary>
        void LoadResult(ILGenerator il);
    }
}
