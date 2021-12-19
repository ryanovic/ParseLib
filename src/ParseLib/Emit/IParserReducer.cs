namespace ParseLib.Emit
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    /// <summary>
    /// Defines the set of reducers for the target grammar. 
    /// </summary>
    public interface IParserReducer
    {
        /// <summary>
        /// Returns a reducer for the token.
        /// </summary>
        MethodInfo GetTokenReducer(string tokenName);

        /// <summary>
        /// Returns a reducer for the production.
        /// </summary>
        MethodInfo GetProductionReducer(string productionName);

        /// <summary>
        /// Returns a handler to be executed when specified prefix is matched.
        /// Prefix represents one or more symbols in the begining of some production defined in the grammar.
        /// </summary>
        MethodInfo GetPrefixHandler(Symbol[] symbols);
    }
}
