using System;
using System.Collections.Generic;
using Xbim.Common.Federation;
using Xbim.Ifc2x3.IO;
using Xbim.ModelGeometry.Scene;
using XbimModel = Xbim.IO.XbimModel;

namespace Xbim.Presentation.LayerStylingV2
{
    public interface ILayerStylerV2
    {
        XbimScene<WpfMeshGeometry3D, WpfMaterial> BuildScene(XbimModel model, Xbim3DModelContext context,
            List<Type> exclude = null);

        DrawingControl3D Control { get; set; }

        void SetFederationEnvironment(IReferencedModel refModel);
    }
}
