using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Xbim.Common.Geometry;

namespace Xbim.Presentation
{

    public class WpfVector3DCollectionEnumerator : IEnumerator<XbimVector3D>
    {
        Vector3DCollection wpfVectors;
        int currentPos = -1;

        public WpfVector3DCollectionEnumerator(Vector3DCollection wpfVectors)
        {
            this.wpfVectors = wpfVectors;
        }

        public XbimVector3D Current
        {
            get
            {
                Vector3D v3d = wpfVectors[currentPos];
                return new XbimVector3D(v3d.X, v3d.Y, v3d.Z);
            }
        }

        public void Dispose()
        {

        }

        object System.Collections.IEnumerator.Current
        {
            get
            {
                Vector3D p3d = wpfVectors[currentPos];
                return new XbimPoint3D(p3d.X, p3d.Y, p3d.Z);
            }
        }

        public bool MoveNext()
        {
            if (wpfVectors == null)
                return false;
            if (currentPos < wpfVectors.Count - 1)
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

    public class WpfVector3DCollection
         : IEnumerable<XbimVector3D>
    {
        Vector3DCollection wpfVectors;
        public WpfVector3DCollection(Vector3DCollection wpfVectors)
        {
            this.wpfVectors = wpfVectors;
        }

        public WpfVector3DCollection(IEnumerable<XbimVector3D> xbimVectors)
        {
            IList<XbimVector3D> realVectors = xbimVectors as IList<XbimVector3D>;
            if (realVectors == null) realVectors = xbimVectors.ToList();
            wpfVectors = new Vector3DCollection(realVectors.Count);
            foreach (var vec in realVectors)
                wpfVectors.Add(new Vector3D(vec.X, vec.Y, vec.Z));
        }
        public static implicit operator Vector3DCollection(WpfVector3DCollection vectors)
        {
            return vectors.wpfVectors;
        }


        public IEnumerator<XbimVector3D> GetEnumerator()
        {
            return new WpfVector3DCollectionEnumerator(this.wpfVectors);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new WpfVector3DCollectionEnumerator(this.wpfVectors);
        }
    }


}
