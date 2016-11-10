using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;
using Xbim.Common.Geometry;

namespace Xbim.Presentation
{

    public class WpfVector3DCollectionEnumerator : IEnumerator<XbimVector3D>
    {
        private readonly Vector3DCollection _wpfVectors;
        private int _currentPos = -1;

        public WpfVector3DCollectionEnumerator(Vector3DCollection wpfVectors)
        {
            _wpfVectors = wpfVectors;
        }

        public XbimVector3D Current
        {
            get
            {
                var v3D = _wpfVectors[_currentPos];
                return new XbimVector3D(v3D.X, v3D.Y, v3D.Z);
            }
        }

        public void Dispose()
        {

        }

        object IEnumerator.Current
        {
            get
            {
                var p3D = _wpfVectors[_currentPos];
                return new XbimPoint3D(p3D.X, p3D.Y, p3D.Z);
            }
        }

        public bool MoveNext()
        {
            if (!(_currentPos < _wpfVectors?.Count - 1))
                return false;
            _currentPos++;
            return true;
        }

        public void Reset()
        {
            _currentPos = -1;
        }
    }

    public class WpfVector3DCollection
         : IEnumerable<XbimVector3D>
    {
        private readonly Vector3DCollection _wpfVectors;

        public WpfVector3DCollection()
        {
            _wpfVectors = new Vector3DCollection();
        }

        public WpfVector3DCollection(Vector3DCollection wpfVectors)
        {
            _wpfVectors = wpfVectors;
        }

        public WpfVector3DCollection(IEnumerable<XbimVector3D> xbimVectors)
        {
            var realVectors = xbimVectors as IList<XbimVector3D> 
                ?? xbimVectors.ToList();
            _wpfVectors = new Vector3DCollection(realVectors.Count);
            foreach (var vec in realVectors)
                _wpfVectors.Add(new Vector3D(vec.X, vec.Y, vec.Z));
        }
        public static implicit operator Vector3DCollection(WpfVector3DCollection vectors)
        {
            return vectors._wpfVectors;
        }
        
        public IEnumerator<XbimVector3D> GetEnumerator()
        {
            return new WpfVector3DCollectionEnumerator(_wpfVectors);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new WpfVector3DCollectionEnumerator(_wpfVectors);
        }
    }
}
