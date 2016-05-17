using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc;
using Xbim.Ifc2x3.IO;
using Xbim.ModelGeometry.Scene;
using Xbim.Presentation;

namespace XbimXplorer.Commands
{
    /// <summary>
    /// 
    /// </summary>
    public static class GeomQuerying
    {
        private static void DumpData(StringBuilder sb, byte[] shapeData)
        {
            var m = new XbimTriangulatedModelStream(shapeData);

            var md = new TextMeshDumper(sb);
            var id = XbimMatrix3D.Identity;
            m.BuildWithNormals(md, id);

            // sb.Append(m.ToString());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="control"></param>
        /// <param name="model"></param>
        /// <param name="entityLabel"></param>
        /// <returns></returns>
        public static string Viewerdata(DrawingControl3D control, IfcStore model, int entityLabel)
        {
            var sb = new StringBuilder();
            control.ReportData(sb, model, entityLabel);
            return sb.ToString();
        }
        
        /// <summary>
        /// 
        /// </summary>
        public class TextMeshDumper : IXbimTriangulatesToPositionsNormalsIndices
        {
            readonly StringBuilder _sb = new StringBuilder();
            int _posPoint = 0;
            int _posNormal = 0;

            /// <summary>
            /// 
            /// </summary>
            public TextMeshDumper()
            {
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="sb"></param>
            public TextMeshDumper(StringBuilder sb)
            {
                _sb = sb;
            }

            /// <summary>
            /// Called to initialise the build
            /// </summary>
            void IXbimTriangulatesToPositionsNormalsIndices.BeginBuild()
            {
                _sb.AppendLine("Begin mesh information.");
            }

            /// <summary>
            /// Called after BeginBuild
            /// </summary>
            /// <param name="numPoints">The number of unique vertices in the model</param>
            void IXbimTriangulatesToPositionsNormalsIndices.BeginPoints(uint numPoints)
            {
                _sb.AppendFormat("Begin Points: {0}\r\n", numPoints.ToString());
            }

            /// <summary>
            /// Called after BeginVertices, once for each unique vertex
            /// </summary>
            /// <param name="point3D"/>
            public void AddPosition(XbimPoint3D point3D)
            {
                _sb.AppendFormat("Point {0}: {1}\r\n", _posPoint++, point3D.ToString());
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="normal"></param>
            public void AddNormal(XbimVector3D normal)
            {
                _sb.AppendFormat("Normal {0}: {1}\r\n", _posNormal++, normal.ToString());
            }

            /// <summary>
            /// Called when all unique vertices have been added 
            /// </summary>
            public void EndPoints()
            {
                _sb.AppendFormat("End Points\r\n");
            }

            /// <summary>
            /// Called after EndNormal
            /// </summary>
            /// <param name="totalNumberTriangles"></param>
            /// <param name="numPolygons">Number of polygons which make the face</param>
            public void BeginPolygons(uint totalNumberTriangles, uint numPolygons)
            {
                _sb.AppendFormat("Begin Polygons: {0} ({1} triangles in total)\r\n", numPolygons.ToString(), totalNumberTriangles.ToString());
            }

            /// <summary>
            /// Called after BeginPolygon, once for each triangulated area that describes the polygon
            /// </summary>
            /// <param name="meshType">The type of triangulation, mesh, fan, triangles etc</param><param name="indicesCount"/>
            public void BeginPolygon(TriangleType meshType, uint indicesCount)
            {
                _sb.AppendFormat("BeginPolygon type: {0} size: {1}\r\n", meshType.ToString(), indicesCount.ToString());
            }

            /// <summary>
            /// Called after BeginTriangulation, once for each index, with respect to the triangulation type
            /// </summary>
            /// <param name="index">index into the list of unique vertices</param>
            public void AddTriangleIndex(uint index)
            {
                _sb.AppendFormat("Index {0}\r\n", index);
            }

            /// <summary>
            /// Triangulation complete
            /// </summary>
            public void EndPolygon()
            {
                
            }

            /// <summary>
            /// All polygon definitions complete
            /// </summary>
            public void EndPolygons()
            {
                
            }

            /// <summary>
            /// Model build complete
            /// </summary>
            public void EndBuild()
            {
                _sb.AppendLine("End mesh information.");
            }

            /// <summary>
            /// 
            /// </summary>
            public int PositionCount
            {
                get {
                    return 0;
                }
            }

            /// <summary>
            /// 
            /// </summary>
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

        internal static string GeomLayers(XbimModel model, int item, List<XbimScene<WpfMeshGeometry3D, WpfMaterial>> scenes)
        {
            StringBuilder sb = new StringBuilder();
            // XbimMeshGeometry3D geometry = new XbimMeshGeometry3D();
            // IModel m = entity.ModelOf;
            foreach (var scene in scenes)
            {
                foreach (var layer in scene.SubLayers)
                {
                    // an entity model could be spread across many layers (e.g. in case of different materials)
                    if (layer.Model == model)
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
