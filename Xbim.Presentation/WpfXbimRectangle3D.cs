using System.Windows.Media;
using System.Windows.Media.Media3D;
using Xbim.Common.Geometry;

namespace Xbim.Presentation
{
    public class WpfXbimRectangle3D
    {
        public GeometryModel3D Geometry { get; } = new GeometryModel3D();

        public WpfXbimRectangle3D(XbimRect3D block)
        {
            Geometry.Geometry = new MeshGeometry3D();
            var brush = new SolidColorBrush(Colors.LightBlue) {Opacity = 0.3};
            var material = new DiffuseMaterial(brush) {AmbientColor = Colors.LightBlue};

            Geometry.BackMaterial = material;
            Geometry.Material = material;

            var mesh = Geometry.Geometry as MeshGeometry3D;
            
            var min = block.Min;
            var max = block.Max;

            var p0 = new Point3D(min.X, min.Y, min.Z);
            var p1 = new Point3D(max.X, min.Y, min.Z);
            var p2 = new Point3D(max.X, max.Y, min.Z);
            var p3 = new Point3D(min.X, max.Y, min.Z);
            var p4 = new Point3D(min.X, min.Y, max.Z);
            var p5 = new Point3D(max.X, min.Y, max.Z);
            var p6 = new Point3D(max.X, max.Y, max.Z);
            var p7 = new Point3D(min.X, max.Y, max.Z);

            mesh.Positions.Add(p0);
            mesh.Positions.Add(p1);
            mesh.Positions.Add(p2);
            mesh.Positions.Add(p3);
            mesh.Positions.Add(p4);
            mesh.Positions.Add(p5);
            mesh.Positions.Add(p6);
            mesh.Positions.Add(p7);

            var normX = new Vector3D(1, 0, 0);
            var normMinX = new Vector3D(-1, 0, 0);
            var normY = new Vector3D(0, 1, 0);
            var normMinY = new Vector3D(0, -1, 0);
            var normZ = new Vector3D(0, 0, 1);
            var normMinZ = new Vector3D(0, 0, -1);

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
