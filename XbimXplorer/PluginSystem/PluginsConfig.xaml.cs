using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using NuGet;
using NuGet.Packaging;
using Xbim.Presentation;

namespace XbimXplorer.PluginSystem
{
    /// <summary>
    /// Interaction logic for PluginsConfig.xaml
    /// </summary>
    public partial class PluginsConfig
    {

        protected Microsoft.Extensions.Logging.ILogger Logger { get; private set; }

        private XplorerMainWindow _mainWindow;

        public PluginsConfig()
        {
            InitializeComponent();
            Logger = XplorerMainWindow.LoggerFactory.CreateLogger<PluginsConfig>();
            _ =RefreshPluginList();
            _mainWindow = Application.Current.MainWindow as XplorerMainWindow;
            DataContext = this;
            SelectedPlugin = new PluginInformationVm(null);
        }

        private async Task ShowRepository()
        {
            var plugins = new List<PluginInformationVm>();
            try
            {
                var option = (PluginChannelOption)Enum.Parse(typeof(PluginChannelOption), DisplayOptionText);
                using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                await foreach (var plugin in _xplorerPlugins.GetPluginsAsync(option, NugetVersion, cts.Token))
                {
                    plugins.Add(new PluginInformationVm(plugin));
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(0, ex, "An error occurred getting repository information.");
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
                var NoSpace = ci.Content as string;
                NoSpace = NoSpace.Replace(" ", "");
                return NoSpace ;
            }
        }
        
        private async Task RefreshPluginList()
        {
            using (new WaitCursor())
            {
                if (DisplayOptionText == "Installed")
                    ShowDiskPlugins();
                else
                {
                    await ShowRepository();
                }
                SelectedPlugin = new PluginInformationVm(null);
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
                    //IPackageMetadata p = new ZipPackage(fInfo.FullName);
                    //var pi = new PluginInformation(p);
                    //pi.ExtractPlugin(
                    //    PluginManagement.GetPluginsDirectory()
                    //    );
                    throw new NotImplementedException("NUget needs sorting");
                }
                catch (Exception ex)
                {
                    Logger.LogError(0, ex, "Error processing package file {filename}.", fname);
                }
            }
            _ =RefreshPluginList();
        }

        private void DisplayOption_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            _ = RefreshPluginList();
        }
    }
}
