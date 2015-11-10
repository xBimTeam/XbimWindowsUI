using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xbim.Presentation
{
    public class HistoryCollection<T>
    {
        private readonly List<T> _items = new List<T>();

        public int Size { get; private set; }

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
    }
}
