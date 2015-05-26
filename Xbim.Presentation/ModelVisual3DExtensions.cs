using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Xbim.Presentation
{
    public static class ModelVisual3DExtensions
    {
        public static void AddGeometry(this ModelVisual3D visual, GeometryModel3D geometry)
        {
            if (visual.Content == null)
                visual.Content = geometry;
            else
            {
                if (visual.Content is Model3DGroup)
                {
                    var m3D = (GeometryModel3D)((Model3DGroup)visual.Content).Children.First();
                    MeshGeometry3D main = (MeshGeometry3D)(m3D.Geometry);
                    MeshGeometry3D toAdd = (MeshGeometry3D)(geometry.Geometry);
                    Point3DCollection pc = new Point3DCollection(main.Positions.Count + toAdd.Positions.Count);
                    foreach (var pt in main.Positions) pc.Add(pt);
                    foreach (var pt in toAdd.Positions)
                    {
                        pc.Add(geometry.Transform.Transform(pt)); 
                    }
                    main.Positions = pc;

                    Vector3DCollection vc = new Vector3DCollection(main.Normals.Count + toAdd.Normals.Count);
                    foreach (var v in main.Normals) vc.Add(v);
                    foreach (var norm in toAdd.Normals) vc.Add(norm);
                    main.Normals = vc;

                    int maxIndices = main.Positions.Count; //we need to increment all indices by this amount
                    foreach (var i in toAdd.TriangleIndices) main.TriangleIndices.Add(i + maxIndices);
                    Int32Collection tc = new Int32Collection(main.TriangleIndices.Count + toAdd.TriangleIndices.Count);
                    foreach (var i in main.TriangleIndices) tc.Add(i);
                    foreach (var i in toAdd.TriangleIndices) tc.Add(i + maxIndices);
                    main.TriangleIndices = tc;
                    
                }
                //it is not a group but now needs to be
                else
                {
                    Model3DGroup m3DGroup = new Model3DGroup();
                    m3DGroup.Children.Add(visual.Content);
                    m3DGroup.Children.Add(geometry);
                    visual.Content = m3DGroup;
                }
            }
        }
    }
}
