#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc.Extensions
// Filename:    SurfaceStyleExtensions.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System.Linq;
using System.Windows.Media.Media3D;
using Xbim.Ifc2x3.PresentationAppearanceResource;

#endregion

namespace Xbim.Presentation
{
    public static class SurfaceStyleExtensions
    {
        /// <summary>
        ///   Returns a material corresponding to this surface style, materials are cached in the ModelDataProvider
        /// </summary>
        /// <param name = "sStyle"></param>
        /// <returns></returns>
        public static Material ToMaterial(this IfcSurfaceStyle sStyle)
        {
            // todo: need to change this to return a material group that considers all types of Styles
            var rendering = sStyle.Styles.OfType<IfcSurfaceStyleRendering>().FirstOrDefault();
            if (rendering != null) 
                return rendering.ToMaterial();
            var shading = sStyle.Styles.OfType<IfcSurfaceStyleShading>().FirstOrDefault();
            return (shading != null) 
                ? shading.ToMaterial() 
                : null;
        }

        public static IfcSurfaceStyleShading GetSurfaceStyleShading(this IfcSurfaceStyle sStyle)
        {
            return sStyle.Styles.OfType<IfcSurfaceStyleShading>().FirstOrDefault();
        }

        public static IfcSurfaceStyleRendering GetSurfaceStyleRendering(this IfcSurfaceStyle sStyle)
        {
            return sStyle.Styles.OfType<IfcSurfaceStyleRendering>().FirstOrDefault();
        }

        public static IfcSurfaceStyleLighting GetSurfaceStyleLighting(this IfcSurfaceStyle sStyle)
        {
            return sStyle.Styles.OfType<IfcSurfaceStyleLighting>().FirstOrDefault();
        }

        public static IfcSurfaceStyleRefraction GetSurfaceStyleRefraction(this IfcSurfaceStyle sStyle)
        {
            return sStyle.Styles.OfType<IfcSurfaceStyleRefraction>().FirstOrDefault();
        }

        public static IfcSurfaceStyleWithTextures GetSurfaceStyleWithTextures(this IfcSurfaceStyle sStyle)
        {
            return sStyle.Styles.OfType<IfcSurfaceStyleWithTextures>().FirstOrDefault();
        }

        public static IfcExternallyDefinedSurfaceStyle GetExternallyDefinedSurfaceStyle(this IfcSurfaceStyle sStyle)
        {
            return sStyle.Styles.OfType<IfcExternallyDefinedSurfaceStyle>().FirstOrDefault();
        }
    }
}