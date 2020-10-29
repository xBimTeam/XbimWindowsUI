using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;
using Xbim.Common;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.MeasureResource;

namespace Xbim.Presentation.Texturing
{
    public class ManualTriangularTextureMapping : ITextureMapping
    {
        /// <summary>
        /// Manual map for texture coordinates. The index of the several vectors are related to the vertice index provided at <a href="::GetTextureMap">GetTextureMap</a>
        /// </summary>
        private IIfcIndexedTriangleTextureMap _ifcTextMap;
        private int _numberOfVertices;

        public ManualTriangularTextureMapping(IIfcIndexedTriangleTextureMap textMap, int numberOfVertices)
        {
            _ifcTextMap = textMap;
            _numberOfVertices = numberOfVertices;
        }

        /// <summary>
        /// Used Method for Texturing
        /// </summary>
        public TextureMapGenerationMethod TexturingMethod
        {
            get
            {
                return TextureMapGenerationMethod.MANUALTRIANGULAR;
            }
        }

        /// <summary>
        /// returns the manual texture map
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Point> GetTextureMap(IEnumerable<Point3D> vertices, IEnumerable<Vector3D> normals, IEnumerable<int> triangles)
        {
            IIfcTriangulatedFaceSet faceSet = _ifcTextMap.MappedTo as IIfcTriangulatedFaceSet;
            Point[] result = new Point[_numberOfVertices];
            for (int triangleIdx = 0; triangleIdx < _ifcTextMap.TexCoordIndex.Count; triangleIdx++)
            {
                var texCoordTriangle = _ifcTextMap.TexCoordIndex[triangleIdx];
                for (int verticeIdx = 0; verticeIdx < texCoordTriangle.Count; verticeIdx++)
                {
                    int texCoordIdx = (int)texCoordTriangle[verticeIdx] - 1; //ifc indexing is one based
                    int verticeRefIdx = (int)faceSet.CoordIndex[triangleIdx][verticeIdx] - 1; //ifc indexing is one based

                    result[verticeRefIdx] = new Point(_ifcTextMap.TexCoords.TexCoordsList[texCoordIdx][0], _ifcTextMap.TexCoords.TexCoordsList[texCoordIdx][1]);
                }
            }

            return result;
        }
    }
}
