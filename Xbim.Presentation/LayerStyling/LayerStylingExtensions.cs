using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Common;
using Xbim.Common.Geometry;
using Xbim.Common.Metadata;
using Xbim.Ifc4.Interfaces;

namespace Xbim.Presentation.LayerStyling
{
    public static class LayerStylingExtensions
    {
        public static HashSet<short> DefaultExclusions(this IModel model, List<Type> exclude)
        {
            var excludedTypes = new HashSet<short>();
            if (exclude == null)
                exclude = new List<Type>()
                {
                    typeof(IIfcSpace),
                    typeof(IIfcFeatureElement)
                };
            foreach (var excludedT in exclude)
            {
                ExpressType ifcT;
                if (excludedT.IsInterface && excludedT.Name.StartsWith("IIfc"))
                {
                    var concreteTypename = excludedT.Name.Substring(1).ToUpper();
                    ifcT = model.Metadata.ExpressType(concreteTypename);
                }
                else
                    ifcT = model.Metadata.ExpressType(excludedT);
                if (ifcT == null) // it could be a type that does not belong in the model schema
                    continue;
                foreach (var exIfcType in ifcT.NonAbstractSubTypes)
                {
                    excludedTypes.Add(exIfcType.TypeId);
                }
            }
            return excludedTypes;
        }

        public static IEnumerable<XbimShapeInstance> FilterShapes(this IEnumerable<XbimShapeInstance> shapeInstances, Dictionary<int, IPersistEntity> onlyInstances, Dictionary<int, IPersistEntity> hiddenInstances, Dictionary<int, IIfcGeometricRepresentationContext> selectedContexts)
        {
            return shapeInstances
                                    .Where(s => null == onlyInstances || onlyInstances.Count == 0 || onlyInstances.Keys.Contains(s.IfcProductLabel))
                                    .Where(s => null == hiddenInstances || hiddenInstances.Count == 0 || !hiddenInstances.Keys.Contains(s.IfcProductLabel))
                                    .Where(s => null == selectedContexts || selectedContexts.Count == 0 || selectedContexts.Keys.Contains(s.RepresentationContext));
        }
    }
}
