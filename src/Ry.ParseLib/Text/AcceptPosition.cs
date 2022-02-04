namespace Ry.ParseLib.Text
{
    /// <summary>
    /// Represents a position that accepts the expression. 
    /// </summary>
    internal sealed class AcceptPosition : Position
    {
        public bool IsLazy { get; }
        public bool IsLookahead { get; }

        public AcceptPosition(int tokenId, bool lazy, bool lookahead)
            : base(tokenId)
        {
            this.IsLazy = lazy;
            this.IsLookahead = lookahead;
        }
    }
}
