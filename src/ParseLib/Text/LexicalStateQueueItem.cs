namespace ParseLib.Text
{
    /// <summary>
    /// Represents a lexical state awaiting initialization.
    /// </summary>
    internal readonly struct LexicalStateQueueItem
    {
        public LexicalState State { get; }
        public Position[] Positions { get; }

        public LexicalStateQueueItem(LexicalState state, Position[] positions)
        {
            this.State = state;
            this.Positions = positions;
        }
    }
}
