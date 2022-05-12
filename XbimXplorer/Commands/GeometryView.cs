using System;
using System.Collections.Generic;
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

        private static void WritePointCoord(TextHighliter sb, double x, double y, double z, bool relative = false)
        {
            //x = Convert.ToSingle(x);
            //y = Convert.ToSingle(y);
            //z = Convert.ToSingle(z);

            var rel = relative ? "@" : "";
            if (!double.IsNaN(z))
                sb.Append($"{rel}{x:0.###########},{y:0.###########},{z:0.###########}", Brushes.Black);
            else
                sb.Append($"{rel}{x:0.###########},{y:0.###########}", Brushes.Black);
        }

        private static void WritePointCoord(TextHighliter sb, IItemSet<Xbim.Ifc4.MeasureResource.IfcLengthMeasure> pt)
        {
            WritePointCoord(sb, pt[0], pt[1], pt[2], false);
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

        private static void Report(IIfcTrimmedCurve trimmed, TextHighliter sb)
        {  
            if (trimmed.BasisCurve is IIfcCircle circle)
            {
                // difficult to cut some shapes, but we know how to cut a circle
                Report(circle, trimmed.Trim1, trimmed.Trim2, sb);
            }
            else
                Report(trimmed.BasisCurve, sb);
        }

        private static void Report(IIfcCircle circle, IItemSet<IIfcTrimmingSelect> trim1, IItemSet<IIfcTrimmingSelect> trim2, TextHighliter sb)
        {
            // this one makes an arc, knowing the circle
            var v1 = trim1.FirstOrDefault();//  as Xbim.Ifc4.MeasureResource.IfcParameterValue;
            var v2 = trim2.FirstOrDefault();//  as Xbim.Ifc4.MeasureResource.IfcParameterValue;
            if (v1 is null || v2 == null || v1 is IIfcCartesianPoint || v2 is IIfcCartesianPoint)
            {
                Report(circle, sb);
                return;
            }
            var startang = ((Xbim.Ifc4.MeasureResource.IfcParameterValue)v1) * circle.Model.ModelFactors.AngleToRadiansConversionFactor;
            var endang = ((Xbim.Ifc4.MeasureResource.IfcParameterValue)v2) * circle.Model.ModelFactors.AngleToRadiansConversionFactor;
            // in acad we need the start point
            var startPx = circle.Radius * Math.Cos(startang);
            var startPy = circle.Radius * Math.Sin(startang);

            SetUcs(sb, circle.Position);
            sb.Append("ARC C", Brushes.Black);
            WritePointCoord(sb, 0, 0, 0);
            WritePointCoord(sb, startPx, startPy, double.NaN);
            sb.Append("A", Brushes.Black);
            var presentedEndAngle = (endang - startang) * 180 / Math.PI;
            sb.Append(presentedEndAngle.ToString(), Brushes.Black);
            SetUcs(sb);
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
                sb.Append("UCS X 90", Brushes.Black);
                sb.Append("UCS Y 90", Brushes.Black);
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
                Report((IIfcTrimmedCurve)obj, sb);
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

            if (obj is IIfcGeometricRepresentationItem cnv)
            {
                Report(cnv, sb);
            }
            else if (obj is IIfcClosedShell)
                Report((IIfcClosedShell)obj, sb);
            else if (obj is IIfcPolyLoop)
                Report((IIfcPolyLoop)obj, sb);
            else if (obj is IIfcSweptDiskSolid)
                Report((IIfcSweptDiskSolid)obj, sb);
            else if (obj is IIfcProductDefinitionShape)
                Report((IIfcProductDefinitionShape)obj, sb);
            else
            {
                sb.Append($"No information for {obj.GetType()}", Brushes.Black);
                return sb;
            }
            sb.Append("3DORBIT", Brushes.Black);
            sb.Append("", Brushes.Black);
            sb.Append("===", Brushes.Black);
            return sb;
        }

        private static void Report(IIfcGeometricRepresentationItem obj, TextHighliter sb)
        {
            if (obj is IIfcCurve crv)
                Report(crv, sb);
            else if (obj is IIfcSolidModel solid)
                Report(solid, sb);
            else if (obj is IIfcTessellatedItem tess)
                Report(tess, sb);
            else
                sb.Append($"{obj.GetType().Name} not implemented in IIfcGeometricRepresentationItem.", Brushes.Red);
        }

        private static void Report(IIfcFacetedBrep obj, TextHighliter sb)
        {
            Report(obj.Outer, sb);
        }
        
        private static void Report(IIfcSolidModel obj, TextHighliter sb)
        {
            if (obj is IIfcSweptDiskSolid swept)
                Report(swept, sb);
            if (obj is IIfcFacetedBrep brep)
                Report(brep, sb);
            else
                sb.Append($"{obj.GetType().Name} not implemented in IIfcSolidModel.", Brushes.Red);
        }
        private static void Report(IIfcTessellatedItem obj, TextHighliter sb)
        {
            if (obj is IIfcTessellatedFaceSet faceset)
                Report(faceset, sb);
            else
                sb.Append($"{obj.GetType().Name} not implemented in IIfcTessellatedItem.", Brushes.Red);
        }

        private static void Report(IIfcTessellatedFaceSet obj, TextHighliter sb)
        {
            if (obj is IIfcPolygonalFaceSet)
            {
                Report((IIfcPolygonalFaceSet)obj, sb);
            }
            else
            {
                sb.Append($"{obj.GetType().Name} not implemented in IIfcTessellatedFaceSet.", Brushes.Red);
            }
        }
        private static void Report(IIfcPolygonalFaceSet obj, TextHighliter sb)
        {
            // - Coordinates    Ifc4.GeometricModelResource.IfcCartesianPointList3D from: IfcTessellatedFaceSet
            // - Closed         Ifc4.MeasureResource.IfcBoolean(Nullable)
            // - Faces          Ifc4.GeometricModelResource.IfcIndexedPolygonalFace(IItemSet)
            // - PnIndex        Ifc4.MeasureResource.IfcPositiveInteger(IOptionalItemSet)

            // sb.Append($"-LAYER M {bound.Points.Count} ", Brushes.Black);
            if (obj.PnIndex != null && obj.PnIndex.Any())
            {
                // todo: implement PnIndex behaviour
                sb.Append("; Warning: PnIndex not implemented in mesher yet.", Brushes.Red);
            }

            foreach (var face in obj.Faces)
            {
                sb.Append("3DPOLY", Brushes.Black);
                foreach (var index in face.CoordIndex)
                {
                    if (index > int.MaxValue)
                    {
                        sb.Append($";value too long for int in face", Brushes.Black);
                        continue;
                    }
                    int asInt = (int)index;
                    var pt = obj.Coordinates.CoordList[asInt - 1];
                    WritePointCoord(sb, pt);
                }
                sb.Append($"", Brushes.Black);
                sb.Append($"-HYPERLINK I O l  #{face.EntityLabel}", Brushes.Black);
                sb.Append($"", Brushes.Black);
                sb.Append($"", Brushes.Black);
            }
        }

		internal static TextHighliter ReportAsObj(IIfcClosedShell ics)
		{
            var sb = new TextHighliter();
            ReportAsObj(ics, sb);
            return sb;
		}

		private static void ReportAsObj(IIfcClosedShell ics, TextHighliter sb)
		{
            List<int> vertices = new List<int>(); // entitylabel of the vertex
            List<int> indices = new List<int>();
            foreach (var face in ics.CfsFaces)
            {
                ReportAsObj(face, sb, vertices, indices);
            }
			foreach (var vert in vertices)
			{
                var v = ics.Model.Instances[vert] as IIfcCartesianPoint;
                sb.Append($"v {v.X} {v.Y} {v.Z}", Brushes.Black);
			}
            for (int i = 0; i < indices.Count; i += 3)
            {
                sb.Append($"f {indices[i]+1} {indices[i + 1]+1} {indices[i + 2]+1}", Brushes.Black);
            }
        }

		private static void ReportAsObj(IIfcFace face, TextHighliter sb, List<int> vertices, List<int> indices)
		{
			foreach (var bound in face.Bounds)
			{
                ReportAsObj(bound, sb, vertices, indices);
            }
		}

		private static void ReportAsObj(IIfcFaceBound bound, TextHighliter sb, List<int> vertices, List<int> indices)
		{
            if (bound.Bound is IIfcPolyLoop pl)
            {
                foreach (var pt in pl.Polygon)
                {
                    if (vertices.Contains(pt.EntityLabel))
					{
                        indices.Add(vertices.IndexOf(pt.EntityLabel));
					}
                    else
					{
                        vertices.Add(pt.EntityLabel);
                        indices.Add(vertices.Count - 1);
					}
                }
            }
		}
	}
}
