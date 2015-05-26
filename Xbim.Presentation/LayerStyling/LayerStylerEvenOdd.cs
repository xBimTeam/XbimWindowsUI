using System.Collections.Generic;
using System.Linq;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;

namespace Xbim.Presentation.LayerStyling
{
    /// <summary>
    /// A sample styler that produces an arbitrary grouping in odd and even EntityLabels.
    /// </summary>
    public class LayerStylerEvenOdd : ILayerStyler
    {
        public Dictionary<string, XbimGeometryHandleCollection> GroupLayers(XbimGeometryHandleCollection inputHandles)
        {
            var retvalues = new Dictionary<string, XbimGeometryHandleCollection>();
            XbimGeometryHandleCollection odd = new XbimGeometryHandleCollection(inputHandles.Where(g => g.ProductLabel % 2 == 0));
            XbimGeometryHandleCollection even = new XbimGeometryHandleCollection(inputHandles.Where(g => g.ProductLabel % 2 == 1));
            if (even.Count > 0)
                retvalues.Add("Even", even);
            if (odd.Count > 0)
                retvalues.Add("Odd", odd);
            return retvalues;
        }

        public XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial> GetLayer(string layerKey, XbimModel model, XbimScene<WpfMeshGeometry3D, WpfMaterial> scene)
        {
            if (layerKey == "Even")
            {
                XbimColour colour = new XbimColour("Red", 1.0, 0.0, 0.0, 1);
                return new XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial>(model, colour) { Name = layerKey };
            }
            else
            {
                XbimColour colour = new XbimColour("Green", 0.0, 1.0, 0.0, 1);
                return new XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial>(model, colour) { Name = layerKey };
            }
        }

        public bool UseIfcSubStyles
        {
            get { return false; }
        }

        public bool IsVisibleLayer(string key)
        {
            return true;
        }

        public void SetFederationEnvironment(XbimReferencedModel refModel) { }
    }
}
