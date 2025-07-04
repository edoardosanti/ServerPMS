// PMS Project V1.0
// LSData - all rights reserved
// ReorderableQueue.cs
//
//
using System.Collections.ObjectModel;


namespace ServerPMS
{
    public class ReorderableQueue<T>:IEnumerable<T>,IEquatable<T>,ICloneable<T>
    {
        List<T> list;

        public T Next => list[0];

        #region Constructors

        public ReorderableQueue()
        {
            list = new List<T>();
        }

        public ReorderableQueue(params T[] items)
        {
            list = new List<T>();

            foreach (T item in items)
            {
                list.Add(item);
            }
        }

        public ReorderableQueue(Collection<T> items)
        {
            list = new List<T>();

            foreach(T item in items)
            {
                Enqueue(item);
            }
        }

        #endregion

        #region Queue Operations

        public void Enqueue(T item)
        {
            list.Add(item);
        }

        public void MoveUp(T item)
        {
            int itemIndex = list.IndexOf(item);

            switch (itemIndex)
            {
                case 0:
                    break;
                case -1:
                    throw new InvalidOperationException("Item not found.");
                default:
                    int targetPos = itemIndex - 1;
                    list.Insert(targetPos, item);
                    list.RemoveAt(itemIndex + 1);
                    break;
            }

        }
        #endregion
    }
}

