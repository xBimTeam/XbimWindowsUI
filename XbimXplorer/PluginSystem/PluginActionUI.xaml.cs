using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Xbim.Presentation;

namespace XbimXplorer.PluginSystem
{
    /// <summary>
    /// Interaction logic for PluginActionUI.xaml
    /// </summary>
    public partial class PluginActionUI : UserControl
    {
        public PluginActionUI()
        {
            InitializeComponent();
        }

        private PluginInformationVm SelectedPlugin => DataContext as PluginInformationVm;

        private void Download(object sender, RoutedEventArgs e)
        {
            using (var crs = new WaitCursor())
            {
                SelectedPlugin?.ExtractPlugin(PluginManagement.GetPluginsDirectory());
            }
        }

        private void Load(object sender, RoutedEventArgs e)
        {
            using (var crs = new WaitCursor())
            {
                SelectedPlugin?.Load();
            }
        }

        private void ToggleEnabled(object sender, RoutedEventArgs e)
        {
            using (var crs = new WaitCursor())
            {
                SelectedPlugin?.ToggleEnabled();
            }
        }
    }
}
