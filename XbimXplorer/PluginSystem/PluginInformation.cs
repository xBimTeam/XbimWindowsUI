using System;
using System.IO;
using System.Linq;
using System.Windows;
using log4net;
using NuGet;

namespace XbimXplorer.PluginSystem
{
    public class PluginInformation
    {
        private static readonly ILog Log = LogManager.GetLogger("XbimXplorer.PluginSystem.PluginConfiguration");
       
        public string PluginId { get; set; }

        internal PluginConfiguration Startup { get; set; }
        
        public string AvailableVersion => _onlinePackage?.Version.ToString() ?? "";
        public string InstalledVersion => _diskManifest?.Version ?? "";
        public string LoadedVersion => MainWindow?.GetLoadedVersion(PluginId) ?? "";

        private IPackage _onlinePackage;
        private ManifestMetadata _diskManifest;
        private DirectoryInfo _directory;
        
        public PluginInformation()
        {
            
        }

        public PluginInformation(DirectoryInfo directoryInfo)
        {
            SetDirectoryInfo(directoryInfo);
        }

        public PluginInformation(IPackage p)
        {
            SetPackage(p);
        }

        internal void SetDirectoryInfo(PluginInformation otherConfiguration)
        {
            SetDirectoryInfo(otherConfiguration._directory);
        }

        internal void SetDirectoryInfo(DirectoryInfo directoryInfo)
        {
            _directory = directoryInfo;
            SetDiskManifest(PluginManagement.GetManifestMetadata(directoryInfo));
            Startup = PluginManagement.GetConfiguration(directoryInfo) ?? new PluginConfiguration();
        }

        public XplorerMainWindow MainWindow => Application.Current.MainWindow as XplorerMainWindow;

        public ManifestMetadata Manifest => _diskManifest;

        public void SetPackage(IPackage package)
        {
            _onlinePackage = package;
            if (string.IsNullOrEmpty(PluginId))
            {
                PluginId = package.Id;
            }
        }

        private void SetDiskManifest(ManifestMetadata manifest)
        {
            _diskManifest = manifest;
            if (string.IsNullOrEmpty(PluginId))
            {
                PluginId = manifest.Id;
            }
        }

        /// <summary>
        /// Extract files and creates manifest
        /// </summary>
        /// <param name="pluginDirectory">Destination folder, a subdir will be created.</param>
        /// <returns>false on error</returns>
        public bool ExtractPlugin(DirectoryInfo pluginDirectory)
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
                return false;
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
                return false;
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
                    return false;
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
                    return false;
                }
            }

            // store manifest information to disk
            // 
            var packageName = Path.Combine(subdir.FullName, $"{_onlinePackage.Id}.manifest");
            try
            {
                if (!_onlinePackage.ExtractManifestFile(packageName))
                {
                    Log.Error($"Error trying to create manifest file for {packageName}");
                    return false;
                }
                SetDirectoryInfo(subdir);
            }
            catch (Exception ex)
            {
                Log.Error($"Error trying to create manifest file for: {packageName}", ex);
                return false;
            }
            return true;
        }

        /// <returns>True if plugin is completely loaded. False if not, for any reason.</returns>
        public bool Load()
        {
            return _directory != null && MainWindow.LoadPlugin(_directory, true);
        }

        public void ToggleEnabled()
        {
            if (Startup == null)
                return;
            Startup.ToggleEnabled();
            if (_directory != null)
                Startup.WriteXml(PluginManagement.GetStartupFileConfig(_directory));
        }
    }
}
