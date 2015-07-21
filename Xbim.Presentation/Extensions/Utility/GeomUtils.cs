using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;
using Xbim.Common.Geometry;

namespace Xbim.Presentation.Extensions.Utility
{
    static class GeomUtils
    {
        public static Vector3DCollection GetVectorCollection(List<XbimVector3D> points)
        {
            var ret = new Vector3DCollection(points.Count);
            foreach (var enumPoint in points)
            {
                ret.Add(new Vector3D(enumPoint.X, enumPoint.Y, enumPoint.Z));
            }
            return ret;
        }

        public static Point3DCollection GetPointCollection(List<XbimPoint3D> points)
        {
            var ret = new Point3DCollection(points.Count);
            foreach (var enumPoint in points)
            {
                ret.Add(new Point3D(enumPoint.X, enumPoint.Y, enumPoint.Z));
            }
            return ret;
        }
    }
}
