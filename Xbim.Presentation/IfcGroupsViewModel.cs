using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Xbim.Common;
using Xbim.Ifc2x3.Kernel;
using Xbim.IO;
using Xbim.IO.ViewModels;
using Xbim.XbimExtensions.Interfaces;

namespace Xbim.Presentation
{
    class IfcGroupsViewModel : IXbimViewModel
    {
        private XbimModel _model;

        public override string ToString()
        {
            return Name;
        }

        public IfcGroupsViewModel(XbimModel model)
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

        public IPersistIfcEntity Entity
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

        public XbimModel Model
        {
            get { return _model; }
        }


        private void load()
        {
            _children = new List<IXbimViewModel>();

            var allGroups = _model.Instances.OfType<IfcGroup>();
            var childGroups = new List<IfcRoot>();
            foreach (var obj in _model.Instances.OfType<IfcRelAssignsToGroup>())
            {
                childGroups.AddRange(obj.RelatedObjects.OfType<IfcGroup>().ToList());
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
