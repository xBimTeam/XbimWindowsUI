#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Presentation
// Filename:    ToolTipController.cs
// Published:   01, 2012
// Last Edited: 9:05 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

#endregion

namespace Xbim.Presentation
{
    internal static class ToolTipController
    {
        public static void Move(ToolTipContentProviderDelegate provider, Point location)
        {
            if (provider != current_provider || location != current_location)
            {
                timer.Stop();
                current_provider = provider;
                current_location = location;
                if (tip.IsOpen) Hide();
                timer.Start();
            }
            else if (tip.IsOpen)
            {
                Vector delta = Mouse.GetPosition(null) - initial_position;
                tip.VerticalOffset = delta.Y;
                tip.HorizontalOffset = delta.X;
            }
        }

        public static void Hide()
        {
            timer.Stop();
            tip.IsOpen = false;
        }

        static ToolTipController()
        {
            timer.Interval = new TimeSpan(ToolTipService.GetInitialShowDelay(tip)*10000);
            timer.Tick += new EventHandler(timer_Tick);
        }

        private static void timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();
            OnShowToolTip();
        }

        private static void OnShowToolTip()
        {
            if (current_provider != null)
            {
                tip.VerticalOffset = 0;
                tip.HorizontalOffset = 0;
                tip.Content = current_provider(current_location);
                tip.IsOpen = tip.Content != null;

                initial_position = Mouse.GetPosition(null);
            }
        }

        private static ToolTipContentProviderDelegate current_provider;
        private static Point current_location;
        private static ToolTip tip = new ToolTip();
        private static Point initial_position;
        private static DispatcherTimer timer = new DispatcherTimer();
    }

    /// <summary>
    ///   returns the content of a ToolTip displayed for object tag
    /// </summary>
    public delegate object ToolTipContentProviderDelegate(Point location);
}