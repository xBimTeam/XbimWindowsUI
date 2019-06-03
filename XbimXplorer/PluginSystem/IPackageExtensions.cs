using System.IO;
using NuGet;
using Ionic.Zip;

namespace XbimXplorer.PluginSystem
{
    internal static class PackageExtensions
    {
        /// <summary>
        /// Extract the manifest file to the specified file name
        /// </summary>
        /// <param name="package"></param>
        /// <param name="targetFileName"></param>
        /// <returns>true if successful, false if fail.</returns>
        internal static bool ExtractManifestFile(this IPackage package, string targetFileName)
        {

            using (var fs = package.GetStream())
            {

                using (var zf = ZipFile.Read(fs))
                {
                    foreach (ZipEntry zipEntry in zf)
                    {
                        if (zipEntry.IsDirectory)
                        {
                            continue; // Ignore directories
                        }
                        var entryFileName = zipEntry.FileName;
                        var extension = Path.GetExtension(entryFileName);
                        if (extension == null || extension.ToLowerInvariant() != ".nuspec")
                            continue;

                        zipEntry.FileName = Path.GetFileName(targetFileName);
                        zipEntry.Extract(Path.GetDirectoryName(targetFileName), ExtractExistingFileAction.OverwriteSilently);

                        return true;
                    }
                }
            }

            return false;
        }
    }
}
