using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Xbim.Ifc2x3.Kernel;
using Xbim.IO;
using Xbim.IO.GroupingAndStyling;
using Xbim.ModelGeometry.Scene;

namespace Xbim.Presentation.LayerStyling
{
    /// <summary>
    /// Demo layer styler for xBim Explorer in WPF.
    /// It's invoked through the Querying window.
    /// </summary>
    public class LayerStylerTypeAndIFCStyleExtended : ILayerStyler, IGeomHandlesGrouping
    {
        HashSet<int> LightTransparentEntities;
        HashSet<Type> LightTransparentTypes;

        HashSet<int> HiddenEntities;
        HashSet<Type> HiddenTypes; 

        public Dictionary<string, XbimGeometryHandleCollection> GroupLayers(XbimGeometryHandleCollection InputHandles)
        {
            // creates a new dictionary and then fills it by type enumerating the known non-abstract subtypes of Product
            Dictionary<string, XbimGeometryHandleCollection> result = new Dictionary<string, XbimGeometryHandleCollection>();

            // prepares transparents first
            HashSet<short> traspTypes = new HashSet<short>();
            foreach (var ttp in LightTransparentTypes)
            {
                traspTypes.Add(IfcMetaData.IfcTypeId(ttp));
            }
            XbimGeometryHandleCollection transp = new XbimGeometryHandleCollection(
                    InputHandles.Where(g =>
                        traspTypes.Contains(g.IfcTypeId)
                        ||
                        LightTransparentEntities.Contains(g.ProductLabel)
                        )
                    );
            result.Add("_LightBlueTransparent", transp);

            // deal with ignore elements
            HashSet<short> HiddTypes = new HashSet<short>();
            foreach (var htp in HiddenTypes)
            {
                HiddTypes.Add(IfcMetaData.IfcTypeId(htp));
            }
            XbimGeometryHandleCollection hidd = new XbimGeometryHandleCollection(
                    InputHandles.Where(g =>
                        HiddTypes.Contains(g.IfcTypeId)
                        ||
                        HiddenEntities.Contains(g.ProductLabel)
                        )
                    );

            // now execute normal type loop, but with the exclusion of hidden and transparent
            //
            IfcType baseType = IfcMetaData.IfcType(typeof(IfcProduct));
            foreach (var subType in baseType.NonAbstractSubTypes)
            {
                short ifcTypeId = IfcMetaData.IfcTypeId(subType);
                XbimGeometryHandleCollection handles = new XbimGeometryHandleCollection(
                    InputHandles.Where(g => 
                        g.IfcTypeId == ifcTypeId
                        &&
                        !(transp.Contains(g))
                        &&
                        !(hidd.Contains(g))
                        )
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
        public LayerStylerTypeAndIFCStyleExtended()
        {
            UseIfcSubStyles = false;
            Initialise();
            SetDefaults();
        }

        private void Initialise()
        {
            _colours = new XbimColourMap(StandardColourMaps.IfcProductTypeMap);
            LightTransparentEntities = new HashSet<int>();
            LightTransparentTypes = new HashSet<Type>();
            HiddenEntities = new HashSet<int>();
            HiddenTypes = new HashSet<Type>();
        }

        private void SetDefaults()
        {
            _colours.Add(new XbimColour("_LightBlueTransparent", 0, 0, 1, 0.5));
            LightTransparentTypes.Add(typeof(Xbim.Ifc2x3.SharedBldgElements.IfcWall));
            LightTransparentTypes.Add(typeof(Xbim.Ifc2x3.SharedBldgElements.IfcWallStandardCase));
            HiddenTypes.Add(typeof(Xbim.Ifc2x3.SharedBldgElements.IfcColumn));
        }
        
        public ModelGeometry.Scene.XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial> GetLayer(
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
                    var dest = LightTransparentTypes;
                    if (action == "hide")
                        dest = HiddenTypes;
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
                    var dest = LightTransparentEntities;
                    if (action == "hide")
                        dest = HiddenEntities;
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

        public void SetFederationEnvironment(XbimReferencedModel refModel) { }
    }
}
