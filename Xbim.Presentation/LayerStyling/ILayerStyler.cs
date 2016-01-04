using Xbim.Common;
using Xbim.Common.Federation;
using Xbim.Ifc2x3.IO;
using Xbim.ModelGeometry.Scene;


namespace Xbim.Presentation.LayerStyling
{
    /// <summary>
    /// Interface defining the functions needed to group and style elements to be visualised in the WPF 3D component.
    /// Note that it inherits from IGeomHandlesGrouping
    /// </summary>
    public interface ILayerStyler : IGeomHandlesGrouping
    {
        /// <summary>
        /// returns a layer for the specified key 
        /// </summary>
        /// <param name="layerKey">It's a string that is generated in the GroupLayers function of the IGeomHandlesGrouping interface.</param>
        XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial> GetLayer(
            string layerKey, 
            XbimModel model,
            XbimScene<WpfMeshGeometry3D, WpfMaterial> scene
            );

        /// <summary>
        /// Determines whether the engine creates sublayers depending on IFC styles in the model.
        /// </summary>
        bool UseIfcSubStyles { get; }

        /// <summary>
        /// Returns a bool determining the visibility of a layer.
        /// </summary>
        /// <param name="key">Similar to layerkey in GetLayer</param>
        /// <returns></returns>
        bool IsVisibleLayer(string key);

        /// <summary>
        /// Provides information to the styler in case the model to render belongs to a federation.
        /// Leave an empty body in case the behaviour of the styler is independent from the federation context.
        /// </summary>
        /// <param name="refModel">The federation environment; refModel will be null for the main federation file.</param>
        void SetFederationEnvironment(IReferencedModel refModel);

        /// <summary>
        /// Sets the current model for the layerStyler
        /// </summary>
        /// <param name="model">Input model</param>
        void SetCurrentModel(IModel model);
    }
}
