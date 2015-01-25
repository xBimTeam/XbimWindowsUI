using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;

namespace Xbim.Presentation.LayerStylingV2
{
    public interface ILayerStylerV2
    {
        XbimScene<WpfMeshGeometry3D, WpfMaterial> BuildScene(XbimModel model, Xbim3DModelContext context,
            List<Type> exclude = null);

        DrawingControl3D Control { get; set; }

        void SetFederationEnvironment(IO.XbimReferencedModel refModel);
    }
}
