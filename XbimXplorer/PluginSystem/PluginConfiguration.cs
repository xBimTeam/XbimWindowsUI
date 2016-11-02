using System;
using System.IO;
using System.Linq;
using log4net;
using NuGet;

namespace XbimXplorer.PluginSystem
{
    public class PluginConfiguration
    {
        private static readonly ILog Log = LogManager.GetLogger("XbimXplorer.PluginSystem.PluginConfiguration");

        internal static ManifestMetadata GetManifestMetadata(DirectoryInfo path)
        {
            var file = path.GetFiles("*.manifest").FirstOrDefault();
            if (file == null)
                return null;
            
            using (var stream = file.OpenRead())
            {
                var rd = Manifest.ReadFrom(stream, false);
                return rd.Metadata;
            }
        }

        internal enum StartupBehaviour
        {
            Ignore,
            Load
        }

        public string PluginId { get; set; }

        internal StartupBehaviour StartupStatus { get; set; }
        
        public string AvailableVersion => _onlinePackage?.Version.ToString() ?? "";
        public string InstalledVersion => _diskManifest?.Version ?? "";

        private IPackage _onlinePackage;
        private ManifestMetadata _diskManifest;

        public void SetOnlinePackage(IPackage package)
        {
            _onlinePackage = package;
        }

        public void SetDiskManifest(ManifestMetadata manifest)
        {
            _diskManifest = manifest;
        }

        public void ExtractPlugin(DirectoryInfo pluginDirectory)
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
            // 
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
                    Log.Error($"Error trying to extract: {destname}", ex);
                    return;
                }
            }

            // store manifest information to disk
            // 
            var packageName = Path.Combine(subdir.FullName, $"{_onlinePackage.Id}.manifest");
            try
            {
                if (_onlinePackage.ExtractManifestFile(packageName))
                    return;
                Log.Error($"Error trying to create manifest file for {packageName}");                
            }
            catch (Exception ex)
            {
                Log.Error($"Error trying to create manifest file for: {packageName}", ex);
            }
        }
    }
}
