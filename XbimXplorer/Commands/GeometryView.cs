using System;
using System.Linq;
using System.Windows.Media;
using Xbim.Common;
using Xbim.Ifc4.Interfaces;

namespace XbimXplorer.Commands
{
    /// <summary>
    /// used to show geometry information in other 3D environments
    /// </summary>
    internal static class GeometryView
    {
        private static void Report(IIfcClosedShell shell, TextHighliter sb)
        {
            foreach (var face in shell.CfsFaces)
            {
                Report(face, sb);
            }
        }

        private static void Report(IIfcFace face, TextHighliter sb)
        {
            foreach (var ifcFaceBound in face.Bounds)
            {
                Report(ifcFaceBound, sb);
            }
        }

        private static void Report(IIfcFaceOuterBound ifcFaceBound, TextHighliter sb)
        {
            Report(ifcFaceBound.Bound, sb);
        }

        private static void Report(IIfcPolyline bound, TextHighliter sb)
        {
            sb.Append($"-LAYER M {bound.Points.Count} ", Brushes.Black);

            sb.Append("3DPOLY", Brushes.Black);
            var first = bound.Points.FirstOrDefault();
            IIfcCartesianPoint last = null;
            foreach (var ifcCartesianPoint in bound.Points)
            {
                WritePointCoord(sb, ifcCartesianPoint);
                last = ifcCartesianPoint;
            }
            if (false && last != null)
            {
                if (!last.Equals(first))
                    sb.Append($";open polyloop", Brushes.Black);
            }
            sb.Append($"", Brushes.Black);
            sb.Append($"-HYPERLINK I O l  #{bound.EntityLabel}", Brushes.Black);
            sb.Append($"", Brushes.Black);
            sb.Append($"", Brushes.Black);
        }

        private static void Report(IIfcPolyLoop bound, TextHighliter sb)
        {
            sb.Append($"-LAYER M {bound.Polygon.Count} ", Brushes.Black);

            sb.Append("3DPOLY", Brushes.Black);
            var first = bound.Polygon.FirstOrDefault();
            IIfcCartesianPoint last = null;
            foreach (var ifcCartesianPoint in bound.Polygon)
            {
                WritePointCoord(sb, ifcCartesianPoint);
                last = ifcCartesianPoint;
            }
            if ( false && last != null)
            {
                if (!last.Equals(first))
                    sb.Append($";open polyloop", Brushes.Black);
            }
            sb.Append($"", Brushes.Black);
            sb.Append($"-HYPERLINK I O l  #{bound.EntityLabel}", Brushes.Black);
            sb.Append($"", Brushes.Black);
            sb.Append($"", Brushes.Black);
        }

        private static void WritePointCoord(TextHighliter sb, double x, double y, double z, bool relative = false)
        {
            //x = Convert.ToSingle(x);
            //y = Convert.ToSingle(y);
            //z = Convert.ToSingle(z);

            var rel = relative ? "@" : "";
            if (!double.IsNaN(z))
                sb.Append($"{rel}{x},{y},{z}", Brushes.Black);
            else
                sb.Append($"{rel}{x},{y}", Brushes.Black);
        }

        private static void WritePointCoord(TextHighliter sb, IIfcCartesianPoint ifcCartesianPoint, bool relative = false)
        {
            WritePointCoord(sb, ifcCartesianPoint.X, ifcCartesianPoint.Y, ifcCartesianPoint.Z, relative);
        }

        private static void WritePointCoord(TextHighliter sb, IIfcDirection ifcCartesianPoint, bool relative = false)
        {
            WritePointCoord(sb, ifcCartesianPoint.X, ifcCartesianPoint.Y, ifcCartesianPoint.Z, relative);
        }

        private static void WritePointCoord(TextHighliter sb, IIfcVector p, bool relative = false)
        {
            double val = Convert.ToDouble(p.Magnitude.Value);
            WritePointCoord(sb, p.Orientation.X * val, 
                                p.Orientation.Y * val, 
                                p.Orientation.Z * val, relative);
        }

        private static void Report(IIfcLoop bound, TextHighliter sb)
        {
            if (bound is IIfcPolyLoop)
            {
                Report((IIfcPolyLoop)bound, sb);
            }
            else
            {
                sb.Append($"{bound.GetType().Name} not implemented in IIfcLoop.", Brushes.Red);
            }
        }

        private static void Report(IIfcFaceBound ifcFaceBound, TextHighliter sb)
        {
            Report(ifcFaceBound.Bound, sb);
        }

        private static void Report(IIfcProductDefinitionShape ifcProductDefinitionShape, TextHighliter sb)
        {
            foreach (var item in ifcProductDefinitionShape.Representations)
            {
                Report(item, sb);
            }
        }

        private static void Report(IIfcRepresentation ifcRepresentation, TextHighliter sb)
        {
            foreach (var item in ifcRepresentation.Items)
            {
                Report(item, sb);
            }
        }


        private static void Report(IIfcConnectedFaceSet item, TextHighliter sb)
        {
            foreach (var face in item.CfsFaces)
            {
                Report(face, sb);
            }
        }

        private static void Report(IIfcFaceBasedSurfaceModel item, TextHighliter sb)
        {
            foreach (var face in item.FbsmFaces)
            {
                Report(face, sb);
            }
        }

       

        private static void Report(IIfcRepresentationItem item, TextHighliter sb)
        {
            if (item is IIfcFaceBasedSurfaceModel)
            {
                Report((IIfcFaceBasedSurfaceModel)item, sb);
            }
            else
            {

            }
        }

        private static void Report(IIfcCompositeCurve curve, TextHighliter sb)
        {
            foreach (var ifcCompositeCurveSegment in curve.Segments)
            {
                Report(ifcCompositeCurveSegment, sb);
            }
        }

        private static void Report(IIfcCompositeCurveSegment ifcCompositeCurveSegment, TextHighliter sb)
        {
            Report(ifcCompositeCurveSegment.ParentCurve, sb);
        }

        private static void Report(IIfcTrimmedCurve ifcCompositeCurveSegment, TextHighliter sb)
        {
            Report(ifcCompositeCurveSegment.BasisCurve, sb);
        }

        private static void Report(IIfcCircle cr, TextHighliter sb)
        {
            SetUcs(sb, cr.Position);           
            sb.Append("CIRCLE", Brushes.Black);
            WritePointCoord(sb, 0, 0, 0);
            sb.Append(cr.Radius.ToString(), Brushes.Black);
            SetUcs(sb);
        }

        private static void Report(IIfcLine line, TextHighliter sb)
        {
            sb.Append("line", Brushes.Black);
            WritePointCoord(sb, line.Pnt);
            WritePointCoord(sb, line.Dir, true);
            sb.Append("", Brushes.Black);
        }
        
        private static void SetUcs(TextHighliter sb, IIfcAxis2Placement pos = null)
        {
            if (pos == null)
            {
                sb.Append("UCS w", Brushes.Black);
            }
            else if (pos is IIfcAxis2Placement3D)
            {
                var as1 = pos as IIfcAxis2Placement3D;
                sb.Append("UCS", Brushes.Black);
                WritePointCoord(sb, as1.Location);
                WritePointCoord(sb, as1.Axis);
                WritePointCoord(sb, as1.RefDirection);
                sb.Append("UCS", Brushes.Black);
                sb.Append("x", Brushes.Black);
                sb.Append("90", Brushes.Black);
            }
            else
            {
                sb.Append($"{pos.GetType().Name} not implemented in IIfcCurve.", Brushes.Red);
            }
        }

       

        private static void Report(IIfcCurve obj, TextHighliter sb)
        {
            if (obj is IIfcCompositeCurve)
            {
                Report((IIfcCompositeCurve)obj, sb);
            }
            else if (obj is IIfcTrimmedCurve)
            {
                Report((IIfcTrimmedCurve) obj, sb);
            }
            else if (obj is IIfcCircle)
            {
                Report((IIfcCircle)obj, sb);
            }
            else if (obj is IIfcPolyline)
            {
                Report((IIfcPolyline)obj, sb);
            }
            else if (obj is IIfcLine)
            {
                Report((IIfcLine)obj, sb);
            }
            else
            {
                sb.Append($"{obj.GetType().Name} not implemented in IIfcCurve.", Brushes.Red);
            }
        }

        private static void Report(IIfcSweptDiskSolid obj, TextHighliter sb)
        {
            Report((IIfcCurve)obj.Directrix, sb);
        }

        internal static TextHighliter ReportAcadScript(IPersistEntity obj)
        {
            var sb = new TextHighliter();
            if (obj is IIfcClosedShell)
                Report((IIfcClosedShell) obj, sb);
            if (obj is IIfcPolyLoop)
                Report((IIfcPolyLoop)obj, sb);
            if (obj is IIfcCurve)
                Report((IIfcCurve)obj, sb);
            if (obj is IIfcSweptDiskSolid)
                Report((IIfcSweptDiskSolid)obj, sb);
            if (obj is IIfcProductDefinitionShape)
                Report((IIfcProductDefinitionShape)obj, sb);
            else
            {
                sb.Append($"No information for {obj.GetType()}", Brushes.Black);
                return sb;
            }
            sb.Append("3DORBIT", Brushes.Black);
            sb.Append("", Brushes.Black);
            sb.Append("===", Brushes.Black);
            return  sb;
        }

        
    }
}
