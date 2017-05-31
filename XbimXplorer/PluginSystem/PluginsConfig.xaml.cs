using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using log4net;
using NuGet;
using Xbim.Presentation;

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
            DataContext = this;
            SelectedPlugin = new PluginInformationVm(null);
        }

        internal string SelectedRepoUrl => "https://www.myget.org/F/xbim-plugins/api/v2";

        private void ShowRepository()
        {
            RefreshLocalPlugins();

            var plugins = new List<PluginInformationVm>();
            var repo = PackageRepositoryFactory.Default.CreateRepository(SelectedRepoUrl);

            try
            {
                var option = DisplayOptionText;
                var allowDevelop = option != "Stable";
                
                var fnd = repo.Search("XplorerPlugin", allowDevelop);
                foreach (var package in fnd)
                {
                    if (option != "All versions")
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

        private PluginInformationVm _selectedPlugin;

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

        private string DisplayOptionText
        {
            get
            {
                var ci = DisplayOption.SelectedItem as ComboBoxItem;
                return ci.Content as string;
            }
        }
        
        private void RefreshPluginList()
        {
            using (new WaitCursor())
            {
                if (DisplayOptionText == "Installed")
                    ShowDiskPlugins();
                else
                {
                    ShowRepository();
                }
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

        public PluginInformationVm SelectedPlugin
        {
            get { return _selectedPlugin; }
            set
            {
                _selectedPlugin = value;
                CurrentPlugin.DataContext = _selectedPlugin;
            }
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

        private void DisplayOption_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            RefreshPluginList();
        }
    }
}
