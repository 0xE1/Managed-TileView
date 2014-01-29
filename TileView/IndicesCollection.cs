using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TileView
{
    public class IndicesCollection : IList<int>
    {
        List<int> m_Internal = new List<int>();
        public IndicesCollection()
        {
        }

        public int IndexOf(int item)
        {
            return m_Internal.IndexOf(item);
        }

        public void Add(int item)
        {
            m_Internal.Add(item);
            if (CollectionChanged != null)
                CollectionChanged.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<int>() { item }));
        }

        public void Insert(int index, int item)
        {
            m_Internal.Insert(index, item);
            if (CollectionChanged != null)
                CollectionChanged.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<int>() { item }));
        }

        public int this[int index]
        {
            get
            {
                return m_Internal[index];
            }
            set
            {
                int oldItem = m_Internal[index];
                m_Internal[index] = value;
                if (CollectionChanged != null)
                    CollectionChanged.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<int>() { value }));
            }
        }

        public void Clear()
        {
            if (CollectionChanged != null)
                CollectionChanged.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, m_Internal));

            m_Internal.Clear();
        }

        public bool Contains(int item)
        {
            return m_Internal.Contains(item);
        }

        public void CopyTo(int[] array, int arrayIndex)
        {
            m_Internal.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return m_Internal.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= m_Internal.Count)
                throw new IndexOutOfRangeException("index out of range");
            int item = this[index];
            m_Internal.RemoveAt(index);

            if (CollectionChanged != null)
                CollectionChanged.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new List<int>() { item }));
        }

        public bool Remove(int item)
        {
            int idx = m_Internal.IndexOf(item);
            bool succ = m_Internal.Remove(item);

            if (succ && CollectionChanged != null)
                CollectionChanged.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new List<int>() { item }));
            return succ;
        }

        public IEnumerator<int> GetEnumerator()
        {
            return m_Internal.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return m_Internal.GetEnumerator();
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public delegate void NotifyCollectionChangedEventHandler(object sender, NotifyCollectionChangedEventArgs args);

        public class NotifyCollectionChangedEventArgs : EventArgs
        {
            public NotifyCollectionChangedAction Action;
            public List<int> ItemsChanged;

            /// <summary>
            /// Initializes NotifyCollectionChangedEventArgs on Add or Remove action with list of changed items
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="action"></param>
            /// <param name="itemsChanged"></param>
            public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, List<int> itemsChanged)
            {
                Action = action;
                ItemsChanged = itemsChanged;
            }
        }
        public enum NotifyCollectionChangedAction
        {
            Add,
            Remove
        }

    }
}
