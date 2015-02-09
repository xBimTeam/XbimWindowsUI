using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Xbim.Presentation;
using Xbim.XbimExtensions.Interfaces;

namespace XbimXplorer.PluginSystem
{
    public enum PluginWindowDefaultUIContainerEnum
    {
        LayoutDoc,
        LayoutAnchorable
    }

    public enum PluginWindowDefaultUIShow
    {
        onMenu,
        onLoad
    }

    public interface xBimXplorerPluginWindow 
    {
        string MenuText { get; }
        string WindowTitle { get; }
        void BindUI(XplorerMainWindow mainWindow);
        PluginWindowDefaultUIContainerEnum DefaultUIContainer { get; }
        PluginWindowDefaultUIShow DefaultUIActivation { get; }
    }
}
