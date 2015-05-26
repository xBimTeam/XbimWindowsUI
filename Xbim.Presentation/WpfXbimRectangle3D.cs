using System.Windows.Media;
using System.Windows.Media.Media3D;
using Xbim.Common.Geometry;

namespace Xbim.Presentation
{
    public class WpfXbimRectangle3D
    {
        private GeometryModel3D _geometry = new GeometryModel3D();

        public GeometryModel3D Geometry { get { return _geometry; } }

        public WpfXbimRectangle3D(XbimRect3D block)
        {
            _geometry.Geometry = new MeshGeometry3D();
            SolidColorBrush brush = new SolidColorBrush(Colors.LightBlue);
            brush.Opacity = 0.3;
            var material = new DiffuseMaterial(brush);
            material.AmbientColor = Colors.LightBlue;

            _geometry.BackMaterial = material;
            _geometry.Material = material;

            var mesh = _geometry.Geometry as MeshGeometry3D;
            
            var min = block.Min;
            var max = block.Max;

            Point3D p0 = new Point3D(min.X, min.Y, min.Z);
            Point3D p1 = new Point3D(max.X, min.Y, min.Z);
            Point3D p2 = new Point3D(max.X, max.Y, min.Z);
            Point3D p3 = new Point3D(min.X, max.Y, min.Z);
            Point3D p4 = new Point3D(min.X, min.Y, max.Z);
            Point3D p5 = new Point3D(max.X, min.Y, max.Z);
            Point3D p6 = new Point3D(max.X, max.Y, max.Z);
            Point3D p7 = new Point3D(min.X, max.Y, max.Z);

            mesh.Positions.Add(p0);
            mesh.Positions.Add(p1);
            mesh.Positions.Add(p2);
            mesh.Positions.Add(p3);
            mesh.Positions.Add(p4);
            mesh.Positions.Add(p5);
            mesh.Positions.Add(p6);
            mesh.Positions.Add(p7);

            Vector3D normX = new Vector3D(1, 0, 0);
            Vector3D normMinX = new Vector3D(-1, 0, 0);
            Vector3D normY = new Vector3D(0, 1, 0);
            Vector3D normMinY = new Vector3D(0, -1, 0);
            Vector3D normZ = new Vector3D(0, 0, 1);
            Vector3D normMinZ = new Vector3D(0, 0, -1);

            mesh.TriangleIndices.Add(0); mesh.Normals.Add(normMinY);
            mesh.TriangleIndices.Add(1); mesh.Normals.Add(normMinY);
            mesh.TriangleIndices.Add(5); mesh.Normals.Add(normMinY);

            mesh.TriangleIndices.Add(0); mesh.Normals.Add(normMinY);
            mesh.TriangleIndices.Add(5); mesh.Normals.Add(normMinY);
            mesh.TriangleIndices.Add(4); mesh.Normals.Add(normMinY);

            mesh.TriangleIndices.Add(1); mesh.Normals.Add(normX);
            mesh.TriangleIndices.Add(2); mesh.Normals.Add(normX);
            mesh.TriangleIndices.Add(6); mesh.Normals.Add(normX);

            mesh.TriangleIndices.Add(1); mesh.Normals.Add(normX);
            mesh.TriangleIndices.Add(6); mesh.Normals.Add(normX);
            mesh.TriangleIndices.Add(5); mesh.Normals.Add(normX);

            mesh.TriangleIndices.Add(4); mesh.Normals.Add(normZ);
            mesh.TriangleIndices.Add(5); mesh.Normals.Add(normZ);
            mesh.TriangleIndices.Add(6); mesh.Normals.Add(normZ);

            mesh.TriangleIndices.Add(4); mesh.Normals.Add(normZ);
            mesh.TriangleIndices.Add(6); mesh.Normals.Add(normZ);
            mesh.TriangleIndices.Add(7); mesh.Normals.Add(normZ);

            mesh.TriangleIndices.Add(0); mesh.Normals.Add(normMinZ);
            mesh.TriangleIndices.Add(2); mesh.Normals.Add(normMinZ);
            mesh.TriangleIndices.Add(1); mesh.Normals.Add(normMinZ);

            mesh.TriangleIndices.Add(0); mesh.Normals.Add(normMinZ);
            mesh.TriangleIndices.Add(3); mesh.Normals.Add(normMinZ);
            mesh.TriangleIndices.Add(2); mesh.Normals.Add(normMinZ);


            mesh.TriangleIndices.Add(0); mesh.Normals.Add(normMinX);
            mesh.TriangleIndices.Add(7); mesh.Normals.Add(normMinX);
            mesh.TriangleIndices.Add(3); mesh.Normals.Add(normMinX);

            mesh.TriangleIndices.Add(0); mesh.Normals.Add(normMinX);
            mesh.TriangleIndices.Add(4); mesh.Normals.Add(normMinX);
            mesh.TriangleIndices.Add(7); mesh.Normals.Add(normMinX);

            mesh.TriangleIndices.Add(3); mesh.Normals.Add(normY);
            mesh.TriangleIndices.Add(6); mesh.Normals.Add(normY);
            mesh.TriangleIndices.Add(2); mesh.Normals.Add(normY);

            mesh.TriangleIndices.Add(3); mesh.Normals.Add(normY);
            mesh.TriangleIndices.Add(7); mesh.Normals.Add(normY);
            mesh.TriangleIndices.Add(6); mesh.Normals.Add(normY);
        }
    }
}
