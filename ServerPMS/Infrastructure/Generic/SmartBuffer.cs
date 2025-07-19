// PMS Project V1.0
// LSData - all rights reserved
// OrderBuffer.cs
//
//
using System;
using System.Collections;
using System.Data;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Spreadsheet;

namespace ServerPMS.Infrastructure.Generic
{
    public class SmartBuffer<T> : IEnumerable<T>,IList<T>
    {
        List<T> MainBuffer;

        public event EventHandler<T> ItemAddedHandler;
        public event EventHandler<T> ItemRemovedHandler;

        public int Count => ((ICollection<T>)MainBuffer).Count;

        public bool IsReadOnly => ((ICollection<T>)MainBuffer).IsReadOnly;

        public T this[int index] { get => ((IList<T>)MainBuffer)[index]; set => ((IList<T>)MainBuffer)[index] = value; }

        public SmartBuffer()
        {
            MainBuffer = new List<T>();
        }

        public T Find(Predicate<T> predicate)
        {
            return MainBuffer.Find(predicate);
        }

        public virtual bool SmartAdd(T item)
        {
            if (!IsInBuffer(item))
            {
                Add(item);
                return true;
            }
            return false;
        }

        private bool IsInBuffer(T item)
        {
            if (MainBuffer.Find(x => x.Equals(item)) == null)
                return false;
            else
                return true;
        }

        public void SmartAdd(T[] items)
        {
            foreach(T item in items)
            {
                SmartAdd(item);
            }
        }

        public virtual bool Remove(Predicate<T> predicate)
        {
            var items = MainBuffer.FindAll(predicate);
            foreach(T item in items)
            {
                OnItemRemoved(item);
            }
            return MainBuffer.RemoveAll(predicate)>0? true:false;
        }

        public void SmartAdd(List<T> items)
        {
            SmartAdd(items.ToArray());
        }

        private void OnItemAdded(T arg)
        {
            ItemAddedHandler?.Invoke(this,arg);
        }

        private void OnItemRemoved(T arg)
        {
            ItemRemovedHandler?.Invoke(this, arg);
        }

        //IEnumerable and IList
        public IEnumerator<T> GetEnumerator()
        {
            return MainBuffer.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)MainBuffer).GetEnumerator();
        }

        public void Add(T item)
        {
            OnItemAdded(item);
            ((ICollection<T>)MainBuffer).Add(item);
        }

        public void Clear()
        {
            ((ICollection<T>)MainBuffer).Clear();
        }

        public bool Contains(T item)
        {
            return ((ICollection<T>)MainBuffer).Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            ((ICollection<T>)MainBuffer).CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            OnItemRemoved(item);
            return ((ICollection<T>)MainBuffer).Remove(item);
        }

        public int IndexOf(T item)
        {
            return ((IList<T>)MainBuffer).IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            ((IList<T>)MainBuffer).Insert(index, item);
            OnItemAdded(item);
        }

        public void RemoveAt(int index)
        {
            OnItemRemoved(MainBuffer[index]);
            ((IList<T>)MainBuffer).RemoveAt(index);
        }
    }
}

