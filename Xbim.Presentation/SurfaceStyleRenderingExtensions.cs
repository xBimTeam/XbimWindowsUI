#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc.Extensions
// Filename:    SurfaceStyleRenderingExtensions.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System.Windows.Media;
using System.Windows.Media.Media3D;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.MeasureResource;

#endregion

namespace Xbim.Presentation
{
    public static class SurfaceStyleRenderingExtensions
    {
        public static Material ToMaterial(this IIfcSurfaceStyleRendering r)
        {

            MaterialGroup grp = new MaterialGroup();
            if (r.DiffuseColour is IfcNormalisedRatioMeasure)
            {
                Brush brush = new SolidColorBrush(r.SurfaceColour.ToColor((IfcNormalisedRatioMeasure)r.DiffuseColour));
                brush.Opacity = r.Transparency.HasValue ? 1.0 - r.Transparency.Value : 1.0;
                grp.Children.Add(new DiffuseMaterial(brush));
            }
            else if (r.DiffuseColour is IIfcColourRgb)
            {
                Brush brush = new SolidColorBrush(((IIfcColourRgb)r.DiffuseColour).ToColor());
                brush.Opacity = r.Transparency.HasValue ? 1.0 - r.Transparency.Value : 1.0;
                grp.Children.Add(new DiffuseMaterial(brush));
            }
            else if (r.DiffuseColour == null)
            {
                Brush brush = new SolidColorBrush(r.SurfaceColour.ToColor());
                brush.Opacity = r.Transparency.HasValue ? 1.0 - r.Transparency.Value : 1.0;
                grp.Children.Add(new DiffuseMaterial(brush));
            }

            if (r.SpecularColour is IfcNormalisedRatioMeasure)
            {
                Brush brush = new SolidColorBrush(r.SurfaceColour.ToColor());
                brush.Opacity = r.Transparency.HasValue ? 1.0 - r.Transparency.Value : 1.0;
                grp.Children.Add(new SpecularMaterial(brush, (IfcNormalisedRatioMeasure)(r.SpecularColour)));
            }
            if (r.SpecularColour is IIfcColourRgb)
            {
                Brush brush = new SolidColorBrush(((IIfcColourRgb)r.SpecularColour).ToColor());
                brush.Opacity = r.Transparency.HasValue ? 1.0 - r.Transparency.Value : 1.0;
                grp.Children.Add(new SpecularMaterial(brush, 100.0));
            }

            if (grp.Children.Count == 1)
            {
                Material mat = grp.Children[0];
                return mat;
            }
            return grp;
        }
    }
}