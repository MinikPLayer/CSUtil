using System;
using System.Collections;
using System.Collections.Generic;

namespace CSUtil.Data
{
    public class OList<T> : IList<T>
    {
        public delegate void CollectionChangedDelegate(OList<T> sender);
        public delegate void ItemAddedDelegate(OList<T> sender, int index, T value);
        public delegate void ItemRemovedDelegate(OList<T> sender, int index, T oldValue);
        public delegate void ItemChangedDelegate(OList<T> sender, int index, T oldValue, T newValue);

        public CollectionChangedDelegate? OnCollectionChanged { get; set; } = null;
        public ItemAddedDelegate? OnItemAdded { get; set; } = null;
        public ItemRemovedDelegate? OnItemRemoved { get; set; } = null;
        public ItemChangedDelegate? OnItemChanged { get; set; } = null;
    
        private readonly List<T> internalList = new List<T>();

        public int Count => internalList.Count;
        public bool IsReadOnly => false;
        
        public IEnumerator<T> GetEnumerator()
        {
            return internalList.GetEnumerator();
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return internalList.GetEnumerator();
        }
        
        public void Add(T item)
        {
            internalList.Add(item);
            int index = internalList.Count - 1;
            OnCollectionChanged?.Invoke(this);
            OnItemAdded?.Invoke(this, index, item);
        }
        
        public void Clear()
        {
            for (int i = internalList.Count - 1; i >= 0; i--)
            {
                RemoveAt(i);
            }
        }
        
        public bool Contains(T item)
        {
            return internalList.Contains(item);
        }
        
        public void CopyTo(T[] array, int arrayIndex)
        {
            internalList.CopyTo(array, arrayIndex);
        }
        
        public bool Remove(T item)
        {
            var index = IndexOf(item);
            if(index == -1)
                return false;
                
            RemoveAt(index);
            return true;
        }

        public int IndexOf(T item)
        {
            return internalList.IndexOf(item);
        }
        
        public void Insert(int index, T item)
        {
            internalList.Insert(index, item);
            OnCollectionChanged?.Invoke(this);
            OnItemAdded?.Invoke(this, index, item);
        }
        
        public void RemoveAt(int index)
        {
            var oldValue = internalList[index];
            internalList.RemoveAt(index);
            OnCollectionChanged?.Invoke(this);
            OnItemRemoved?.Invoke(this, index, oldValue);
        }

        public T this[int index]
        {
            get => internalList[index];
            set
            {
                var oldValue = internalList[index];
                internalList[index] = value;
                OnCollectionChanged?.Invoke(this);
                OnItemChanged?.Invoke(this, index, oldValue, value);
            }
        }

        public OList(params T[] data)
        {
            foreach(var t in data)
                Add(t);
        }
    }
}