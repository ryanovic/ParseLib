namespace ParseLib.Runtime
{
    using System;

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class CompleteTokenAttribute : Attribute
    {
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
