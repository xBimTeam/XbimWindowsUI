using System.ComponentModel;
using System.IO;
using System.Linq;
using Xbim.Common.Federation;
using Xbim.Common.Geometry;
using Xbim.Ifc2x3.IO;
using Xbim.Ifc4.Interfaces;
using Xbim.ModelGeometry.Scene;
using IfcRoleEnum = Xbim.Ifc2x3.ActorResource.IfcRoleEnum;

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
                var organization = ReferencedModel.Name;
                if (organization != null)
                    return organization.Name;
                return _organisationName;
            }
            set
            {
                if (ReferencedModel != null)
                {
                    var organization = ReferencedModel.DocumentInformation.DocumentOwner as IIfcOrganization;
                    if (organization != null)
                    {
                        using (var tnx = ReferencedModel.DocumentInfoTransaction)
                        {
                            organization.Name = value; 
                            tnx.Commit();
                        }
                    }
                }
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
                var ownerAsIIfcOrganization = ReferencedModel.DocumentInformation.DocumentOwner as IIfcOrganization;
                var roles = ownerAsIIfcOrganization.Roles;
                var role = roles != null ? roles.FirstOrDefault() : null;
                if (role == null)
                    return "";
                return role.Role == IfcRoleEnum.USERDEFINED 
                    ? role.UserDefinedRole.ToString() 
                    : role.Role.ToString();
            }
            set
            {
                _organisationRole = value;

                if (ReferencedModel != null)
                {
                    var ownerAsIIfcOrganization = ReferencedModel.DocumentInformation.DocumentOwner as IIfcOrganization;
                    var role = ownerAsIIfcOrganization.Roles.FirstOrDefault(); // assumes the first to be modified
                    using (var tnx = ReferencedModel.DocumentInfoTransaction)
                    {
                        role.RoleString = value; // the string is converted appropriately by the IfcActorRoleClass
                        tnx.Commit();
                    }
                }
                OnPropertyChanged("OrganisationRole");
            }
        }

        public XbimReferencedModelViewModel() {}

        public XbimReferencedModelViewModel(XbimReferencedModel model)
        {
            ReferencedModel = model;
        }

        /// <summary>
        /// Validates all data and creates model. 
        /// Provide a "XbimModel model = DataContext as XbimModel;"
        /// </summary>
        /// <returns>Returns XbimReferencedModel == null </returns>
        public bool TryBuild(XbimModel model)
        {
            //it's already build, so no need to recreate it
            if (ReferencedModel != null)
                return true;

		    if (string.IsNullOrWhiteSpace(Name))
                return false;
            var ext = Path.GetExtension(Name).ToLowerInvariant();
            using (var refM = new XbimModel())
            {
                var xbimName = Path.ChangeExtension(Name, "xbim");
                if (ext != ".xbim" && !File.Exists(xbimName))
                {
                    refM.CreateFrom(Name, null, null, true);
                    var m3D = new Xbim3DModelContext(refM);
                    m3D.CreateContext(geomStorageType: XbimGeometryType.PolyhedronBinary, progDelegate: null);
                    Name = Path.ChangeExtension(Name, "xbim");
                }
                Name = xbimName;
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
