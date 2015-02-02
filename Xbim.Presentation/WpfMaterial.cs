using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.PresentationAppearanceResource;
using Xbim.Ifc2x3.PresentationResource;
using Xbim.ModelGeometry.Scene;

namespace Xbim.Presentation
{
    public class WpfMaterial : IXbimRenderMaterial
    {
        Material Material;
        string _Description;
        public bool IsTransparent;
        
        public static implicit operator Material(WpfMaterial wpfMaterial)
        {
            return wpfMaterial.Material;
        }
       
        public void CreateMaterial(XbimTexture texture)
        {
            
            if (texture.ColourMap.Count > 1)
            {
                Material = new MaterialGroup();
                _Description = "Texture" ; 
                bool transparent = true;
                foreach (var colour in texture.ColourMap)
                {
                    if (!colour.IsTransparent) transparent = false; //only transparent if everything is transparent
                    _Description += " " + colour.ToString();
                    ((MaterialGroup)Material).Children.Add(MaterialFromColour(colour));
                }
                IsTransparent = transparent;
            }
            else if(texture.ColourMap.Count == 1)
            {
                XbimColour colour = texture.ColourMap[0];
                Material = MaterialFromColour(colour);
                _Description = "Texture " + colour.ToString();
                IsTransparent = colour.IsTransparent;
            }
        }

        public void CreateMaterial(XbimColour colour)
        {
            Material = MaterialFromColour(colour);
        }

        private Material MaterialFromColour(XbimColour colour)
        {
            _Description = "Colour " + colour.ToString();
            Color col = Color.FromScRgb(colour.Alpha, colour.Red, colour.Green, colour.Blue);
            
            Brush brush = new SolidColorBrush(col);
            if (colour.SpecularFactor > 0)
                return new SpecularMaterial(brush, colour.SpecularFactor * 100);
            else if (colour.ReflectionFactor > 0)
                return new EmissiveMaterial(brush);
            else
                return new DiffuseMaterial(brush);
            
        }

        public string Description
        {
            get
            {
                return _Description;
            }
        }


        public bool IsCreated
        {
            get { return Material != null; }
        }
    }
}
