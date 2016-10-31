using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using NuGet;
using System.Collections.ObjectModel;

namespace XbimXplorer.PluginSystem
{
    /// <summary>
    /// Interaction logic for PluginsConfig.xaml
    /// </summary>
    public partial class PluginsConfig : Window
    {
        public PluginsConfig()
        {
            InitializeComponent();
        }
        
        internal ObservableCollection<PluginConfigurationVm> Plugins { get; } = new ObservableCollection<PluginConfigurationVm>();

        private void Refresh(object sender, RoutedEventArgs e)
        {
            Plugins.Clear();
            var repo = PackageRepositoryFactory.Default.CreateRepository("https://www.myget.org/F/xbim-develop/api/v2");
            // var verFnd = repo.FindPackage("ToSpec", new SemanticVersion(3, 1, 1, 1));
            
            var fnd = repo.Search("XplorerPlugin", true);
            foreach (var package in fnd)
            {
                if (!package.IsAbsoluteLatestVersion)
                    return;
                var pv = new PluginConfiguration
                {
                    PluginId = package.Id,
                    OnLineVersion = package.Version
                };
                pv.setOnlinePackage(package);
                Debug.Print("{0}: {1}", pv.PluginId, pv.OnLineVersion);
                Plugins.Add(new PluginConfigurationVm(pv));
            }
            PluginList.ItemsSource = Plugins;
        }

        private void Download(object sender, RoutedEventArgs e)
        {
            var current = PluginList.SelectedItem as PluginConfigurationVm;
            current?.ExtractLibs(XplorerMainWindow.GetPluginDirectory());
        }
    }
}
