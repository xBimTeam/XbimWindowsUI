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
using Xbim.Ifc;


#endregion

namespace Xbim.Presentation
{
    public class MaterialDictionary : Dictionary<object, Material>
    {
    }
    
    public class ModelDataProvider : ObjectDataProvider 
    {
        static ModelDataProvider()
        {
            var transparentBrush = new SolidColorBrush(Colors.LightBlue) {Opacity = 0.5};
            var windowMaterial = new MaterialGroup();
            windowMaterial.Children.Add(new DiffuseMaterial(transparentBrush));
            windowMaterial.Children.Add(new SpecularMaterial(transparentBrush, 40));


            DefaultMaterials = new MaterialDictionary
            {
                {"IfcProduct", new DiffuseMaterial(new SolidColorBrush(Colors.Wheat))},
                {"IfcBuildingElementProxy", new DiffuseMaterial(new SolidColorBrush(Colors.Snow))},
                {"IfcWall", new DiffuseMaterial(new SolidColorBrush(Colors.White))},
                {"IfcRoof", new DiffuseMaterial(new SolidColorBrush(Colors.LightSteelBlue))},
                {"IfcSlab", new DiffuseMaterial(new SolidColorBrush(Colors.LightSteelBlue))},
                {"IfcWindow", windowMaterial},
                {"IfcPlate", windowMaterial},
                {"IfcDoor", new DiffuseMaterial(new SolidColorBrush(Colors.CadetBlue))},
                {"IfcStair", new DiffuseMaterial(new SolidColorBrush(Colors.Wheat))},
                {"IfcBeam", new DiffuseMaterial(new SolidColorBrush(Colors.LightSlateGray))},
                {"IfcColumn", new DiffuseMaterial(new SolidColorBrush(Colors.LightSlateGray))},
                {"IfcFurnishingElement", new DiffuseMaterial(new SolidColorBrush(Colors.WhiteSmoke) {Opacity = 0.7})},
                {"IfcDistributionFlowElement", new DiffuseMaterial(new SolidColorBrush(Colors.AntiqueWhite) {Opacity = 1.0}) },
                {"IfcFlowFitting", new DiffuseMaterial(new SolidColorBrush(Colors.PaleGoldenrod) {Opacity = 1.0})},
                {"IfcFlowSegment", new DiffuseMaterial(new SolidColorBrush(Colors.PaleVioletRed) {Opacity = 1.0})},
                {"IfcFlowTerminal", new DiffuseMaterial(new SolidColorBrush(Colors.IndianRed) {Opacity = 1.0})},
                {"IfcSpace", new DiffuseMaterial(new SolidColorBrush(Colors.Red) {Opacity = 0.4})},
                {"IfcRailing", new DiffuseMaterial(new SolidColorBrush(Colors.Goldenrod))},
                {"IfcOpeningElement", new DiffuseMaterial(new SolidColorBrush(Colors.Red) {Opacity = 0.4})}
            };
        }

        #region Fields

        private double _transparency = 0.5;

       

        
        #endregion


        public static XbimMaterialProvider GetDefaultMaterial(IModel model, string typeName)
        {
            var elemType = model.Metadata.ExpressType(typeName);
            while (elemType != null)
            {
                Material mat;
                if (DefaultMaterials.TryGetValue(elemType.Type.Name, out mat))
                    return new XbimMaterialProvider(mat);
                elemType = elemType.SuperType;
            }
            return null;
        }

        public static XbimMaterialProvider GetDefaultMaterial( IPersistEntity obj)
        {
            return obj != null 
                ? GetDefaultMaterial(obj.Model, obj.GetType().Name) 
                : null;
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
        /// Dictionary of shared materials, key is normally an Ifc object that the material represents
        /// </summary>
        public MaterialDictionary Materials { get; } = new MaterialDictionary();

        public static MaterialDictionary DefaultMaterials { get; }


        public IfcStore Model
        {
            get 
            {
                return (IfcStore)ObjectInstance;
            }
            set
            {
                ObjectInstance = value;
            }
        }

        public double Transparency
        {
            get { return _transparency; }
            set
            {
                foreach (var item in Materials)
                {
                    var dMat = item.Value as DiffuseMaterial;
                    var br = dMat?.Brush as SolidColorBrush;
                    if (br != null)
                        br.Opacity = value;
                }
                foreach (var kvp in DefaultMaterials)
                {
                    var dMat = kvp.Value as DiffuseMaterial;
                    var br = dMat?.Brush as SolidColorBrush;
                    if (br == null)
                        continue;
                    br.Opacity = ((string) kvp.Key) != "IfcSpace"
                        ? Math.Max(value, 0)
                        : Math.Max(value*-1, 0);
                }
                _transparency = value;
            }
        }
    }
}