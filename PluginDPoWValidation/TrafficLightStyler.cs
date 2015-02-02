using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Validation.mvdXML;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using Xbim.Presentation;
using Xbim.Presentation.LayerStyling;
using Validation.ValidationExtensions;

namespace Validation
{
    public class TrafficLightStyler : ILayerStyler
    {
        private XbimModel _Model;
        private MainWindow _Window;

        public bool UseAmber { get; set; }

        public TrafficLightStyler(XbimModel Model, MainWindow Window)
        {
            _Model = Model;
            _Window = Window;
        }

        public Dictionary<string, XbimGeometryHandleCollection> GroupLayers(XbimGeometryHandleCollection InputHandles)
        {
            // preparation
            var retvalues = new Dictionary<string, XbimGeometryHandleCollection>();
            XbimGeometryHandleCollection Red = new XbimGeometryHandleCollection();
            XbimGeometryHandleCollection Amber = new XbimGeometryHandleCollection();
            XbimGeometryHandleCollection Green = new XbimGeometryHandleCollection();



            Dictionary<short, List<MvdConceptRoot>> ConceptRootsDic = new Dictionary<short, List<MvdConceptRoot>>();

            // short, List<MVDConceptRoot
            foreach (var handle in InputHandles)
            {
                var ent = _Model.Instances[handle.ProductLabel];
                if (ent is Xbim.Ifc2x3.ProductExtension.IfcSpace)
                    continue;
                if (!ConceptRootsDic.ContainsKey(ent.IfcTypeId()))
                {
                    var t = ent.IfcType();
                    List<MvdConceptRoot> v = new List<MvdConceptRoot>();
                    while (t != null)
                    {
                        string s = t.Name;
                        v.AddRange(_Window.Doc.GetConceptRoots(s));
                        t = t.IfcSuperType;
                    }
                    ConceptRootsDic.Add(
                        ent.IfcTypeId(),
                        v
                        );
                }

                var suitableroots = ConceptRootsDic[ent.IfcTypeId()];
                if (!suitableroots.Any())
                {
                    if (UseAmber)
                        Amber.Add(handle);
                }
                else
                {
                    bool bPassed = true;
                    foreach (var validRoot in suitableroots)
                    {
                        foreach (var cpt in validRoot.Concepts)
                        {
                            if (!ent.PassesConceptRules(cpt))
                            {
                                bPassed = false;
                                break;
                            }
                        }
                        if (bPassed == false)
                            break;
                    }
                    if (bPassed)
                        Green.Add(handle);
                    else
                        Red.Add(handle);
                }
            }

            if (Red.Any())
                retvalues.Add("Red", Red);
            if (Amber.Any())
                retvalues.Add("Amber", Amber);
            if (Green.Any())
                retvalues.Add("Green", Green);

            return retvalues;
        }

        public void SetFederationEnvironment(XbimReferencedModel refModel)
        {
            
        }

        public XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial> GetLayer(string LayerKey, XbimModel model, XbimScene<WpfMeshGeometry3D, WpfMaterial> scene)
        {
            if (LayerKey == "Red")
            {
                var colour = new XbimColour("Red", 1.0, 0.0, 0.0, 0.8);
                return new XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial>(model, colour) { Name = LayerKey };
            }
            else if (LayerKey == "Amber")
            {
                //FFA500
                var colour = new XbimColour("Amber", 1.0, 0.64, 0.0, 0.8);
                return new XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial>(model, colour) { Name = LayerKey };
            }
            else
            {
                var colour = new XbimColour("Green", 0.0, 1.0, 0.0, 0.8);
                return new XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial>(model, colour) { Name = LayerKey };
            }
        }

        public bool UseIfcSubStyles
        {
            get { return false; }
        }

        public bool IsVisibleLayer(string key)
        {
            return true;
        }
    }
}