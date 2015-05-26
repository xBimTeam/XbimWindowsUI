using System;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Xbim.Common.Geometry;

namespace Xbim.Presentation
{
    public static class WpfExtensions
    {
        #region Point3DCollection Extensions


        /// <summary>
        /// Grows a collection by the required size
        /// </summary>
        /// <param name="pointColl"></param>
        /// <param name="growSize"></param>
        public static Point3DCollection GrowBy(this Point3DCollection pointColl, int growSize)
        {
            Point3DCollection grown = new Point3DCollection(pointColl.Count + growSize);
            foreach (var pt in pointColl) grown.Add(pt);
            return grown;
        }

        #endregion

        #region Vector3DCollection Extensions

        /// <summary>
        /// Grows a collection by the required size
        /// </summary>
        /// <param name="vecColl"></param>
        /// <param name="growSize"></param>
        public static Vector3DCollection GrowBy(this Vector3DCollection vecColl, int growSize)
        {
            Vector3DCollection grown = new Vector3DCollection(vecColl.Count + growSize);
            foreach (var v in vecColl) grown.Add(v);
            return grown;
        }

        #endregion
        #region Int32Collection  Extensions

        /// <summary>
        /// Grows a collection by the required size
        /// </summary>
        /// <param name="int32Coll"></param>
        /// <param name="growSize"></param>
        public static Int32Collection GrowBy(this Int32Collection int32Coll, int growSize)
        {
            Int32Collection grown = new Int32Collection(int32Coll.Count + growSize);
            foreach (var v in int32Coll) grown.Add(v);
            return grown;
        }

        #endregion

        #region QuaternionRotation3D Extensions

        public static QuaternionRotation3D GetQuaternionRotation3D(this XbimQuaternion xq)
        {
            return new QuaternionRotation3D(new Quaternion(xq.X, xq.Y, xq.Z, xq.W * (180.0 / Math.PI)));
        }


        #endregion

        #region RotateTransform3D Extensions
        public static RotateTransform3D GetRotateTransform3D(this XbimQuaternion xq)
        {
            RotateTransform3D r = new RotateTransform3D();
            r.Rotation = new QuaternionRotation3D(new Quaternion(xq.X, xq.Y, xq.Z, xq.W * (180.0 / Math.PI)));
            return r;
        }
        public static RotateTransform3D GetRotateTransform3D(this XbimMatrix3D m)
        {
            RotateTransform3D r = new RotateTransform3D();
            XbimQuaternion xq = m.GetRotationQuaternion();
            r.Rotation = new QuaternionRotation3D(new Quaternion(xq.X, xq.Y, xq.Z, xq.W * (180.0 / Math.PI)));
            return r;
        }
        #endregion
    }
}
