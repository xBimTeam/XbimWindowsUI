using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;
using Xbim.Common.Geometry;

namespace Xbim.Presentation
{
    public class WpfPoint3DCollectionEnumerator : IEnumerator<XbimPoint3D>
    {
        private readonly Point3DCollection _wpfPoints;
        private int _currentPos=-1;
        
        public WpfPoint3DCollectionEnumerator(Point3DCollection wpfPoints)
        {
            _wpfPoints = wpfPoints;
        }

        public XbimPoint3D Current
        {
            get
            {
                var p3D = _wpfPoints[_currentPos];
                return new XbimPoint3D(p3D.X, p3D.Y, p3D.Z);
            }
        }

        public void Dispose()
        {
            
        }

        object IEnumerator.Current
        {
            get 
            {
                var p3D = _wpfPoints[_currentPos];
                return new XbimPoint3D(p3D.X, p3D.Y, p3D.Z);
            }
        }

        public bool MoveNext()
        {
            if (_currentPos < _wpfPoints.Count-1)
            {
                _currentPos++;
                return true;
            }
            return false;
        }

        public void Reset()
        {
            _currentPos = -1;
        }
    }

    public class WpfPoint3DCollection
         : IEnumerable<XbimPoint3D>
    {
        private readonly Point3DCollection _wpfPoints;
       
        public WpfPoint3DCollection(Point3DCollection wpfPoints)
        {
            _wpfPoints = wpfPoints;
        }

        public WpfPoint3DCollection(IEnumerable<XbimPoint3D> xbimPoints)
        {
            var realPoints = xbimPoints as IList<XbimPoint3D> ?? xbimPoints.ToList();
            _wpfPoints = new Point3DCollection(realPoints.Count);
            foreach (var pt in realPoints)
                _wpfPoints.Add(new Point3D(pt.X, pt.Y, pt.Z));
        }

        public WpfPoint3DCollection(int c)
        {
            _wpfPoints = new Point3DCollection(c);
        }

        public static implicit operator Point3DCollection(WpfPoint3DCollection points)
        {
            return points._wpfPoints;
        }


        public IEnumerator<XbimPoint3D> GetEnumerator()
        {
            return new WpfPoint3DCollectionEnumerator(_wpfPoints);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new WpfPoint3DCollectionEnumerator(_wpfPoints);
        }
    }
   
}
