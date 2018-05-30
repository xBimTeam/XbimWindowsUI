// this code is conveted from a vb version contributed to the project @
// https://github.com/xBimTeam/XbimGeometry/issues/33
// thanks @divyalp
//

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Xbim.Presentation.Extensions
{
    class BiDimensional
    {
        public Point3DCollection ConvertMesh3DToPolylineX(MeshGeometry3D vMesh3D, bool isClosed)
        {
            if (vMesh3D == null)
                return new Point3DCollection();
            Point3DCollection vPoint3DCol = new Point3DCollection();
            for (var i = 0; i <= vMesh3D.Positions.Count - 1; i++)
                vPoint3DCol.Add(new Point3D(vMesh3D.Positions[i].X, vMesh3D.Positions[i].Y, 0));
             Int32Collection vNewIndices = new Int32Collection();
            Point3DCollection vNewPos = new Point3DCollection();
            int vIndex1;
            int vIndex2;
            int vIndex3;
            Triangle vTriangle;
            List<Triangle> vListTriangles = new List<Triangle>();
            Point3D p1, p2, p3;
            for (var i = 0; i <= vMesh3D.TriangleIndices.Count - 1; i += 3)
            {
                p1 = vPoint3DCol[vMesh3D.TriangleIndices[i]];
                p2 = vPoint3DCol[vMesh3D.TriangleIndices[i + 1]];
                p3 = vPoint3DCol[vMesh3D.TriangleIndices[i + 2]];
                vTriangle = new Triangle(p1, p2, p3);
                if (!vTriangle.IsValid)
                    continue;
                vIndex1 = vNewPos.IndexOf(p1);
                vIndex2 = vNewPos.IndexOf(p2);
                vIndex3 = vNewPos.IndexOf(p3);
                if (!vListTriangles.Contains(vTriangle))
                {
                    vListTriangles.Add(vTriangle);
                    if (vIndex1 != -1)
                        vNewIndices.Add(vIndex1);
                    else
                    {
                        vNewIndices.Add(vNewPos.Count);
                        vNewPos.Add(p1);
                    }

                    if (vIndex2 != -1)
                        vNewIndices.Add(vIndex2);
                    else
                    {
                        vNewIndices.Add(vNewPos.Count);
                        vNewPos.Add(p2);
                    }

                    if (vIndex3 != -1)
                        vNewIndices.Add(vIndex3);
                    else
                    {
                        vNewIndices.Add(vNewPos.Count);
                        vNewPos.Add(p3);
                    }
                }
            }

            return Get2DOutline(vNewIndices, vNewPos, isClosed);
        }

        private Point3DCollection Get2DOutline(Int32Collection vIndices, Point3DCollection vPositions, bool isClosed)
        {
            PathGeometry vPathResult = new PathGeometry();
            Point3D p1, p2, p3;
            for (var i = 0; i <= vIndices.Count - 1; i += 3)
            {
                p1 = vPositions[vIndices[i]];
                p2 = vPositions[vIndices[i + 1]];
                p3 = vPositions[vIndices[i + 2]];
                StreamGeometry geo = new StreamGeometry();
                using (StreamGeometryContext ctx = geo.Open())
                {
                    ctx.BeginFigure(new System.Windows.Point(p1.X, p1.Y), true, true);
                    ctx.LineTo(new System.Windows.Point(p2.X, p2.Y), true, true);
                    ctx.LineTo(new System.Windows.Point(p3.X, p3.Y), true, true);
                    ctx.LineTo(new System.Windows.Point(p1.X, p1.Y), true, true);
                    ctx.Close();
                }

                vPathResult = System.Windows.Media.Geometry.Combine(vPathResult, geo, GeometryCombineMode.Union, null /* TODO Change to default(_) if this is not a reference type */);
            }

            vPathResult = vPathResult.GetWidenedPathGeometry(new System.Windows.Media.Pen(System.Windows.Media.Brushes.Black, 2E-06), 1E-06, System.Windows.Media.ToleranceType.Absolute);
            Point3DCollection vPoly = new Point3DCollection();
            System.Windows.Point p;
            System.Windows.Point tg;
            for (var i = 600; i <= 1200 - 1; i++)
            {
                vPathResult.GetPointAtFractionLength(i / 1200, out p, out tg);
                vPoly.Add(new Point3D(p.X, p.Y, 0));
            }

            vPoly.Add(vPoly[0]);
            SimplifyPoints(ref vPoly);
            return vPoly;
        }

        public void SimplifyPoints(ref Point3DCollection vPoints)
        {
            float angulo;
            Point3DCollection listRemove = new Point3DCollection();
            bool doRecursion = false;
            for (int i = 0; i <= vPoints.Count - 3; i += 2)
            {
                angulo = (float)angleByVectors(
                    new Point(vPoints[i].X, vPoints[i].Y), 
                    new Point(vPoints[i + 1].X, vPoints[i + 1].Y), 
                    new Point(vPoints[i + 2].X, vPoints[i + 2].Y)
                    );
                if (angulo >= -0.05 & angulo <= 0.05)
                    listRemove.Add(vPoints[i + 1]);
            }

            for (int i = 0; i <= listRemove.Count - 1; i++)
            {
                vPoints.Remove(listRemove[i]);
                doRecursion = true;
            }

            if (doRecursion)
                SimplifyPoints(ref vPoints);
        }

        public static double angleByVectors(Point ponto1, Point ponto2, Point ponto3)
        {
            Point vet1;
            Point vet2;
            float prodEscalar;
            float multNorma;
            vet1 = new Point(ponto2.X - ponto1.X, ponto2.Y - ponto1.Y);
            vet2 = new Point(ponto3.X - ponto2.X, ponto3.Y - ponto2.Y);
            prodEscalar = (float)(vet1.X * vet2.X + vet1.Y * vet2.Y);

            multNorma =(float) (
                Math.Sqrt(Math.Pow(vet1.X, 2) + Math.Pow(vet1.Y, 2)) *
                Math.Sqrt(Math.Pow(vet2.X, 2) + Math.Pow(vet2.Y, 2))
                );

            return Math.Acos(prodEscalar / multNorma);
        }

        public class Triangle : IEquatable<Triangle>
        {
            public Point3D p1;
            public Point3D p2;
            public Point3D p3;
            public bool IsValid
            {
                get
                {
                    if (p1.Equals(p2))
                        return false;
                    if (p1.Equals(p3))
                        return false;
                    if (p2.Equals(p3))
                        return false;
                    return true;
                }
            }

            public Triangle(Point3D vP1, Point3D vP2, Point3D vP3)
            {
                p1 = vP1;
                p2 = vP2;
                p3 = vP3;
            }

            public bool Equals(Triangle other)
            {
                if (p1.Equals(other.p1) | p1.Equals(other.p2) | p1.Equals(other.p3))
                    if (p2.Equals(other.p1) | p2.Equals(other.p2) | p2.Equals(other.p3))
                        if (p3.Equals(other.p1) | p3.Equals(other.p2) | p3.Equals(other.p3))
                            return true;
                return false;
            }
        }
    }
}
