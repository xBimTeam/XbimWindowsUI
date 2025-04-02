using Microsoft.Extensions.Logging;
using NuGet.Packaging;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace XbimXplorer.PluginSystem
{
    public class PluginInformation
    {
        protected Microsoft.Extensions.Logging.ILogger Logger { get; private set; }

        public string PluginId { get; set; }

        internal PluginConfiguration Config { get; set; }
        
        public NuGetVersion AvailableVersion => _onlinePackage?.Identity.Version;
        public NuGetVersion InstalledVersion => _diskManifest?.Version;
        public string LoadedVersion => MainWindow?.GetLoadedVersion(PluginId) ?? "";

        private IPackageSearchMetadata _onlinePackage;
		private PackageArchiveReader _localPackage;
        private ManifestMetadata _diskManifest;
        private DirectoryInfo _directory;
        
        public PluginInformation(PluginManagement manager)
        {
            Logger = XplorerMainWindow.LoggerFactory.CreateLogger<PluginInformation>();
			Manager = manager;
		}

        public PluginInformation(DirectoryInfo directoryInfo, PluginManagement manager) : this(manager)
        {
            SetDirectoryInfo(directoryInfo);
        }

        public PluginInformation(IPackageSearchMetadata p, PluginManagement manager) : this(manager)
        {
            SetPackage(p);
        }

		public PluginInformation(PackageArchiveReader reader, PluginManagement manager): this(manager)
		{
			SetPackage(reader);
		}

        internal void SetDirectoryInfo(PluginInformation otherConfiguration)
        {
            SetDirectoryInfo(otherConfiguration._directory);
        }

        internal void SetDirectoryInfo(DirectoryInfo directoryInfo)
        {
            _directory = directoryInfo;
            if (directoryInfo != null)
                SetDiskManifest(PluginManagement.GetManifestMetadata(directoryInfo));
            else
                SetDiskManifest(null);
            Config = PluginManagement.GetConfiguration(directoryInfo) ?? new PluginConfiguration();
        }

        public XplorerMainWindow MainWindow => Application.Current.MainWindow as XplorerMainWindow;

        public ManifestMetadata Manifest => _diskManifest;

		public PluginManagement Manager { get; }

		internal void DeleteFromDisk()
        {
            _directory.Delete(true);
            _directory = null;
            SetDirectoryInfo(_directory);
        }

		public void SetPackage(PackageArchiveReader reader)
		{
			_onlinePackage = null;
			_localPackage = reader;
			if (string.IsNullOrEmpty(PluginId))
			{
				PluginId = reader.GetIdentity().Id;
			}
		}

		public void SetPackage(IPackageSearchMetadata package)
        {
            _onlinePackage = package;
			_localPackage = null;
            if (string.IsNullOrEmpty(PluginId))
            {
                PluginId = package.Identity.Id;
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


		public bool EnsurePluginDirectoryExists(DirectoryInfo pluginDirectory)
		{
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
			var subdir = GetPluginDirectory(pluginDirectory);
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

			return true;
		}

		private DirectoryInfo GetPluginDirectory(DirectoryInfo pluginHome)
		{
			return new DirectoryInfo(Path.Combine(Path.Combine(pluginHome.FullName, PluginId)));
		}

        /// <summary>
        /// Extract files and creates manifest
        /// </summary>
        /// <param name="pluginDirectory">Destination folder, a subdir will be created.</param>
        /// <returns>false on error</returns>
        public async Task<bool> ExtractPlugin(DirectoryInfo pluginDirectory = null)
		{
			pluginDirectory ??= PluginManagement.GetPluginsDirectory();

			using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
			var token = cts.Token;

			// ensure top level plugin directory exists
			if (EnsurePluginDirectoryExists(pluginDirectory) == false)
				return false;

			// now extract files
			// 
			var pluginDir = GetPluginDirectory(pluginDirectory);
			if(_localPackage != null)
			{
				return await ExtractPackageContents(_localPackage, pluginDir, token);
			}
			else if (_onlinePackage != null)
			{
				using MemoryStream packageStream = new MemoryStream();
				using var packageReader = await Manager.DownloadPluginAsync(_onlinePackage, packageStream, token);
				return await ExtractPackageContents(packageReader, pluginDir, token);
			}
			else
			{
				throw new InvalidOperationException("Cannot extract Plugin: No package source was specified");
			}
		}

		private async Task<bool> ExtractPackageContents(PackageArchiveReader packageReader, DirectoryInfo targetDir, CancellationToken token)
		{
			var frameworkReference = (await packageReader.GetReferenceItemsAsync(token)).FirstOrDefault();

			foreach (var fileName in frameworkReference.Items)
			{
				// TODO: Currently we flatten lib files. If we want to multi-target we should preserve the lib folder structure for net48, net6 etc
				var localPath = Path.GetFileName(fileName);
				var destname = Path.Combine(targetDir.FullName, localPath);
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
					using var fileStream = File.Create(destname);
					var contentStream = await packageReader.GetStreamAsync(fileName, token);

					contentStream.CopyTo(fileStream);
				}
				catch (Exception ex)
				{
					Logger.LogError(0, ex, "Error trying to extract: {destname}", destname);
					return false;
				}
			}


			// store manifest information to disk
			// 
			var packageName = Path.Combine(targetDir.FullName, $"{PluginId}.manifest");
			try
			{

				var nuspecManifest = await packageReader.GetNuspecFileAsync(token);

				using (var fileStream = File.Create(packageName))
				{
					var contentStream = await packageReader.GetStreamAsync(nuspecManifest, token);

					contentStream.CopyTo(fileStream);
				}
				SetDirectoryInfo(targetDir);
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
            if (Config == null)
                return;
            Config.ToggleEnabled();
            if (_directory != null)
                Config.WriteXml(PluginManagement.GetStartupFileConfig(_directory));
        }
    }
}
