using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace Xbim.Presentation.Texturing
{
    public class SphericalTextureMap : ITextureMapping
    {
        /// <summary>
        /// Calculates the texture by using the algorithm of spherical texture mapping
        /// </summary>
        /// <param name="vertices">vertices of the mesh which shall be textured</param>
        /// <returns>A spherical texture map. The indices of the texture map are related 
        /// to the indices of the given vertices</returns>
        IEnumerable<Point> ITextureMapping.GetTextureMap(IEnumerable<Point3D> vertices)
        {
            //Spherical uv mapping
            //calculate mid point of the shape
            List<Point> textureCoordinates = new List<Point>();
            double minX, minY, minZ, maxX, maxY, maxZ;
            minX = vertices.Select(x => x.X).Min();
            maxX = vertices.Select(x => x.X).Max();
            minY = vertices.Select(x => x.Y).Min();
            maxY = vertices.Select(x => x.Y).Max();
            minZ = vertices.Select(x => x.Z).Min();
            maxZ = vertices.Select(x => x.Z).Max();
            Vector3D midPoint = new Vector3D((minX + maxX) / 2, (minY + maxY) / 2, (minZ + maxZ) / 2);

            foreach (Point3D meshPoint in vertices)
            {
                Vector3D direction = (Vector3D)(meshPoint - midPoint);
                double theta = Math.Acos(direction.Z / direction.Length);
                if (direction.Z < 0)
                {
                    theta = theta * -1;
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

                double u = Math.Sin(theta) * Math.Cos(phi);
                double v = Math.Sin(theta) * Math.Sin(phi);

                textureCoordinates.Add(new Point(u, v));
            }
            return textureCoordinates;
        }
    }
}
