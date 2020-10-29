﻿using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;
using Xbim.Common;
using Xbim.Common.Federation;
using Xbim.Common.Geometry;
using Xbim.Ifc4.Interfaces;

namespace Xbim.Presentation.LayerStyling
{
    public interface ILayerStyler
    {
        XbimScene<WpfMeshGeometry3D, WpfMaterial> BuildScene(IModel model, XbimMatrix3D modelTransform, 
            ModelVisual3D opaqueShapes, ModelVisual3D transparentShapes, List<IPersistEntity> isolateInstances = null, 
            List<IPersistEntity> hideInstances = null, List<IIfcGeometricRepresentationContext> selectContexts = null, 
            List<Type> excludeTypes = null);
        void SetFederationEnvironment(IReferencedModel refModel);
    }

    public interface IProgressiveLayerStyler
    {
        event System.ComponentModel.ProgressChangedEventHandler ProgressChanged;
    }
}
