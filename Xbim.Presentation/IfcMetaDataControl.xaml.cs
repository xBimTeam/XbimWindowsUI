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
using System.Drawing.Design;
using System.Linq;
using System.Runtime.Remoting;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Navigation;
using Xbim.Ifc2x3.ControlExtension;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.MaterialResource;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.PropertyResource;
using Xbim.Ifc2x3.QuantityResource;
using Xbim.Ifc2x3.SharedMgmtElements;
using Xbim.Ifc2x3.StructuralAnalysisDomain;
using Xbim.IO;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;
using Xbim.XbimExtensions.SelectTypes;

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

        private IPersistIfcEntity _entity;

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
            {
                _materialGroups.GroupDescriptions.Add(new PropertyGroupDescription("PropertySetName"));
            }
            _relationsGroups = new ListCollectionView(_relationsProperties);
            if (_relationsGroups.GroupDescriptions != null)
            {
                _relationsGroups.GroupDescriptions.Add(new PropertyGroupDescription("PropertySetName"));
                _relationsGroups.SortDescriptions.Add(new SortDescription("PropertySetName", ListSortDirection.Ascending));
            }
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
            else if (selectedTab == RelationsTab)
                FillRelationsData();
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

        private readonly ListCollectionView _relationsGroups;

        public ListCollectionView RelationsGroups
        {
            get { return _relationsGroups; }
        }

        private readonly ObservableCollection<PropertyItem> _objectProperties = new ObservableCollection<PropertyItem>();

        public ObservableCollection<PropertyItem> ObjectProperties
        {
            get { return _objectProperties; }
        }

        private readonly ObservableCollection<PropertyItem> _relationsProperties = new ObservableCollection<PropertyItem>();

        public ObservableCollection<PropertyItem> RelationsProperties
        {
            get { return _relationsProperties; }
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

        public IPersistIfcEntity SelectedEntity
        {
            get { return (IPersistIfcEntity) GetValue(SelectedEntityProperty); }
            set { SetValue(SelectedEntityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IfcInstance.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedEntityProperty =
            DependencyProperty.Register("SelectedEntity", typeof (IPersistIfcEntity), typeof (IfcMetaDataControl),
                new UIPropertyMetadata(null, OnSelectedEntityChanged));


        private static void OnSelectedEntityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = d as IfcMetaDataControl;
            if (ctrl != null && e.NewValue is IPersistIfcEntity)
            {
                ctrl.DataRebind((IPersistIfcEntity) e.NewValue);
            }
        }

        private void DataRebind(IPersistIfcEntity entity)
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
            var ifcObj = _entity as IfcObject;
            if (ifcObj == null)
                return;
            var typeEntity = ifcObj.GetDefiningType();
            if (typeEntity == null)
                return;
            var ifcType = IfcMetaData.IfcType(typeEntity);
            _typeProperties.Add(new PropertyItem {Name = "Type", Value = ifcType.Type.Name});
            _typeProperties.Add(new PropertyItem {Name = "Ifc Label", Value = "#" + typeEntity.EntityLabel});

            _typeProperties.Add(new PropertyItem {Name = "Name", Value = typeEntity.Name});
            _typeProperties.Add(new PropertyItem {Name = "Description", Value = typeEntity.Description});
            _typeProperties.Add(new PropertyItem {Name = "GUID", Value = typeEntity.GlobalId});
            _typeProperties.Add(new PropertyItem
            {
                Name = "Ownership",
                Value =
                    typeEntity.OwnerHistory.OwningUser + " using " +
                    typeEntity.OwnerHistory.OwningApplication.ApplicationIdentifier
            });
            //now do properties in further specialisations that are text labels
            foreach (var pInfo in ifcType.IfcProperties.Where
                (p => p.Value.IfcAttribute.Order > 4
                      && p.Value.IfcAttribute.State != IfcAttributeState.DerivedOverride)
                ) //skip the first for of root, and derived and things that are objects
            {
                var val = pInfo.Value.PropertyInfo.GetValue(typeEntity, null);
                if (!(val is ExpressType))
                    continue;
                var pi = new PropertyItem {Name = pInfo.Value.PropertyInfo.Name, Value = ((ExpressType) val).ToPart21};
                _typeProperties.Add(pi);
            }
        }

        private void FillQuantityData()
        {
            if (_quantities.Count > 0) return; //don't fill unless empty
            //now the property sets for any 

            // local cache for efficiency

            if (_entity is IfcObject)
            {
                var ifcObj = _entity as IfcObject;
                var modelUnits = _entity.ModelOf.Instances.OfType<IfcUnitAssignment>().FirstOrDefault();
                    // not optional, should never return void in valid model

                foreach (
                    var relDef in
                        ifcObj.IsDefinedByProperties.Where(r => r.RelatingPropertyDefinition is IfcElementQuantity))
                {
                    var pSet = relDef.RelatingPropertyDefinition as IfcElementQuantity;
                    AddQuantityPSet(pSet, modelUnits);
                }
            }
            else if (_entity is IfcTypeObject)
            {
                var asIfcTypeObject = _entity as IfcTypeObject;
                var modelUnits = _entity.ModelOf.Instances.OfType<IfcUnitAssignment>().FirstOrDefault();
                // not optional, should never return void in valid model

                if (asIfcTypeObject.HasPropertySets == null)
                    return;
                foreach (var pSet in asIfcTypeObject.HasPropertySets.OfType<IfcElementQuantity>())
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

        private void AddQuantityPSet(IfcElementQuantity pSet, IfcUnitAssignment modelUnits)
        {
            if (pSet == null)
                return;
            foreach (var item in pSet.Quantities.OfType<IfcPhysicalSimpleQuantity>())
                // currently only handles IfcPhysicalSimpleQuantity
            {
                var v = modelUnits.GetUnitFor(item);
                _quantities.Add(new PropertyItem
                {
                    PropertySetName = pSet.Name,
                    Name = item.Name,
                    Value = item + " " + v.GetName()
                });
            }
        }

        private void FillPropertyData()
        {
            if (_properties.Any()) //don't try to fill unless empty
                return;
            //now the property sets for any 

            if (_entity is IfcObject)
            {
                var asIfcObject = (IfcObject) _entity;
                foreach (
                    var pSet in
                        asIfcObject.IsDefinedByProperties.Select(
                            relDef => relDef.RelatingPropertyDefinition as IfcPropertySet)
                    )
                    AddPropertySet(pSet);
            }
            else if (_entity is IfcTypeObject)
            {
                var asIfcTypeObject = _entity as IfcTypeObject;
                if (asIfcTypeObject.HasPropertySets == null)
                    return;
                foreach (var pSet in asIfcTypeObject.HasPropertySets.OfType<IfcPropertySet>())
                {
                    AddPropertySet(pSet);
                }
            }

        }

        private void AddPropertySet(IfcPropertySet pSet)
        {
            if (pSet == null)
                return;
            foreach (var item in pSet.HasProperties.OfType<IfcPropertySingleValue>()) //only handle simple properties
            {
                var val = "";
                if (item.NominalValue != null)
                {
                    var nomVal = (ExpressType) (item.NominalValue);
                    val = nomVal.Value != null
                        ? nomVal.Value.ToString()
                        : item.NominalValue.ToString();
                }
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

            if (_entity is IfcObject)
            {
                var ifcObj = _entity as IfcObject;
                var matRels = ifcObj.HasAssociations.OfType<IfcRelAssociatesMaterial>();
                foreach (var matRel in matRels)
                {
                    AddMaterialData(matRel.RelatingMaterial, "");
                }
            }
            else if (_entity is IfcTypeObject)
            {
                var ifcObj = _entity as IfcTypeObject;
                var matRels = ifcObj.HasAssociations.OfType<IfcRelAssociatesMaterial>();
                foreach (var matRel in matRels)
                {
                    AddMaterialData(matRel.RelatingMaterial, "");
                }
            }
        }

        private void AddMaterialData(IfcMaterialSelect matSel, string setName)
        {
            if (matSel is IfcMaterial) //simplest just add it
                _materials.Add(new PropertyItem
                {
                    Name = string.Format("{0} [#{1}]", ((IfcMaterial) matSel).Name, matSel.EntityLabel),
                    PropertySetName = setName,
                    Value = ""
                });
            else if (matSel is IfcMaterialLayer)
                _materials.Add(new PropertyItem
                {
                    Name = string.Format("{0} [#{1}]", ((IfcMaterialLayer) matSel).Material.Name, matSel.EntityLabel),
                    Value = ((IfcMaterialLayer) matSel).LayerThickness.Value.ToString(),
                    PropertySetName = setName
                });
            else if (matSel is IfcMaterialList)
            {
                foreach (var mat in ((IfcMaterialList) matSel).Materials)
                {
                    _materials.Add(new PropertyItem
                    {
                        Name = string.Format("{0} [#{1}]", mat.Name, mat.EntityLabel),
                        PropertySetName = setName,
                        Value = ""
                    });
                }
            }
            else if (matSel is IfcMaterialLayerSet)
            {
                foreach (var item in ((IfcMaterialLayerSet) matSel).MaterialLayers) //recursive call to add materials
                {
                    AddMaterialData(item, ((IfcMaterialLayerSet) matSel).LayerSetName);
                }
            }
            else if (matSel is IfcMaterialLayerSetUsage)
            {
                foreach (var item in ((IfcMaterialLayerSetUsage) matSel).ForLayerSet.MaterialLayers)
                    //recursive call to add materials
                {
                    AddMaterialData(item, ((IfcMaterialLayerSetUsage) matSel).ForLayerSet.LayerSetName);
                }
            }
        }

        private void ReportProp(IPersistIfcEntity entity, IfcMetaProperty prop, bool verbose)
        {
            var propVal = prop.PropertyInfo.GetValue(entity, null);
            if (propVal == null)
            {
                if (!verbose)
                    return;
                propVal = "<null>";
            }
            
            if (prop.IfcAttribute.IsEnumerable)
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
            var propLabel = 0;
            if (propVal is IPersistIfcEntity pe)
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

        private void FillRelationsData()
        {
            if (_relationsProperties.Any()) //don't fill unless empty
                return;
            if (_entity == null)
                return;

            if (_entity is IfcProduct ifcProd)
            {
                foreach (var item in ifcProd.IsDefinedBy.OfType<IfcRelDefinesByType>())
                {
                    foreach (var ret in GetViewGridEntries(item, "IsDefinedByType", _entity))
                    {
                        _relationsProperties.Add(ret);
                    }
                }
                foreach (var item in ifcProd.HasAssignments)
                {
                    foreach (var ret in GetViewGridEntries(item, "HasAssignments", _entity))
                    {
                        _relationsProperties.Add(ret);
                    }
                }
                foreach (var item in ifcProd.HasAssociations)
                {
                    foreach (var ret in GetViewGridEntries(item, "HasAssociations", _entity))
                    {
                        _relationsProperties.Add(ret);
                    }
                }
            }
            if (_entity is IfcElement ifcElem)
            {
                foreach (var item in ifcElem.ConnectedFrom)
                {
                    foreach (var ret in GetViewGridEntries(item, "ConnectedFrom", _entity))
                    {
                        _relationsProperties.Add(ret);
                    }
                }
                foreach (var item in ifcElem.ConnectedTo)
                {
                    foreach (var ret in GetViewGridEntries(item, "ConnectedTo", _entity))
                    {
                        _relationsProperties.Add(ret);
                    }
                }
                foreach (var item in ifcElem.ProvidesBoundaries)
                {
                    foreach (var ret in GetViewGridEntries(item, "ProvidesBoundaries", _entity))
                    {
                        _relationsProperties.Add(ret);
                    }
                }
            }
        }

        private IEnumerable<PropertyItem> GetViewGridEntries(IfcRelSpaceBoundary item, string groupName, IPersistIfcEntity entity)
        {
            var other = item.RelatingSpace;

            var nm = item.GetType().Name;
            var lab = other.EntityLabel;
            var desc = other.GetType().ToString();
            yield return new PropertyItem { Name = nm, Value = desc, PropertySetName = groupName, IfcLabel = lab };
        }

        private IEnumerable<PropertyItem> GetViewGridEntries(IfcRelConnectsElements item, string groupName, IPersistIfcEntity entity)
        {
            IfcElement other = item.RelatingElement.EntityLabel == entity.EntityLabel
                ? item.RelatedElement
                : item.RelatingElement;


            var nm = item.GetType().Name;
            var lab = other.EntityLabel;
            var desc = other.GetType().ToString();
            yield return new PropertyItem { Name = nm, Value = desc, PropertySetName = groupName, IfcLabel = lab };
        }

        private IEnumerable<PropertyItem> GetViewGridEntries(IfcRelDefinesByType item, string groupName, IPersistIfcEntity entity)
        {           
            var nm = item.GetType().Name;
            var lab = item.RelatingType.EntityLabel;
            var desc = item.RelatingType.GetType().ToString();
            yield return new PropertyItem { Name = nm, Value = desc, PropertySetName = groupName, IfcLabel = lab };
        }

        private IEnumerable<PropertyItem> GetViewGridEntries(IfcRelAssigns item, string groupName, IPersistIfcEntity entity)
        {
            // IfcRelAssignsToProcess, IfcRelAssignsToProduct, IfcRelAssignsToControl, IfcRelAssignsToResource, IfcRelAssignsToActor, IfcRelAssignsToGroup
            var nm = item.GetType().Name;
            var lab = item.EntityLabel;
            var desc = item.Description;

            if (item is IfcRelAssignsToProcess pcs)
            {
                if (pcs != null)
                {
                    lab = pcs.RelatingProcess.EntityLabel;
                    desc = pcs.RelatingProcess.GetType().ToString();
                }
            }
            else if (item is IfcRelAssignsToProduct pdc)
            {
                if (pdc != null)
                {
                    lab = pdc.RelatingProduct.EntityLabel;
                    desc = pdc.RelatingProduct.GetType().ToString();
                }
            }
            else if (item is IfcRelAssignsToControl ctl)
            {
                if (ctl != null)
                {
                    lab = ctl.RelatingControl.EntityLabel;
                    desc = ctl.RelatingControl.GetType().ToString();
                }
            }
            else if (item is IfcRelAssignsToResource res)
            {
                if (res != null)
                {
                    lab = res.RelatingResource.EntityLabel;
                    desc = res.RelatingResource.GetType().ToString();
                }
            }
            else if (item is IfcRelAssignsToActor act)
            {
                if (act != null)
                {
                    lab = act.RelatingActor.EntityLabel;
                    desc = act.RelatingActor.GetType().ToString();
                }
            }
            else if (item is IfcRelAssignsToGroup grp)
            {
                if (grp != null)
                {
                    lab = grp.RelatingGroup.EntityLabel;
                    desc = grp.RelatingGroup.GetType().ToString();
                }
            }

            yield return new PropertyItem { Name = nm, Value = desc, PropertySetName = groupName, IfcLabel = lab };
        }
        private IEnumerable<PropertyItem> GetViewGridEntries(IfcRelAssociates item, string groupName, IPersistIfcEntity entity)
        {
            var nm = item.GetType().Name;
            var lab = item.EntityLabel;
            var desc = item.Description;

            if (item is IfcRelAssociatesClassification cls)
            {
                if (cls != null)
                {
                    lab = cls.RelatingClassification.EntityLabel;
                    desc = cls.RelatingClassification.GetType().ToString();
                }
            }
            else if (item is IfcRelAssociatesDocument doc)
            {
                if (doc != null)
                {
                    lab = doc.RelatingDocument.EntityLabel;
                    desc = doc.RelatingDocument.GetType().ToString();
                }
            }
            else if (item is IfcRelAssociatesLibrary lib)
            {
                if (lib != null)
                {
                    lab = lib.RelatingLibrary.EntityLabel;
                    desc = lib.RelatingLibrary.GetType().ToString();
                }
            }
            else if (item is IfcRelAssociatesMaterial mat)
            {
                if (mat != null)
                {
                    lab = mat.RelatingMaterial.EntityLabel;
                    desc = mat.RelatingMaterial.GetType().ToString();
                }
            }
            else if (item is IfcRelAssociatesProfileProperties prof)
            {
                if (prof != null)
                {
                    lab = prof.RelatingProfileProperties.EntityLabel;
                    desc = prof.RelatingProfileProperties.GetType().ToString();
                }
            }
            else if (item is IfcRelAssociatesAppliedValue appVal)
            {
                if (appVal != null)
                {
                    lab = appVal.RelatingAppliedValue.EntityLabel;
                    desc = appVal.RelatingAppliedValue.GetType().ToString();
                }
            }
            else if (item is IfcRelAssociatesApproval approv)
            {
                if (approv != null)
                {
                    lab = approv.RelatingApproval.EntityLabel;
                    desc = approv.RelatingApproval.GetType().ToString();
                }
            }
            else if (item is IfcRelAssociatesConstraint constr)
            {
                if (constr != null)
                {
                    lab = constr.RelatingConstraint.EntityLabel;
                    desc = constr.RelatingConstraint.GetType().ToString();
                }
            }
            yield return new PropertyItem { Name = nm, Value = desc, PropertySetName = groupName, IfcLabel = lab };
        }

        private void FillObjectData()
        {
            if (_objectProperties.Any()) 
                return; //don't fill unless empty
            if (_entity == null) 
                return;

            _objectProperties.Add(new PropertyItem { Name = "Ifc Label", Value = "#" + _entity.EntityLabel, PropertySetName = "General" });

            var ifcType = IfcMetaData.IfcType(_entity);
            _objectProperties.Add(new PropertyItem {Name = "Type", Value = ifcType.Type.Name, PropertySetName = "General"});

            var ifcObj = _entity as IfcObject;
            if (ifcObj != null)
            {
                var typeEntity = ifcObj.GetDefiningType();
                if (typeEntity != null)
                {
                    _objectProperties.Add(new PropertyItem { Name = "Defining Type", Value = typeEntity.Name, PropertySetName = "General", IfcLabel = typeEntity.EntityLabel });
                }
            }

            var props = ifcType.IfcProperties.Values;
            foreach (var prop in props)
            {
                ReportProp(_entity, prop, ChkVerbose.IsChecked.HasValue && ChkVerbose.IsChecked.Value);
            }
            var invs = ifcType.IfcInverses;
            
            foreach (var inverse in invs)
            {
                ReportProp(_entity, inverse, false);
            }

            

            var root = _entity as IfcRoot;
            if (root == null)
                return;
            _objectProperties.Add(new PropertyItem {Name = "Name", Value = root.Name, PropertySetName = "OldUI"});
            _objectProperties.Add(new PropertyItem { Name = "Description", Value = root.Description, PropertySetName = "OldUI" });
            _objectProperties.Add(new PropertyItem { Name = "GUID", Value = root.GlobalId, PropertySetName = "OldUI" });
            _objectProperties.Add(new PropertyItem
            {
                Name = "Ownership",
                Value =
                    root.OwnerHistory.OwningUser + " using " +
                    root.OwnerHistory.OwningApplication.ApplicationIdentifier,
                PropertySetName = "OldUI"
            });
            //now do properties in further specialisations that are text labels
            foreach (var pInfo in ifcType.IfcProperties.Where
                (p => p.Value.IfcAttribute.Order > 4
                      && p.Value.IfcAttribute.State != IfcAttributeState.DerivedOverride)
                ) //skip the first for of root, and derived and things that are objects
            {
                var val = pInfo.Value.PropertyInfo.GetValue(_entity, null);
                if (val == null || !(val is ExpressType))
                    continue;
                var pi = new PropertyItem
                {
                    Name = pInfo.Value.PropertyInfo.Name,
                    Value = ((ExpressType) val).ToPart21,
                    PropertySetName = "OldUI"
                };
                _objectProperties.Add(pi);
            }
        }

        public XbimModel Model
        {
            get { return (XbimModel) GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Model.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register("Model", typeof (XbimModel), typeof (IfcMetaDataControl),
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
            _relationsProperties.Clear();
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
                var iLabel = 0;
                if (int.TryParse(lab, out iLabel))
                {
                    SelectedEntity = Model.InstancesLocal[iLabel];
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

        private readonly HistoryCollection<IPersistIfcEntity> _history = new HistoryCollection<IPersistIfcEntity>(20);
    }
}