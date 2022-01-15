namespace ParseLib.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Defiens a target handler for a production prefix. Allows to handle partially completed productions.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class HandleAttribute : Attribute
    {
        public string Prefix { get; }

        /// <summary>
        /// Creates an instance of <see cref="HandleAttribute"/> class.
        /// </summary>
        /// <param name="prefix">A string representing a space separated list of symbol names.</param>
        public HandleAttribute(string prefix)
        {
            if (prefix == null)
            {
                throw new ArgumentNullException(nameof(prefix));
            }

            this.Prefix = prefix;
        }
    }
}
