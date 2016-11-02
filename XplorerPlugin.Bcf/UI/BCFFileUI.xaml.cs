using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace Xbim.BCF.UI
{
    /// <summary>
    /// Interaction logic for BCFFile.xaml
    /// </summary>
    public partial class BcfFileUi : UserControl
    {
        BcffIleViewModel _vm;

        public BcfFileUi()
        {
            InitializeComponent();
            _vm = new BcffIleViewModel();
            DataContext = _vm;
        }

        public void Load(string fileName)
        {
            _vm.LoadFrom(fileName);
        }

        public BcfInstance NewInstance(string instanceName, BitmapImage img, VisualizationInfo vi)
        {
            BcfInstance i = new BcfInstance();
            i.Markup.Topic.Title = instanceName;
            if (img != null)
                i.SnapShot = img;
            if (vi != null)
                i.VisualizationInfo = vi;
            _vm.File.Instances.Add(i);
            return i;
        }

        public void Load(BcfFile file)
        {
            _vm.File = file;
        }

        private void t_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (T.SelectedItem == null || !(T.SelectedItem is BcfInstance))
            {
                SelInstance.Visibility = System.Windows.Visibility.Hidden;
            }
            SelInstance.Visibility = System.Windows.Visibility.Visible;

            BcfInstanceViewModel vm = new BcfInstanceViewModel((BcfInstance)T.SelectedItem);
            SelInstance.DataContext = vm;
        }

        private void CameraEvent(object sender, MouseButtonEventArgs e)
        {
            BcfInstanceCommands.GotoCameraPosition.Execute(null, null);
        }


        public bool CanSave
        {
            get
            {
                return (_vm.File != null && _vm.File.Instances.Any());
            }
        }

        internal void Save(string filename)
        {
            _vm.SaveTo(filename);
        }
    }
}
