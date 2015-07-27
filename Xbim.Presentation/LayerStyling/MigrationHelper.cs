using System.Collections.Generic;
using System.Linq;
using Xbim.Common.Geometry;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using XbimGeometry.Interfaces;

namespace Xbim.Presentation.LayerStyling
{
    static public class MigrationHelper
    {
        public static XbimGeometryHandleCollection GetApproximateGeometryHandles(this Xbim3DModelContext context, IEnumerable<int> loadLabels = null)
        {
            var retCollection = new XbimGeometryHandleCollection();

            if (loadLabels != null)
            {
                foreach (var shapeInstance in context.ShapeInstances()
                    .Where(x => loadLabels.Contains(x.InstanceLabel)))
                {
                    if (context.Model.Instances[shapeInstance.IfcProductLabel] is )
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
