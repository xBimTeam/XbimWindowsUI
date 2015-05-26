using System;
using System.Collections.Generic;
using Xbim.IO;
using Xbim.IO.GroupingAndStyling;
using Xbim.ModelGeometry.Scene;

namespace Xbim.Presentation.LayerStyling
{
    public class LayerStylerPerEntity : ILayerStyler
    {
        private EntityLabel LayerGrouper { get; set; }

        public LayerStylerPerEntity()
        {
            UseIfcSubStyles = true;
            LayerGrouper = new EntityLabel();
        }

        public Dictionary<string, XbimGeometryHandleCollection> GroupLayers(XbimGeometryHandleCollection inputHandles)
        {
            return LayerGrouper.GroupLayers(inputHandles);
        }

        public XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial> GetLayer(
            string layerKey, 
            XbimModel model,
            XbimScene<WpfMeshGeometry3D, WpfMaterial> scene
            )
        {
            int iLab;
            string layerName = layerKey;
            bool conversionok = Int32.TryParse(layerKey, out iLab);
            if (conversionok)
            {
                layerKey = model.Instances[iLab].GetType().Name;
            }
            XbimColour colour = scene.LayerColourMap[layerKey];
            return new XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial>(model, colour) { Name = layerName };
        }

        public bool UseIfcSubStyles { get; set; }

        public bool IsVisibleLayer(string key)
        {
            return true;
        }

        public void SetFederationEnvironment(XbimReferencedModel refModel) { }
    }
}