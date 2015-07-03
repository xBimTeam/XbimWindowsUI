using System;
using System.Collections.Generic;
using Xbim.Presentation.LayerStyling;
using Xbim.XbimExtensions.Interfaces;

namespace XbimLibrary.Xbim.Presentation.Extensions.LayerStyler
{
    public interface IExtendedLayerStyler : ILayerStyler
    {
        HashSet<Type> HiddenTypes { get; }

        void SetModelStyle(bool status);

        bool NeedsGeometryComputation { get; }

        void Show(IEnumerable<IPersistIfcEntity> entities);

        void Isolate(IEnumerable<IPersistIfcEntity> entities);

        void Hide(IEnumerable<IPersistIfcEntity> entities);

        void Show(Type ifcType);

        void Isolate(Type ifcType);

        void Hide(Type ifcType);

        void ResetAll();

        bool IsHidden(IPersistIfcEntity entity);
    }
}
