using System.Windows.Media;
using System.Windows.Media.Media3D;
using Xbim.Ifc;
using Xbim.ModelGeometry.Scene;

namespace Xbim.Presentation
{
    public class WpfMaterial : IXbimRenderMaterial
    {
        Material _material;
        string _description;
        public bool IsTransparent;
        
        public static implicit operator Material(WpfMaterial wpfMaterial)
        {
            return wpfMaterial._material;
        }
       
        public void CreateMaterial(XbimTexture texture)
        {
            
            if (texture.ColourMap.Count > 1)
            {
                _material = new MaterialGroup();
                _description = "Texture" ; 
                bool transparent = true;
                foreach (var colour in texture.ColourMap)
                {
                    if (!colour.IsTransparent) transparent = false; //only transparent if everything is transparent
                    _description += " " + colour;
                    ((MaterialGroup)_material).Children.Add(MaterialFromColour(colour));
                }
                IsTransparent = transparent;
            }
            else if(texture.ColourMap.Count == 1)
            {
                XbimColour colour = texture.ColourMap[0];
                _material = MaterialFromColour(colour);
                _description = "Texture " + colour;
                IsTransparent = colour.IsTransparent;
            }
            _material.Freeze();
        }

        public void CreateMaterial(XbimColour colour)
        {
            _material = MaterialFromColour(colour);
            _material.Freeze();
        }

        private Material MaterialFromColour(XbimColour colour)
        {
            _description = "Colour " + colour;
            Color col = Color.FromScRgb(colour.Alpha, colour.Red, colour.Green, colour.Blue);
            
            Brush brush = new SolidColorBrush(col);
            if (colour.SpecularFactor > 0)
                return new SpecularMaterial(brush, colour.SpecularFactor * 100);
            if (colour.ReflectionFactor > 0)
                return new EmissiveMaterial(brush);
            return new DiffuseMaterial(brush);
        }

        public string Description
        {
            get
            {
                return _description;
            }
        }


        public bool IsCreated
        {
            get { return _material != null; }
        }
    }
}
