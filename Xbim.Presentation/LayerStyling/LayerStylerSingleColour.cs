using System.Collections.Generic;
using System.Linq;
using Xbim.Common;
using Xbim.Common.Federation;
using Xbim.Ifc;
using Xbim.IO.Esent;
using Xbim.ModelGeometry.Scene;
using XbimModel = Xbim.IO.XbimModel;

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

        public void SetFederationEnvironment(IReferencedModel refModel)
        {
            var federationColours = new XbimColourMap(StandardColourMaps.Federation);
            //TODO fix interface to support doc info
            //var key = refModel.DocumentInformation.DocumentOwner.RoleName();
           // _colour = federationColours[key];
            _colour = federationColours[0];

        }

        public Dictionary<string, XbimGeometryHandleCollection> GroupLayers(IModel model, XbimGeometryHandleCollection inputHandles)
        {
            var retvalues = new Dictionary<string, XbimGeometryHandleCollection>();
            if (inputHandles.Any() )
                retvalues.Add("WholeModel", inputHandles);
            return retvalues;
        }

        public void SetCurrentModel(IModel model) { }
    }
}
