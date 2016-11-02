using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace XbimXplorer.PluginSystem
{
    internal static class PluginManagement
    {
        internal static IEnumerable<DirectoryInfo> GetPluginDirectories()
        {
            var di = GetPluginDirectory();
            return !di.Exists
                ? null
                : di.GetDirectories();
        }

        internal static DirectoryInfo GetPluginDirectory()
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
    }
}