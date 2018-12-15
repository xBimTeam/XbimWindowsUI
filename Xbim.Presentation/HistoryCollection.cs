using System.Collections.Generic;
using System.Linq;

namespace Xbim.Presentation
{
    /// <summary>
    /// A generic collection that keeps a limited amount of items in the list.
    /// When the number of elements exceeds Size the oldest items are dropped.
    /// </summary>
    /// <typeparam name="T">The type of item to colelct</typeparam>
    public class HistoryCollection<T>
    {
        private readonly List<T> _items = new List<T>();

        public int Size { get; }

        public HistoryCollection(int size)
        {
            Size = size;
        }

        public bool Any()
        {
            return _items.Any();
        }

        public void Push(T item)
        {
            _items.Add(item);
            while (_items.Count > Size)
            {
                _items.RemoveAt(0);
            }
        }
        public T Pop()
        {
            if (!_items.Any()) 
                return default(T);
            var temp = _items[_items.Count - 1];
            _items.RemoveAt(_items.Count - 1);
            return temp;
        }

        public void Remove(int itemAtPosition)
        {
            _items.RemoveAt(itemAtPosition);
        }

        public void Clear()
        {
            _items.Clear();
        }
    }
}
