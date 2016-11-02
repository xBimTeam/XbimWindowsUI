using System.ComponentModel;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace Xbim.Presentation.FederatedModel
{
    // todo: for this class to work DefaultOwningUser in Essentials needs to be changed 
    // to allow the identificaiton of an existing OwningUser if it already exists in the file.
    
    public class FederatedModelViewModel: INotifyPropertyChanged
    {
        IfcStore _model;
        
        public IfcStore Model
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
                var p = _model.Instances.FirstOrDefault<IIfcProject>();
                return 
                    p != null 
                    ? (string) p.Name 
                    : "";
            }
            set
            {
                if (_model == null)
                    return;
                using (var txn = _model.BeginTransaction())
                {
                    var project = _model.Instances.FirstOrDefault<IIfcProject>();

                    if (project is Ifc2x3.Kernel.IfcProject)
                    {
                        var x3 = project as Ifc2x3.Kernel.IfcProject;
                        x3.Name  = value;
                    }
                    else if (project is Ifc4.Kernel.IfcProject)
                    {
                        var x4 = project as Ifc4.Kernel.IfcProject;
                        x4.Name = value;
                    }
                    
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
                    var person = _model.DefaultOwningUser.ThePerson;

                    if (person is Ifc2x3.ActorResource.IfcPerson)
                    {
                        var x3 = person as Ifc2x3.ActorResource.IfcPerson;
                        x3.FamilyName = value;
                    }
                    else if (person is Ifc4.ActorResource.IfcPerson)
                    {
                        var x4 = person as Ifc4.ActorResource.IfcPerson;
                        x4.FamilyName = value;
                    }
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
                    var org = _model.DefaultOwningUser.TheOrganization;
                    if (org is Ifc2x3.ActorResource.IfcOrganization)
                    {
                        var x3 = org as Ifc2x3.ActorResource.IfcOrganization;
                        x3.Name = value;
                    }
                    else if (org is Ifc4.ActorResource.IfcOrganization)
                    {
                        var x4 = org as Ifc4.ActorResource.IfcOrganization;
                        x4.Name = value;
                    }
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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}
