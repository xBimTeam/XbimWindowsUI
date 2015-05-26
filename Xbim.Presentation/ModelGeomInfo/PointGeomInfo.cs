using System.Windows.Media.Media3D;
using Xbim.XbimExtensions.Interfaces;

namespace Xbim.Presentation.ModelGeomInfo
{
    public class PointGeomInfo
    {
        public IPersistIfcEntity Entity;
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
    }
}
