using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XbimXplorer.PluginSystem
{
    public interface IxBimXplorerPluginWindowMessaging
    {
        void ProcessMessage(object Sender, string MessageTypeString, object MessageData);
    }
}
