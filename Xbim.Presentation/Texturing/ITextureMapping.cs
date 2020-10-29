using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;
using Xbim.Common;

namespace Xbim.Presentation.Texturing
{
    public interface ITextureMapping
    {
        /// <summary>
        /// returns the texture map for a given set of vertices
        /// </summary>
        /// <returns>a texture map for the related mesh</returns>
        IEnumerable<Point> GetTextureMap(IEnumerable<Point3D> vertices, IEnumerable<Vector3D> normals, IEnumerable<int> triangles);

        /// <summary>
        /// Method for the Texturing
        /// </summary>
        TextureMapGenerationMethod TexturingMethod {get;}
    }
}
