using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Xbim.Common.Geometry;
using Xbim.Common.XbimExtensions;
using Xbim.Ifc4.Interfaces;
using Xbim.ModelGeometry.Scene;

namespace Xbim.Presentation
{
    public class WpfMeshGeometry3D : IXbimMeshGeometry3D
    {
        public GeometryModel3D WpfModel;
        private List<Point3D> _unfrozenPositions;
        private List<Int32> _unfrozenIndices;
        private List<Vector3D> _unfrozenNormals;
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
            WpfModel = new GeometryModel3D();
            WpfModel.Geometry = new MeshGeometry3D();
            Mesh.Positions = new WpfPoint3DCollection(0);
            Mesh.Normals = new WpfVector3DCollection();
            Mesh.TriangleIndices = new Int32Collection();
            _meshes = new XbimMeshFragmentCollection();
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
                if (WpfModel == null)
                    WpfModel = new GeometryModel3D();
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
                if (WpfModel == null || WpfModel.Geometry == null)
                {
                    _unfrozenPositions = new List<Point3D>(value.Count());
                    foreach (var xbimPoint3D in value)
                    {
                        _unfrozenPositions.Add(new Point3D(xbimPoint3D.X, xbimPoint3D.Y, xbimPoint3D.Z));
                    }
                }
                else
                    Mesh.Positions = new WpfPoint3DCollection(value);
            }
        }

        public IEnumerable<XbimVector3D> Normals
        {
            get { return new WpfVector3DCollection(Mesh.Normals); }
            set
            {
                if (WpfModel == null || WpfModel.Geometry == null)
                {
                    _unfrozenNormals = new List<Vector3D>(value.Count());
                    foreach (var xbimV3D in value)
                    {
                        _unfrozenNormals.Add(new Vector3D(xbimV3D.X, xbimV3D.Y, xbimV3D.Z));
                    }
                }
                else
                    Mesh.Normals = new WpfVector3DCollection(value);
            }
        }

        public IList<int> TriangleIndices
        {
            get { return Mesh.TriangleIndices; }
            set
            {
                if (WpfModel == null || WpfModel.Geometry == null)
                {
                    _unfrozenIndices = value.ToList();
                }
                else
                    Mesh.TriangleIndices = new Int32Collection(value);
            }
        }

        public void MoveTo(IXbimMeshGeometry3D toMesh)
        {
            if (_meshes.Any()) //if no meshes nothing to move
            {
                toMesh.Positions = new List<XbimPoint3D>(Positions); 
                toMesh.Normals = new List<XbimVector3D>(Normals); 
                toMesh.TriangleIndices = new List<int>(TriangleIndices);

                toMesh.Meshes = new XbimMeshFragmentCollection(Meshes); 
                
                _meshes.Clear();
                WpfModel.Geometry = new MeshGeometry3D();
               
            }
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
            get
            {
                if (Mesh == null && _unfrozenPositions == null)
                {
                    return 0;
                }
                return Mesh != null
                    ? Mesh.Positions.Count
                    : _unfrozenPositions.Count;
            }
        }

        public int TriangleIndexCount
        {
            get
            {
                if (Mesh == null && _unfrozenPositions == null)
                {
                    return 0;
                }
                return Mesh == null ? _unfrozenIndices.Count : Mesh.TriangleIndices.Count;
            }
        }

        XbimMeshFragment IXbimMeshGeometry3D.Add(IXbimGeometryModel geometryModel, IIfcProduct product, XbimMatrix3D transform, double? deflection, short modelId)
        {
            return geometryModel.MeshTo(this, product, transform, deflection ?? product.Model.ModelFactors.DeflectionTolerance, modelId);
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

        public void Add(string mesh, short productTypeId, int productLabel, int geometryLabel, XbimMatrix3D? transform, short modelId)
        {
            XbimMeshFragment frag = new XbimMeshFragment(PositionCount, TriangleIndexCount, productTypeId, productLabel, geometryLabel, modelId);
            Read(mesh, transform);
            frag.EndPosition = PositionCount - 1;
            frag.EndTriangleIndex = TriangleIndexCount - 1;
            _meshes.Add(frag);
        }

        public XbimMeshFragment Add(IXbimGeometryModel geometryModel, IIfcProduct product, XbimMatrix3D transform, double? deflection, short modelId = 0)
        {
            throw new NotImplementedException();
        }



        public bool Read(string data, XbimMatrix3D? tr = null)
        {
            int version = 1;
            using (var sr = new StringReader(data))
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
                var vertexList = new Point3DCollection(); //holds the actual positions of the vertices in this data set in the mesh
                var normalList = new Vector3DCollection(); //holds the actual normals of the vertices in this data set in the mesh
                string line;
                // Read and display lines from the data until the end of
                // the data is reached.
                while ((line = sr.ReadLine()) != null)
                {
                    var tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens.Length <= 1) 
                        continue;
                    var command = tokens[0].Trim().ToUpper();
                    switch (command)
                    {
                        case "P":
                            var pointCount = 512;
// ReSharper disable once NotAccessedVariable
                            var faceCount = 128;
// ReSharper disable once NotAccessedVariable
                            var triangleCount = 256;
                            var normalCount = 512;
                            if (tokens.Length > 0)
                                version = Int32.Parse(tokens[1]);
                            if (tokens.Length > 1)
                                pointCount = Int32.Parse(tokens[2]);
                            // ReSharper disable once RedundantAssignment
                            if (tokens.Length > 2) 
                                faceCount = Int32.Parse(tokens[3]);
// ReSharper disable once RedundantAssignment
                            if (tokens.Length > 3) 
                                triangleCount = Int32.Parse(tokens[4]);
                            if (tokens.Length > 4) 
                                normalCount = Math.Max(Int32.Parse(tokens[5]),pointCount); //can't really have less normals than points
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
                            var currentNormal = new Vector3D();
                            //each time we start a new mesh face we have to duplicate the vertices to ensure that we get correct shading of planar and non planar faces
                            var writtenVertices = new Dictionary<int, int>();

                            for (var i = 1; i < tokens.Length; i++)
                            {
                                var triangleIndices = tokens[i].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                if (triangleIndices.Length != 3) throw new Exception("Invalid triangle definition");
                                for (var t = 0; t < 3; t++)
                                {
                                    var indexNormalPair = triangleIndices[t].Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

                                    if (indexNormalPair.Length > 1) //we have a normal defined
                                    {
                                        if (version == 1)
                                        {
                                            var normalStr = indexNormalPair[1].Trim();
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
                                        }
                                        else //we have support for packed normals
                                        {
                                            var packedNormal = new XbimPackedNormal(ushort.Parse(indexNormalPair[1]));
                                            var n = packedNormal.Normal;
                                            currentNormal = new Vector3D(n.X, n.Y, n.Z);
                                        }
                                        if (tr.HasValue)
                                        {
                                            currentNormal = r.Transform(currentNormal);
                                        }
                                    }
                                    //now add the index
                                    var index = int.Parse(indexNormalPair[0]);

                                    int alreadyWrittenAt; //in case it is the first mesh
                                    if (!writtenVertices.TryGetValue(index, out alreadyWrittenAt)) //if we haven't  written it in this mesh pass, add it again unless it is the first one which we know has been written
                                    {
                                        //all vertices will be unique and have only one normal
                                        writtenVertices.Add(index, PositionCount);

                                        _unfrozenIndices.Add(PositionCount);
                                        _unfrozenPositions.Add(vertexList[index]);
                                        _unfrozenNormals.Add(currentNormal);

                                    }
                                    else //just add the index reference
                                    {
                                        if (_unfrozenNormals[alreadyWrittenAt] == currentNormal)
                                            _unfrozenIndices.Add(alreadyWrittenAt);
                                        else //we need another
                                        {
                                            _unfrozenIndices.Add(PositionCount);
                                            _unfrozenPositions.Add(vertexList[index]);
                                            _unfrozenNormals.Add(currentNormal);
                                        }
                                    }
                                   
                                }
                            }

                            break;
                        default:
                            throw new Exception("Invalid Geometry Command");
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
            int indexBase = _unfrozenPositions.Count;
            var qrd = new RotateTransform3D();
            Matrix3D? matrix3D = null;
            if (transform.HasValue)
            {
                var xq = transform.Value.GetRotationQuaternion();
                var quaternion = new Quaternion(xq.X, xq.Y, xq.Z, xq.W);
                if (!quaternion.IsIdentity)
                    qrd.Rotation = new QuaternionRotation3D(quaternion);
                else
                    qrd = null;
                matrix3D = transform.Value.ToMatrix3D();
            }
            using (var ms = new MemoryStream(mesh))
            {
                using (var br = new BinaryReader(ms))
                {
                    // ReSharper disable once UnusedVariable
                    var version = br.ReadByte(); //stream format version
                    var numVertices = br.ReadInt32();
                    var numTriangles = br.ReadInt32();

                    var uniqueVertices = new List<Point3D>(numVertices);
                    var vertices = new List<Point3D>(numVertices * 4); //approx the size
                    var triangleIndices = new List<Int32>(numTriangles * 3);
                    var normals = new List<Vector3D>(numVertices * 4);
                    for (var i = 0; i < numVertices; i++)
                    {
                        double x = br.ReadSingle();
                        double y = br.ReadSingle();
                        double z = br.ReadSingle();
                        var p = new Point3D(x, y, z);
                        if (matrix3D.HasValue)
                            p = matrix3D.Value.Transform(p);
                        uniqueVertices.Add(p);
                    }
                    var numFaces = br.ReadInt32();

                    for (var i = 0; i < numFaces; i++)
                    {
                        var numTrianglesInFace = br.ReadInt32();
                        if (numTrianglesInFace == 0) continue;
                        var isPlanar = numTrianglesInFace > 0;
                        numTrianglesInFace = Math.Abs(numTrianglesInFace);
                        if (isPlanar)
                        {
                            var normal = br.ReadPackedNormal().Normal;
                            var wpfNormal = new Vector3D(normal.X, normal.Y, normal.Z);
                            if (qrd != null) //transform the normal if we have to
                                wpfNormal = qrd.Transform(wpfNormal);
                            var uniqueIndices = new Dictionary<int, int>();
                            for (var j = 0; j < numTrianglesInFace; j++)
                            {
                                for (int k = 0; k < 3; k++)
                                {
                                    int idx = ReadIndex(br, numVertices);
                                    int writtenIdx;
                                    if (!uniqueIndices.TryGetValue(idx, out writtenIdx)) //we haven't got it, so add it
                                    {
                                        writtenIdx = vertices.Count;
                                        vertices.Add(uniqueVertices[idx]);
                                        uniqueIndices.Add(idx, writtenIdx);
                                        //add a matching normal
                                        normals.Add(wpfNormal);
                                    }
                                    triangleIndices.Add(indexBase + writtenIdx);
                                }
                            }
                        }
                        else
                        {     
                            for (var j = 0; j < numTrianglesInFace; j++)
                            {
                                for (var k = 0; k < 3; k++)
                                {
                                    var idx = ReadIndex(br, numVertices);
                                    var normal = br.ReadPackedNormal().Normal;

                                    triangleIndices.Add(indexBase + vertices.Count);
                                    vertices.Add(uniqueVertices[idx]);

                                    var wpfNormal = new Vector3D(normal.X, normal.Y, normal.Z);
                                    if (qrd != null) //transform the normal if we have to
                                        wpfNormal = qrd.Transform(wpfNormal);
                                    normals.Add(wpfNormal);

                                }
                            }
                        }
                       
                    }

                    _unfrozenPositions.AddRange(vertices);
                    _unfrozenIndices.AddRange(triangleIndices);
                    _unfrozenNormals.AddRange(normals);
                }
                // if(m3D.CanFreeze) m3D.Freeze(); //freeze the mesh to improve performance
            }
        }
        private static int ReadIndex(BinaryReader br, int maxVertexCount)
        {
            if (maxVertexCount <= 0xFF)
                return br.ReadByte();
            if (maxVertexCount <= 0xFFFF)
                return br.ReadUInt16();
            return (int)br.ReadUInt32(); //this should never go over int32
        }
        /// <summary>
        /// Ends an update and freezes the geometry
        /// </summary>
        public void EndUpdate()
        {
            WpfModel.Geometry = new MeshGeometry3D();
            Mesh.Positions = new Point3DCollection(_unfrozenPositions);
            _unfrozenPositions = null;
            Mesh.TriangleIndices = new Int32Collection(_unfrozenIndices);
            _unfrozenIndices = null;
            Mesh.Normals = new Vector3DCollection(_unfrozenNormals);
            _unfrozenNormals = null;
            Mesh.Freeze();
        }
        public void BeginUpdate()
        {
            _unfrozenPositions = new List<Point3D>(Mesh.Positions);
            _unfrozenIndices = new List<int>(Mesh.TriangleIndices);
            _unfrozenNormals = new List<Vector3D>(Mesh.Normals);
            WpfModel.Geometry = null;
        }





        public void Add(string mesh, Type productType, int productLabel, int geometryLabel, XbimMatrix3D? transform = null, short modelId = 0)
        {
            throw new NotImplementedException();
        }
    }
}
