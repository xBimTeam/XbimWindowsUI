using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

// source from http://stevemdev.wordpress.com/2012/01/12/observable-mru-list/

namespace XbimXplorer
{
    public class ObservableMruList<T> : ObservableCollection<T>
    {
        private readonly int _maxSize = -1;
        private readonly IEqualityComparer<T> _itemComparer = null;

        public ObservableMruList()
            : base()
        { }

        public ObservableMruList(IEnumerable<T> collection)
            : base(collection)
        { }

        public ObservableMruList(List<T> list)
            : base(list)
        { }

        public ObservableMruList(int maxSize, IEqualityComparer<T> itemComparer)
            : base()
        {
            _maxSize = maxSize;
            _itemComparer = itemComparer;
        }

        public ObservableMruList(IEnumerable<T> collection, int maxSize, IEqualityComparer<T> itemComparer)
            : base(collection)
        {
            _maxSize = maxSize;
            _itemComparer = itemComparer;
            RemoveOverflow();
        }

        public ObservableMruList(List<T> list, int maxSize, IEqualityComparer<T> itemComparer)
            : base(list)
        {
            _maxSize = maxSize;
            _itemComparer = itemComparer;
            RemoveOverflow();
        }

        public int MaxSize
        {
            get { return _maxSize; }
        }

        public new void Add(T item)
        {
            int indexOfMatch = IndexOf(item);
            if (indexOfMatch < 0)
            {
                base.Insert(0, item);
            }
            else
            {
                base.Move(indexOfMatch, 0);
            }
            RemoveOverflow();
        }

        public new bool Contains(T item)
        {
            return this.Contains(item, _itemComparer);
        }

        public new int IndexOf(T item)
        {
            int indexOfMatch = -1;
            if (_itemComparer != null)
            {
                for (int idx = 0; idx < Count; idx++)
                {
                    if (_itemComparer.Equals(item, this[idx]))
                    {
                        indexOfMatch = idx; break;
                    }
                }
            }
            else
            {
                indexOfMatch = base.IndexOf(item);
            }
            return indexOfMatch;
        }

        public new bool Remove(T item)
        {
            bool opResult = false;

            int targetIndex = IndexOf(item);
            if (targetIndex > -1)
            {
                RemoveAt(targetIndex);
                opResult = true;
            }

            return opResult;
        }

        private void RemoveOverflow()
        {
            if (MaxSize > 0)
            {
                while (Count > MaxSize)
                {
                    RemoveAt(Count - 1);
                }
            }
        }
    }
}