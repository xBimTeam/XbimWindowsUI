using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

// source from http://stevemdev.wordpress.com/2012/01/12/observable-mru-list/

namespace XbimXplorer
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ObservableMruList<T> : ObservableCollection<T>
    {
        private readonly int _maxSize = -1;
        private readonly IEqualityComparer<T> _itemComparer;

        /// <summary>
        /// 
        /// </summary>
        public ObservableMruList()
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="collection"></param>
        public ObservableMruList(IEnumerable<T> collection)
            : base(collection)
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="list"></param>
        public ObservableMruList(List<T> list)
            : base(list)
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="maxSize"></param>
        /// <param name="itemComparer"></param>
        public ObservableMruList(int maxSize, IEqualityComparer<T> itemComparer)
        {
            _maxSize = maxSize;
            _itemComparer = itemComparer;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="maxSize"></param>
        /// <param name="itemComparer"></param>
        public ObservableMruList(IEnumerable<T> collection, int maxSize, IEqualityComparer<T> itemComparer)
            : base(collection)
        {
            _maxSize = maxSize;
            _itemComparer = itemComparer;
            RemoveOverflow();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="list"></param>
        /// <param name="maxSize"></param>
        /// <param name="itemComparer"></param>
        public ObservableMruList(List<T> list, int maxSize, IEqualityComparer<T> itemComparer)
            : base(list)
        {
            _maxSize = maxSize;
            _itemComparer = itemComparer;
            RemoveOverflow();
        }

        /// <summary>
        /// 
        /// </summary>
        public int MaxSize
        {
            get { return _maxSize; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        public new void Add(T item)
        {
            int indexOfMatch = IndexOf(item);
            if (indexOfMatch < 0)
            {
                Insert(0, item);
            }
            else
            {
                Move(indexOfMatch, 0);
            }
            RemoveOverflow();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public new bool Contains(T item)
        {
            return this.Contains(item, _itemComparer);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
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