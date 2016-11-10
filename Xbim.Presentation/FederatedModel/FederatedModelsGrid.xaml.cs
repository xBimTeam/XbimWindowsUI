using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace Xbim.Presentation.FederatedModel
{
    // todo: the whole class needs to be reviewed
    public partial class FederatedModelsGrid
    {
        private IfcStore _model;

        public ObservableCollection<XbimReferencedModelViewModel> ReferencedModelWrappers { get; set; } = new ObservableCollection<XbimReferencedModelViewModel>();

        public List<string> Roles { get; } = new List<string>();

        public FederatedModelsGrid()
        {
            //get available roles
            var roles = Enum.GetValues(typeof (IfcRoleEnum));
            foreach (var role in roles)
            {
                Roles.Add(role.ToString());
            }
            Roles = Roles.OrderBy(x => x.ToString()).ToList();

            InitializeComponent();
            DataContextChanged += FederatedModelProperties_DataContextChanged;
            ReferencedModelWrappers.CollectionChanged += ReferencedModels_CollectionChanged;
            PropertyGrid.RowEditEnding += propertyGrid_RowEditEnding;
        }

        private void propertyGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            var modelWrapper = (XbimReferencedModelViewModel)e.Row.Item;
            try
            {
                //if build fails, cancel add row
                if (!modelWrapper.TryBuildAndAddTo(DataContext as IfcStore))
                {
                    e.Cancel = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Something went wrong, object might not have been added\n\n" + ex.Message);
            }
        }

        private static void ReferencedModels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // TODO: resolve reference models addition and removal.
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    break;
                case NotifyCollectionChangedAction.Remove:
                    //Remove model
                    var oldItems = new List<IfcStore>();
                    foreach (var item in e.OldItems)
                    {
                        var model = item as XbimReferencedModelViewModel;
                        //bool res = _model.ReferencedModels.Remove(model.ReferencedModel);
                        //model.ReferencedModel.Model.Close();
                    }
                    break;
            }
        }

        public IEnumerable SelectedItems => PropertyGrid.SelectedItems;

        private void FederatedModelProperties_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            _model = DataContext as IfcStore;
            if (_model == null)
                return;
            var tempRefModHolder = _model.ReferencedModels;

            foreach (var refMod in tempRefModHolder)
            {
                ReferencedModelWrappers.Add(new XbimReferencedModelViewModel(refMod));
            }
        }

        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // todo: review this code
            var txt = sender as TextBlock;
            if (txt != null && txt.DataContext.GetType() != typeof(XbimReferencedModelViewModel))
                return;
            var wrapper = txt?.DataContext as XbimReferencedModelViewModel;
            if (wrapper?.ReferencedModel != null)
                return;
            var fbDlg = new OpenFileDialog {Filter = "xBIM Files (.txt)|*.xbim|All Files (*.*)|*.*"};
            var result = fbDlg.ShowDialog();
            if (result == true)
            {
                txt.Text = fbDlg.FileName;
            }
        }

        private void DeleteRowButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var wrapper = button?.DataContext as XbimReferencedModelViewModel;
            if (wrapper?.ReferencedModel != null)
                ReferencedModelWrappers.Remove(wrapper);
        }
    }
}
