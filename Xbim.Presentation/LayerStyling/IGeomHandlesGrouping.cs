using System.Collections.Generic;
using Xbim.Common;
using Xbim.IO.Esent;

namespace Xbim.Presentation.LayerStyling
{
    /// <summary>
    /// Defines a method to organise a collection of geometry handles in subgroups 
    /// </summary>
    public interface IGeomHandlesGrouping
    {
        /// <summary>
        /// Analyses the handles and returns them in groups that are organised by string keys.
        /// </summary>
        /// <param name="handles">The handles to be organised</param>
        /// <returns>A dictionary that will later be enumerated by key to retrieve the style</returns>
        Dictionary<string, XbimGeometryHandleCollection> GroupLayers(IModel model, XbimGeometryHandleCollection handles);
    }
}
