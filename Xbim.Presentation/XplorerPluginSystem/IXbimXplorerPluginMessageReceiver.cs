using System;

namespace Xbim.Presentation.XplorerPluginSystem
{
    [System.Obsolete("The plugin system is in alpha version, it will likely require a substantial redesign.", false)]
    public interface IXbimXplorerPluginMessageReceiver
    {
        void ProcessMessage(object sender, string messageTypeString, object messageData);
    }
}