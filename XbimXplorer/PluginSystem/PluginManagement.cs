using Microsoft.Extensions.Logging;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xbim.Common.Metadata;
using Xbim.Ifc4.Interfaces;

namespace XbimXplorer.PluginSystem
{
    public enum PluginChannelOption
    {
        Installed,
        LatestStable,
        LatestIncludingDevelopment,
        AllCompatibleVersions
    }

    public class PluginManagement
    {
        private readonly Dictionary<string, PluginInformation> _diskPlugins =
           new Dictionary<string, PluginInformation>();

        private readonly SourceCacheContext _cache = new SourceCacheContext();

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
                var pc = new PluginInformation(directoryInfo , this);
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
            var tmp = new ManifestMetadata() { Id = path.Name, Version = new NuGet.Versioning.NuGetVersion("0.0.0") };
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


        internal async Task<PackageArchiveReader> DownloadPluginAsync(IPackageSearchMetadata package, Stream packageStream, CancellationToken? cancellationToken)
        {
            var ct = cancellationToken?? CancellationToken.None;

            var logger = NuGet.Common.NullLogger.Instance;
                        
            SourceRepository repository = Repository.Factory.GetCoreV3(SelectedRepoUrl);

            FindPackageByIdResource resource = await repository.GetResourceAsync<FindPackageByIdResource>();
         

            await resource.CopyNupkgToStreamAsync(
                package.Identity.Id,
                package.Identity.Version,
                packageStream,
                _cache,
                logger,
                ct);

            Console.WriteLine($"Downloaded package {package.Identity.Id} {package.Identity.Version}");

            return new PackageArchiveReader(packageStream);

        }

        internal async IAsyncEnumerable<PluginInformation> GetPluginsAsync(PluginChannelOption option, string winUiNugetVersion, 
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var logger = NuGet.Common.NullLogger.Instance;
            RefreshLocalPlugins();
            var allowDevelop = option != PluginChannelOption.LatestStable;
          

            SourceRepository repository = Repository.Factory.GetCoreV3(SelectedRepoUrl);
            PackageSearchResource resource = await repository.GetResourceAsync<PackageSearchResource>();
            SearchFilter searchFilter = new SearchFilter(includePrerelease: allowDevelop);


            var invokingVerion = new NuGetVersion(winUiNugetVersion);

            IEnumerable<IPackageSearchMetadata> results = await resource.SearchAsync(
                "XplorerPlugin",
                searchFilter,
                skip: 0,
                take: 20,
                logger,
                cancellationToken);

            var tmpPackages = new List<IPackageSearchMetadata>();

            // Use any type in the schema
			Module mod = typeof(Xbim.Ifc4.Kernel.IfcRoot).Module;
			ExpressMetaData meta = ExpressMetaData.GetMetadata(mod);

            // Find all sub types of IfcProduct. You will probably also want to do IfcTypeObjects as well
            var product = meta.ExpressType(typeof(Xbim.Ifc4.Kernel.IfcProduct));

            foreach(var type in product.SubTypes)
            {
				ExpressMetaProperty predefinedProp = type.Properties.Values.FirstOrDefault(v => v.Name == nameof(IIfcDoor.PredefinedType));
                if (predefinedProp != null)
                {
                    // It has a PredefinedType property - get the underlyng Type
					Type enumType = predefinedProp.PropertyInfo.PropertyType;
                    enumType = Nullable.GetUnderlyingType(enumType) ?? enumType;

                    // Get the values
					Array enumValues = Enum.GetValues(enumType);
                    foreach (var predefinedValue in enumValues)
                    {
                        Console.WriteLine("{0}: {1}", type.Name, predefinedValue);
                    }

                }
            }

            foreach (IPackageSearchMetadata package in results)
            {
                Console.WriteLine($"Found package {package.Identity.Id} {package.Identity.Version}");
                // drop develop if latest stable
                System.Diagnostics.Debug.WriteLine($"Evaluating {package.Identity}");
                
                if (option == PluginChannelOption.LatestStable && package.Identity.Version.IsPrerelease)
                {
                    continue;
                }
                // check it is compatible
                var sel = package.DependencySets.SelectMany(x => x.Packages.Where(y => y.Id.StartsWith("Xbim.WindowsUI"))).FirstOrDefault();
                if (sel != null && sel.VersionRange.Satisfies(invokingVerion, VersionComparison.Version))
                {
                    
                    tmpPackages.Add(package);
                }
            }
            
            if (option == PluginChannelOption.LatestStable || option == PluginChannelOption.LatestIncludingDevelopment)
            {
                // only one per ID
                var selPackages = new List<IPackageSearchMetadata>();
                var grouped = tmpPackages.GroupBy(x => x.Identity.Id);
                foreach (var element in grouped)
                {
                    var maxVersion = element.Max(x => x.Identity.Version);
                    selPackages.Add(element.FirstOrDefault(x => x.Identity.Version == maxVersion));
                }
                tmpPackages = selPackages;
            }

            foreach (var package in tmpPackages)
            {
                var pv = new PluginInformation(package, this);
                if (_diskPlugins.ContainsKey(package.Identity.Id))
                {
                    pv.SetDirectoryInfo(_diskPlugins[package.Identity.Id]);
                }
                yield return pv;
            }

        }

        public static string GetEntryFile(DirectoryInfo dir, string filename = null)
        {
            var f = filename == null 
                ? Path.Combine(dir.FullName, dir.Name + ".dll") 
                : Path.Combine(dir.FullName, filename);
            return f;
        }

        internal static string GetStartupFileConfig(DirectoryInfo dir)
        {
            if (dir == null)
                return "";
            return Path.Combine(dir.FullName, "PluginConfig.xml");
        }

        public static void SetStartup(DirectoryInfo dir, PluginConfiguration.PluginFlag behaviour)
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