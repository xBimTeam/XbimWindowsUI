using System.Collections.Generic;
using System.Linq;
using Xbim.Common;
using Xbim.Common.Metadata;
using Xbim.Ifc;

namespace XbimXplorer.Dialogs.ExcludedTypes
{
    internal class ExpressTypeExpander : ITreeElement
    {
        public ExpressType ExpressType;

        public IfcStore Model { get; set; }

        public ExpressTypeExpander(ExpressType view, IfcStore model)
        {
            ExpressType = view;
            Model = model;
        }

        IEnumerable<ObjectViewModel> ITreeElement.GetChildren()
        {
            foreach (var child in ExpressType.SubTypes)
            {
                yield return new ObjectViewModel() { Tag = new ExpressTypeExpander(child, Model) };                
            }
        }

        public string Quantity
        {
            get
            {
                if (Model == null || Model.FileName == null)
                    return "";
                if (!ExpressType.Type.FullName.ToLowerInvariant().Contains(((IModel)Model).SchemaVersion.ToString().ToLowerInvariant()))
                    return "";
                if (Model.GeometryStore == null)
                    return "";
                return string.Format(" ({0})",
                    Model.Instances.OfType(ExpressType.Name, false).Count()
                    );
            }
        }

        public override string ToString()
        {
            return ExpressType.Name + Quantity;
        }
    }
}
