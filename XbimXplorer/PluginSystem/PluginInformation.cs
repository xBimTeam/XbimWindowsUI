using Microsoft.Extensions.Logging;
using NuGet;
using System;
using System.IO;
using System.Windows;

namespace XbimXplorer.PluginSystem
{
    public class PluginInformation
    {

        protected Microsoft.Extensions.Logging.ILogger Logger { get; private set; }

        public string PluginId { get; set; }

        internal PluginConfiguration Startup { get; set; }
        
        public string AvailableVersion => _onlinePackage?.Version.ToString() ?? "";
        public string InstalledVersion => _diskManifest?.Version ?? "";
        public string LoadedVersion => MainWindow?.GetLoadedVersion(PluginId) ?? "";

        private IPackage _onlinePackage;
        private ManifestMetadata _diskManifest;
        private DirectoryInfo _directory;
        
        public PluginInformation(Microsoft.Extensions.Logging.ILogger logger = null)
        {
            Logger = logger ?? XplorerMainWindow.LoggerFactory.CreateLogger<PluginInformation>();
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
            catch (Exception ex)
            {
                Logger.LogError(0, ex, "Could not create directory {directory}", pluginDirectory.FullName);
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
            catch (Exception ex)
            {
                Logger.LogError(0, ex, "Could not create directory {directory}", subdir.FullName);
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
                    Logger.LogError(0, ex, "Error trying to delete: {destname}", destname);
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
                    Logger.LogError(0, ex, "Error trying to extract: {destname}", destname);
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
                    Logger.LogError("Error trying to create manifest file for {packageName}", packageName);
                    return false;
                }
                SetDirectoryInfo(subdir);
            }
            catch (Exception ex)
            {
                Logger.LogError(0, ex, "Error trying to create manifest file for: {packageName}", packageName);
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
