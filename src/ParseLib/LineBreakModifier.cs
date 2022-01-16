namespace ParseLib
{
    /// <summary>
    /// Represents a line-break modifier.
    /// </summary>
    public enum LineBreakModifier
    {
        /// <summary>
        /// Indicates that a line-break may precede symbol.
        /// </summary>
        None = 0,

        /// <summary>
        /// Indicates that a line-break can NOT precede a symbol.
        /// </summary>
        NoLineBreak = 1,

        /// <summary>
        /// Indicates that a line-break must precede a symbol.
        /// </summary>
        LineBreak = 2,

        /// <summary>
        /// Indicates an invalid state.
        /// </summary>
        Forbidden = 3
    }
}
