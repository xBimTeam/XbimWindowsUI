using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc.ViewModels;
using Xbim.Ifc4.Interfaces;

namespace Xbim.Presentation
{
    class IfcGroupsViewModel : IXbimViewModel
    {
        private IfcStore _model;

        public override string ToString()
        {
            return Name;
        }

        public IfcGroupsViewModel(IfcStore model)
        {
            _model = model;
        }

        public IEnumerable<IXbimViewModel> Children
        {
            get
            {
                if (_children == null)
                    load();
                return _children;
            }
        }

        public IXbimViewModel CreatingParent
        {
            get { return null; }
            set { }
        }

        public IPersistEntity Entity
        {
            get { return null; }
        }

        public int EntityLabel
        {
            get { return -1; }
        }

        private bool _isSelected;
        private bool _isExpanded;
        private List<IXbimViewModel> _children;

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                _isExpanded = value;
                NotifyPropertyChanged("IsExpanded");
            }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set { _isSelected = value; NotifyPropertyChanged("IsSelected"); }
        }
        
        public string Name
        {
            get { return "First level groups"; }
        }

        public IModel Model
        {
            get { return _model; }
        }


        private void load()
        {
            _children = new List<IXbimViewModel>();

            var allGroups = _model.FederatedInstances.OfType<IIfcGroup>();
            var childGroups = new List<IIfcRoot>();
            foreach (var obj in _model.FederatedInstances.OfType<IIfcRelAssignsToGroup>())
            {
                childGroups.AddRange(obj.RelatedObjects.OfType<IIfcGroup>().ToList());
            }

            foreach (var item in allGroups)
            {
                if (!childGroups.Contains(item))
                    _children.Add(new GroupViewModel(item, this)); //add only root groups/systems
            }
        }

        #region INotifyPropertyChanged Members

        [field: NonSerialized] //don't serialize events
        public event PropertyChangedEventHandler PropertyChanged;
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { PropertyChanged += value; }
            remove { PropertyChanged -= value; }
        }
        void NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}
