using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using Xbim.Common.Geometry;

namespace Xbim.Presentation.ModelGeomInfo
{
    public class PolylineGeomInfo
    {
        Point3DCollection _VisualPoints;
        public Point3DCollection VisualPoints
        {
            get
            {
                if (_VisualPoints == null)
                {
                    _VisualPoints = GeneratePoints();
                }
                return _VisualPoints;
            }
        }

        private Point3DCollection GeneratePoints()
        {
            int n = _GeomPoints.Count;
            var result = new Point3DCollection(_GeomPoints.Count);

            for (int i = 0; i < n; i++)
            {
                var pt = _GeomPoints[i].Point;
                result.Add(pt);
                if (i > 0 && i < n - 1)
                    result.Add(pt);
            }
            return result;
        }

        private List<PointGeomInfo> _GeomPoints;

        public EntitySelection ParticipatingEntities
        {
            get
            {
                EntitySelection ret = new EntitySelection(false);
                foreach (var item in _GeomPoints)
                {
                    if (!ret.Contains(item.Entity))
                        ret.Add(item.Entity);
                }
                return ret;
            }
        }

        public override string ToString()
        {
            double d = this.GetArea();
            if (!double.IsNaN(d))
                return string.Format("Lenght: {0:0.##}m Area: {1:0.##}sqm", this.GetLenght(), d);
            else
                return string.Format("Lenght: {0:0.##}m", this.GetLenght());
        }

        public PolylineGeomInfo()
        {
            _GeomPoints = new List<PointGeomInfo>();
        }

        public double GetLenght()
        {
            if (_GeomPoints == null)
                return 0;

            double ret = 0;
            for (int i = 1; i < _GeomPoints.Count(); i++)
            {
                ret += _GeomPoints[i - 1].Point.DistanceTo(_GeomPoints[i].Point);
            }
            return ret;
        }

        public double GetArea()
        {
            // the normal can be taken from the product of two segments on the polyline
            if (Count() < 3)
                return double.NaN;

            XbimVector3D normal = this.Normal() * -1;
            XbimVector3D firstSegment = this.firstSegment();
            XbimVector3D up = XbimVector3D.CrossProduct(normal, firstSegment);
            
            XbimVector3D campos = new XbimVector3D(
                _GeomPoints[0].Point.X,
                _GeomPoints[0].Point.Y,
                _GeomPoints[0].Point.Z
                ); 
            XbimVector3D target = campos + normal;
            XbimMatrix3D m = XbimMatrix3D.CreateLookAt(campos, target, up);


            XbimPoint3D[] point = new XbimPoint3D[Count()];
            for (int i = 0; i < point.Length; i++)
            {
                XbimPoint3D pBefore = new XbimPoint3D(
                    _GeomPoints[i].Point.X,
                    _GeomPoints[i].Point.Y,
                    _GeomPoints[i].Point.Z
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

        private XbimVector3D firstSegment()
        {
            Vector3D ret = _GeomPoints[1].Point - _GeomPoints[0].Point;
            return new XbimVector3D(ret.X, ret.Y, ret.Z);
        }

        private XbimVector3D Normal()
        {
            Vector3D seg1 = _GeomPoints[1].Point - _GeomPoints[0].Point;
            Vector3D seg2 = _GeomPoints[2].Point - _GeomPoints[1].Point;
            var ret = Vector3D.CrossProduct(seg1, seg2);
            ret.Normalize();
            return new XbimVector3D(ret.X, ret.Y, ret.Z);
        }

        /// <summary>
        /// Creates a rather ugly visual representatino of the polyline.
        /// Fixed in size with respect to the model.
        /// </summary>
        /// <param name="Highlighted">The destination visual component to replace the content of.</param>
        internal void SetToVisual(MeshVisual3D Highlighted)
        {
            if (_GeomPoints == null)
                return;

            var lines = new LinesVisual3D { Color = Colors.Yellow };
            var axesMeshBuilder = new MeshBuilder();

            List<Point3D> path = new List<Point3D>();
            foreach (var item in _GeomPoints)
            {
                axesMeshBuilder.AddSphere(item.Point, 0.1);
                path.Add(item.Point);
            }
            if (_GeomPoints.Count > 1)
            {
                double LineThickness = 0.05;
                axesMeshBuilder.AddTube(path, LineThickness, 9, false);
            }
            Highlighted.Content = new GeometryModel3D(axesMeshBuilder.ToMesh(), Materials.Yellow);
        }

        /// <summary>
        /// The count of points in the polyline
        /// </summary>
        /// <returns>an integer positive or 0 value.</returns>
        internal int Count()
        {
            if (_GeomPoints == null)
                return 0;
            return _GeomPoints.Count;
        }

        /// <summary>
        /// Empties the point collection.
        /// </summary>
        internal void Clear()
        {
            _VisualPoints = null;
            if (_GeomPoints != null)
                _GeomPoints.Clear();
        }

        internal void Add(PointGeomInfo p)
        {
            _VisualPoints = null;
            _GeomPoints.Add(p);
        }

        public bool IsEmpty
        {
            get
            {
                return (_GeomPoints.Count == 0);
            }
        }

        public Point3D? Last3DPoint {
            get
            {
                if (_GeomPoints.Count > 0)
                {
                    return _GeomPoints[_GeomPoints.Count - 1].Point;
                }
                return null;
            }
        }

        /// <summary>
        /// Removes the last point in the underlying PointGeomInfo collection.
        /// </summary>
        internal void RemoveLast()
        {
            _GeomPoints.RemoveAt(_GeomPoints.Count - 1);
            _VisualPoints = null;
        }
    }
}
