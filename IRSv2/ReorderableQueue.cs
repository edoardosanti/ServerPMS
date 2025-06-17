// PMS Project V1.0
// LSData - all rights reserved
// ReorderableQueue.cs
//
//

using System.Collections;

namespace IRSv2
{
    public class ReorderableQueue<T> : IEnumerable<T>, IEquatable<T>
    {


        List<T> list;
        List<int> idTable;

        public ReorderableQueue(params T[] values)
        {
            list = new List<T>();
            idTable = new List<int>();

            if (values != null)
            {
                foreach (T x in values)
                    Enqueue(x);
            }
        }

        public void Enqueue(T item)
        {
            list.Add(item);
            idTable.Add(list.FindIndex(x => x.Equals(item)));

        }

        public bool Dequeue(T item)
        {
            idTable.RemoveAll(x => x>=list.Count);
            int lmt = idTable.FindIndex(x=>x==list.FindIndex(x => x.Equals(item)));
            idTable.RemoveAll(x => x == list.FindIndex(x => x.Equals(item)));
            for(int i = 0; i < idTable.Count; i++)
            {
                if (idTable[i] > lmt)
                    idTable[i]--;
            }
             
            return list.Remove(item);

        }

        public void MoveDown(Predicate<T> predicate, int jmps)
        {
            if (jmps < 0)
                throw new ArgumentOutOfRangeException("Jumps in queue can be only values from zero.");
            for (int i = 0; i < jmps; i++)
            {
                MoveDown(predicate);
            }
        }

        public void MoveUp(Predicate<T> predicate, int jmps)
        {
            if (jmps < 0)
                throw new ArgumentOutOfRangeException("Jumps in queue can be only values from zero.");
            for (int i = 0; i < jmps; i++)
            {
                MoveUp(predicate);
            }
        }

        public void MoveUp(Predicate<T> predicate)
        {

            int itemIndex = list.FindIndex(predicate);
            if (itemIndex == -1)
                throw new ArgumentException("Item not found.");

            int oldIndex = idTable.FindIndex(x => x.Equals(itemIndex));
            int newIndex = oldIndex - 1;
            if (newIndex >= 0)
            {
                idTable.Insert(newIndex, itemIndex);
            }
            else
            {
                idTable.Insert(0, itemIndex);
            }
            idTable.RemoveAt(oldIndex+1);
        }

        public void MoveDown(Predicate<T> predicate)
        {

            int itemIndex = list.FindIndex(predicate);
            if (itemIndex == -1)
                throw new ArgumentException("Item not found.");

            int oldIndex = idTable.FindIndex(x => x.Equals(itemIndex));
            int newIndex = oldIndex + 2;
            if (newIndex < idTable.Count)
            {
                idTable.Insert(newIndex, itemIndex);
            }
            else
            {
                idTable.Add(itemIndex);
            }
            idTable.RemoveAt(oldIndex);


        }

        public T GetNextAndDequeue()
        {
            T next = Next;
            Dequeue(Next);
            return next;
        }

        public int Count { get { return idTable.Count; } }

        public T Next
        {
            get
            {
                try
                {
                    return list.ElementAt(idTable.First());
                } catch (Exception)
                {
                    return default(T);
                }
            }
        }

        public virtual string StrDump()
        {
            string s = string.Empty;
            foreach (T item in this)
            {
                s += item.ToString()+"\n";
            }
            return s;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new RQEnumerator(list, idTable);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Equals(T? other)
        {
            if (this.Equals(other))
                return true;
            else
                return false;
        }

        class RQEnumerator : IEnumerator<T>
        {
            List<T> list;
            List<int> indexes;
            private int position = -1;

            public RQEnumerator(List<T> list, List<int> indexes)
            {
                this.list = list;
                this.indexes = indexes;
            }

            public T Current => list[indexes[position]];

            object IEnumerator.Current => Current;

            public void Dispose() { }

            public bool MoveNext()
            {
                position++;
                return position < list.Count;
            }

            public void Reset()
            {
                position = -1;
            }
        }
    }
}