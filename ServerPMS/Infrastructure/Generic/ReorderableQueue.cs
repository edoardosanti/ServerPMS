// PMS Project V1.0
// LSData - all rights reserved
// ReorderableQueue.cs
//
//
using System.Collections;
using System.Collections.ObjectModel;
using DocumentFormat.OpenXml.Drawing;


namespace ServerPMS
{

    public class ReorderableQueue<T> : IEnumerable<T>, IEquatable<ReorderableQueue<T>>, ICloneable<ReorderableQueue<T>>
    {

        public event EventHandler<T> ItemEnqueuedHandler;
        public event EventHandler<T> ItemDequeuedHandler;
        public event EventHandler<T> ItemMovedHandler;
        public event EventHandler<T> ItemMovedUpHandler;
        public event EventHandler<T> ItemMovedDownHandler;
        public event EventHandler<T> ItemRemovedHandler;

        List<T> list;

        public T Current => list.Count > 0 ? list[0] : default;
        public T Next => list.Count > 1 ? list[1] : default;
        public T Last => list.Count > 0 ? list[list.Count-1] : default;

        public int Count => list.Count;

        #region Constructors

        public ReorderableQueue()
        {
            list = new List<T>();
        }

        public ReorderableQueue(IEnumerable<T> items)
        {
            list = new List<T>();

            foreach(T item in items)
            {
                Enqueue(item);
            }
        }
        #endregion

        #region Queue Operations

        public void SmartEnqueue(T item)
        {
            if (!Contains(item))
                Enqueue(item);
        }

        public void SmartEnqueue(IEnumerable<T> items)
        {
            foreach (T item in items)
            {
                SmartEnqueue(item);
            }
        }

        public void Enqueue(T item)
        {
            list.Add(item);
            OnItemEnqueued(item);
        }

        public void Enqueue(IEnumerable<T> items)
        {
            foreach (T item in items)
            {
                Enqueue(item);
            }
        }

        public void InsertInPosition(int position, T item)
        {
            list.Insert(position, item);
        }

        public T Dequeue()
        {
            if (Count == 0)
                throw new InvalidOperationException("Queue is empty.");

            T item = list[0];
            list.RemoveAt(0);
            OnItemDequeued(item);
            return item;
        }

        private void _Move(T item, int offset)
        {
            int currentIndex = list.IndexOf(item);

            //check if item in list
            if (currentIndex == -1)
                throw new InvalidOperationException("Item not found.");

            //if offset is 0 do nothing
            if (offset == 0)
                return;

            //clamp targetIndex to bounds
            int targetIndex = currentIndex + offset;
            if (targetIndex < 0)
                targetIndex = 0;
            else if (targetIndex >= list.Count)
                targetIndex = list.Count - 1;

            //remove old item
            list.RemoveAt(currentIndex);

            //reinsert the item in the new position
            list.Insert(targetIndex, item);
        }

        public void Move(T item, int offset)
        {
            _Move(item, offset);
            OnItemMoved(item);
        }

        public void MoveUp(T item, int steps=1)
        {
            if (steps < 0)
                throw new ArgumentOutOfRangeException(nameof(steps), "Steps must be non-negative.");
            _Move(item, -steps);
            OnItemMovedUp(item);
        }

        public void MoveDown(T item, int steps = 1)
        {
            if (steps < 0)
                throw new ArgumentOutOfRangeException(nameof(steps), "Steps must be non-negative.");
            _Move(item, steps);
            OnItemMovedDown(item);
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index > Count)
                throw new ArgumentOutOfRangeException(nameof(index), "Index must be non-negative and less than the queue count.");
            T tmp = list.ElementAt(index);
            list.RemoveAt(index);
            OnItemRemoved(tmp);
        }

        public void Remove(T item)
        {
            list.Remove(item);
            OnItemRemoved(item);

        }

        public T Find(Predicate<T> predicate)
        {
            return list.Find(predicate);
        }

        public List<T> FindAll(Predicate<T> predicate)
        {
            return list.FindAll(predicate);
        }  

        public bool Contains(T item)
        {
            return list.Contains(item);
        }

        public int PositionOf(T item)
        {
            return list.IndexOf(item);
        }

        #endregion

        #region Events

        public void OnItemEnqueued(T item)
        {
            ItemEnqueuedHandler?.Invoke(this, item);
        }

        public void OnItemDequeued(T item)
        {
            ItemDequeuedHandler?.Invoke(this, item);
        }

        public void OnItemMoved(T item)
        {
            ItemMovedHandler?.Invoke(this, item);
        }

        public void OnItemMovedUp(T item)
        {
            ItemMovedUpHandler?.Invoke(this, item);
        }

        public void OnItemMovedDown(T item)
        {
            ItemMovedDownHandler?.Invoke(this, item);
        }

        public void OnItemRemoved(T item)
        {
            ItemRemovedHandler?.Invoke(this, item);
        }

        #endregion

        #region IClonable

        public ReorderableQueue<T> Clone()
        {
            return new ReorderableQueue<T>(list.ToArray());
        }
        #endregion

        #region IEquatable

        public bool Equals(ReorderableQueue<T>? x)
        {
            if (x == null)
                return false;
            else if (ReferenceEquals(this, x))
                return true;
            else
            {
                bool areEquals = false;
                foreach (T item in x)
                {
                    areEquals = list.Contains(item);
                }
                return areEquals;
            }
        }
        #endregion

        #region IEnumerable

        public IEnumerator<T> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetEnumerator();
        }
        #endregion
    }
}

