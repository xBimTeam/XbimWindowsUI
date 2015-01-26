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
    public partial class BCFFileUI : UserControl
    {
        BCFFIleViewModel _vm;

        public BCFFileUI()
        {
            InitializeComponent();
            _vm = new BCFFIleViewModel();
            this.DataContext = _vm;
        }

        public void Load(string fileName)
        {
            _vm.LoadFrom(fileName);
        }

        public BCFInstance NewInstance(string InstanceName, BitmapImage img, VisualizationInfo vi)
        {
            BCFInstance i = new BCFInstance();
            i.Markup.Topic.Title = InstanceName;
            if (img != null)
                i.SnapShot = img;
            if (vi != null)
                i.VisualizationInfo = vi;
            _vm.File.Instances.Add(i);
            return i;
        }

        public void Load(BCFFile file)
        {
            _vm.File = file;
        }

        private void t_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (t.SelectedItem == null || !(t.SelectedItem is BCFInstance))
            {
                SelInstance.Visibility = System.Windows.Visibility.Hidden;
            }
            SelInstance.Visibility = System.Windows.Visibility.Visible;

            BCFInstanceViewModel vm = new BCFInstanceViewModel((BCFInstance)t.SelectedItem);
            this.SelInstance.DataContext = vm;
        }

        private void CameraEvent(object sender, MouseButtonEventArgs e)
        {
            BCFInstanceCommands.GotoCameraPosition.Execute(null, null);
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
