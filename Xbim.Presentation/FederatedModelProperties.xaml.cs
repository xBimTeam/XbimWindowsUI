using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Xbim.IO;

namespace Xbim.Presentation
{
    /// <summary>
    /// Interaction logic for FederatedModelProperties.xaml
    /// </summary>
    public partial class FederatedModelProperties : UserControl
    {
        public FederatedModelProperties()
        {
            InitializeComponent();
            this.DataContextChanged += FederatedModelProperties_DataContextChanged;
        }

        public IEnumerable SelectedItems
        {
            get
            {
                return propertyGrid.SelectedItems;
            }
        }

        void FederatedModelProperties_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            XbimModel model = DataContext as XbimModel;
            if (model != null)
            {
                propertyGrid.ItemsSource = model.ReferencedModels;
            }
        }
    }
}
