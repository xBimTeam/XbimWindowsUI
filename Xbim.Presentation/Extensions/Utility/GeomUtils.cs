using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Xbim.Common.Geometry;

namespace Xbim.Presentation.Extensions.Utility
{
    static class GeomUtils
    {
        public static Vector3DCollection CombineVectorCollection(Vector3DCollection initialVectors, List<XbimVector3D> points)
        {
            var ret = new Vector3DCollection(initialVectors.Count + points.Count);
            foreach (var enumVector in initialVectors)
            {
                ret.Add(enumVector);
            }
            foreach (var enumPoint in points)
            {
                ret.Add(new Vector3D(enumPoint.X, enumPoint.Y, enumPoint.Z));
            }
            return ret;
        }

        public static Vector3DCollection GetVectorCollection(List<XbimVector3D> points)
        {
            var ret = new Vector3DCollection(points.Count);
            foreach (var enumPoint in points)
            {
                ret.Add(new Vector3D(enumPoint.X, enumPoint.Y, enumPoint.Z));
            }
            return ret;
        }

        public static Int32Collection CombineIndexCollection(Int32Collection initialIndices, List<int> addingIndices, int offset)
        {

            var ret = new Int32Collection(initialIndices.Count + addingIndices.Count);
            foreach (var origIndex in initialIndices)
            {
                ret.Add(origIndex);
            }
            foreach (var origIndex in addingIndices)
            {
                ret.Add(origIndex + offset);
            }
            return ret;
        }


        public static Point3DCollection CombinePointCollection(Point3DCollection initialPOints, List<XbimPoint3D> points)
        {
            var ret = new Point3DCollection(initialPOints.Count + points.Count);
            foreach (var enumPoint in initialPOints)
            {
                ret.Add(enumPoint);
            }
            foreach (var enumPoint in points)
            {
                ret.Add(new Point3D(enumPoint.X, enumPoint.Y, enumPoint.Z));
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
