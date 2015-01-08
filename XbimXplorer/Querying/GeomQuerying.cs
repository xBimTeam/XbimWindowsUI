using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Common.Geometry;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using Xbim.Presentation;
using Xbim.XbimExtensions;
using XbimGeometry.Interfaces;

namespace XbimXplorer.Querying
{
    public static class GeomQuerying
    {
        public static string GeomInfoBoundBox(XbimModel model, int iEntLabel)
        {
            XbimGeometryData geomdata = model.GetGeometryData(iEntLabel, XbimGeometryType.BoundingBox).FirstOrDefault();
            if (geomdata == null)
                return "<not found>";
            
            XbimRect3D r3d = XbimRect3D.FromArray(geomdata.ShapeData);
            return string.Format("Bounding box (position, size): {0}", r3d.ToString());
        }

        public static string GeomInfoMesh(XbimModel model, int iEntLabel)
        {
            StringBuilder sb = new StringBuilder();

            var geomdata = model.GetGeometryData(iEntLabel, XbimGeometryType.TriangulatedMesh);
            foreach (var geom in geomdata)
            {
                DumpData(sb, geom.ShapeData); // XbimTriangulatedModelStream
                // sb.AppendLine(BitConverter.ToString(geom.ShapeData));
            }
            return sb.ToString();
        }

        private static void DumpData(StringBuilder sb, byte[] ShapeData)
        {
            XbimTriangulatedModelStream m = new XbimTriangulatedModelStream(ShapeData);

            TextMeshDumper md = new TextMeshDumper(sb);
            XbimMatrix3D id = XbimMatrix3D.Identity;
            m.BuildWithNormals(md, id);

            // sb.Append(m.ToString());
        }

        public static string Viewerdata(DrawingControl3D control, XbimModel model, int EntityLabel)
        {
            StringBuilder sb = new StringBuilder();
            control.ReportData(sb, model, EntityLabel);
            return sb.ToString();
        }



        public class TextMeshDumper : IXbimTriangulatesToPositionsNormalsIndices
        {
            StringBuilder _sb = new StringBuilder();
            int _PosPoint = 0;
            int _PosNormal = 0;

            public TextMeshDumper()
            {
            }
            public TextMeshDumper(StringBuilder sb)
            {
                _sb = sb;
            }

            public void BeginBuild()
            {
                _sb.AppendLine("Begin mesh information.");
            }

            public void BeginPoints(uint numPoints)
            {
                _sb.AppendFormat("Begin Points: {0}\r\n", numPoints.ToString());
            }

            public void AddPosition(XbimPoint3D point3D)
            {
                _sb.AppendFormat("Point {0}: {1}\r\n", _PosPoint++, point3D.ToString());
            }

            public void AddNormal(XbimVector3D normal)
            {
                _sb.AppendFormat("Normal {0}: {1}\r\n", _PosNormal++, normal.ToString());
            }

            public void EndPoints()
            {
                _sb.AppendFormat("End Points\r\n");
            }

            public void BeginPolygons(uint totalNumberTriangles, uint numPolygons)
            {
                _sb.AppendFormat("Begin Polygons: {0} ({1} triangles in total)\r\n", numPolygons.ToString(), totalNumberTriangles.ToString());
            }

            public void BeginPolygon(TriangleType meshType, uint indicesCount)
            {
                _sb.AppendFormat("BeginPolygon type: {0} size: {1}\r\n", meshType.ToString(), indicesCount.ToString());
            }

            public void AddTriangleIndex(uint index)
            {
                _sb.AppendFormat("Index {0}\r\n", index);
            }

            public void EndPolygon()
            {
                
            }

            public void EndPolygons()
            {
                
            }

            public void EndBuild()
            {
                _sb.AppendLine("End mesh information.");
            }

            public int PositionCount
            {
                get {
                    return 0;
                }
            }

            public int TriangleIndexCount
            {
                get {
                    return 0;
                }
            }
        }

        //public static string GeomInfoMesh(XbimModel model, int iEntLabel)
        //{
        //    StringBuilder sb = new StringBuilder();

        //    var geomdata = model.GetGeometryData(iEntLabel, XbimGeometryType.TriangulatedMesh);
        //    foreach (var geom in geomdata)
        //    {
        //        sb.Append("RawData:");
        //        sb.AppendLine(BitConverter.ToString(geom.ShapeData));
        //    }
        //    return sb.ToString();
        //}

        internal static string GeomLayers(XbimModel Model, int item, List<Xbim.ModelGeometry.Scene.XbimScene<Xbim.Presentation.WpfMeshGeometry3D, Xbim.Presentation.WpfMaterial>> scenes)
        {
            StringBuilder sb = new StringBuilder();
            // XbimMeshGeometry3D geometry = new XbimMeshGeometry3D();
            // IModel m = entity.ModelOf;
            foreach (var scene in scenes)
            {
                foreach (var layer in scene.SubLayers)
                {
                    // an entity model could be spread across many layers (e.g. in case of different materials)
                    if (layer.Model == Model)
                    {
                        foreach (var mi in layer.GetMeshInfo(item))
                        {
                            sb.AppendLine(mi.ToString());
                        }
                    }
                }    
            }
            return sb.ToString();            
        }
    }
}
