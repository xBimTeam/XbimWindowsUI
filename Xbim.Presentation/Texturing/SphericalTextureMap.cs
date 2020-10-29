using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;
using Xbim.Common;

namespace Xbim.Presentation.Texturing
{
    public class SphericalTextureMap : ITextureMapping
    {
        /// <summary>
        /// Used Method for Texturing
        /// </summary>
        public TextureMapGenerationMethod TexturingMethod
        {
            get
            {
                return TextureMapGenerationMethod.SPHERE;
            }
        }

        /// <summary>
        /// Calculates the texture by using the algorithm of spherical texture mapping
        /// </summary>
        /// <returns>A spherical texture map. The indices of the texture map are related 
        /// to the indices of the given vertices</returns>
        public IEnumerable<Point> GetTextureMap(IEnumerable<Point3D> vertices, IEnumerable<Vector3D> normals, IEnumerable<int> triangles)
        {
            Point[] textureCoordinates = new Point[vertices.Count()];
            //Spherical uv mapping
            //calculate mid point of the shape
            double minX, minY, minZ, maxX, maxY, maxZ;
            minX = vertices.Select(x => x.X).Min();
            maxX = vertices.Select(x => x.X).Max();
            minY = vertices.Select(x => x.Y).Min();
            maxY = vertices.Select(x => x.Y).Max();
            minZ = vertices.Select(x => x.Z).Min();
            maxZ = vertices.Select(x => x.Z).Max();
            Vector3D midPoint = new Vector3D((minX + maxX) / 2, (minY + maxY) / 2, (minZ + maxZ) / 2);

            Parallel.For(0, textureCoordinates.Length, (verticeIndex) =>
            {
                Point3D meshPoint = vertices.ElementAt(verticeIndex);
                Vector3D direction = (Vector3D)(meshPoint - midPoint);
                double theta = Math.Acos(direction.Z / direction.Length);
                if (direction.Z < 0)
                {
                    theta *= -1;
                }

                double phi;
                if (direction.X > 0)
                {
                    phi = Math.Atan(direction.Y / direction.X);
                }
                else if (direction.X == 0)
                {
                    phi = Math.Sign(direction.Y) * Math.PI / 2.0;
                }
                else if (direction.X < 0 && direction.Y >= 0)
                {
                    phi = Math.Atan(direction.Y / direction.X) + Math.PI;
                }
                else
                {
                    phi = Math.Atan(direction.Y / direction.X) - Math.PI;
                }

                //double u = Math.Sin(theta) * Math.Cos(phi);
                //double v = Math.Sin(theta) * Math.Sin(phi);
                double u = phi;
                double v = theta;

                textureCoordinates[verticeIndex] = new Point(u, v);
            });
            return textureCoordinates;
        }
    }
}
