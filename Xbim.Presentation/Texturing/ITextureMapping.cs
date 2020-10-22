using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace Xbim.Presentation.Texturing
{
    public interface ITextureMapping
    {
        /// <summary>
        /// returns the texture map for a given set of vertices
        /// </summary>
        /// <param name="vertices">Vertices of the related mesh</param>
        /// <returns>a texture map for the related mesh</returns>
        IEnumerable<Point> GetTextureMap(IEnumerable<Point3D> vertices);
    }
}
