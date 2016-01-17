using System.ComponentModel;
using Xbim.Common.Federation;


namespace Xbim.Presentation.FederatedModel
{
    public class FederatedModelViewModel: INotifyPropertyChanged
    {
        IReferencedModel _model;

        public IReferencedModel Model
        {
            get { return _model; }
            set { _model = value; }
        }

        
        public string Role
        {
            get
            {              
                return _model.Role;
            }
            set
            {
                _model.Role = value;
                OnPropertyChanged("Author");
            }
        }
        public string Organization
        {
            get
            {
                
                return _model.OwningOrganisation;
            }
            set
            {
                _model.OwningOrganisation = value;
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
