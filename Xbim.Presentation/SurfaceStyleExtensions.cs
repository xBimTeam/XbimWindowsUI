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
using Xbim.Ifc4.Interfaces;

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
        public static Material ToMaterial(this IIfcSurfaceStyle sStyle)
        {
            // todo: need to change this to return a material group that considers all types of Styles
            var rendering = sStyle.Styles.OfType<IIfcSurfaceStyleRendering>().FirstOrDefault();
            if (rendering != null) 
                return rendering.ToMaterial();
            var shading = sStyle.Styles.OfType<IIfcSurfaceStyleShading>().FirstOrDefault();
            return (shading != null) 
                ? shading.ToMaterial() 
                : null;
        }

        public static IIfcSurfaceStyleShading GetSurfaceStyleShading(this IIfcSurfaceStyle sStyle)
        {
            return sStyle.Styles.OfType<IIfcSurfaceStyleShading>().FirstOrDefault();
        }

        public static IIfcSurfaceStyleRendering GetSurfaceStyleRendering(this IIfcSurfaceStyle sStyle)
        {
            return sStyle.Styles.OfType<IIfcSurfaceStyleRendering>().FirstOrDefault();
        }

        public static IIfcSurfaceStyleLighting GetSurfaceStyleLighting(this IIfcSurfaceStyle sStyle)
        {
            return sStyle.Styles.OfType<IIfcSurfaceStyleLighting>().FirstOrDefault();
        }

        public static IIfcSurfaceStyleRefraction GetSurfaceStyleRefraction(this IIfcSurfaceStyle sStyle)
        {
            return sStyle.Styles.OfType<IIfcSurfaceStyleRefraction>().FirstOrDefault();
        }

        public static IIfcSurfaceStyleWithTextures GetSurfaceStyleWithTextures(this IIfcSurfaceStyle sStyle)
        {
            return sStyle.Styles.OfType<IIfcSurfaceStyleWithTextures>().FirstOrDefault();
        }

        public static IIfcExternallyDefinedSurfaceStyle GetExternallyDefinedSurfaceStyle(this IIfcSurfaceStyle sStyle)
        {
            return sStyle.Styles.OfType<IIfcExternallyDefinedSurfaceStyle>().FirstOrDefault();
        }
    }
}