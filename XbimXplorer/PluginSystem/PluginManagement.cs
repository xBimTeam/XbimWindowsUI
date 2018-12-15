using Microsoft.Extensions.Logging;
using NuGet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace XbimXplorer.PluginSystem
{
    public enum PluginChannelOption
    {
        Installed,
        Stable,
        Development,
        Versions
    }

    internal class PluginManagement
    {
        private readonly Dictionary<string, PluginInformation> _diskPlugins =
           new Dictionary<string, PluginInformation>();

        public PluginManagement(Microsoft.Extensions.Logging.ILogger logger = null)
        {
            Logger = logger ?? XplorerMainWindow.LoggerFactory.CreateLogger<PluginManagement>();
        }

        protected static Microsoft.Extensions.Logging.ILogger Logger { get; private set; }

        internal static IEnumerable<DirectoryInfo> GetPluginDirectories()
        {
            var di = GetPluginsDirectory();
            return !di.Exists
                ? Enumerable.Empty<DirectoryInfo>()
                : di.GetDirectories();
        }

        internal IEnumerable<PluginInformation> DiskPlugins => _diskPlugins.Values;

        internal void RefreshLocalPlugins()
        {
            _diskPlugins.Clear();
            var dirs = PluginManagement.GetPluginDirectories();
            foreach (var directoryInfo in dirs)
            {
                var pc = new PluginInformation(directoryInfo);
                _diskPlugins.Add(pc.PluginId, pc);
            }
        }

        internal static DirectoryInfo GetPluginsDirectory()
        {
            var path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            if (!string.IsNullOrWhiteSpace(path))
                path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (path == null)
                return null;
            path = Path.Combine(path, "Plugins");
            var di = new DirectoryInfo(path);
            return di;
        }

        internal static ManifestMetadata GetManifestMetadata(DirectoryInfo path)
        {
            var tmp = new ManifestMetadata() { Id = path.Name, Version = "<undetermined>" };
            var file = path.GetFiles("*.manifest").FirstOrDefault();
            if (file == null)
            {
                return tmp;
            }
            using (var stream = file.OpenRead())
            {
                try
                {
                    var rd = Manifest.ReadFrom(stream, false);
                    return rd.Metadata;
                }
                catch (Exception ex)
                {
                    Logger.LogError(0, ex, "Error loading plugin manifest from {path}", path);
                }
            }
            return tmp;
        }

        internal static string SelectedRepoUrl => "https://www.myget.org/F/xbim-plugins/api/v2";

        internal IEnumerable<PluginInformation> GetPlugins(PluginChannelOption option)
        {
            RefreshLocalPlugins();
            var repo = PackageRepositoryFactory.Default.CreateRepository(SelectedRepoUrl);
            var allowDevelop = option != PluginChannelOption.Stable;

            var fnd = repo.Search("XplorerPlugin", allowDevelop);
            foreach (var package in fnd)
            {
                if (option != PluginChannelOption.Versions)
                {
                    if (allowDevelop && !package.IsAbsoluteLatestVersion)
                        continue;
                    if (!allowDevelop && !package.IsLatestVersion)
                        continue;
                }
                var pv = new PluginInformation(package);
                if (_diskPlugins.ContainsKey(package.Id))
                {
                    pv.SetDirectoryInfo(_diskPlugins[package.Id]);
                }
                yield return pv;
            }
        }

        public static string GetEntryFile(DirectoryInfo dir, string filename = null)
        {
            var f = filename == null 
                ? Path.Combine(dir.FullName, dir.Name + ".exe") 
                : Path.Combine(dir.FullName, filename);
            return f;
        }

        internal static string GetStartupFileConfig(DirectoryInfo dir)
        {
            return Path.Combine(dir.FullName, "PluginConfig.xml");
        }

        public static void SetStartup(DirectoryInfo dir, PluginConfiguration.StartupBehaviour behaviour)
        {
            var p = new PluginConfiguration {OnStartup = behaviour};
            p.WriteXml(GetStartupFileConfig(dir));
        }

        public static PluginConfiguration GetConfiguration(DirectoryInfo dir)
        {
            var read = PluginConfiguration.ReadXml(GetStartupFileConfig(dir));
            return read;
        }
    }
}