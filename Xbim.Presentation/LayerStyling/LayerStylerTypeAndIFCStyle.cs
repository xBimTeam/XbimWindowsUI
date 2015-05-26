using System.Collections.Generic;
using Xbim.IO;
using Xbim.IO.GroupingAndStyling;
using Xbim.ModelGeometry.Scene;

namespace Xbim.Presentation.LayerStyling
{
    /// <summary>
    /// Default layer styler for xBim Explorer in WPF
    /// </summary>
    public class LayerStylerTypeAndIfcStyle : ILayerStyler
    {
        /// <summary>
        /// Default initialisation
        /// </summary>
        public LayerStylerTypeAndIfcStyle()
        {
            UseIfcSubStyles = true;
            LayerGrouper = new TypeAndStyle();
        }

        /// <summary>
        /// this private member takes care of handling the IGeomHandlesGrouping interface
        /// </summary>
        private TypeAndStyle LayerGrouper { get; set; }

        // redirects the grouping requirement to the style using the LayerGrouper
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
            XbimColour colour = scene.LayerColourMap[layerKey];
            return new XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial>(model, colour) { Name = layerKey };
        }

        public bool UseIfcSubStyles { get; set; }

        public bool IsVisibleLayer(string key)
        {
            return true;
        }

        public void SetFederationEnvironment(XbimReferencedModel refModel) { }
    }
}
