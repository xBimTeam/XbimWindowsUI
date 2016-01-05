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
using Xbim.Ifc2x3.ActorResource;
using Xbim.Ifc2x3.IO;

namespace Xbim.Presentation.FederatedModel
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class FederatedModelsGrid : UserControl
    {
        XbimModel _model;
        List<string> _roles = new List<string>();
        ObservableCollection<XbimReferencedModelViewModel> _referencedModelsWrappers = new ObservableCollection<XbimReferencedModelViewModel>();

        public ObservableCollection<XbimReferencedModelViewModel> ReferencedModelWrappers
        {
            get { return _referencedModelsWrappers; }
            set { _referencedModelsWrappers = value; }
        }

        public List<string> Roles
        {
            get { return _roles; }
        }

        public FederatedModelsGrid()
        {
            //get available roles
            var roles = Enum.GetValues(typeof (IfcRoleEnum));
            foreach (var role in roles)
            {
                _roles.Add(role.ToString());
            }
            _roles = _roles.OrderBy(x => x.ToString()).ToList();

            InitializeComponent();
            DataContextChanged += FederatedModelProperties_DataContextChanged;
            ReferencedModelWrappers.CollectionChanged += ReferencedModels_CollectionChanged;
            PropertyGrid.RowEditEnding += propertyGrid_RowEditEnding;
        }

        void propertyGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            XbimReferencedModelViewModel modelWrapper = (XbimReferencedModelViewModel)e.Row.Item;
            try
            {
                //if build fails, cancel add row
                if (!modelWrapper.TryBuild(DataContext as XbimModel))
                {
                    e.Cancel = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Something went wrong, object might not have been added\n\n" + ex.Message);
            }
        }

        void ReferencedModels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            //TODO resolve reference models
            //switch (e.Action)
            //{
            //    case NotifyCollectionChangedAction.Add:
            //        break;
            //    case NotifyCollectionChangedAction.Remove:
            //        //Remove model

            //        var oldItems = new List<XbimModel>();
            //        foreach (var item in e.OldItems)
            //        {
            //            var model = item as XbimReferencedModelViewModel;
            //            bool res = _model.ReferencedModels.Remove(model.ReferencedModel);
            //            model.ReferencedModel.Model.Close();
            //        }

            //        break;
            //}
        }

        public IEnumerable SelectedItems
        {
            get
            {
                return PropertyGrid.SelectedItems;
            }
        }

        void FederatedModelProperties_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            _model = DataContext as XbimModel;
            if (_model != null)
            {
                var tempRefModHolder = _model.ReferencedModels;

                foreach (var refMod in tempRefModHolder)
                {
                    //TODO resolve reference models
                 //   _referencedModelsWrappers.Add(new XbimReferencedModelViewModel(refMod));
                }
            }
        }

        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TextBlock txt = sender as TextBlock;
            XbimReferencedModelViewModel wrapper;
            if (txt.DataContext.GetType() == typeof(XbimReferencedModelViewModel))
            {
                wrapper = txt.DataContext as XbimReferencedModelViewModel;
                if (wrapper.ReferencedModel == null)
                {
                    OpenFileDialog fbDlg = new OpenFileDialog();
                    fbDlg.Filter = "xBIM Files (.txt)|*.xbim|All Files (*.*)|*.*"; ;
                    bool? result = fbDlg.ShowDialog();
                    if (result == true)
                    {
                        txt.Text = fbDlg.FileName;
                    }
                }
            }
        }

        private void DeleteRowButton_Click(object sender, RoutedEventArgs e)
        {
            var wrapper = (sender as Button).DataContext as XbimReferencedModelViewModel;
            if (wrapper.ReferencedModel != null)
                ReferencedModelWrappers.Remove(wrapper);
        }
    }
}
