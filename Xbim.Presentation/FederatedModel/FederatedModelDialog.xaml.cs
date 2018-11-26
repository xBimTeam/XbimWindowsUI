using System.Windows;
using Xbim.Common.Federation;
using Xbim.Ifc;


namespace Xbim.Presentation.FederatedModel
{
    /// <summary>
    /// Interaction logic for fed1.xaml
    /// </summary>
    public partial class FederatedModelDialog
    {
		public FederatedModelViewModel FederatedModel { get; set; }

        public FederatedModelDialog()
        {
            FederatedModel = new FederatedModelViewModel();
            DataContext = FederatedModel;
            DataContextChanged += FederatedModelDlg_DataContextChanged;
            InitializeComponent();
        }

        void FederatedModelDlg_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            FederatedModel.Model = DataContext as IfcStore;
            FederatedModel.NotifyAll();
            //OnPropertyChanged("Project");
            //OnPropertyChanged("Author");
            //OnPropertyChanged("Organization");
        }

        //private void Add_Click(object sender, RoutedEventArgs e)
        //{
        //    XbimModel model = DataContext as XbimModel;
        //    if (model != null)
        //    {
        //        AddFederatedModel fdlg = new AddFederatedModel();
        //        bool? done = fdlg.ShowDialog();
        //        if (done.HasValue && done.Value == true)
        //        {
        //            string fileName = fdlg.FileName;
        //            string ext = System.IO.Path.GetExtension(fileName);
        //            using (XbimModel refM = new XbimModel())
        //            {
        //                if (string.Compare(ext, ".xbim", true) != 0)
        //                {
        //                    refM.CreateFrom(fileName, null, null, true);
        //                    XbimMesher.GenerateGeometry(refM);
        //                    fileName = System.IO.Path.ChangeExtension(fileName, "xbim");
        //                }
        //            }
        //            IfcRole role = fdlg.Role;
        //            if (role == IfcRole.UserDefined)
        //                model.AddModelReference(fileName, fdlg.OrganisationName, fdlg.RoleName);
        //            else
        //                model.AddModelReference(fileName, fdlg.OrganisationName, role);
        //        }
        //    }
        //}

        //private void Modify_Click(object sender, RoutedEventArgs e)
        //{
        //    XbimModel model = DataContext as XbimModel;
        //    foreach (var item in FederatedList.SelectedItems)
        //    {
        //        if (item is XbimReferencedModel)
        //        {
        //            var rItem = item as XbimReferencedModel;
        //            AddFederatedModel fdlg = new AddFederatedModel(rItem);

        //            bool? done = fdlg.ShowDialog();
        //            if (done.HasValue && done.Value == true)
        //            {
        //                string fileName = fdlg.FileName;
        //                string ext = System.IO.Path.GetExtension(fileName);
        //                using (XbimModel refM = new XbimModel())
        //                {
        //                    if (string.Compare(ext, "xBIM Files (.txt)|*.txt|All Files (*.*)|*.*", true) != 0)
        //                    {
        //                        refM.CreateFrom(fileName, null, null, true);
        //                        XbimMesher.GenerateGeometry(refM);
        //                        fileName = System.IO.Path.ChangeExtension(fileName, "xbim");
        //                    }
        //                }
        //                using (var txn = model.BeginTransaction())
        //                {
        //                    rItem.DocumentInformation.Name = fileName;
        //                    txn.Commit();
        //                }
        //            }
        //        }
        //    }
        //}



        //public event PropertyChangedEventHandler PropertyChanged;
        //public void OnPropertyChanged(string prop)
        //{
        //    PropertyChanged(this, new PropertyChangedEventArgs(prop));
        //}
    }
}
