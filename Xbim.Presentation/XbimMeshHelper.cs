// the code on this page has been cloned from the Helix toolkit to fix a bug in the MeshHelper class.
// The class will be removed (or wrap the helix toolkit one) if they accept changes to their codebase.  
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CuttingPlaneGroup.cs" company="Helix 3D Toolkit">
//   http://helixtoolkit.codeplex.com, license: MIT
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace Xbim.Presentation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Windows.Media;
    using System.Windows.Media.Media3D;
    using HelixToolkit.Wpf;

    class XbimMeshHelper
    {
        /// <summary>
        /// Cuts the mesh with the specified plane.
        /// </summary>
        /// <param name="mesh">
        /// The mesh.
        /// </param>
        /// <param name="p">
        /// The plane origin.
        /// </param>
        /// <param name="n">
        /// The plane normal.
        /// </param>
        /// <returns>
        /// The <see cref="MeshGeometry3D"/>.
        /// </returns>
        public static MeshGeometry3D Cut(MeshGeometry3D mesh, Point3D p, Vector3D n)
        {
            var ch = new ContourHelper(p, n);
            var mb = new MeshBuilder(false, false);
            foreach (var pos in mesh.Positions)
            {
                mb.Positions.Add(pos);
            }

            int j = mb.Positions.Count;
            for (int i = 0; i < mesh.TriangleIndices.Count; i += 3)
            {
                int i0 = mesh.TriangleIndices[i];
                int i1 = mesh.TriangleIndices[i + 1];
                int i2 = mesh.TriangleIndices[i + 2];
                var p0 = mesh.Positions[i0];
                var p1 = mesh.Positions[i1];
                var p2 = mesh.Positions[i2];
                Point3D s0, s1;
                int r = ch.ContourFacet(p0, p1, p2, out s0, out s1);
                switch (r)
                {
                    case -1:
                        mb.TriangleIndices.Add(i0);
                        mb.TriangleIndices.Add(i1);
                        mb.TriangleIndices.Add(i2);
                        break;
                    case 0:
                        mb.Positions.Add(s1);
                        mb.Positions.Add(s0);
                        mb.TriangleIndices.Add(i0);
                        mb.TriangleIndices.Add(j++);
                        mb.TriangleIndices.Add(j++);
                        break;
                    case 1:
                        mb.Positions.Add(s0);
                        mb.Positions.Add(s1);
                        mb.TriangleIndices.Add(i1);
                        mb.TriangleIndices.Add(j++);
                        mb.TriangleIndices.Add(j++);
                        break;
                    case 2:
                        mb.Positions.Add(s0);
                        mb.Positions.Add(s1);
                        mb.TriangleIndices.Add(i2);
                        mb.TriangleIndices.Add(j++);
                        mb.TriangleIndices.Add(j++);
                        break;
                    case 10:
                        mb.Positions.Add(s0);
                        mb.Positions.Add(s1);
                        mb.TriangleIndices.Add(i1);
                        mb.TriangleIndices.Add(i2);
                        mb.TriangleIndices.Add(j);
                        mb.TriangleIndices.Add(j++);
                        mb.TriangleIndices.Add(j++);
                        mb.TriangleIndices.Add(i1);
                        break;
                    case 11:
                        mb.Positions.Add(s1);
                        mb.Positions.Add(s0);
                        mb.TriangleIndices.Add(i2);
                        mb.TriangleIndices.Add(i0);
                        mb.TriangleIndices.Add(j);
                        mb.TriangleIndices.Add(j++);
                        mb.TriangleIndices.Add(j++);
                        mb.TriangleIndices.Add(i2);
                        break;
                    case 12:
                        mb.Positions.Add(s1);
                        mb.Positions.Add(s0);
                        mb.TriangleIndices.Add(i0);
                        mb.TriangleIndices.Add(i1);
                        mb.TriangleIndices.Add(j);
                        mb.TriangleIndices.Add(j++);
                        mb.TriangleIndices.Add(j++);
                        mb.TriangleIndices.Add(i0);
                        break;
                }
            }
            // begin bonghi: this is different from the original HelixToolkit version
            if (mb.TriangleIndices.Count == 0)
                return new MeshGeometry3D();
            // end bonghi
            return mb.ToMesh();
        }
    }
}
