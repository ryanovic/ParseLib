namespace ParseLib.Text
{
    using System;
    using System.Collections.Generic;

    internal readonly struct Selection
    {
        private readonly int[] selected;

        public Selection(int size)
        {
            this.selected = new int[size];
        }

        public void Add(int index)
        {
            selected[index]++;
        }

        public void Add(IList<int> indexes)
        {
            for (int i = 0; i < indexes.Count; i++)
            {
                selected[indexes[i]]++;
            }
        }

        public void Remove(IList<int> indexes)
        {
            for (int i = 0; i < indexes.Count; i++)
            {
                selected[indexes[i]]--;
            }
        }

        public void Clear()
        {
            Array.Clear(selected, 0, selected.Length);
        }

        public bool Contains(int index) => selected[index] > 0;
    }
}