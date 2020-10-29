using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xbim.Presentation.Texturing
{
    /// <summary>
    /// Methods to generate a texture
    /// </summary>
    public enum TextureMapGenerationMethod
    {
        /// <summary>
        /// Manual mapping
        /// </summary>
        MANUALTRIANGULAR,
        
        /// <summary>
        /// direct mapping of vertices and coordinates
        /// </summary>
        MANUALDIRECT,

        /// <summary>
        /// Spherical mapping
        /// </summary>
        SPHERE,

        /// <summary>
        /// Camera space normal mapping
        /// </summary>
        CAMERASPACENORMAL,

        /// <summary>
        /// camera space position normal mapping
        /// </summary>
        CAMERASPACEPOSITION,

        /// <summary>
        /// camera space reflection vector mapping
        /// </summary>
        CAMERASPACEREFLECTIONVECTOR,
        SPHERE_LOCAL,

        /// <summary>
        /// 
        /// </summary>
        COORD,

        /// <summary>
        /// 
        /// </summary>
        COORD_EYE,

        /// <summary>
        /// 
        /// </summary>
        NOISE,

        /// <summary>
        /// 
        /// </summary>
        NOISE_EYE,

        /// <summary>
        /// 
        /// </summary>
        SPHERE_REFLECT,

        /// <summary>
        /// 
        /// </summary>
        SPHERE_REFLECT_LOCAL
    }
}
