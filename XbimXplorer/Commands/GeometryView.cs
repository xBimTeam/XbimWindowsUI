using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Xbim.Ifc4.Interfaces;

namespace XbimXplorer.Commands
{
    /// <summary>
    /// used to show geometry information in other 3D environments
    /// </summary>
    internal static class GeometryView
    {
        internal static void ReportAcadScript(IIfcClosedShell shell, TextHighliter sb)
        {
            foreach (var face in shell.CfsFaces)
            {
                ReportAcadScript(face, sb);
            }
        }

        private static void ReportAcadScript(IIfcFace face, TextHighliter sb)
        {
            foreach (var ifcFaceBound in face.Bounds)
            {
                ReportAcadScript(ifcFaceBound, sb);
            }
        }

        private static void ReportAcadScript(IIfcFaceOuterBound ifcFaceBound, TextHighliter sb)
        {
            ReportAcadScript(ifcFaceBound.Bound, sb);
        }

        private static void ReportAcadScript(IIfcPolyLoop bound, TextHighliter sb)
        {
            sb.Append($"-LAYER M {bound.Polygon.Count} ", Brushes.Black);

            sb.Append("3DPOLY", Brushes.Black);
            var first = bound.Polygon.FirstOrDefault();
            IIfcCartesianPoint last = null;
            foreach (var ifcCartesianPoint in bound.Polygon)
            {
                sb.Append($"{ifcCartesianPoint.X},{ifcCartesianPoint.Y},{ifcCartesianPoint.Z}" , Brushes.Black);
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

        private static void ReportAcadScript(IIfcLoop bound, TextHighliter sb)
        {
            if (bound is IIfcPolyLoop)
            {
                ReportAcadScript((IIfcPolyLoop)bound, sb);
            }
            else
            {
                sb.Append($"{bound.GetType().Name} not implemented.", Brushes.Red);
            }
        }

        private static void ReportAcadScript(IIfcFaceBound ifcFaceBound, TextHighliter sb)
        {
            if (ifcFaceBound is IIfcFaceOuterBound)
            {
                ReportAcadScript((IIfcFaceOuterBound)ifcFaceBound, sb);
            }
            else
            {
                sb.Append($"{ifcFaceBound.GetType().Name} not implemented.", Brushes.Red);
            }
        }

        internal static TextHighliter ReportAcadScript(object obj)
        {
            var sb = new TextHighliter();
            if (obj is IIfcClosedShell)
                ReportAcadScript((IIfcClosedShell) obj, sb);
            if (obj is IIfcPolyLoop)
                ReportAcadScript((IIfcPolyLoop)obj, sb);
            else
            {
                sb.Append("No information", Brushes.Black);
                return sb;
            }
            sb.Append("3DORBIT", Brushes.Black);
            sb.Append("", Brushes.Black);
            sb.Append("===", Brushes.Black);
            return  sb;
        }
    }
}
