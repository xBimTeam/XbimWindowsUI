using System.Windows.Media.Media3D;
using Xbim.Common.Geometry;

namespace Xbim.Presentation
{
    public static class XbimMatrix3DExtensions
    {
        public static Matrix3D ToMatrix3D(this XbimMatrix3D m)
        {
            return new Matrix3D(m.M11, m.M12, m.M13, m.M14,
                    m.M21, m.M22, m.M23, m.M24,
                    m.M31, m.M32, m.M33, m.M34,
                    m.OffsetX, m.OffsetY, m.OffsetZ, m.M44);
        }

        public static MatrixTransform3D ToMatrixTransform3D(this XbimMatrix3D m)
        {
            return new MatrixTransform3D(m.ToMatrix3D());
        }
    }
}
