#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    ModelDataProvider.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Collections.Generic;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Xbim.Common;
using Xbim.Common.Metadata;
using Xbim.Ifc2x3.IO;

#endregion

namespace Xbim.Presentation
{
    

    public class MaterialDictionary : Dictionary<object, Material>
    {
    }


    public class ModelDataProvider : ObjectDataProvider 
    {

        private static readonly MaterialDictionary _defaultMaterials;
        static ModelDataProvider()
        {
            SolidColorBrush transparentBrush = new SolidColorBrush(Colors.LightBlue);
            transparentBrush.Opacity = 0.5;
            MaterialGroup windowMaterial = new MaterialGroup();
            windowMaterial.Children.Add(new DiffuseMaterial(transparentBrush));
            windowMaterial.Children.Add(new SpecularMaterial(transparentBrush, 40));


            _defaultMaterials = new MaterialDictionary();
            _defaultMaterials.Add("IfcProduct", new DiffuseMaterial(new SolidColorBrush(Colors.Wheat)));
            _defaultMaterials.Add("IfcBuildingElementProxy", new DiffuseMaterial(new SolidColorBrush(Colors.Snow)));
            _defaultMaterials.Add("IfcWall", new DiffuseMaterial(new SolidColorBrush(Colors.White)));
            _defaultMaterials.Add("IfcRoof", new DiffuseMaterial(new SolidColorBrush(Colors.LightSteelBlue)));
            _defaultMaterials.Add("IfcSlab", new DiffuseMaterial(new SolidColorBrush(Colors.LightSteelBlue)));
            _defaultMaterials.Add("IfcWindow", windowMaterial);
            _defaultMaterials.Add("IfcPlate", windowMaterial); 
            _defaultMaterials.Add("IfcDoor", new DiffuseMaterial(new SolidColorBrush(Colors.CadetBlue)));
            _defaultMaterials.Add("IfcStair",
                                  new DiffuseMaterial(new SolidColorBrush(Colors.Wheat)));
            _defaultMaterials.Add("IfcBeam", new DiffuseMaterial(new SolidColorBrush(Colors.LightSlateGray)));
            _defaultMaterials.Add("IfcColumn", new DiffuseMaterial(new SolidColorBrush(Colors.LightSlateGray)));
            _defaultMaterials.Add("IfcFurnishingElement",
                                  new DiffuseMaterial(new SolidColorBrush(Colors.WhiteSmoke) {Opacity = 0.7}));
            _defaultMaterials.Add("IfcDistributionFlowElement",
                                  new DiffuseMaterial(new SolidColorBrush(Colors.AntiqueWhite) {Opacity = 1.0}));
            _defaultMaterials.Add("IfcFlowFitting",
                                  new DiffuseMaterial(new SolidColorBrush(Colors.PaleGoldenrod) { Opacity = 1.0 }));
            _defaultMaterials.Add("IfcFlowSegment",
                                  new DiffuseMaterial(new SolidColorBrush(Colors.PaleVioletRed) { Opacity = 1.0 }));
            _defaultMaterials.Add("IfcFlowTerminal",
                                  new DiffuseMaterial(new SolidColorBrush(Colors.IndianRed) { Opacity = 1.0 }));
            _defaultMaterials.Add("IfcSpace", new DiffuseMaterial(new SolidColorBrush(Colors.Red) {Opacity = 0.4}));
    
            _defaultMaterials.Add("IfcRailing", new DiffuseMaterial(new SolidColorBrush(Colors.Goldenrod)));
            _defaultMaterials.Add("IfcOpeningElement", new DiffuseMaterial(new SolidColorBrush(Colors.Red) { Opacity = 0.4 }));
        }

        #region Fields

        
        private readonly MaterialDictionary _materials = new MaterialDictionary();
        private double _transparency = 0.5;

       

        
        #endregion


        public static XbimMaterialProvider GetDefaultMaterial(IModel model, string typeName)
        {
            Material mat;
            var elemType = model.Metadata.ExpressType(typeName);
            while (elemType != null)
            {
                if (_defaultMaterials.TryGetValue(elemType.Type.Name, out mat))
                    return new XbimMaterialProvider(mat);
                elemType = elemType.SuperType;
            }
            return null;
        }

        public static XbimMaterialProvider GetDefaultMaterial( IPersistEntity obj)
        {
            if (obj != null)
                return GetDefaultMaterial(obj.Model, obj.GetType().Name);
            return null;
        }

        public static XbimMaterialProvider GetDefaultMaterial(IModel model, Type entityType)
        {
            return GetDefaultMaterial(model, entityType.Name);  
        }

        public static XbimMaterialProvider GetDefaultMaterial(IModel model, short entityTypeId)
        {
            var ifcType = model.Metadata.ExpressType(entityTypeId);
            return GetDefaultMaterial(model, ifcType.Type.Name);
        }

        public static XbimMaterialProvider GetDefaultMaterial(IModel model, ExpressType ifcType)
        {
            return GetDefaultMaterial(model, ifcType.Type.Name);
        }

        /// <summary>
        ///   Dictionary of shared materials, key is normally an Ifc object that the material represents
        /// </summary>
        public MaterialDictionary Materials
        {
            get { return _materials; }
        }

        public static MaterialDictionary DefaultMaterials
        {
            get { return _defaultMaterials; }
        }


        public XbimModel Model
        {
            get 
            {
                return (XbimModel)ObjectInstance;
            }
            set
            {
                ObjectInstance = value;
                //NotifyPropertyChanged("Model");
            }
        }

        public double Transparency
        {
            get { return _transparency; }
            set
            {
                foreach (KeyValuePair<object, Material> item in _materials)
                {
                    DiffuseMaterial dMat = item.Value as DiffuseMaterial;
                    if (dMat != null)
                    {
                        SolidColorBrush br = dMat.Brush as SolidColorBrush;
                        if (br != null)
                            br.Opacity = value;
                    }
                }
                foreach (KeyValuePair<object, Material> kvp in _defaultMaterials)
                {
                    DiffuseMaterial dMat = kvp.Value as DiffuseMaterial;
                    if (dMat != null)
                    {
                        SolidColorBrush br = dMat.Brush as SolidColorBrush;
                        if (br != null)
                        {
                            if (((string) kvp.Key) != "IfcSpace")
                                br.Opacity = Math.Max(value, 0);
                            else
                                br.Opacity = Math.Max(value*-1, 0);
                        }
                    }
                }
                _transparency = value;
            }
        }

        
    }
}