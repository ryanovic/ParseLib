namespace ParseLib.Runtime
{
    using System;

    /// <summary>
    /// Defines a target reducer for a specified terminal.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class CompleteTokenAttribute : Attribute
    {
        /// <summary>
        /// Gets the token name.
        /// </summary>
        public string Token { get; }

        public CompleteTokenAttribute(string token)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            this.Token = token;
        }
    }
}
