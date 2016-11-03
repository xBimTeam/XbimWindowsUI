using System.Collections.Generic;
using System.Windows;
using NuGet;

namespace XbimXplorer.PluginSystem
{
    /// <summary>
    /// Interaction logic for PluginsConfig.xaml
    /// </summary>
    public partial class PluginsConfig
    {
        public PluginsConfig()
        {
            InitializeComponent();
            RefreshPluginList();
        }

        internal string SelectedRepoUrl => RepoSource.Text;
        
        private void ShowRepository()
        {
            RefreshLocalPlugins();

            var plugins = new List<PluginConfigurationVm>();
            var repo = PackageRepositoryFactory.Default.CreateRepository(SelectedRepoUrl);
           
            var fnd = repo.Search("XplorerPlugin", true);
            foreach (var package in fnd)
            {
                if (LatestOnly.IsChecked.HasValue && LatestOnly.IsChecked.Value && !package.IsAbsoluteLatestVersion)
                    continue;
                var pv = new PluginConfiguration
                {
                    PluginId = package.Id,
                };
                pv.SetOnlinePackage(package);
                if (_diskPlugins.ContainsKey(package.Id))
                {
                    pv.SetDiskManifest(_diskPlugins[package.Id]);
                }

                plugins.Add(new PluginConfigurationVm(pv));
            }
            PluginList.ItemsSource = plugins;
        }

        private readonly Dictionary<string, ManifestMetadata> _diskPlugins = new Dictionary<string, ManifestMetadata>();

        private void RefreshLocalPlugins()
        {
            _diskPlugins.Clear();
            var dirs = PluginManagement.GetPluginDirectories();
            foreach (var directoryInfo in dirs)
            {
                var md = PluginConfiguration.GetManifestMetadata(directoryInfo);
                _diskPlugins.Add(md.Id, md);
            }
        }

        private void Download(object sender, RoutedEventArgs e)
        {
            var current = PluginList.SelectedItem as PluginConfigurationVm;
            current?.ExtractPlugin(PluginManagement.GetPluginDirectory());
            RefreshPluginList();
        }

        private void RefreshPluginList(object sender, RoutedEventArgs e)
        {
            RefreshPluginList();
        }

        private void RefreshPluginList()
        {
            if (SelectedRepoUrl.StartsWith("http"))
                ShowRepository();
            else
            {
                ShowDiskPlugins();
            }
        }

        private void ShowDiskPlugins()
        {
            RefreshLocalPlugins();
            var plugins = new List<PluginConfigurationVm>();

            foreach (var package in _diskPlugins.Values)
            {
                var pv = new PluginConfiguration
                {
                    PluginId = package.Id,
                };
                pv.SetDiskManifest(package);
                plugins.Add(new PluginConfigurationVm(pv));
            }
            PluginList.ItemsSource = plugins;
        }
    }
}
