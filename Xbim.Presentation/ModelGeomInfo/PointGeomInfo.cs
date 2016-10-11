using System.Windows.Media.Media3D;
using Xbim.Common;

namespace Xbim.Presentation.ModelGeomInfo
{
    public class PointGeomInfo
    {
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
    }
}
