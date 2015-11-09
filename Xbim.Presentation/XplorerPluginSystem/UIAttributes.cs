using System;

namespace Xbim.Presentation.XplorerPluginSystem
{
    public class XplorerUiElement : Attribute
    {
        public XplorerUiElement(
            PluginWindowUiContainerEnum initialContainer,
            PluginWindowActivation activation,
            string menuText = ""
            )
        {
            InitialContainer = initialContainer;
            Activation = activation;
            MenuText = menuText;
        }

        public string MenuText { get; private set; }
        public PluginWindowUiContainerEnum InitialContainer { get; private set; }
        public PluginWindowActivation Activation { get; private set; }
    }
}
