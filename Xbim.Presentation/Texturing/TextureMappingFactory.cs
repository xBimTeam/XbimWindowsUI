using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;
using Xbim.Ifc4.Interfaces;

namespace Xbim.Presentation.Texturing
{
    public static class TextureMappingFactory
    {
        /// <summary>
        /// Create a spherical texture mapping
        /// </summary>
        /// <returns>A spherical mapping object for the given vertices</returns>
        public static ITextureMapping CreateSphericalTextureMapping()
        {
            return new SphericalTextureMap();
        }

        /// <summary>
        /// create a new manual texture mapping
        /// </summary>
        /// <param name="textMap"></param>
        /// <param name="numberOfVertices"></param>
        /// <returns></returns>
        public static ITextureMapping CreateManualTextureMapping(IIfcIndexedTriangleTextureMap textMap, int numberOfVertices)
        {
            return new ManualTriangularTextureMapping(textMap, numberOfVertices);
        }

        /// <summary>
        /// create the corresponding Texturemapping to a related texture coordinate element
        /// </summary>
        /// <param name="textureCoordinate"></param>
        /// <returns></returns>
        public static ITextureMapping CreateTextureMapping(IIfcTextureCoordinate textureCoordinate)
        {
            if (textureCoordinate is IIfcTextureMap)
            {
                return null;
            }
            else if (textureCoordinate is IIfcTextureCoordinateGenerator generator)
            {
                TextureMapGenerationMethod method = (TextureMapGenerationMethod)Enum.Parse(typeof(TextureMapGenerationMethod), generator.Mode);
                switch (method)
                {
                    case TextureMapGenerationMethod.SPHERE:
                        return new SphericalTextureMap();
                    default:
                        return null;
                }
            }
            else if (textureCoordinate is IIfcIndexedTriangleTextureMap triTextureMap)
            {
                return new ManualTriangularTextureMapping(triTextureMap, triTextureMap.MappedTo.Coordinates.CoordList.Count);
            }
            else
            {
                return null;
            }
        }
    }
}
