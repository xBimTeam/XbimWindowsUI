using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using log4net;
using NuGet;

namespace XbimXplorer.PluginSystem
{
    /// <summary>
    /// Interaction logic for PluginsConfig.xaml
    /// </summary>
    public partial class PluginsConfig
    {
        private static readonly ILog Log = LogManager.GetLogger("XbimXplorer.PluginSystem.PluginsConfig");

        private XplorerMainWindow _mainWindow;

        public PluginsConfig()
        {
            InitializeComponent();
            RefreshPluginList();
            _mainWindow = Application.Current.MainWindow as XplorerMainWindow;
        }

        internal string SelectedRepoUrl => RepoSource.Text;

        private void ShowRepository()
        {
            RefreshLocalPlugins();

            var plugins = new List<PluginInformationVm>();
            var repo = PackageRepositoryFactory.Default.CreateRepository(SelectedRepoUrl);

            try
            {
                var fnd = repo.Search("XplorerPlugin", true);
                foreach (var package in fnd)
                {
                    if (LatestOnly.IsChecked.HasValue && LatestOnly.IsChecked.Value && !package.IsAbsoluteLatestVersion)
                        continue;
                    var pv = new PluginInformation(package);
                    if (_diskPlugins.ContainsKey(package.Id))
                    {
                        pv.SetDirectoryInfo(_diskPlugins[package.Id]);
                    }

                    plugins.Add(new PluginInformationVm(pv));
                }
            }
            catch (Exception ex)
            {
                Log.Error("An error occurred getting repository information.", ex);
            }

            PluginList.ItemsSource = plugins;
        }

        private readonly Dictionary<string, PluginInformation> _diskPlugins =
            new Dictionary<string, PluginInformation>();

        private void RefreshLocalPlugins()
        {
            _diskPlugins.Clear();
            var dirs = PluginManagement.GetPluginDirectories();
            foreach (var directoryInfo in dirs)
            {
                var pc = new PluginInformation(directoryInfo);
                _diskPlugins.Add(pc.PluginId, pc);
            }
        }

        PluginInformationVm SelectedPlugin => PluginList.SelectedItem as PluginInformationVm;

        private void Download(object sender, RoutedEventArgs e)
        {
            SelectedPlugin?.ExtractPlugin(PluginManagement.GetPluginsDirectory());
            RefreshPluginList();
        }

        private void RefreshPluginList(object sender, RoutedEventArgs e)
        {
            RefreshPluginList();
        }

        private void RefreshPluginList()
        {
            if (SelectedRepoUrl.StartsWith("http://") || SelectedRepoUrl.StartsWith("https://"))
                ShowRepository();
            else
            {
                ShowDiskPlugins();
            }
        }

        private void ShowDiskPlugins()
        {
            RefreshLocalPlugins();
            var plugins =
                _diskPlugins.Values.Where(x => x != null)
                    .Select(pluginConfig => new PluginInformationVm(pluginConfig))
                    .ToList();
            PluginList.ItemsSource = plugins;
        }

        private void Load(object sender, RoutedEventArgs e)
        {
            var v = SelectedPlugin;
            v?.Load();
            RefreshPluginList();
        }

        private void ToggleEnabled(object sender, RoutedEventArgs e)
        {
            var v = SelectedPlugin;
            v?.ToggleEnabled();
        }

        private void PluginList_OnDrop(object sender, DragEventArgs e)
        {
            var d = e.Data.GetFormats();
            if (!d.Contains("FileNameW"))
                return;
            var fnames = e.Data.GetData("FileNameW") as IEnumerable<string>;
            if (fnames == null)
                return;
            foreach (var fname in fnames)
            {
                try
                {
                    var fInfo = new FileInfo(fname);
                    if (fInfo.Extension != ".nupkg")
                        continue;
                    IPackage p = new ZipPackage(fInfo.FullName);
                    var pi = new PluginInformation(p);
                    pi.ExtractPlugin(
                        PluginManagement.GetPluginsDirectory()
                        );
                }
                catch (Exception ex)
                {
                    Log.Error($"Error processing package file [{fname}].", ex);
                }
            }
            RefreshPluginList();
        }
    }
}
