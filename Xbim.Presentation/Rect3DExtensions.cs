using System.Windows.Media.Media3D;
using Xbim.Common.Geometry;

namespace Xbim.Presentation
{
    public static  class Rect3DExtensions
    {
        public static XbimRect3D ToXbimRect3D(this Rect3D r3D)
        {
            return new XbimRect3D(r3D.X, r3D.Y, r3D.Z, r3D.SizeX, r3D.SizeY, r3D.SizeZ);
        }
    }
}
