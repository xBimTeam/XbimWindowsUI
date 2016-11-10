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
            if (provider != _currentProvider || location != _currentLocation)
            {
                _timer.Stop();
                _currentProvider = provider;
                _currentLocation = location;
                if (_tip.IsOpen) Hide();
                _timer.Start();
            }
            else if (_tip.IsOpen)
            {
                Vector delta = Mouse.GetPosition(null) - _initialPosition;
                _tip.VerticalOffset = delta.Y;
                _tip.HorizontalOffset = delta.X;
            }
        }

        public static void Hide()
        {
            _timer.Stop();
            _tip.IsOpen = false;
        }

        static ToolTipController()
        {
            _timer.Interval = new TimeSpan(ToolTipService.GetInitialShowDelay(_tip)*10000);
            _timer.Tick += timer_Tick;
        }

        private static void timer_Tick(object sender, EventArgs e)
        {
            _timer.Stop();
            OnShowToolTip();
        }

        private static void OnShowToolTip()
        {
            if (_currentProvider != null)
            {
                _tip.VerticalOffset = 0;
                _tip.HorizontalOffset = 0;
                _tip.Content = _currentProvider(_currentLocation);
                _tip.IsOpen = _tip.Content != null;

                _initialPosition = Mouse.GetPosition(null);
            }
        }

        private static ToolTipContentProviderDelegate _currentProvider;
        private static Point _currentLocation;
        private static readonly ToolTip _tip = new ToolTip();
        private static Point _initialPosition;
        private static readonly DispatcherTimer _timer = new DispatcherTimer();
    }

    /// <summary>
    ///   returns the content of a ToolTip displayed for object tag
    /// </summary>
    public delegate object ToolTipContentProviderDelegate(Point location);
}