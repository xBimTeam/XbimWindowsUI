using System.Collections.Generic;
using System.Linq;
using Xbim.Ifc2x3.Extensions;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;

namespace Xbim.Presentation.LayerStyling
{
    public class LayerStylerSingleColour : ILayerStyler
    {
        XbimColour _colour = new XbimColour("LightGrey", 0.8, 0.8, 0.8);

        public XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial> GetLayer(string layerKey, XbimModel model, XbimScene<WpfMeshGeometry3D, WpfMaterial> scene)
        {
            return new XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial>(model, _colour) { Name = layerKey };
        }

        public bool UseIfcSubStyles
        {
            get { return false; }
        }

        public bool IsVisibleLayer(string key)
        {
            return true;
        }

        public void SetFederationEnvironment(XbimReferencedModel refModel)
        {
            var federationColours = new XbimColourMap(StandardColourMaps.Federation);
            var key = refModel.DocumentInformation.DocumentOwner.RoleName();
            _colour = federationColours[key];
            
        }

        public Dictionary<string, XbimGeometryHandleCollection> GroupLayers(XbimGeometryHandleCollection inputHandles)
        {
            var retvalues = new Dictionary<string, XbimGeometryHandleCollection>();
            if (inputHandles.Any() )
                retvalues.Add("WholeModel", inputHandles);
            return retvalues;
        }
    }
}
