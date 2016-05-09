using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using NuGet;

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

        private void Test(object sender, RoutedEventArgs e)
        {
            lst.Items.Clear();
            var repo = PackageRepositoryFactory.Default.CreateRepository("https://www.myget.org/F/xbim-develop/api/v2");
            // var verFnd = repo.FindPackage("ToSpec", new SemanticVersion(3, 1, 1, 1));

            var ps = new List<PluginConfiguration>();

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
            }
        }

        private void Download(object sender, RoutedEventArgs e)
        {

        }
    }
}
