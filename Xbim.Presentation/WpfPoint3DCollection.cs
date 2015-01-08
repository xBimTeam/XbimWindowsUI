using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;
using Xbim.Common.Geometry;

namespace Xbim.Presentation
{
    public class WpfPoint3DCollectionEnumerator : IEnumerator<XbimPoint3D>
    {
        Point3DCollection wpfPoints;
        int currentPos=-1;
        
        public WpfPoint3DCollectionEnumerator(Point3DCollection wpfPoints)
        {
            this.wpfPoints = wpfPoints;
        }

        public XbimPoint3D Current
        {
            get
            {
                Point3D p3d = wpfPoints[currentPos];
                return new XbimPoint3D(p3d.X, p3d.Y, p3d.Z);
            }
        }

        public void Dispose()
        {
            
        }

        object System.Collections.IEnumerator.Current
        {
            get 
            {
                Point3D p3d = wpfPoints[currentPos];
                return new XbimPoint3D(p3d.X, p3d.Y, p3d.Z);
            }
        }

        public bool MoveNext()
        {
            if (currentPos < wpfPoints.Count-1)
            {
                currentPos++;
                return true;
            }
            else
                return false;


        }

        public void Reset()
        {
            currentPos = -1;
        }
    }

    public class WpfPoint3DCollection
         : IEnumerable<XbimPoint3D>
    {
        Point3DCollection wpfPoints;
       
        public WpfPoint3DCollection(Point3DCollection wpfPoints)
        {
            this.wpfPoints = wpfPoints;
        }

        public WpfPoint3DCollection(IEnumerable<XbimPoint3D> xbimPoints)
        {
            IList<XbimPoint3D> realPoints = xbimPoints as  IList<XbimPoint3D>;
            if(realPoints == null) realPoints = xbimPoints.ToList();
            wpfPoints = new Point3DCollection(realPoints.Count);
            foreach (var pt in realPoints)
                wpfPoints.Add(new Point3D(pt.X, pt.Y, pt.Z));
        }

        public WpfPoint3DCollection(int c)
        {
            wpfPoints = new Point3DCollection(c);
        }

        public static implicit operator Point3DCollection(WpfPoint3DCollection points)
        {
            return points.wpfPoints;
        }


        public IEnumerator<XbimPoint3D> GetEnumerator()
        {
            return new WpfPoint3DCollectionEnumerator(this.wpfPoints);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new WpfPoint3DCollectionEnumerator(this.wpfPoints);
        }
    }
   
}
