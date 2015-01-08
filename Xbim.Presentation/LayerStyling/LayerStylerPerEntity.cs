using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;

namespace Xbim.Presentation.LayerStyling
{
    public class LayerStylerPerEntity : ILayerStyler
    {
        private Xbim.IO.GroupingAndStyling.EntityLabel LayerGrouper { get; set; }

        public LayerStylerPerEntity()
        {
            UseIfcSubStyles = true;
            LayerGrouper = new Xbim.IO.GroupingAndStyling.EntityLabel();
        }

        public Dictionary<string, XbimGeometryHandleCollection> GroupLayers(XbimGeometryHandleCollection InputHandles)
        {
            return LayerGrouper.GroupLayers(InputHandles);
        }

        public ModelGeometry.Scene.XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial> GetLayer(
            string layerKey, 
            XbimModel model,
            XbimScene<WpfMeshGeometry3D, WpfMaterial> scene
            )
        {
            int iLab;
            string LayerName = layerKey;
            bool conversionok = Int32.TryParse(layerKey, out iLab);
            if (conversionok)
            {
                layerKey = model.Instances[iLab].GetType().Name;
            }
            XbimColour colour = scene.LayerColourMap[layerKey];
            return new XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial>(model, colour) { Name = LayerName };
        }

        public bool UseIfcSubStyles { get; set; }

        public bool IsVisibleLayer(string key)
        {
            return true;
        }

        public void SetFederationEnvironment(XbimReferencedModel refModel) { }
    }
}