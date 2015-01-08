using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.Extensions;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;


namespace Xbim.Presentation.LayerStyling
{
    public class LayerStylerSingleColour : ILayerStyler
    {
        XbimColour _colour = new XbimColour("LightGrey", 0.8, 0.8, 0.8);

        public ModelGeometry.Scene.XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial> GetLayer(string LayerKey, IO.XbimModel model, ModelGeometry.Scene.XbimScene<WpfMeshGeometry3D, WpfMaterial> scene)
        {
            return new XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial>(model, _colour) { Name = LayerKey };
        }

        public bool UseIfcSubStyles
        {
            get { return false; }
        }

        public bool IsVisibleLayer(string key)
        {
            return true;
        }

        public void SetFederationEnvironment(IO.XbimReferencedModel refModel)
        {
            var federationColours = new XbimColourMap(StandardColourMaps.Federation);
            var key = refModel.DocumentInformation.DocumentOwner.RoleName();
            _colour = federationColours[key];
            
        }

        public Dictionary<string, IO.XbimGeometryHandleCollection> GroupLayers(IO.XbimGeometryHandleCollection InputHandles)
        {
            var retvalues = new Dictionary<string, XbimGeometryHandleCollection>();
            if (InputHandles.Any() )
                retvalues.Add("WholeModel", InputHandles);
            return retvalues;
        }
    }
}
