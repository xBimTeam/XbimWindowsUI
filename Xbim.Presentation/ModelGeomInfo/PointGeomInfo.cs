using System.Windows.Media.Media3D;
using Xbim.Common;
using Xbim.Common.Geometry;

namespace Xbim.Presentation.ModelGeomInfo
{
    public class PointGeomInfo
    {
        public XbimVector3D ModelReferencePoint { get; set; } = XbimVector3D.Zero;
        public IPersistEntity Entity;
        public int EntityLabel
        {
            get
            {
                if (Entity == null)
                    return 0;
                return Entity.EntityLabel;
            }
        }
        public Point3D Point;
        public XbimVector3D ModelPoint => new XbimVector3D(Point.X, Point.Y, Point.Z) + ModelReferencePoint;
    }
}
