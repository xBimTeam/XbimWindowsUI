using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XbimXplorer.Commands
{
    internal class SquareBracketIndexer
    {
        internal string Property;
        internal int Index = -1;
        internal SquareBracketIndexer(string start)
        {
            Property = start;
            if (start.Contains('['))
            {
                string[] cut = start.Split(new char[] { '[', ']' }, StringSplitOptions.RemoveEmptyEntries);
                Property = cut[0];
                int.TryParse(cut[1], out Index);
            }
        }

        internal IEnumerable<T> GetItem<T>(IEnumerable<T> labels)
        {
            if (Index == -1)
                return labels;
            if (labels.Count() > Index)
            {
                T iVal = labels.ElementAt(Index);
                return new T[] { iVal };
            }
            return new T[] { };
        }
    }
}
