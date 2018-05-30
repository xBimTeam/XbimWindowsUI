using System;
using System.Text;
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

        // empty constructor
        public WpfMaterial()
        {
        }
        
        public WpfMaterial(XbimColour colour)
        {
            _material = MaterialFromColour(colour);
            _description = "Colour " + colour;
            IsTransparent = colour.IsTransparent;
        }

        public static implicit operator Material(WpfMaterial wpfMaterial)
        {
            return wpfMaterial._material;
        }
       
        public void CreateMaterial(XbimTexture texture)
        {            
            if (texture.ColourMap.Count > 1)
            {
                _material = new MaterialGroup();
                var descBuilder = new StringBuilder();
                descBuilder.Append("Texture");
                
                var transparent = true;
                foreach (var colour in texture.ColourMap)
                {
                    if (!colour.IsTransparent)
                        transparent = false; //only transparent if everything is transparent
                    descBuilder.AppendFormat(" {0}", colour);
                    ((MaterialGroup)_material).Children.Add(MaterialFromColour(colour));
                }
                _description = descBuilder.ToString();
                IsTransparent = transparent;
            }
            else if(texture.ColourMap.Count == 1)
            {
                var colour = texture.ColourMap[0];
                _material = MaterialFromColour(colour);
                _description = "Texture " + colour;
                IsTransparent = colour.IsTransparent;
            }
            _material.Freeze();
        }

        /// <summary>
        /// Obsolete, please use constructor instead. 17 May 2017
        /// </summary>
        /// <param name="colour"></param>
        [Obsolete]
        public void CreateMaterial(XbimColour colour)
        {
            _material = MaterialFromColour(colour);
            _description = "Colour " + colour;
            IsTransparent = colour.IsTransparent;
        }

        private static Material MaterialFromColour(XbimColour colour)
        {
            var col = Color.FromScRgb(colour.Alpha, colour.Red, colour.Green, colour.Blue);
            Brush brush = new SolidColorBrush(col);

            // build material
            Material mat;
            if (colour.SpecularFactor > 0)
                mat = new SpecularMaterial(brush, colour.SpecularFactor * 100);
            else if (colour.ReflectionFactor > 0)
                mat = new  EmissiveMaterial(brush);
            else
                mat = new DiffuseMaterial(brush);

            // freeze and return
            mat.Freeze();
            return mat;
        }

        public string Description => _description;
        
        public bool IsCreated => _material != null;
    }
}
