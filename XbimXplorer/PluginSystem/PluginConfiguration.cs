using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using log4net;
using NuGet;

namespace XbimXplorer.PluginSystem
{
    public class PluginConfiguration
    {
        private static readonly ILog Log = LogManager.GetLogger("XbimXplorer.PluginSystem.PluginConfiguration");

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

        public string PluginId { get; set; }

        internal AssemblyAvailability RuntimeAvailability { get; set; }

        internal StartupBehaviour StartupStatus { get; set; }

        internal SemanticVersion OnDiskVersion;

        internal SemanticVersion OnLineVersion;

        private IPackage _onlinePackage;

        public void setOnlinePackage(IPackage package)
        {
            _onlinePackage = package;
        }

        public void ExtractLibs(DirectoryInfo pluginDirectory)
        {
            // ensure top leved plugin directory exists
            try
            {
                if (!pluginDirectory.Exists)
                    pluginDirectory.Create();
            }
            catch (Exception)
            {
                Log.Error($"Could not create directory {pluginDirectory.FullName}");
                return;
            }

            // ensure specific plugin directory exists
            //
            var subdir = new DirectoryInfo(Path.Combine(pluginDirectory.FullName, PluginId));
            try
            {
                if (!subdir.Exists)
                    subdir.Create();
            }
            catch (Exception)
            {
                Log.Error($"Could not create directory {subdir.FullName}");
                return;
            }

            // now extract files

            foreach (var file in _onlinePackage.GetLibFiles())
            {
                var destname = Path.Combine(subdir.FullName, file.EffectivePath);
                try
                {

                    if (File.Exists(destname))
                        File.Delete(destname);
                }
                catch (Exception ex)
                {
                    Log.Error($"Error trying to delete: {destname}", ex);
                    return;
                }

                try
                {
                    using (var fileStream = File.Create(destname))
                    {
                        file.GetStream().Seek(0, SeekOrigin.Begin);
                        file.GetStream().CopyTo(fileStream);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Error trying to delete: {destname}", ex);
                    return;
                }
            }
        }
    }
}
