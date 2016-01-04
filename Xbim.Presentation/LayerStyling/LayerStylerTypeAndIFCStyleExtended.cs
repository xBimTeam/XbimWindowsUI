using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Xbim.Common;
using Xbim.Common.Federation;
using Xbim.Ifc;
using Xbim.Ifc2x3.IO;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.IO.Esent;
using Xbim.ModelGeometry.Scene;
using XbimModel = Xbim.IO.XbimModel;

namespace Xbim.Presentation.LayerStyling
{
    /// <summary>
    /// Demo layer styler for xBim Explorer in WPF.
    /// It's invoked through the Querying window.
    /// </summary>
    public class LayerStylerTypeAndIfcStyleExtended : ILayerStyler, IGeomHandlesGrouping
    {
        HashSet<int> _lightTransparentEntities;
        HashSet<Type> _lightTransparentTypes;

        HashSet<int> _hiddenEntities;
        HashSet<Type> _hiddenTypes;
        private IModel _model;
        public Dictionary<string, XbimGeometryHandleCollection> GroupLayers(IModel model, XbimGeometryHandleCollection inputHandles)
        {
            // creates a new dictionary and then fills it by type enumerating the known non-abstract subtypes of Product
            Dictionary<string, XbimGeometryHandleCollection> result = new Dictionary<string, XbimGeometryHandleCollection>();
            var metaData = _model.Metadata;
            // prepares transparents first
            HashSet<short> traspTypes = new HashSet<short>();
            foreach (var ttp in _lightTransparentTypes)
            {
                traspTypes.Add(metaData.ExpressTypeId(ttp));
            }
            XbimGeometryHandleCollection transp = new XbimGeometryHandleCollection(
                    inputHandles.Where(g =>
                        traspTypes.Contains(g.ExpressTypeId)
                        ||
                        _lightTransparentEntities.Contains(g.ProductLabel)
                        ),metaData
                    );
            result.Add("_LightBlueTransparent", transp);

            // deal with ignore elements
            HashSet<short> hiddTypes = new HashSet<short>();
            foreach (var htp in _hiddenTypes)
            {
                hiddTypes.Add(metaData.ExpressTypeId(htp));
            }
            XbimGeometryHandleCollection hidd = new XbimGeometryHandleCollection(
                    inputHandles.Where(g =>
                        hiddTypes.Contains(g.ExpressTypeId)
                        ||
                        _hiddenEntities.Contains(g.ProductLabel)
                        ), metaData
                    );

            // now execute normal type loop, but with the exclusion of hidden and transparent
            //
            var baseType = metaData.ExpressType(typeof(IfcProduct));
            foreach (var subType in baseType.NonAbstractSubTypes)
            {
                short ifcTypeId = metaData.ExpressTypeId(subType);
                XbimGeometryHandleCollection handles = new XbimGeometryHandleCollection(
                    inputHandles.Where(g => 
                        g.ExpressTypeId == ifcTypeId
                        &&
                        !(transp.Contains(g))
                        &&
                        !(hidd.Contains(g))
                        ), metaData
                    );
                
                // only add the item if there are handles in it
                if (handles.Count > 0) 
                    result.Add(subType.Name, handles);
            }
            return result;
        }

        private XbimColourMap _colours;

        /// <summary>
        /// Default initialisation
        /// </summary>
        public LayerStylerTypeAndIfcStyleExtended()
        {
            UseIfcSubStyles = false;
            Initialise();
            SetDefaults();
        }

        private void Initialise()
        {
            _colours = new XbimColourMap(StandardColourMaps.IfcProductTypeMap);
            _lightTransparentEntities = new HashSet<int>();
            _lightTransparentTypes = new HashSet<Type>();
            _hiddenEntities = new HashSet<int>();
            _hiddenTypes = new HashSet<Type>();
        }

        private void SetDefaults()
        {
            _colours.Add(new XbimColour("_LightBlueTransparent", 0, 0, 1, 0.5));
            _lightTransparentTypes.Add(typeof(IfcWall));
            _lightTransparentTypes.Add(typeof(IfcWallStandardCase));
            _hiddenTypes.Add(typeof(IfcColumn));
        }
        
        public XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial> GetLayer(
            string layerKey, 
            XbimModel model,
            XbimScene<WpfMeshGeometry3D, WpfMaterial> scene
            )
        {
            XbimColour colour = _colours[layerKey];
            return new XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial>(model, colour) { Name = layerKey };
        }

        public bool UseIfcSubStyles { get; set; }

        public string SendCommand(string command, EntitySelection entitySelection)
        {
            StringBuilder sb = new StringBuilder();
            Regex re = new Regex("(?<action>HIDE|MT) (?<selection>SET|SE)", RegexOptions.IgnoreCase);
            if (command == "default")
            {
                Initialise();
                SetDefaults();
                sb.AppendLine("Done.");
            }
            else if (command == "reset")
            {
                Initialise();
                sb.AppendLine("Done.");
            }
            else if (re.IsMatch(command))
            {
                var m = re.Match(command);
                string action = m.Groups["action"].Value;
                string selection = m.Groups["selection"].Value;

                if (selection == "set") // selection type
                {
                    var dest = _lightTransparentTypes;
                    if (action == "hide")
                        dest = _hiddenTypes;
                    foreach (var item in entitySelection)
                    {
                        if (!dest.Contains(item.GetType()))
                        {
                            dest.Add(item.GetType());
                        }
                    }
                }
                if (selection == "se") // selection items
                {
                    var dest = _lightTransparentEntities;
                    if (action == "hide")
                        dest = _hiddenEntities;
                    foreach (var item in entitySelection)
                    {
                        int i = Math.Abs(item.EntityLabel);
                        if (!dest.Contains(i))
                        {
                            dest.Add(i);
                        }
                    }
                }
            }
            else
            {
                sb.AppendLine("Command not understood from Styler.");
                sb.AppendLine("- reset");
                sb.AppendLine("- default");
                sb.AppendLine("- HIDE/MT SE/SET");
            }
            return sb.ToString();
        }

        public bool IsVisibleLayer(string key)
        {
            return true;
        }

        public void SetFederationEnvironment(IReferencedModel refModel) { }

        public void SetCurrentModel(IModel model)
        {
            _model = model;
        }
    }
}
