using System.ComponentModel;
using Xbim.IO;

namespace Xbim.Presentation.FederatedModel
{
    public class FederatedModelViewModel: INotifyPropertyChanged
    {
        XbimModel _model;

        public XbimModel Model
        {
            get { return _model; }
            set { _model = value; }
        }

        public string Project
        {
            get
            {
                if (_model == null)
                    return "";
                return _model.IfcProject.Name;
            }
            set
            {
                if (_model == null)
                    return;
                using (var txn = _model.BeginTransaction())
                {
                    _model.IfcProject.Name = value;
                    txn.Commit();
                }
                OnPropertyChanged("Project");
            }
        }
        public string Author
        {
            get
            {
                if (_model == null)
                    return "";
                return _model.DefaultOwningUser.ThePerson.FamilyName;
            }
            set
            {
                if (_model == null)
                    return;
                using (var txn = _model.BeginTransaction())
                {
                    var defUser = _model.DefaultOwningUser;
                    
                    _model.DefaultOwningUser.ThePerson.FamilyName = value;
                    txn.Commit();
                }
                OnPropertyChanged("Author");
            }
        }
        public string Organization
        {
            get
            {
                if (_model == null)
                    return "";
                return _model.DefaultOwningUser.TheOrganization.Name;
            }
            set
            {
                if (_model == null)
                    return;
                using (var txn = _model.BeginTransaction())
                {
                    _model.DefaultOwningUser.TheOrganization.Name = value;
                    txn.Commit();
                }
                OnPropertyChanged("Organization");
            }
        }



        public void NotifyAll()
        {
            OnPropertyChanged("Project");
            OnPropertyChanged("Author");
            OnPropertyChanged("Organization");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string prop)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
            }
        }
    }
}
