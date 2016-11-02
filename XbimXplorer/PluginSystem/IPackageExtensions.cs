using System.IO;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using NuGet;

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
            ZipFile zf = null;
            try
            {
                using (var fs = package.GetStream())
                {
                    zf = new ZipFile(fs);    
                    foreach (ZipEntry zipEntry in zf)
                    {
                        if (!zipEntry.IsFile)
                        {
                            continue; // Ignore directories
                        }
                        var entryFileName = zipEntry.Name;
                        var extension = Path.GetExtension(entryFileName);
                        if (extension == null || extension.ToLowerInvariant() != ".nuspec")
                            continue;                          
                        var buffer = new byte[4096]; // 4K is optimum
                        var zipStream = zf.GetInputStream(zipEntry);
                        
                        using (var streamWriter = File.Create(targetFileName))
                        {
                            StreamUtils.Copy(zipStream, streamWriter, buffer);
                        }
                        return true;
                    }
                }
            }
            finally
            {
                if (zf != null)
                {
                    zf.IsStreamOwner = true; // Makes close also shut the underlying stream
                    zf.Close(); // Ensure we release resources
                }
            }
            return false;
        }
    }
}
