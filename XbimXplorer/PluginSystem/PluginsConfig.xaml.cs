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

        

        private void ShowRepository()
        {
            _xplorerPlugins.RefreshLocalPlugins();
            var plugins = new List<PluginInformationVm>();
            try
            {
                var option = (PluginChannelOption)Enum.Parse(typeof(PluginChannelOption), DisplayOptionText);
                foreach (var plugin in _xplorerPlugins.GetPlugins(option))
                {
                    plugins.Add(new PluginInformationVm(plugin));
                }
            }
            catch (Exception ex)
            {
                Log.Error("An error occurred getting repository information.", ex);
            }
            PluginList.ItemsSource = plugins;
        }

        private PluginManagement _xplorerPlugins = new PluginManagement();

        private PluginInformationVm _selectedPlugin;
        
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
            _xplorerPlugins.RefreshLocalPlugins();
            var plugins = _xplorerPlugins.DiskPlugins.Where(x => x != null)
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
