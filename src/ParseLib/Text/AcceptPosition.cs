namespace ParseLib.Text
{
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
