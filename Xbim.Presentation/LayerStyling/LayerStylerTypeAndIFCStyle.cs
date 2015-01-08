using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;

namespace Xbim.Presentation.LayerStyling
{
    /// <summary>
    /// Default layer styler for xBim Explorer in WPF
    /// </summary>
    public class LayerStylerTypeAndIFCStyle : ILayerStyler
    {
        /// <summary>
        /// Default initialisation
        /// </summary>
        public LayerStylerTypeAndIFCStyle()
        {
            UseIfcSubStyles = true;
            _LayerGrouper = new Xbim.IO.GroupingAndStyling.TypeAndStyle();
        }

        /// <summary>
        /// this private member takes care of handling the IGeomHandlesGrouping interface
        /// </summary>
        private Xbim.IO.GroupingAndStyling.TypeAndStyle _LayerGrouper { get; set; }

        // redirects the grouping requirement to the style using the LayerGrouper
        public Dictionary<string, XbimGeometryHandleCollection> GroupLayers(XbimGeometryHandleCollection InputHandles)
        {
            return _LayerGrouper.GroupLayers(InputHandles);
        }


        public ModelGeometry.Scene.XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial> GetLayer(
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
