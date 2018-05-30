using System;

namespace Xbim.Presentation.XplorerPluginSystem
{
    public class XplorerUiElement : Attribute
    {
        public XplorerUiElement(
            PluginWindowUiContainerEnum initialContainer,
            PluginWindowActivation activation,
            string menuText = "",
            string iconPath = ""
            )
        {
            InitialContainer = initialContainer;
            Activation = activation;
            MenuText = menuText;
            IconPath = iconPath;
        }

        private PluginWindowCloseAction? _closeAction;

        public PluginWindowCloseAction CloseAction
        {
            get
            {
                if (_closeAction == null)
                {
                    return Activation==PluginWindowActivation.OnLoad 
                        ? PluginWindowCloseAction.Hide 
                        : PluginWindowCloseAction.Close;
                }
                return _closeAction.Value;
            }
            set { _closeAction = value; }
        }

        public string MenuText { get; private set; }
        public string IconPath { get; private set; }
        public PluginWindowUiContainerEnum InitialContainer { get; private set; }
        public PluginWindowActivation Activation { get; }

        
    }
}
