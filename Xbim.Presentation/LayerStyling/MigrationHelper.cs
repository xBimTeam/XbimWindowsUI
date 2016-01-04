using System.Collections.Generic;
using System.Linq;
using Xbim.Common.Geometry;
using Xbim.IO.Esent;
using Xbim.ModelGeometry.Scene;

namespace Xbim.Presentation.LayerStyling
{
    public static class MigrationHelper
    {
        public static XbimGeometryHandleCollection GetApproximateGeometryHandles(this Xbim3DModelContext context, IEnumerable<int> loadLabels = null)
        {
            var retCollection = new XbimGeometryHandleCollection();

            if (loadLabels != null)
            {
                foreach (var shapeInstance in context.ShapeInstances()
                    .Where(x => loadLabels.Contains(x.InstanceLabel)))
                {
                    retCollection.Add(
                        new XbimGeometryHandle(
                            shapeInstance.InstanceLabel,
                            XbimGeometryType.TriangulatedMesh,
                            shapeInstance.IfcProductLabel,
                            shapeInstance.IfcTypeId,
                            shapeInstance.StyleLabel
                            )
                        );
                }
            }
            else
            {
                foreach (var shapeInstance in context.ShapeInstances()
                    .Where(s => s.RepresentationType == XbimGeometryRepresentationType.OpeningsAndAdditionsIncluded))
                {
                    retCollection.Add(
                        new XbimGeometryHandle(
                            shapeInstance.InstanceLabel,
                            XbimGeometryType.TriangulatedMesh,
                            shapeInstance.IfcProductLabel,
                            shapeInstance.IfcTypeId,
                            shapeInstance.StyleLabel
                            )
                        );
                }    
            }
            return retCollection;
        }
    }
}
