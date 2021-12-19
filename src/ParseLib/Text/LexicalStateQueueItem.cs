using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParseLib.Text
{
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
