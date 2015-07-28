using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using Xbim.Common.Geometry;

namespace Xbim.Presentation.ModelGeomInfo
{
    public class PolylineGeomInfo
    {
        Point3DCollection _visualPoints;
        public Point3DCollection VisualPoints
        {
            get
            {
                if (_visualPoints == null)
                {
                    _visualPoints = GeneratePoints();
                }
                return _visualPoints;
            }
        }

        private Point3DCollection GeneratePoints()
        {
            int n = _geomPoints.Count;
            var result = new Point3DCollection(_geomPoints.Count);

            for (int i = 0; i < n; i++)
            {
                var pt = _geomPoints[i].Point;
                result.Add(pt);
                if (i > 0 && i < n - 1)
                    result.Add(pt);
            }
            return result;
        }

        private List<PointGeomInfo> _geomPoints;

        public EntitySelection ParticipatingEntities
        {
            get
            {
                EntitySelection ret = new EntitySelection(false);
                foreach (var item in _geomPoints)
                {
                    if (!ret.Contains(item.Entity))
                        ret.Add(item.Entity);
                }
                return ret;
            }
        }

        public override string ToString()
        {
            if (_geomPoints.Count == 1)
                return string.Format("Selected point: {0:0.##}x {1:0.##}y {2:0.##}z",
                    _geomPoints[0].Point.X, _geomPoints[0].Point.Y, _geomPoints[0].Point.Z);
            var d = GetArea();
            return !double.IsNaN(d) 
                ? string.Format("Lenght: {0:0.##}m Area: {1:0.##}sqm", GetLenght(), d) 
                : string.Format("Lenght: {0:0.##}m", GetLenght());
        }

        public PolylineGeomInfo()
        {
            _geomPoints = new List<PointGeomInfo>();
        }

        public double GetLenght()
        {
            if (_geomPoints == null)
                return 0;

            double ret = 0;
            for (int i = 1; i < _geomPoints.Count(); i++)
            {
                ret += _geomPoints[i - 1].Point.DistanceTo(_geomPoints[i].Point);
            }
            return ret;
        }

        public double GetArea()
        {
            // the normal can be taken from the product of two segments on the polyline
            if (Count() < 3)
                return double.NaN;

            XbimVector3D normal = Normal() * -1;
            XbimVector3D firstSegment = this.FirstSegment();
            XbimVector3D up = XbimVector3D.CrossProduct(normal, firstSegment);
            
            XbimVector3D campos = new XbimVector3D(
                _geomPoints[0].Point.X,
                _geomPoints[0].Point.Y,
                _geomPoints[0].Point.Z
                ); 
            XbimVector3D target = campos + normal;
            XbimMatrix3D m = XbimMatrix3D.CreateLookAt(campos, target, up);


            XbimPoint3D[] point = new XbimPoint3D[Count()];
            for (int i = 0; i < point.Length; i++)
            {
                XbimPoint3D pBefore = new XbimPoint3D(
                    _geomPoints[i].Point.X,
                    _geomPoints[i].Point.Y,
                    _geomPoints[i].Point.Z
                    );
                XbimPoint3D pAft = m.Transform(pBefore);
                point[i] = pAft;
            }

            // http://stackoverflow.com/questions/2553149/area-of-a-irregular-shape
            // it assumes that the last point is NOT the same of the first one, but it tolerates the case.
            double area = 0.0f;
            
            int numVertices = Count();
            for (int i = 0; i < numVertices - 1; ++i)
            {
                area += point[i].X * point[i + 1].Y - point[i + 1].X * point[i].Y;
            }
            area += point[numVertices - 1].X * point[0].Y - point[0].X * point[numVertices - 1].Y;
            area /= 2.0;
            return area;
        }

        private XbimVector3D FirstSegment()
        {
            Vector3D ret = _geomPoints[1].Point - _geomPoints[0].Point;
            return new XbimVector3D(ret.X, ret.Y, ret.Z);
        }

        private XbimVector3D Normal()
        {
            Vector3D seg1 = _geomPoints[1].Point - _geomPoints[0].Point;
            Vector3D seg2 = _geomPoints[2].Point - _geomPoints[1].Point;
            var ret = Vector3D.CrossProduct(seg1, seg2);
            ret.Normalize();
            return new XbimVector3D(ret.X, ret.Y, ret.Z);
        }

        /// <summary>
        /// Creates a rather ugly visual representatino of the polyline.
        /// Fixed in size with respect to the model.
        /// </summary>
        /// <param name="highlighted">The destination visual component to replace the content of.</param>
        internal void SetToVisual(MeshVisual3D highlighted)
        {
            if (_geomPoints == null)
                return;

            var lines = new LinesVisual3D { Color = Colors.Yellow };
            var axesMeshBuilder = new MeshBuilder();

            List<Point3D> path = new List<Point3D>();
            foreach (var item in _geomPoints)
            {
                axesMeshBuilder.AddSphere(item.Point, 0.1);
                path.Add(item.Point);
            }
            if (_geomPoints.Count > 1)
            {
                double lineThickness = 0.05;
                axesMeshBuilder.AddTube(path, lineThickness, 9, false);
            }
            highlighted.Content = new GeometryModel3D(axesMeshBuilder.ToMesh(), Materials.Yellow);
        }

        /// <summary>
        /// The count of points in the polyline
        /// </summary>
        /// <returns>an integer positive or 0 value.</returns>
        internal int Count()
        {
            if (_geomPoints == null)
                return 0;
            return _geomPoints.Count;
        }

        /// <summary>
        /// Empties the point collection.
        /// </summary>
        internal void Clear()
        {
            _visualPoints = null;
            if (_geomPoints != null)
                _geomPoints.Clear();
        }

        internal void Add(PointGeomInfo p)
        {
            _visualPoints = null;
            _geomPoints.Add(p);
        }

        public bool IsEmpty
        {
            get
            {
                return (_geomPoints.Count == 0);
            }
        }

        public Point3D? Last3DPoint {
            get
            {
                if (_geomPoints.Count > 0)
                {
                    return _geomPoints[_geomPoints.Count - 1].Point;
                }
                return null;
            }
        }

        /// <summary>
        /// Removes the last point in the underlying PointGeomInfo collection.
        /// </summary>
        internal void RemoveLast()
        {
            _geomPoints.RemoveAt(_geomPoints.Count - 1);
            _visualPoints = null;
        }
    }
}
