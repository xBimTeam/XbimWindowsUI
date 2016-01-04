using System.Collections.Generic;
using System.Linq;
using Xbim.Common;
using Xbim.Ifc2x3.Kernel;
using Xbim.IO.Esent;

namespace Xbim.Presentation.LayerStyling
{
    /// <summary>
    /// Organises the inputhandles per type
    /// </summary>
    public class TypeAndStyle : IGeomHandlesGrouping
    {
        public Dictionary<string, XbimGeometryHandleCollection> GroupLayers(IModel model, XbimGeometryHandleCollection inputHandles)
        {
            // creates a new dictionary and then fills it by type enumerating the known non-abstract subtypes of Product
            Dictionary<string, XbimGeometryHandleCollection> result = new Dictionary<string, XbimGeometryHandleCollection>();
            var baseType = model.Metadata.ExpressType(typeof(IfcProduct));
            foreach (var subType in baseType.NonAbstractSubTypes)
            {
                short ifcTypeId = model.Metadata.ExpressTypeId(subType);
                XbimGeometryHandleCollection handles = new XbimGeometryHandleCollection(inputHandles.Where(g => g.ExpressTypeId == ifcTypeId),model.Metadata);
                
                // only add the item if there are handles in it
                if (handles.Count > 0) 
                    result.Add(subType.Name, handles);
            }
            return result;
        }
    }
}
