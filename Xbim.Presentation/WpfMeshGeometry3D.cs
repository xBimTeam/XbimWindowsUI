using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Xbim.Common.Geometry;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using Xbim.XbimExtensions;

namespace Xbim.Presentation
{
    public class WpfMeshGeometry3D : IXbimMeshGeometry3D
    {
        public GeometryModel3D WpfModel;
        XbimMeshFragmentCollection _meshes = new XbimMeshFragmentCollection();
        private TriangleType _meshType;

        uint _previousToLastIndex;
        uint _lastIndex;
        uint _pointTally;
        uint _fanStartIndex;
        uint _indexOffset;
     
#region standard calls

        private void Init()
        {
            _indexOffset = (uint)Mesh.Positions.Count;
        }

        private void StandardBeginPolygon(TriangleType meshType)
        {
            _meshType = meshType;
            _pointTally = 0;
            _previousToLastIndex = 0;
            _lastIndex = 0;
            _fanStartIndex = 0;
        }
 #endregion

        public void ReportGeometryTo(StringBuilder sb)
        {
            int i = 0;
            var pEn = Positions.GetEnumerator();
            var nEn = Normals.GetEnumerator();
            while (pEn.MoveNext() && nEn.MoveNext())
            {
                var p = pEn.Current;
                var n = nEn.Current;
                sb.AppendFormat("{0} pos: {1} nrm:{2}\r\n", i++, p, n);
            }

            i = 0;
            sb.AppendLine("Triangles:");
            foreach (var item in TriangleIndices)
            {
                sb.AppendFormat("{0}, ", item);
                i++;
                if (i % 3 == 0)
                {
                    sb.AppendLine();
                }
            }
        }

        public WpfMeshGeometry3D()
        {
          
        }

        public WpfMeshGeometry3D(IXbimMeshGeometry3D mesh)
        {
            WpfModel = new GeometryModel3D();
            WpfModel.Geometry = new MeshGeometry3D();
            Mesh.Positions = new WpfPoint3DCollection(mesh.Positions);
            Mesh.Normals = new WpfVector3DCollection(mesh.Normals);
            Mesh.TriangleIndices = new Int32Collection (mesh.TriangleIndices);
            _meshes = new XbimMeshFragmentCollection(mesh.Meshes);
        }

        public WpfMeshGeometry3D(WpfMaterial material, WpfMaterial backMaterial = null)
        {
            WpfModel = new GeometryModel3D(new MeshGeometry3D(),material);
           
            if (backMaterial != null) WpfModel.BackMaterial = backMaterial;
        }
        
        public static implicit operator GeometryModel3D(WpfMeshGeometry3D mesh)
        {
             if(mesh.WpfModel==null)
                mesh.WpfModel=new GeometryModel3D();
             return mesh.WpfModel;
        }

        public MeshGeometry3D Mesh
        {
            get
            { 
                return WpfModel.Geometry as MeshGeometry3D;
            }
        }
        //public IEnumerable<XbimPoint3D> Positions
        //{
        //    get { return Mesh.Positions; }
        //}

        //public IList<XbimVector3D> Normals
        //{
        //    get { return Mesh.Normals; }
        //}

        //public IList<int> TriangleIndices
        //{
        //    get { return Mesh.TriangleIndices; }
        //}



        public XbimMeshFragmentCollection Meshes
        {
            get { return _meshes; }
            set
            {
                _meshes = new XbimMeshFragmentCollection(value);
            }
        }

        /// <summary>
        /// Do not use this rather create a XbimMeshGeometry3D first and construct this from it, appending WPF collections is slow
        /// </summary>
        /// <param name="geometryMeshData"></param>
        /// <param name="modelId"></param>
        public bool Add(XbimGeometryData geometryMeshData, short modelId)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<XbimPoint3D> Positions
        {
            get { return new WpfPoint3DCollection(Mesh.Positions); }
            set
            {
                Mesh.Positions = new WpfPoint3DCollection(value);
            }
        }

        public IEnumerable<XbimVector3D> Normals
        {
            get { return new WpfVector3DCollection(Mesh.Normals); }
            set
            {
                Mesh.Normals = new WpfVector3DCollection(value);
            }
        }

        public IList<int> TriangleIndices
        {
            get { return Mesh.TriangleIndices; }
            set
            {
                Mesh.TriangleIndices = new Int32Collection(value);
            }
        }

        public void MoveTo(IXbimMeshGeometry3D toMesh)
        {
            if (_meshes.Any()) //if no meshes nothing to move
            {
                toMesh.BeginUpdate();
                
                toMesh.Positions = new List<XbimPoint3D>(Positions); 
                toMesh.Normals = new List<XbimVector3D>(Normals); 
                toMesh.TriangleIndices = new List<int>(TriangleIndices);

                toMesh.Meshes = new XbimMeshFragmentCollection(Meshes); _meshes.Clear();
                WpfModel.Geometry = new MeshGeometry3D();
                toMesh.EndUpdate();
            }
        }

        public void BeginUpdate()
        {
            if (WpfModel == null)
                WpfModel = new GeometryModel3D();
            WpfModel.Geometry = new MeshGeometry3D();
        }

        public void EndUpdate()
        {
            WpfModel.Geometry.Freeze();
        }

        public GeometryModel3D ToGeometryModel3D()
        {
            return WpfModel;
        }

        public MeshGeometry3D GetWpfMeshGeometry3D(XbimMeshFragment frag)
        {
            var m3D = new MeshGeometry3D();
            var m = Mesh;
            if (m != null)
            {
                for (int i = frag.StartPosition; i <= frag.EndPosition; i++)
                {   
                    Point3D p = m.Positions[i];
                    m3D.Positions.Add(p);
                    if (m.Normals != null)
                    {
                        Vector3D v = m.Normals[i];
                        m3D.Normals.Add(v);
                    }
                }
                for (int i = frag.StartTriangleIndex; i <= frag.EndTriangleIndex; i++)
                {
                    m3D.TriangleIndices.Add(m.TriangleIndices[i] - frag.StartPosition);
                }
            }
            return m3D;
        }

        public IXbimMeshGeometry3D GetMeshGeometry3D(XbimMeshFragment frag)
        { 
            var m3D = new XbimMeshGeometry3D();
            var m = Mesh;
            if (m != null)
            {
                for (int i = frag.StartPosition; i <= frag.EndPosition; i++)
                {
                    Point3D p = m.Positions[i];
                    Vector3D v = m.Normals[i];
                    m3D.Positions.Add(new XbimPoint3D(p.X, p.Y, p.Z));
                    m3D.Normals.Add(new XbimVector3D(v.X, v.Y, v.Z));
                }
                for (int i = frag.StartTriangleIndex; i <= frag.EndTriangleIndex; i++)
                {
                    m3D.TriangleIndices.Add(m.TriangleIndices[i] - frag.StartPosition);
                }
                m3D.Meshes.Add(new XbimMeshFragment(0, 0,0)
                {
                    EndPosition = m3D.PositionCount - 1,
                    StartTriangleIndex = frag.StartTriangleIndex - m3D.PositionCount - 1,
                    EndTriangleIndex = frag.EndTriangleIndex - m3D.PositionCount - 1
                });
            }
            return m3D;
        }

        public XbimRect3D GetBounds()
        {
            bool first = true;
            XbimRect3D boundingBox = XbimRect3D.Empty;
            foreach (var pos in Positions)
            {   
                if (first)
                {
                    boundingBox = new XbimRect3D(pos);
                    first = false;
                }
                else
                    boundingBox.Union(pos);

            }
            return boundingBox;
        }



        public int PositionCount
        {
            get { return Mesh.Positions.Count; }
        }

        public int TriangleIndexCount
        {
            get { return Mesh.TriangleIndices.Count; }
        }

        public XbimMeshFragment Add(IXbimGeometryModel geometryModel, Ifc2x3.Kernel.IfcProduct product, XbimMatrix3D transform, double? deflection = null, short modelId=0)
        {
            return geometryModel.MeshTo(this, product, transform, deflection ?? product.ModelOf.ModelFactors.DeflectionTolerance, modelId);
        }

        public void BeginBuild()
        {
            Init();
        }

        public void BeginPositions(uint numPoints)
        {
            Mesh.Positions = new WpfPoint3DCollection((int) numPoints);
        }

        public void AddPosition(XbimPoint3D pt)
        {
            Mesh.Positions.Add(new Point3D(pt.X, pt.Y, pt.Z));
        }

        public void EndPositions()
        {
           
        }

        public void BeginPolygons(uint totalNumberTriangles, uint numPolygons)
        {
           
        }

        public void BeginPolygon(TriangleType meshType, uint indicesCount)
        {
            StandardBeginPolygon(meshType);
        }

        private int Offset(uint index)
        {
            return (int)(index + _indexOffset);
        }

        public void AddTriangleIndex(uint index)
        {
            if (_pointTally == 0)
                _fanStartIndex = index;
            if (_pointTally < 3) //first time
            {
                TriangleIndices.Add(Offset(index));
                // _meshGeometry.Positions.Add(_points[(int)index]);
            }
            else
            {
                switch (_meshType)
                {
                    case TriangleType.GL_Triangles://      0x0004
                        TriangleIndices.Add(Offset(index));
                        break;
                    case TriangleType.GL_Triangles_Strip:// 0x0005
                        if (_pointTally % 2 == 0)
                        {
                            TriangleIndices.Add(Offset(_previousToLastIndex));
                            TriangleIndices.Add(Offset(_lastIndex));
                        }
                        else
                        {
                            TriangleIndices.Add(Offset(_lastIndex));
                            TriangleIndices.Add(Offset(_previousToLastIndex));
                        }
                        TriangleIndices.Add(Offset(index));
                        break;
                    case TriangleType.GL_Triangles_Fan://   0x0006
                        TriangleIndices.Add(Offset(_fanStartIndex));
                        TriangleIndices.Add(Offset(_lastIndex));
                        TriangleIndices.Add(Offset(index));
                        break;
                }
            }
            _previousToLastIndex = _lastIndex;
            _lastIndex = index;
            _pointTally++;
        }

        public void EndPolygon()
        {
        }

        public void EndPolygons()
        {
            
        }

        public void EndBuild()
        {
            
        }


        public void BeginPoints(uint numPoints)
        {
            
        }

        public void AddNormal(XbimVector3D normal)
        {
           // throw new NotImplementedException();
        }

        public void EndPoints()
        {
            
        }

        public void Add(string mesh, short productTypeId, int productLabel, int geometryLabel, XbimMatrix3D? transform = null, short modelId = 0)
        {
            XbimMeshFragment frag = new XbimMeshFragment(PositionCount, TriangleIndexCount, productTypeId, productLabel, geometryLabel, modelId);
            Read(mesh, transform);
            frag.EndPosition = PositionCount - 1;
            frag.EndTriangleIndex = TriangleIndexCount - 1;
            _meshes.Add(frag);
        }

        public void Add(string mesh, Type productType, int productLabel, int geometryLabel, XbimMatrix3D? transform = null,short modelId=0)
        {
            Add(mesh, IfcMetaData.IfcTypeId(productType), productLabel, geometryLabel, transform, modelId);
        }

        public void Add(byte[] mesh, Type productType, int productLabel, int geometryLabel, XbimMatrix3D? transform = null, short modelId = 0)
        {
            Add(mesh, IfcMetaData.IfcTypeId(productType), productLabel, geometryLabel, transform, modelId);
        }
        public bool Read(String data, XbimMatrix3D? tr = null)
        {
           
            
            using (StringReader sr = new StringReader(data))
            {
                Matrix3D? m3D = null;
                var r = new RotateTransform3D();
                if (tr.HasValue) //set up the windows media transforms
                {
                    m3D = new Matrix3D(tr.Value.M11, tr.Value.M12, tr.Value.M13, tr.Value.M14,
                                                  tr.Value.M21, tr.Value.M22, tr.Value.M23, tr.Value.M24,
                                                  tr.Value.M31, tr.Value.M32, tr.Value.M33, tr.Value.M34,
                                                  tr.Value.OffsetX, tr.Value.OffsetY, tr.Value.OffsetZ, tr.Value.M44);
                    r = tr.Value.GetRotateTransform3D();
                }
                Point3DCollection vertexList = new Point3DCollection(); //holds the actual positions of the vertices in this data set in the mesh
                Vector3DCollection normalList = new Vector3DCollection(); //holds the actual normals of the vertices in this data set in the mesh
                String line;
                // Read and display lines from the data until the end of
                // the data is reached.
                while ((line = sr.ReadLine()) != null)
                {
                    string[] tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens.Length > 1) //we need a command and some data
                    {
                        string command = tokens[0].Trim().ToUpper();
                        switch (command)
                        {
                            case "P":
                                int pointCount = 512;
// ReSharper disable once NotAccessedVariable
                                int faceCount = 128;
// ReSharper disable once NotAccessedVariable
                                int triangleCount = 256;
                                int normalCount = 512;
                                if (tokens.Length > 1) pointCount = Int32.Parse(tokens[2]);
// ReSharper disable once RedundantAssignment
                                if (tokens.Length > 2) faceCount = Int32.Parse(tokens[3]);
// ReSharper disable once RedundantAssignment
                                if (tokens.Length > 3) triangleCount = Int32.Parse(tokens[4]);
                                if (tokens.Length > 4) normalCount = Math.Max(Int32.Parse(tokens[5]),pointCount); //can't really have less normals than points
                                vertexList = new Point3DCollection(pointCount);
                                normalList = new Vector3DCollection(normalCount);
                                //for efficienciency avoid continual regrowing
                                //this.Mesh.Positions = this.Mesh.Positions.GrowBy(pointCount);
                                //this.Mesh.Normals = this.Mesh.Normals.GrowBy(normalCount);
                                //this.Mesh.TriangleIndices = this.Mesh.TriangleIndices.GrowBy(triangleCount*3);
                                break;
                            case "F":
                                break;
                            case "V": //process vertices
                                for (int i = 1; i < tokens.Length; i++)
                                {
                                    string[] xyz = tokens[i].Split(',');
                                    Point3D p = new Point3D(Convert.ToDouble(xyz[0], CultureInfo.InvariantCulture),
                                                                      Convert.ToDouble(xyz[1], CultureInfo.InvariantCulture),
                                                                      Convert.ToDouble(xyz[2], CultureInfo.InvariantCulture));
                                    if (m3D.HasValue)
                                        p = m3D.Value.Transform(p);
                                    vertexList.Add(p);
                                }
                                break;
                            case "N": //processes normals
                                for (int i = 1; i < tokens.Length; i++)
                                {
                                    string[] xyz = tokens[i].Split(',');
                                    Vector3D v = new Vector3D(Convert.ToDouble(xyz[0], CultureInfo.InvariantCulture),
                                                                       Convert.ToDouble(xyz[1], CultureInfo.InvariantCulture),
                                                                       Convert.ToDouble(xyz[2], CultureInfo.InvariantCulture));
                                    normalList.Add(v);
                                }
                                break;
                            case "T": //process triangulated meshes
                                Vector3D currentNormal = new Vector3D();
                                //each time we start a new mesh face we have to duplicate the vertices to ensure that we get correct shading of planar and non planar faces
                                Dictionary<int, int> writtenVertices = new Dictionary<int, int>();

                                for (int i = 1; i < tokens.Length; i++)
                                {
                                    string[] triangleIndices = tokens[i].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                    if (triangleIndices.Length != 3) throw new Exception("Invalid triangle definition");
                                    for (int t = 0; t < 3; t++)
                                    {
                                        string[] indexNormalPair = triangleIndices[t].Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

                                        if (indexNormalPair.Length > 1) //we have a normal defined
                                        {
                                            string normalStr = indexNormalPair[1].Trim();
                                            switch (normalStr)
                                            {
                                                case "F": //Front
                                                    currentNormal = new Vector3D(0, -1, 0);
                                                    break;
                                                case "B": //Back
                                                    currentNormal = new Vector3D(0, 1, 0);
                                                    break;
                                                case "L": //Left
                                                    currentNormal = new Vector3D(-1, 0, 0);
                                                    break;
                                                case "R": //Right
                                                    currentNormal = new Vector3D(1, 0, 0);
                                                    break;
                                                case "U": //Up
                                                    currentNormal = new Vector3D(0, 0, 1);
                                                    break;
                                                case "D": //Down
                                                    currentNormal = new Vector3D(0, 0, -1);
                                                    break;
                                                default: //it is an index number
                                                    int normalIndex = int.Parse(indexNormalPair[1]);
                                                    currentNormal = normalList[normalIndex];
                                                    break;
                                            }
                                            if (tr.HasValue)
                                            {
                                                currentNormal = r.Transform(currentNormal);
                                            }
                                        }
                                        //now add the index
                                        int index = int.Parse(indexNormalPair[0]);

                                        int alreadyWrittenAt; //in case it is the first mesh
                                        if (!writtenVertices.TryGetValue(index, out alreadyWrittenAt)) //if we haven't  written it in this mesh pass, add it again unless it is the first one which we know has been written
                                        {
                                            //all vertices will be unique and have only one normal
                                            writtenVertices.Add(index, PositionCount);
                                            Mesh.TriangleIndices.Add(PositionCount);
                                            Mesh.Positions.Add(vertexList[index]);
                                            Mesh.Normals.Add(currentNormal);
                                        }
                                        else //just add the index reference
                                        {
                                            Mesh.TriangleIndices.Add(alreadyWrittenAt);
                                        }
                                    }
                                }

                                break;
                            default:
                                throw new Exception("Invalid Geometry Command");

                        }
                    }
                }
            }
            return true;
        }

        public void Add(byte[] mesh, short productTypeId, int productLabel, int geometryLabel, XbimMatrix3D? transform = null, short modelId = 0)
        {
            var frag = new XbimMeshFragment(PositionCount, TriangleIndexCount, productTypeId, productLabel, geometryLabel, modelId);
            Read(mesh, transform);
            frag.EndPosition = PositionCount - 1;
            frag.EndTriangleIndex = TriangleIndexCount - 1;
            _meshes.Add(frag);
        }

     
        
        /// <summary>
        /// Reads a triangulated mesh from a byte array 
        /// </summary>
        /// <param name="mesh">the binary data of the mesh</param>
        /// <param name="transform">transforms the mesh if the matrix is not null</param>
        public void Read(byte[] mesh, XbimMatrix3D? transform = null)
        {
            Mesh.Read(mesh,transform);
        }

    }
}
