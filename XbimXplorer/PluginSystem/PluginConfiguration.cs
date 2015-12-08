using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NuGet;

namespace XbimXplorer.PluginSystem
{
    internal class PluginConfiguration
    {
        internal enum AssemblyAvailability
        {
            OnLine,
            OnDisk,
            InMemory
        }

        internal enum StartupBehaviour
        {
            Ignore,
            Load
        }
        
        internal string PluginId { get; set; }

        internal AssemblyAvailability RuntimeAvailability { get; set; }

        internal StartupBehaviour StartupStatus { get; set; }

        internal SemanticVersion OnDiskVersion;

        internal SemanticVersion OnLineVersion;

        private IPackage _onlinePackage;

        public void setOnlinePackage(IPackage package)
        {
            _onlinePackage = package;
        }
    }
}
