using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Xbim.Common.Geometry;

namespace Xbim.Presentation
{
    public static class MeshGeometry3DExtensions
    {

        public static void Read(this MeshGeometry3D m3D, string shapeData, XbimMatrix3D? transform = null)
        {
            
            RotateTransform3D qrd = new RotateTransform3D();
            Matrix3D? matrix3D = null;
            if (transform.HasValue)
            {
                XbimQuaternion xq = transform.Value.GetRotationQuaternion();
                qrd.Rotation = new QuaternionRotation3D(new Quaternion(xq.X, xq.Y, xq.Z, xq.W));
                matrix3D = transform.Value.ToMatrix3D();
            }
            
            using (StringReader sr = new StringReader(shapeData))
            {

                List<Point3D> vertexList = new List<Point3D>(512); //holds the actual unique positions of the vertices in this data set in the mesh
                List<Vector3D> normalList = new List<Vector3D>(512); //holds the actual unique normals of the vertices in this data set in the mesh

                List<Point3D> positions = new List<Point3D>(1024); //holds the actual positions of the vertices in this data set in the mesh
                List<Vector3D> normals = new List<Vector3D>(1024); //holds the actual normals of the vertices in this data set in the mesh
                List<int> triangleIndices = new List<int>(2048);
                String line;
                // Read and display lines from the data until the end of
                // the data is reached.

                while ((line = sr.ReadLine()) != null)
                {

                    string[] tokens = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens.Length > 0) //we need a command
                    {
                        string command = tokens[0].Trim().ToUpper();
                        switch (command)
                        {
                            case "P":
                                vertexList = new List<Point3D>(512);
                                normalList = new List<Vector3D>(512);
                                break;
                            case "V": //process vertices
                                for (int i = 1; i < tokens.Length; i++)
                                {
                                    string[] xyz = tokens[i].Split(',');
                                    Point3D p = new Point3D(Convert.ToDouble(xyz[0], CultureInfo.InvariantCulture),
                                                                      Convert.ToDouble(xyz[1], CultureInfo.InvariantCulture),
                                                                      Convert.ToDouble(xyz[2], CultureInfo.InvariantCulture));
                                    if (matrix3D.HasValue)
                                        p = matrix3D.Value.Transform(p);
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
                                Vector3D currentNormal = new Vector3D(0,0,0);
                                //each time we start a new mesh face we have to duplicate the vertices to ensure that we get correct shading of planar and non planar faces
                                Dictionary<int, int> writtenVertices = new Dictionary<int, int>();

                                for (int i = 1; i < tokens.Length; i++)
                                {
                                    string[] indices = tokens[i].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                    if (indices.Length != 3) throw new Exception("Invalid triangle definition");
                                    for (int t = 0; t < 3; t++)
                                    {
                                        string[] indexNormalPair = indices[t].Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

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
                                            if (matrix3D.HasValue)
                                            { 
                                                currentNormal = qrd.Transform(currentNormal);
                                            }
                                        }

                                        //now add the index
                                        int index = int.Parse(indexNormalPair[0]);

                                        int alreadyWrittenAt = index; //in case it is the first mesh
                                        if (!writtenVertices.TryGetValue(index, out alreadyWrittenAt)) //if we haven't  written it in this mesh pass, add it again unless it is the first one which we know has been written
                                        {
                                            //all vertices will be unique and have only one normal
                                            writtenVertices.Add(index, positions.Count);
                                            triangleIndices.Add(positions.Count + m3D.TriangleIndices.Count);
                                            positions.Add(vertexList[index]);
                                            normals.Add(currentNormal);
                                        }
                                        else //just add the index reference
                                        {
                                            triangleIndices.Add(alreadyWrittenAt);
                                        }
                                    }
                                }

                                break;
                            case "F": //skip faces for now, can be used to draw edges
                                break;
                            default:
                                throw new Exception("Invalid Geometry Command");

                        }
                    }
                   
                }
                
                m3D.Positions = new Point3DCollection(m3D.Positions.Concat(positions)); //we do this for wpf performance issues
                m3D.Normals = new Vector3DCollection(m3D.Normals.Concat(normals)); //we do this for wpf performance issues
                m3D.TriangleIndices = new Int32Collection(m3D.TriangleIndices.Concat(triangleIndices)); //we do this for wpf performance issues
            }
            
        }

        //public static void Read(this MeshGeometry3D m3D, byte[] shapeData, XbimMatrix3D? transform = null)
        //{

        //    var qrd = new RotateTransform3D();
        //    Matrix3D? matrix3D = null;
        //    if (transform.HasValue)
        //    {
        //        XbimQuaternion xq = transform.Value.GetRotationQuaternion();
        //        qrd.Rotation = new QuaternionRotation3D(new Quaternion(xq.X, xq.Y, xq.Z, xq.W));
        //        matrix3D = transform.Value.ToMatrix3D();
        //    }

        //    using (var br = new BinaryReader(new MemoryStream(shapeData) ))
        //    {

                

        //        List<Point3D> vertexList = new List<Point3D>(512); //holds the actual unique positions of the vertices in this data set in the mesh
        //        List<Vector3D> normalList = new List<Vector3D>(512); //holds the actual unique normals of the vertices in this data set in the mesh

        //        List<Point3D> positions = new List<Point3D>(1024); //holds the actual positions of the vertices in this data set in the mesh
        //        List<Vector3D> normals = new List<Vector3D>(1024); //holds the actual normals of the vertices in this data set in the mesh
        //        List<int> triangleIndices = new List<int>(2048);
        //        String line;
        //        // Read and display lines from the data until the end of
        //        // the data is reached.

        //        while ((line = sr.ReadLine()) != null)
        //        {

        //            string[] tokens = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        //            if (tokens.Length > 0) //we need a command
        //            {
        //                string command = tokens[0].Trim().ToUpper();
        //                switch (command)
        //                {
        //                    case "P":
        //                        vertexList = new List<Point3D>(512);
        //                        normalList = new List<Vector3D>(512);
        //                        break;
        //                    case "V": //process vertices
        //                        for (int i = 1; i < tokens.Length; i++)
        //                        {
        //                            string[] xyz = tokens[i].Split(',');
        //                            Point3D p = new Point3D(Convert.ToDouble(xyz[0], CultureInfo.InvariantCulture),
        //                                                              Convert.ToDouble(xyz[1], CultureInfo.InvariantCulture),
        //                                                              Convert.ToDouble(xyz[2], CultureInfo.InvariantCulture));
        //                            if (matrix3D.HasValue)
        //                                p = matrix3D.Value.Transform(p);
        //                            vertexList.Add(p);
        //                        }
        //                        break;
        //                    case "N": //processes normals
        //                        for (int i = 1; i < tokens.Length; i++)
        //                        {
        //                            string[] xyz = tokens[i].Split(',');
        //                            Vector3D v = new Vector3D(Convert.ToDouble(xyz[0], CultureInfo.InvariantCulture),
        //                                                               Convert.ToDouble(xyz[1], CultureInfo.InvariantCulture),
        //                                                               Convert.ToDouble(xyz[2], CultureInfo.InvariantCulture));
        //                            normalList.Add(v);
        //                        }
        //                        break;
        //                    case "T": //process triangulated meshes
        //                        Vector3D currentNormal = new Vector3D(0, 0, 0);
        //                        //each time we start a new mesh face we have to duplicate the vertices to ensure that we get correct shading of planar and non planar faces
        //                        Dictionary<int, int> writtenVertices = new Dictionary<int, int>();

        //                        for (int i = 1; i < tokens.Length; i++)
        //                        {
        //                            string[] indices = tokens[i].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        //                            if (indices.Length != 3) throw new Exception("Invalid triangle definition");
        //                            for (int t = 0; t < 3; t++)
        //                            {
        //                                string[] indexNormalPair = indices[t].Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

        //                                if (indexNormalPair.Length > 1) //we have a normal defined
        //                                {
        //                                    string normalStr = indexNormalPair[1].Trim();
        //                                    switch (normalStr)
        //                                    {
        //                                        case "F": //Front
        //                                            currentNormal = new Vector3D(0, -1, 0);
        //                                            break;
        //                                        case "B": //Back
        //                                            currentNormal = new Vector3D(0, 1, 0);
        //                                            break;
        //                                        case "L": //Left
        //                                            currentNormal = new Vector3D(-1, 0, 0);
        //                                            break;
        //                                        case "R": //Right
        //                                            currentNormal = new Vector3D(1, 0, 0);
        //                                            break;
        //                                        case "U": //Up
        //                                            currentNormal = new Vector3D(0, 0, 1);
        //                                            break;
        //                                        case "D": //Down
        //                                            currentNormal = new Vector3D(0, 0, -1);
        //                                            break;
        //                                        default: //it is an index number
        //                                            int normalIndex = int.Parse(indexNormalPair[1]);
        //                                            currentNormal = normalList[normalIndex];
        //                                            break;
        //                                    }
        //                                    if (matrix3D.HasValue)
        //                                    {
        //                                        currentNormal = qrd.Transform(currentNormal);
        //                                    }
        //                                }

        //                                //now add the index
        //                                int index = int.Parse(indexNormalPair[0]);

        //                                int alreadyWrittenAt = index; //in case it is the first mesh
        //                                if (!writtenVertices.TryGetValue(index, out alreadyWrittenAt)) //if we haven't  written it in this mesh pass, add it again unless it is the first one which we know has been written
        //                                {
        //                                    //all vertices will be unique and have only one normal
        //                                    writtenVertices.Add(index, positions.Count);
        //                                    triangleIndices.Add(positions.Count + m3D.TriangleIndices.Count);
        //                                    positions.Add(vertexList[index]);
        //                                    normals.Add(currentNormal);
        //                                }
        //                                else //just add the index reference
        //                                {
        //                                    triangleIndices.Add(alreadyWrittenAt);
        //                                }
        //                            }
        //                        }

        //                        break;
        //                    case "F": //skip faces for now, can be used to draw edges
        //                        break;
        //                    default:
        //                        throw new Exception("Invalid Geometry Command");

        //                }
        //            }

        //        }

        //        m3D.Positions = new Point3DCollection(m3D.Positions.Concat(positions)); //we do this for wpf performance issues
        //        m3D.Normals = new Vector3DCollection(m3D.Normals.Concat(normals)); //we do this for wpf performance issues
        //        m3D.TriangleIndices = new Int32Collection(m3D.TriangleIndices.Concat(triangleIndices)); //we do this for wpf performance issues
        //    }

        //}
    }
}
