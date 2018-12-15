using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reflection;
using Xbim.Presentation.XplorerPluginSystem;

namespace XbimXplorer.PluginSystem
{
    internal static class XbimXplorerPluginWindowExtensions
    {

        internal static PluginWindowUiContainerEnum GetUiContainerMode(this Type pluginType)
        {
            var attribute = pluginType.GetUiAttribute();
            return DefaultContainer(attribute);
        }

        internal static PluginWindowActivation GetUiActivation(this Type pluginType)
        {
            var attribute = pluginType.GetUiAttribute();
            return DefaultActivation(attribute);
        }

        internal static PluginWindowUiContainerEnum GetUiContainerMode(this IXbimXplorerPluginWindow pluginWindow)
        {
            var attribute = pluginWindow.GetUiAttribute();
            return DefaultContainer(attribute);
        }
        
        internal static PluginWindowActivation GetUiActivation(this IXbimXplorerPluginWindow pluginWindow)
        {
            var attribute = pluginWindow.GetUiAttribute();
            return DefaultActivation(attribute);
        }

        internal static XplorerUiElement GetUiAttribute(this Type pluginWindowType)
        {
            MemberInfo info = pluginWindowType;
            return GetUiAttribute(info);
        }

        internal static PluginWindowCloseAction GetUiCloseAction(this IXbimXplorerPluginWindow pluginWindow)
        {
            var attribute = pluginWindow.GetUiAttribute();
            return DefaultUiCloseAction(attribute);
        }
        
        internal static XplorerUiElement GetUiAttribute(this IXbimXplorerPluginWindow pluginWindow)
        {
            return GetUiAttribute(pluginWindow.GetType());
        }

        private static XplorerUiElement GetUiAttribute(MemberInfo info)
        {
            var attribute = info.GetCustomAttributes(true).OfType<XplorerUiElement>().FirstOrDefault();
            if (attribute == null)
            {
                var log = XplorerMainWindow.LoggerFactory.CreateLogger(nameof(XbimXplorerPluginWindowExtensions));
                log.LogInformation("XplorerUiElement attribute is null on type: {attributeName}", info.Name);
            }
            return attribute;
        }
        
        private static PluginWindowUiContainerEnum DefaultContainer(XplorerUiElement attribute)
        {
            var useContainer = attribute != null
                ? attribute.InitialContainer
                : PluginWindowUiContainerEnum.LayoutDoc;
            return useContainer;
        }

        private static PluginWindowActivation DefaultActivation(XplorerUiElement attribute)
        {
            var useContainer = attribute != null
                ? attribute.Activation
                : PluginWindowActivation.OnLoad;
            return useContainer;
        }

        private static PluginWindowCloseAction DefaultUiCloseAction(XplorerUiElement attribute)
        {
            var useContainer = attribute != null
                ? attribute.CloseAction
                : PluginWindowCloseAction.Close;
            return useContainer;
        }
    }
}
