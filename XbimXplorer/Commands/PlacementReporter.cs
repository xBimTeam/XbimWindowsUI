using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Xbim.Common;
using Xbim.Ifc4.Interfaces;

namespace XbimXplorer.Commands
{
    internal class PlacementReporter
    {
        private class PlacementNodeInfo
        {
            internal PlacementNodeInfo Parent { get; private set; }
            internal List<PlacementNodeInfo> Children = new List<PlacementNodeInfo>();
            
            internal List<IIfcProduct> products = new List<IIfcProduct>();
            internal IIfcObjectPlacement ObjectPlacement;

            public PlacementNodeInfo(IIfcObjectPlacement objectPlacement)
            {
                this.ObjectPlacement = objectPlacement;
            }

            public override string ToString()
            {
                if (Parent == null)
                    return "Root" + ObjectPlacement.EntityLabel;

                return ObjectPlacement.EntityLabel.ToString();
            }

            internal void SetParent(PlacementNodeInfo newP)
            {
                Parent = newP;
                newP.Children.Add(this);
            }
        }

        IModel _model;

        internal PlacementReporter(IModel model)
        {
            _model = model;
        }

        /// <summary>
        /// Add individual entity (and its placement tree) to the report
        /// </summary>
        /// <param name="objectEntityLabel">entity lable of object or placement</param>
        public void Add(int objectEntityLabel)
        {
            var ent = _model.Instances[objectEntityLabel];
            if (ent is IIfcProduct)
            {
                var asprod = ent as IIfcProduct;
                var refernce = GetOrCreate(asprod.ObjectPlacement);
                refernce.products.Add(asprod);
            }
            else if (ent is IIfcObjectPlacement)
            {
                var asPlacement = ent as IIfcObjectPlacement;
                GetOrCreate(asPlacement);
            }
        }

        private void Add(IIfcProduct asprod)
        {
            GetOrCreate(asprod.ObjectPlacement);
        }

        Dictionary<IIfcObjectPlacement, PlacementNodeInfo> _placements = new Dictionary<IIfcObjectPlacement, PlacementNodeInfo>();

        private PlacementNodeInfo GetOrCreate(IIfcObjectPlacement objectPlacement)
        {
            PlacementNodeInfo ret;
            if (_placements.TryGetValue(objectPlacement, out ret))
                return ret;

            ret = new PlacementNodeInfo(objectPlacement);
            _placements.Add(objectPlacement, ret);
            if (objectPlacement is IIfcLocalPlacement)
            {
                var asLocP = objectPlacement as IIfcLocalPlacement;
                var parent = asLocP.PlacementRelTo;
                
                if (parent != null)
                {
                    ret.SetParent(GetOrCreate(parent));
                }
            }
            return ret;
        }
        

        internal TextHighliter ToReporter()
        {
            var sb = new TextHighliter();
            foreach (var placement in _placements.Values)
            {
                if (placement.Parent == null)
                    Report(sb, placement, 0);
            }
            return sb;
        }

        private void Report(TextHighliter sb, PlacementNodeInfo placement, int level)
        {
            var indent = new string(' ', level * 2);
            string refs = "";
            if (placement.products.Any())
                refs = "products: " + string.Join(", ",  placement.products.Select(x=> $"[#{x.EntityLabel}]"));
            else
            {
                var ifcOb = _model.Instances.OfType<IIfcProduct>().FirstOrDefault(x => x.ObjectPlacement != null && x.ObjectPlacement.EntityLabel == placement.ObjectPlacement.EntityLabel);
                if (ifcOb != null)
                {
                    refs = $"{ifcOb.GetType().Name} [#{ifcOb.EntityLabel}]";
                }
            }
                        
            sb.Append($"{indent}[#{placement.ObjectPlacement.EntityLabel}] {refs}", Brushes.Black);
            foreach (var child in placement.Children)
            {
                Report(sb, child, level + 1);
            }
        }
    }
}
