#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc.Extensions
// Filename:    SurfaceStyleShadingExtensions.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Xbim.Ifc4.Interfaces;

#endregion

namespace Xbim.Presentation
{
    public static class SurfaceStyleShadingExtensions
    {
        public static Material ToMaterial(this IIfcSurfaceStyleShading shading)
        {
            if (shading is IIfcSurfaceStyleRendering)
                return ((IIfcSurfaceStyleRendering) shading).ToMaterial();
            byte red = Convert.ToByte(shading.SurfaceColour.Red*255);
            byte green = Convert.ToByte(shading.SurfaceColour.Green*255);
            byte blue = Convert.ToByte(shading.SurfaceColour.Blue*255);
            Color col = Color.FromRgb(red, green, blue);
            Brush brush = new SolidColorBrush(col);
            Material mat = new DiffuseMaterial(brush);
            return mat;
        }
    }
}