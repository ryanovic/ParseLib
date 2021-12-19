namespace ParseLib.Text
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    internal sealed class UnicodeRangeMinPQ
    {
        private int count;
        private int left;
        private UnicodeRange[][] sets;
        private int[] heap;
        private int[] pos;

        public bool IsEmpty => count == 0;

        public UnicodeRangeMinPQ(UnicodeRange[][] sets)
        {
            if (sets == null) throw new ArgumentNullException(nameof(sets));

            this.left = -1;
            this.sets = sets;
            this.heap = new int[this.sets.Length];
            this.pos = new int[this.sets.Length];

            Populate();
        }

        public (UnicodeRange, int[]) PopMin()
        {
            if (count == 0) throw new InvalidOperationException();

            var tmp = Get(0);
            var queue = new int[count];
            int queueIndex = 0, queueCount = 0, from = Math.Max(left, tmp.From), to = tmp.To;

            queue[queueCount++] = 0; // head element

            while (queueIndex < queueCount)
            {
                void Visit(int j)
                {
                    if (j < count)
                    {
                        tmp = Get(j);

                        if (Math.Max(left, tmp.From) == from)
                        {
                            // match and check children after
                            queue[queueCount++] = j;
                        }
                        else
                        {
                            // narrow current to the next range boundary
                            to = Math.Min(to, tmp.From - 1);
                        }
                    }
                }

                int i = queue[queueIndex++];
                Visit(i * 2 + 1); // left child
                Visit(i * 2 + 2); // right child
            }

            left = to + 1;
            var matched = new int[queueIndex];

            // process queue in a reverse order to collect sources're matched and refresh the heap
            while (--queueIndex >= 0)
            {
                int i = queue[queueIndex];
                matched[queueIndex] = heap[i];

                if (!Shift(i))
                {
                    // source set is empty now, so reduce the count
                    heap[i] = heap[--count];
                };

                Sink(i); // update the heap position
            }

            return (new UnicodeRange(from, to), matched);
        }

        public void Reset()
        {
            (left, count) = (-1, 0);
            Populate();
        }

        private void Populate()
        {
            for (int i = 0; i < sets.Length; i++)
            {
                if (sets[i].Length > 0)
                {
                    heap[count++] = i;
                    pos[i] = 0;
                }
            }

            Build();
        }

        private void Build()
        {
            for (int i = count / 2 - 1; i >= 0; i--)
            {
                Sink(i); // update the heap position
            }
        }

        private void Sink(int i)
        {
            int lt = i * 2 + 1; // left child
            int rt = i * 2 + 2; // right child

            while (lt < count)
            {
                int min = rt < count ? Min(lt, rt) : lt;

                if (Compare(i, min) <= 0)
                {
                    break;
                }

                var tmp = heap[i];
                heap[i] = heap[min];
                heap[min] = tmp;

                i = min;
                lt = i * 2 + 1;
                rt = i * 2 + 2;
            }
        }

        private int Min(int i, int j)
        {
            return Compare(i, j) < 0 ? i : j;
        }

        private int Compare(int i, int j)
        {
            var a = Get(i);
            var b = Get(j);

            int eq = Math.Max(left, a.From) - Math.Max(left, b.From);

            if (eq == 0)
            {
                eq = a.To - b.To; // make shorter less
            }

            return eq;
        }

        private UnicodeRange Get(int i)
        {
            int set = heap[i];
            return sets[set][pos[set]];
        }

        private bool Shift(int i)
        {
            int set = heap[i];
            return sets[set][pos[set]].To >= left || ++pos[set] < sets[set].Length;
        }
    }
}
