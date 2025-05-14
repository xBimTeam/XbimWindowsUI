using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media;
using Xbim.Common;
using Xbim.Common.Geometry;
using Xbim.Ifc.Extensions;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4x3.GeometryResource;
using Xbim.Ifc4x3.MeasureResource;

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
			TagLastWithEntityLabel(sb, bound);
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
			TagLastWithEntityLabel(sb, bound);
        }

		internal static void WritePointCoord(acadPoint pt, TextHighliter sb)
		{
			WritePointCoord(sb, pt.X, pt.Y, pt.Z);
		}

		private static void WritePointCoord(TextHighliter sb, XbimPoint3D trsfrmd)
		{
			WritePointCoord(sb, trsfrmd.X, trsfrmd.Y, trsfrmd.Z);
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
			if (curve is Xbim.Ifc4x3.GeometryResource.IfcCompositeCurve v43)
			{
				foreach (var ifcCompositeCurveSegment in v43.Segments)
				{
					Report(ifcCompositeCurveSegment, sb);
				}
			}
			else
        {
            foreach (var ifcCompositeCurveSegment in curve.Segments)
            {
                Report(ifcCompositeCurveSegment, sb);
            }
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
            else if (obj is IIfcFace fc)
                Report(fc, sb);
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

		[DebuggerDisplay("{X} {Y} {Z}")]
		internal class acadPoint
		{
			public acadPoint() { }
			public acadPoint(double x, double y, double z)
			{
				X = x;
				Y = y;
				Z = z;
			}

			internal double X { get; set; } = 0;
			internal double Y { get; set; } = 0;
			internal double Z { get; set; } = 0;
		}

		private static void Report(Xbim.Ifc4x3.GeometryResource.IfcCurveSegment obj, TextHighliter sb)
		{
			
			sb.Append($"; Segment of #{obj.ParentCurve.EntityLabel}={obj.ParentCurve.GetType().Name}.", Brushes.Red);
			sb.Append($"; start #{obj.SegmentStart} len: {obj.SegmentLength}.", Brushes.Red);
			if (obj.ParentCurve is IfcClothoid clot && obj.SegmentStart is IfcLengthMeasure lm && lm.Value is double strt && clot.ClothoidConstant.Value is double k)
			{
				if (k > 0)
				{
					if (strt < 0)
						sb.Append("-COLOR RED", Brushes.Black);
					else
						sb.Append("-COLOR YELLOW", Brushes.Black);
				}
				else
				{
					if (strt < 0)
						sb.Append("-COLOR CYAN", Brushes.Black);
					else
						sb.Append("-COLOR BLUE", Brushes.Black);
				}

			}
			else
				sb.Append("-COLOR BYLAYER", Brushes.Black);
			var pts = GetPoints(obj).ToList();
			if (pts.Any())
			{
				var m2 = obj.Placement.ToMatrix3D();
				XbimMatrix3D mat = GetMatrix(obj.Placement, obj.ParentCurve is IfcClothoid);
				var tfmd = pts.Select(pt => mat.Transform(new XbimPoint3D(pt.X, pt.Y, 0))).ToList();
				
				if (pts.Count == 1)
				{
					sb.Append("POINT", Brushes.Black);
					WritePointCoord(sb, tfmd[0].X, tfmd[0].Y, double.NaN);
				}
				else
				{
					sb.Append("PLINE", Brushes.Black);
					foreach (var trsfrmd in tfmd)
					{
						WritePointCoord(sb, trsfrmd.X, trsfrmd.Y, double.NaN);
					}
					// add delta from prev point
					sb.Append("", Brushes.Black);
				}
				TagLastWithEntityLabel(sb, obj);
			}
			// sb.Append($"- start: {obj.SegmentStart.Value}, len: {obj.SegmentLength.Value}", Brushes.Red);
			// sb.Append($"{obj.GetType().Name} not implemented in IIfcGeometricRepresentationItem.", Brushes.Red);
		}

		private static XbimMatrix3D GetMatrix(Xbim.Ifc4x3.GeometryResource.IfcPlacement placement, bool rot = false)
		{
			var trs = (placement.Location is IIfcCartesianPoint p)
				? XbimMatrix3D.CreateTranslation(p.X, p.Y, 0)
				: XbimMatrix3D.Identity;
			if (placement is Xbim.Ifc4x3.GeometryResource.IfcAxis2Placement2D p2d)
			{
				var tp = new XbimPoint3D(p2d.RefDirection.X, p2d.RefDirection.Y, 0);
				var mRot = XbimMatrix3D.CreateRotation(
					new XbimPoint3D(1, 0, 0),
					tp
					);
				trs = mRot * trs;
			}

			//var trs = (placement. is IIfcCartesianPoint p)
			//	? XbimMatrix3D.CreateTranslation(p.X, p.Y, 0)
			//	: XbimMatrix3D.Identity;


			return trs;
		}

		private static object GetDistance(XbimPoint3D p1, XbimPoint3D p2)
		{
			return Math.Sqrt(
				Math.Pow(p2.X - p1.X, 2) +
				Math.Pow(p2.Y - p1.Y, 2) +
				Math.Pow(p2.Z - p1.Z, 2)
				);
		}

		private static IEnumerable<acadPoint> GetPoints(IfcCurveSegment obj)
		{
			if (obj.ParentCurve is Xbim.Ifc4x3.GeometryResource.IfcLine line)
			{
				var param1 = GetParam(obj.SegmentStart);
				var p1 = PointOnLine(line, param1);
				var param2 = GetParam(obj.SegmentLength);
				var p2 = PointOnLine(line, param2);
				// var dist = GetDistance(p1, p2);
				yield return p1;
				yield return p2;
			}
			else if (obj.ParentCurve is Xbim.Ifc4x3.GeometryResource.IfcCircle circle)
			{
				var start = GetParam(obj.SegmentStart);
				var len = GetParam(obj.SegmentLength);
				if (start != 0)
				{ }
				if (obj.SegmentLength is IfcParameterValue pv)
				{
					// ok
				}
				else if (obj.SegmentLength is IfcLengthMeasure lm)
				{
					len = lm / circle.Radius;
				}
				else
				{
					// unexpected
				}
				var mid = (start + len) / 2;

				yield return PointOnCircle(circle, start);
				yield return PointOnCircle(circle, mid);
				yield return PointOnCircle(circle, len);
			}
			else if (obj.ParentCurve is Xbim.Ifc4x3.GeometryResource.IfcClothoid clothoid)
			{
				if (obj.SegmentLength is IfcLengthMeasure sl
					&& obj.SegmentStart is IfcLengthMeasure st
					&& sl.Value is double dLen
					&& st.Value is double dStart
					&& clothoid.ClothoidConstant.Value is double dCostant
					)
				{
					GetPositionInfo(clothoid.Position, out var p, out var refDir);
					var clotPoints = GetCoreClothoid(dCostant, dStart, dLen, .2);
					var newPts = Fix(clotPoints, clothoid.Position);
					foreach (var trsfrmd in newPts)
					{
						yield return trsfrmd;
					}
				}
				else
				{
					throw new NotImplementedException();
				}
				// yield return new acadPoint(0, 0, 0);
			}
			else
			{

			}

			yield break;
			
		}

		private static IEnumerable<acadPoint> Fix(IEnumerable<acadPoint> clotPoints, Xbim.Ifc4x3.GeometryResource.IfcAxis2Placement position)
		{
			// GetPositionInfo(clothoid.Position, out var p, out var refDir);
			var m = position.ToMatrix3D();
			foreach (var item in clotPoints)
			{
				var tsrf = m.Transform(new XbimPoint3D(item.X, item.Y, item.Z));
				yield return new acadPoint(tsrf.X, tsrf.Y, tsrf.Z);
			}
		}

		internal static IEnumerable<acadPoint> GetCoreClothoid(double constant, double dStart, double dLen, double stepSize)
		{
			var dEnd = dStart + dLen;
			if (dStart != 0 && dEnd != 0)
			{
				return Enumerable.Empty<acadPoint>();
			}

			var N = (int)Math.Ceiling(dLen / stepSize);
			var deltaS = dLen / N;
			// determining direction
			var lambda = (dStart != 0) ? -1 : 1;
			double zedValue = 0;
	
			List<acadPoint> ret = new List<acadPoint>(N);
			ret.Add(new acadPoint(0, 0, zedValue));
			double prevS = 0;
			var runX = 0.0;
			var runY = 0.0;
			for (int i = 0; i < N; i++)
			{
				var angle = lambda * (Math.Pow(prevS, 2)) / (2 * Math.Pow(constant, 2));
				double dx = deltaS * Math.Cos(angle);
				double dy = deltaS * Math.Sin(angle);
				runX += dx;
				runY += dy;
				prevS += deltaS;
				ret.Add(new acadPoint(runX, runY, zedValue));
			}
			if (constant < 0)
			{
				ret = ret.Select(x => new acadPoint(x.X, -x.Y, x.Z)).ToList();
			}
			if (lambda == -1)
			{
				ret = ret.Select(x => new acadPoint(-x.X, x.Y, x.Z)).ToList();
				ret.Reverse();
				var angle = lambda * (Math.Pow(prevS, 2)) / (2 * Math.Pow(constant, 2));
				var mt = XbimMatrix3D.CreateTranslation(-ret[0].X, -ret[0].Y, 0);

				var angDir = constant > 0
					? new XbimPoint3D(Math.Cos(angle), -Math.Sin(angle), 0)
					: new XbimPoint3D(Math.Cos(angle), Math.Sin(angle), 0);
				var rot = XbimMatrix3D.CreateRotation(
					angDir,
					new XbimPoint3D(1,0,0)
					);
				var t = mt * rot;
				ret = ret.Select(x => Trasform(x, t)).ToList();
			}
			
			return ret;
		}

		private static acadPoint Trasform(acadPoint pt, XbimMatrix3D mt)
		{
			var t = new XbimPoint3D(pt.X, pt.Y, pt.Z);
			var tmpP = mt.Transform(t);
			return new acadPoint(tmpP.X, tmpP.Y, tmpP.Z);
		}

		private static double GetDistance(acadPoint p1, acadPoint p2)
		{
			if (!double.IsNaN(p2.Z) && double.IsNaN(p1.Z))
				return Math.Sqrt(
					Math.Pow(p2.X - p1.X, 2) +
					Math.Pow(p2.Y - p1.Y, 2) +
					Math.Pow(p2.Z - p1.Z, 2)
					);
			return Math.Sqrt(
					Math.Pow(p2.X - p1.X, 2) +
					Math.Pow(p2.Y - p1.Y, 2)
					);
		}

		private static acadPoint PointOnCircle(Xbim.Ifc4x3.GeometryResource.IfcCircle circle, double start)
		{
			GetPositionInfo(circle.Position, out var p, out var refDir);
			double radius = circle.Radius.Value is double d
				? d
				: 0;
			var param = refDir + start;
			
			var D = new acadPoint(
				p.X + radius * Math.Sin(param),
				p.Y + radius - radius * Math.Cos(param),
				double.NaN
				);
			return D;
		}

		private static void GetPositionInfo(Xbim.Ifc4x3.GeometryResource.IfcAxis2Placement pos, out XbimPoint3D p, out double refDir)
		{
			p = pos switch
			{
				IIfcAxis2Placement2D p2dLoc => new XbimPoint3D(p2dLoc.Location.X, p2dLoc.Location.Y, double.NaN),
				_ => new XbimPoint3D()
			};
			if (pos is IIfcAxis2Placement2D p2d && p2d.RefDirection is IIfcDirection p2dRefDir)	
				refDir = Math.Atan2(p2dRefDir.Y, p2dRefDir.X);
			else
				refDir = 0;
		}

		private static double GetParam(IfcCurveMeasureSelect segmentStart)
		{
			if (segmentStart.Value is double param)
				return param;
			return 0;
		}

		private static acadPoint PointOnLine(Xbim.Ifc4x3.GeometryResource.IfcLine line, double param)
		{
			var p = line.Pnt.XbimPoint3D();
			XbimVector3D v = new XbimVector3D();
			if (line.Dir is IIfcVector vtr)
			{
				v = new XbimVector3D(vtr.Orientation.X, vtr.Orientation.Y, vtr.Orientation.Z);
				var t = p + (v * param);
				return new acadPoint()
				{
					X = t.X,
					Y = t.Y,
					Z = t.Z,
				};
			}
			return new acadPoint();
		}

        private static void Report(IIfcGeometricRepresentationItem obj, TextHighliter sb)
        {
			if (obj is Xbim.Ifc4x3.GeometryResource.IfcCurveSegment cs)
				Report(cs, sb);
			else if (obj is IIfcCurve crv)
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
				TagLastWithEntityLabel(sb, face);
			}
		}

		private static void TagLastWithEntityLabel(TextHighliter sb, IPersistEntity entity)
		{
			sb.Append($"-HYPERLINK I O l  #{entity.EntityLabel}", Brushes.Black);
                sb.Append($"", Brushes.Black);
                sb.Append($"", Brushes.Black);
        }

		internal static TextHighliter ReportAsObj(IIfcClosedShell ics)
		{
            var sb = new TextHighliter();
            ReportAsObj(ics, sb);
            return sb;
		}

		private static void ReportAsObj(IIfcClosedShell ics, TextHighliter sb)
		{
            List<int> vertexLabels = new List<int>(); // entitylabel of the vertex
            List<int> indices = new List<int>();
            foreach (var face in ics.CfsFaces)
            {
                ReportAsObj(face, sb, vertexLabels, indices);
                if (indices.Count %3 != 0)
                {
                    sb.Append($"Error in face #{face.EntityLabel}", Brushes.Red);
                }
            }
			foreach (var vert in vertexLabels)
			{
                var v = ics.Model.Instances[vert] as IIfcCartesianPoint;
                sb.Append($"v {v.X} {v.Y} {v.Z}", Brushes.Black);
			}
            for (int i = 0; i < indices.Count; i += 3)
            {
                if (i + 2 >= indices.Count)
                {
                    sb.Append($"Error in indices", Brushes.Red);
                    continue;
                }
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
