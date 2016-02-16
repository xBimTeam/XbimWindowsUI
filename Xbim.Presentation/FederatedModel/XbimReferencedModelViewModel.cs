using System.ComponentModel;
using System.IO;
using Xbim.Common;
using Xbim.Common.Federation;
using Xbim.Common.Geometry;
using Xbim.Ifc;

using Xbim.ModelGeometry.Scene;

namespace Xbim.Presentation.FederatedModel
{
    /// <summary>
    /// This class can hold all data neccesary to create a XbimReferencedModel.
    /// The purpose of this class is to hold data until it's complete enough to create a XbimReferencedModel. Once created, the model will be preserved in this object
    /// </summary>
    public class XbimReferencedModelViewModel : INotifyPropertyChanged
    {
        #region fields
        IReferencedModel _xbimReferencedModel;
        string _identifier = "";
        string _name = "";
        string _organisationName = "";
        string _organisationRole = "";
        #endregion fields

        public IReferencedModel ReferencedModel
        {
            get { return _xbimReferencedModel; }
            set { _xbimReferencedModel = value; }
        }

        public string Identifier
        {
            get
            {
                return ReferencedModel != null 
                    ? ReferencedModel.Identifier 
                    : _identifier;
            }
            //set
            //{
            //    _identifier = value;
            //    if (ReferencedModel != null)
            //    {
            //        ReferencedModel.DocumentInformation.DocumentId = _identifier;
            //    }
            //    OnPropertyChanged("Identifier");
            //}
        }

        public string Name
        {
            get
            {
                if (ReferencedModel != null)
                {
                    return ReferencedModel.Name;
                }
                return _name;
            }
            set 
            {
                //can't change the model, once it's created. User should delete and add it again.
                if (ReferencedModel != null) 
                    return;
                _name = value;
                OnPropertyChanged("Name");
            }
        }

        public string OrganisationName
        {
            get
            {
                if (ReferencedModel == null) 
                    return _organisationName;
                return ReferencedModel.Name;
            }
            set
            {
                if (ReferencedModel != null)
                    ReferencedModel.OwningOrganisation = value;
                _organisationName = value;
                OnPropertyChanged("OrganisationName");
            }
        }

        public string OrganisationRole
        {
            get
            {
                if (ReferencedModel == null) 
                    return _organisationRole;
                return ReferencedModel.Role;           
            }
            set
            {
                _organisationRole = value;

                if (ReferencedModel != null)
                {
                    ReferencedModel.Role = value;
                }
                OnPropertyChanged("OrganisationRole");
            }
        }

        public XbimReferencedModelViewModel() {}

        public XbimReferencedModelViewModel(IReferencedModel model)
        {
            ReferencedModel = model;
        }

        /// <summary>
        /// Validates all data and creates model. 
        
        /// </summary>
        /// <returns>Returns XbimReferencedModel == null </returns>
        public bool TryBuild(IfcStore model)
        {
            //it's already build, so no need to recreate it
            if (ReferencedModel != null)
                return true;

		    if (string.IsNullOrWhiteSpace(Name))
                return false;
            var ext = Path.GetExtension(Name).ToLowerInvariant();
            using (var refM = IfcStore.Open(Name))
            {
                Xbim3DModelContext m3D;
                if (refM.GeometryStore.IsEmpty)
                {
                    m3D = new Xbim3DModelContext(refM);
                    m3D.CreateContext();
                }
                Name = refM.FileName;
            }
            _xbimReferencedModel = model.AddModelReference(Name, OrganisationName, OrganisationRole);

            if (_xbimReferencedModel == null) 
                return ReferencedModel != null;
            //refresh all
            OnPropertyChanged("Identifier");
            OnPropertyChanged("Name");
            OnPropertyChanged("OrganisationName");
            OnPropertyChanged("OrganisationRole");
            return ReferencedModel != null;
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
