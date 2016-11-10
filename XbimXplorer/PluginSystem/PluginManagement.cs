using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using log4net;
using NuGet;

namespace XbimXplorer.PluginSystem
{
    internal static class PluginManagement
    {
        private static readonly ILog Log = LogManager.GetLogger("XbimXplorer.PluginSystem.PluginManagement");

        internal static IEnumerable<DirectoryInfo> GetPluginDirectories()
        {
            var di = GetPluginsDirectory();
            return !di.Exists
                ? Enumerable.Empty<DirectoryInfo>()
                : di.GetDirectories();
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
                    Log.Error($"Error loading plugin manifest from [{path}]", ex);
                }
            }
            return tmp;
        }

        public static string GetEntryFile(DirectoryInfo dir, string filename = null)
        {
            var f = filename == null 
                ? Path.Combine(dir.FullName, dir.Name + ".exe") 
                : Path.Combine(dir.FullName, filename);
            return f;
        }

        // ReSharper disable once SuggestBaseTypeForParameter
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