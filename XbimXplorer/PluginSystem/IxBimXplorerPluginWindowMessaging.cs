using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XbimXplorer.PluginSystem
{
    public interface IxBimXplorerPluginWindowMessaging
    {
        void ProcessMessage(object sender, string messageTypeString, object messageData);
    }
}
