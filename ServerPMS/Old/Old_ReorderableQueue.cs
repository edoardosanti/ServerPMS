// PMS Project V1.0
// LSData - all rights reserved
// ReorderableQueue.cs
//
//

using System.Collections;
using System.Globalization;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ServerPMS.Old
{
    public class OldBroken_ReorderableQueue<T> : IEnumerable<T>
    {


        List<T> list;
        List<int> idTable;

        public event EventHandler<T> ItemEnqueuedHandler;
        public event EventHandler<T> ItemMovedUpHandler;
        public event EventHandler<T> ItemMovedDownHandler;

        private void OnItemEnqueued(T item)
        { 
            ItemEnqueuedHandler?.Invoke(this, item);
        }

        private void OnItemMovedUp(T item)
        {
            ItemMovedDownHandler?.Invoke(this, item);
        }

        private void OnItemMovedDown(T item)
        {
            ItemMovedDownHandler?.Invoke(this, item);
        }


        public OldBroken_ReorderableQueue(params T[] values)
        {
            list = new List<T>();
            idTable = new List<int>();

            if (values != null)
            {
                foreach (T x in values)
                    Enqueue(x);
            }
        }

        public bool SmartEnqueue(T item)
        {
            bool added = !list.Contains(item);
            if(added)
                Enqueue(item);
            return added;
        }

        public void Enqueue(T item)
        {
            list.Add(item);
            idTable.Add(list.IndexOf(item));
            OnItemEnqueued(item);

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
            //get list index of predicate
            int itemIndex = list.FindIndex(predicate);

            //throws exception if none object found
            if (itemIndex == -1)
                throw new ArgumentException("Item not found.");

            //get index in idTable of index
            int oldIndex = idTable.FindIndex(x => x.Equals(itemIndex));

            //generate new idTable index
            int newIndex = oldIndex - 1;

            //if new index < 0 puts index at 0 otherwise executes normally
            if (newIndex >= 0)
            {
                idTable.Insert(newIndex, itemIndex);
            }
            else
            {
                idTable.Insert(0, itemIndex);
            }

            //remove old index from idTable
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

        public override string ToString()
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