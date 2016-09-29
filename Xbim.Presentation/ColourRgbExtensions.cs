#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc.Extensions
// Filename:    ColourRgbExtensions.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Windows.Media;
using Xbim.Ifc4.Interfaces;

#endregion

namespace Xbim.Presentation
{
    public static class ColourRgbExtensions
    {
        /// <summary>
        ///   Converts a ColourRgb to a Windows Color
        /// </summary>
        /// <param name = "rgb"></param>
        /// <returns></returns>
        public static Color ToColor(this IIfcColourRgb rgb)
        {
            byte red = Convert.ToByte(rgb.Red*255);
            byte green = Convert.ToByte(rgb.Green*255);
            byte blue = Convert.ToByte(rgb.Blue*255);
            return Color.FromRgb(red, green, blue);
        }

        /// <summary>
        ///   Converts to a Windows Color and applies the factor to each component
        /// </summary>
        /// <param name = "rgb"></param>
        /// <param name = "factor"></param>
        /// <returns></returns>
        public static Color ToColor(this IIfcColourRgb rgb, double factor)
        {
            byte red = Convert.ToByte(rgb.Red*255*factor);
            byte green = Convert.ToByte(rgb.Green*255*factor);
            byte blue = Convert.ToByte(rgb.Blue*255*factor);
            return Color.FromRgb(red, green, blue);
        }
    }
}