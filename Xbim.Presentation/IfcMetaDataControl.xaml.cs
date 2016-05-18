#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Presentation
// Filename:    IfcMetaDataControl.xaml.cs
// Published:   01, 2012
// Last Edited: 9:05 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Navigation;
using Xbim.Common;
using Xbim.Common.Metadata;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

#endregion

namespace Xbim.Presentation
{
    /// <summary>
    ///   Interaction logic for IfcMetaDataControl.xaml
    /// </summary>
    public partial class IfcMetaDataControl : INotifyPropertyChanged
    {
        public class PropertyItem
        {
            public string Units { get; set; }

            public string PropertySetName { get; set; }

            public string Name { get; set; }

            public int IfcLabel { get; set; }

            public string IfcUri
            {
                get { return "xbim://EntityLabel/" + IfcLabel; }
            }

            public bool IsLabel
            {
                get { return IfcLabel > 0; }
            }

            public string Value { get; set; }

            private readonly string[] _schemas = {"file", "ftp", "http", "https"};


            public bool IsLink
            {
                get
                {
                    Uri uri;
                    if (!Uri.TryCreate(Value, UriKind.Absolute, out uri))
                        return false;
                    var schema = uri.Scheme;
                    return _schemas.Contains(schema);
                }
            }
        }

        private IPersistEntity _entity;

        public IfcMetaDataControl()
        {
            InitializeComponent();
            TheTabs.SelectionChanged += TheTabs_SelectionChanged;

            _objectGroups = new ListCollectionView(_objectProperties);
            if (_objectGroups.GroupDescriptions != null)
            {
                _objectGroups.GroupDescriptions.Add(new PropertyGroupDescription("PropertySetName"));
                _objectGroups.SortDescriptions.Add(new SortDescription("PropertySetName", ListSortDirection.Ascending));
            }
            _propertyGroups = new ListCollectionView(_properties);
            if (_propertyGroups.GroupDescriptions != null)
            {
                _propertyGroups.GroupDescriptions.Add(new PropertyGroupDescription("PropertySetName"));
                _propertyGroups.SortDescriptions.Add(new SortDescription("PropertySetName", ListSortDirection.Ascending));
            }
            _materialGroups = new ListCollectionView(_materials);
            if (_materialGroups.GroupDescriptions != null)
                _materialGroups.GroupDescriptions.Add(new PropertyGroupDescription("PropertySetName"));
        }

        private void TheTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count <= 0)
                return;
            var selectedTab = e.AddedItems[0] as TabItem; // Gets selected tab
            FillTabValues(selectedTab);
        }

        private void FillTabValues(TabItem selectedTab)
        {
            //only fill tabs on demand when they are activated
            if (selectedTab == null)
                return;

            // ReSharper disable PossibleUnintendedReferenceComparison
            if (selectedTab == ObjectTab)
                FillObjectData();
            else if (selectedTab == TypeTab)
                FillTypeData();
            else if (selectedTab == PropertyTab)
                FillPropertyData();
            else if (selectedTab == QuantityTab)
                FillQuantityData();
            else if (selectedTab == MaterialTab)
                FillMaterialData();
            // ReSharper restore PossibleUnintendedReferenceComparison
        }

        private readonly ListCollectionView _propertyGroups;

        public ListCollectionView PropertyGroups
        {
            get { return _propertyGroups; }
        }

        private readonly ListCollectionView _materialGroups;

        public ListCollectionView MaterialGroups
        {
            get { return _materialGroups; }
        }

        private readonly ListCollectionView _objectGroups;

        public ListCollectionView ObjectGroups
        {
            get { return _objectGroups; }
        }

        private readonly ObservableCollection<PropertyItem> _objectProperties = new ObservableCollection<PropertyItem>();

        public ObservableCollection<PropertyItem> ObjectProperties
        {
            get { return _objectProperties; }
        }

        private readonly ObservableCollection<PropertyItem> _quantities = new ObservableCollection<PropertyItem>();

        public ObservableCollection<PropertyItem> Quantities
        {
            get { return _quantities; }
        }

        private readonly ObservableCollection<PropertyItem> _properties = new ObservableCollection<PropertyItem>();

        public ObservableCollection<PropertyItem> Properties
        {
            get { return _properties; }

        }

        private readonly ObservableCollection<PropertyItem> _materials = new ObservableCollection<PropertyItem>();

        public ObservableCollection<PropertyItem> Materials
        {
            get { return _materials; }
        }

        private readonly ObservableCollection<PropertyItem> _typeProperties = new ObservableCollection<PropertyItem>();

        public ObservableCollection<PropertyItem> TypeProperties
        {
            get { return _typeProperties; }
        }

        public IPersistEntity SelectedEntity
        {
            get { return (IPersistEntity) GetValue(SelectedEntityProperty); }
            set { SetValue(SelectedEntityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IfcInstance.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedEntityProperty =
            DependencyProperty.Register("SelectedEntity", typeof (IPersistEntity), typeof (IfcMetaDataControl),
                new UIPropertyMetadata(null, OnSelectedEntityChanged));


        private static void OnSelectedEntityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = d as IfcMetaDataControl;
            if (ctrl != null && e.NewValue is IPersistEntity)
            {
                ctrl.DataRebind((IPersistEntity) e.NewValue);
            }
        }

        private void DataRebind(IPersistEntity entity)
        {
            if (_entity != null && !_preventHistory)
            {
                _history.Push(_entity);
                UpdateButtonBack();
            }
            Clear(); //remove any bindings
            _entity = null;
            if (entity != null)
            {
                _entity = entity;
                FillTabValues(TheTabs.SelectedItem as TabItem);
            }
            else
                _entity = null;
        }

        private void UpdateButtonBack()
        {
            BtnBack.IsEnabled = _history.Any();
        }

        private void FillTypeData()
        {
            if (_typeProperties.Count > 0) return; //don't fill unless empty
            var ifcObj = _entity as IIfcObject;
            if (ifcObj == null)
                return;
            var reldefines = ifcObj.IsTypedBy.FirstOrDefault();
            
            if (reldefines == null)        
                return;
            var typeEntity = reldefines.RelatingType ;
            var ifcType = typeEntity.ExpressType;
            _typeProperties.Add(new PropertyItem {Name = "Type", Value = ifcType.Type.Name});
            _typeProperties.Add(new PropertyItem {Name = "Ifc Label", Value = "#" + typeEntity.EntityLabel});

            _typeProperties.Add(new PropertyItem {Name = "Name", Value = typeEntity.Name});
            _typeProperties.Add(new PropertyItem {Name = "Description", Value = typeEntity.Description});
            _typeProperties.Add(new PropertyItem {Name = "GUID", Value = typeEntity.GlobalId});
            if (typeEntity.OwnerHistory!=null)
            {_typeProperties.Add(new PropertyItem
                {
                    Name = "Ownership",
                    Value =
                    typeEntity.OwnerHistory.OwningUser + " using " +
                    typeEntity.OwnerHistory.OwningApplication.ApplicationIdentifier
                 });
            }
            //now do properties in further specialisations that are text labels
            foreach (var pInfo in ifcType.Properties.Where
                (p => p.Value.EntityAttribute.Order > 4
                      && p.Value.EntityAttribute.State != EntityAttributeState.DerivedOverride)
                ) //skip the first for of root, and derived and things that are objects
            {
                var val = pInfo.Value.PropertyInfo.GetValue(typeEntity, null);
                if (!(val is ExpressType))
                    continue;
                var pi = new PropertyItem {Name = pInfo.Value.PropertyInfo.Name, Value = ((ExpressType) val).ToString()};
                _typeProperties.Add(pi);
            }
        }

        private void FillQuantityData()
        {
            if (_quantities.Count > 0) return; //don't fill unless empty
            //now the property sets for any 

            // local cache for efficiency

            var o = _entity as IIfcObject;
            if (o != null)
            {
                var ifcObj = o;
                var modelUnits = _entity.Model.Instances.OfType<IIfcUnitAssignment>().FirstOrDefault();
                    // not optional, should never return void in valid model

                foreach (
                    var relDef in
                        ifcObj.IsDefinedBy.Where(r => r.RelatingPropertyDefinition is IIfcElementQuantity))
                {
                    var pSet = relDef.RelatingPropertyDefinition as IIfcElementQuantity;
                    AddQuantityPSet(pSet, modelUnits);
                }
            }
            else if (_entity is IIfcTypeObject)
            {
                var asIfcTypeObject = _entity as IIfcTypeObject;
                var modelUnits = _entity.Model.Instances.OfType<IIfcUnitAssignment>().FirstOrDefault();
                // not optional, should never return void in valid model

                if (asIfcTypeObject.HasPropertySets == null)
                    return;
                foreach (var pSet in asIfcTypeObject.HasPropertySets.OfType<IIfcElementQuantity>())
                {
                    AddQuantityPSet(pSet, modelUnits);
                }

                //foreach (var relDef in ifcObj. IsDefinedByProperties.Where(r => r.RelatingPropertyDefinition is IfcElementQuantity))
                //{
                //    var pSet = relDef.RelatingPropertyDefinition as IfcElementQuantity;
                //    AddQuantityPSet(pSet, modelUnits);
                //}
            }
        }

        private void AddQuantityPSet(IIfcElementQuantity pSet, IIfcUnitAssignment modelUnits)
        {
            if (pSet == null)
                return;
            if (modelUnits == null) throw new ArgumentNullException("modelUnits");
            foreach (var item in pSet.Quantities.OfType<IIfcPhysicalSimpleQuantity>())
                // currently only handles IfcPhysicalSimpleQuantity
            {
                _quantities.Add(new PropertyItem
                {
                    PropertySetName = pSet.Name,
                    Name = item.Name,
                    Value = item + " " + item.Unit.FullName
                });
            }
        }

        private void FillPropertyData()
        {
            if (_properties.Any()) //don't try to fill unless empty
                return;
            //now the property sets for any 

            if (_entity is IIfcObject)
            {
                var asIfcObject = (IIfcObject) _entity;
                foreach (
                    var pSet in
                        asIfcObject.IsDefinedBy.Select(
                            relDef => relDef.RelatingPropertyDefinition as IIfcPropertySet)
                    )
                    AddPropertySet(pSet);
            }
            else if (_entity is IIfcTypeObject)
            {
                var asIfcTypeObject = _entity as IIfcTypeObject;
                if (asIfcTypeObject.HasPropertySets == null)
                    return;
                foreach (var pSet in asIfcTypeObject.HasPropertySets.OfType<IIfcPropertySet>())
                {
                    AddPropertySet(pSet);
                }
            }

        }

        private void AddPropertySet(IIfcPropertySet pSet)
        {
            if (pSet == null)
                return;
            foreach (var item in pSet.HasProperties.OfType<IIfcPropertySingleValue>()) //only handle simple properties
            {
                var val = "";
                var nomVal = item.NominalValue;
                if (nomVal != null)
                    val = nomVal.ToString();
                _properties.Add(new PropertyItem
                {
                    IfcLabel = item.EntityLabel,
                    PropertySetName = pSet.Name,
                    Name = item.Name,
                    Value = val
                });
            }
        }

        private void FillMaterialData()
        {
            if (_materials.Any())
                return; //don't fill unless empty

            if (_entity is IIfcObject)
            {
                var ifcObj = _entity as IIfcObject;
                var matRels = ifcObj.HasAssociations.OfType<IIfcRelAssociatesMaterial>();
                foreach (var matRel in matRels)
                {
                    AddMaterialData(matRel.RelatingMaterial, "");
                }
            }
            else if (_entity is IIfcTypeObject)
            {
                var ifcObj = _entity as IIfcTypeObject;
                var matRels = ifcObj.HasAssociations.OfType<IIfcRelAssociatesMaterial>();
                foreach (var matRel in matRels)
                {
                    AddMaterialData(matRel.RelatingMaterial, "");
                }
            }
        }

        private void AddMaterialData(IIfcMaterialSelect matSel, string setName)
        {
            if (matSel is IIfcMaterial) //simplest just add it
                _materials.Add(new PropertyItem
                {
                    Name = string.Format("{0} [#{1}]", ((IIfcMaterial) matSel).Name, matSel.EntityLabel),
                    PropertySetName = setName,
                    Value = ""
                });
            else if (matSel is IIfcMaterialLayer)
                _materials.Add(new PropertyItem
                {
                    Name = string.Format("{0} [#{1}]", ((IIfcMaterialLayer) matSel).Material.Name, matSel.EntityLabel),
                    Value = ((IIfcMaterialLayer) matSel).LayerThickness.Value.ToString(),
                    PropertySetName = setName
                });
            else if (matSel is IIfcMaterialList)
            {
                foreach (var mat in ((IIfcMaterialList) matSel).Materials)
                {
                    _materials.Add(new PropertyItem
                    {
                        Name = string.Format("{0} [#{1}]", mat.Name, mat.EntityLabel),
                        PropertySetName = setName,
                        Value = ""
                    });
                }
            }
            else if (matSel is IIfcMaterialLayerSet)
            {
                foreach (var item in ((IIfcMaterialLayerSet) matSel).MaterialLayers) //recursive call to add materials
                {
                    AddMaterialData(item, ((IIfcMaterialLayerSet) matSel).LayerSetName);
                }
            }
            else if (matSel is IIfcMaterialLayerSetUsage)
            {
                foreach (var item in ((IIfcMaterialLayerSetUsage) matSel).ForLayerSet.MaterialLayers)
                    //recursive call to add materials
                {
                    AddMaterialData(item, ((IIfcMaterialLayerSetUsage) matSel).ForLayerSet.LayerSetName);
                }
            }
        }

        private void ReportProp(IPersistEntity entity, ExpressMetaProperty prop, bool verbose)
        {
            var propVal = prop.PropertyInfo.GetValue(entity, null);
            if (propVal == null)
            {
                if (!verbose)
                    return;
                propVal = "<null>";
            }
            
            if (prop.EntityAttribute.IsEnumerable)
            {
                var propCollection = propVal as IEnumerable<object>;
                
                if (propCollection != null)
                {
                    var propVals = propCollection.ToArray();

                    switch (propVals.Length)
                    {
                        case 0:
                            if (!verbose)
                                return;
                            _objectProperties.Add(new PropertyItem { Name = prop.PropertyInfo.Name, Value = "<empty>", PropertySetName = "General" });
                            break;
                        case 1:
                            var tmpSingle = GetPropItem(propVals[0]);
                            tmpSingle.Name = prop.PropertyInfo.Name + " (∞)";
                            tmpSingle.PropertySetName = "General";
                            _objectProperties.Add(tmpSingle);
                            break;
                        default:
                            foreach (var item in propVals)
                            {
                                var tmpLoop = GetPropItem(item);
                                tmpLoop.Name = item.GetType().Name;
                                tmpLoop.PropertySetName = prop.PropertyInfo.Name;
                                _objectProperties.Add(tmpLoop);
                            }
                            break;
                    }
                }
                else
                {
                    if (!verbose)
                        return;
                    _objectProperties.Add(new PropertyItem { Name = prop.PropertyInfo.Name, Value = "<not an enumerable>" });
                }
            }
            else
            {
                var tmp = GetPropItem(propVal);
                tmp.Name = prop.PropertyInfo.Name;
                tmp.PropertySetName = "General";
                _objectProperties.Add(tmp);
            }
        }

        private PropertyItem GetPropItem(object propVal)
        {
            var retItem = new PropertyItem();

            var pe = propVal as IPersistEntity;
            var propLabel = 0;
            if (pe != null)
            {
                propLabel = pe.EntityLabel;
            }
            var ret = propVal.ToString();
            if (ret == propVal.GetType().FullName)
            {
                ret = propVal.GetType().Name;
            }

            retItem.Value = ret;
            retItem.IfcLabel = propLabel;

            return retItem;
        }

        private void FillObjectData()
        {
            if (_objectProperties.Count > 0) 
                return; //don't fill unless empty
            if (_entity == null) 
                return;

            _objectProperties.Add(new PropertyItem { Name = "Ifc Label", Value = "#" + _entity.EntityLabel, PropertySetName = "General" });

            var ifcType = _entity.ExpressType;
            _objectProperties.Add(new PropertyItem { Name = "Type", Value = ifcType.Type.Name, PropertySetName = "General" });

            var ifcObj = _entity as IIfcObject;
            if (ifcObj != null)
            {
                var reldefines = ifcObj.IsTypedBy.FirstOrDefault();

                if (reldefines != null)
                {
                    var typeEntity = reldefines.RelatingType;

                    _objectProperties.Add(new PropertyItem { Name = "Defining Type", Value = typeEntity.Name, PropertySetName = "General", IfcLabel = typeEntity.EntityLabel });
                }
            }

            var props = ifcType.Properties.Values;
            foreach (var prop in props)
            {
                ReportProp(_entity, prop, ChkVerbose.IsChecked.HasValue && ChkVerbose.IsChecked.Value);
            }
            var invs = ifcType.Inverses;
            
            foreach (var inverse in invs)
            {
                ReportProp(_entity, inverse, false);
            }

            

            var root = _entity as IIfcRoot;
            if (root == null)
                return;            
            _objectProperties.Add(new PropertyItem {Name = "Name", Value = root.Name, PropertySetName = "OldUI"});
            _objectProperties.Add(new PropertyItem { Name = "Description", Value = root.Description, PropertySetName = "OldUI" });
            _objectProperties.Add(new PropertyItem { Name = "GUID", Value = root.GlobalId, PropertySetName = "OldUI" });
            if (root.OwnerHistory != null)
            {
                _objectProperties.Add(new PropertyItem
                {
                    Name = "Ownership",
                    Value =
                        root.OwnerHistory.OwningUser + " using " +
                        root.OwnerHistory.OwningApplication.ApplicationIdentifier,
                    PropertySetName = "OldUI"
                });
            }

            //now do properties in further specialisations that are text labels
            foreach (var pInfo in ifcType.Properties.Where
                (p => p.Value.EntityAttribute.Order > 4
                      && p.Value.EntityAttribute.State != EntityAttributeState.DerivedOverride)
                ) //skip the first for of root, and derived and things that are objects
            {
                var val = pInfo.Value.PropertyInfo.GetValue(_entity, null);
                if (val == null || !(val is ExpressType))
                    continue;
                var pi = new PropertyItem
                {
                    Name = pInfo.Value.PropertyInfo.Name,
                    Value = ((ExpressType) val).ToString(),
                    PropertySetName = "OldUI"
                };
                _objectProperties.Add(pi);
            }
        }

        public IfcStore Model
        {
            get { return (IfcStore) GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Model.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register("Model", typeof (IfcStore), typeof (IfcMetaDataControl),
                new PropertyMetadata(null, OnModelChanged));


        private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = d as IfcMetaDataControl;
            if (ctrl == null) 
                return;
            if (e.NewValue == null)
            {
                ctrl.Clear();
            }
            ctrl.DataRebind(null);
        }


        private void Clear()
        {
            _objectProperties.Clear();
            _quantities.Clear();
            _properties.Clear();
            _typeProperties.Clear();
            _materials.Clear();
            NotifyPropertyChanged("Properties");
            NotifyPropertyChanged("PropertySets");
        }
        
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        #endregion

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            var hyperlink = sender as Hyperlink;
            if (hyperlink == null)
                throw new ArgumentNullException();
            if (e.Uri.Host == "entitylabel")
            {
                var lab = e.Uri.AbsolutePath.Substring(1);
                int iLabel;
                if (int.TryParse(lab, out iLabel))
                {
                    SelectedEntity = Model.Instances[iLabel];
                }
            }
        }

        private void CheckBoxChanged(object sender, RoutedEventArgs e)
        {
            _objectProperties.Clear();
            FillObjectData();
        }

        private bool _preventHistory;

        private void Back(object sender, RoutedEventArgs e)
        {
            _preventHistory = true;
            var v = _history.Pop();
            if (v != null)
                SelectedEntity = v;
            _preventHistory = false;
            UpdateButtonBack();
        }

        private readonly HistoryCollection<IPersistEntity> _history = new HistoryCollection<IPersistEntity>(20);
    }
}