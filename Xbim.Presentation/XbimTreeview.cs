using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PropertyTools.Wpf;
using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc.ViewModels;
using Xbim.Ifc4.Interfaces;

namespace Xbim.Presentation
{
    public class XbimTreeview : TreeListBox
    {

        public XbimTreeview()
        {
            SelectionMode = SelectionMode.Single; //always use single selection mode
        }

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);
            if (e.AddedItems.Count <= 0)
                return;
            var p = ((IXbimViewModel)(e.AddedItems[0])).Entity;
            var p2 = SelectedEntity;
            if (p2 == null)
                SelectedEntity = p;
            else if (!(Equals(p.Model, p2.Model) && p.EntityLabel==p2.EntityLabel)) 
                SelectedEntity = p;
        }
        
        public IPersistEntity SelectedEntity
        {
            get { return (IPersistEntity)GetValue(SelectedEntityProperty); }
            set { SetValue(SelectedEntityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedEntity.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedEntityProperty =
            DependencyProperty.Register("SelectedEntity", typeof(IPersistEntity), typeof(XbimTreeview), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, OnSelectedEntityChanged));

        private static void OnSelectedEntityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            XbimTreeview view = d as XbimTreeview;
            if (view != null && e.NewValue is IPersistEntity)
            {
                view.UnselectAll();
                IPersistEntity newVal = (IPersistEntity)(e.NewValue);
                if (newVal != null) view.Select(newVal);
            }
        }

        // todo: bonghi: this one is too slow on Architettonico_def.xBIM, so I'm patching it for a specific hierarchy, but it needs serious redesign for efficiency
        private void Select(IPersistEntity newVal, bool tryOptimise = true)
        {
            if (ViewDefinition == XbimViewType.SpatialStructure && tryOptimise)
            {
                /*
                We know that the structure in this case looks like:
                 
                XbimModelViewModel
                    model.project.GetSpatialStructuralElements (into SpatialViewModel)
                    model.RefencedModels (into XbimRefModelViewModel)
                        model.project.GetSpatialStructuralElements (into SpatialViewModel)

                SpatialViewModel
                    SpatialViewModel 
                    ContainedElementsViewModel
                        IfcProductModelView
                            IfcProductModelView
                 
                If a model is a product then find its space with breadth first then expand to it with depth first.
                todo: bonghi: this is still not optimal, because it can only point to simple IPersistEntity and not intermediate IXbimViewModels.
 
                */
                var p = newVal as IIfcProduct;
                if (p != null)
                {
                    var found = FindUnderContainingSpace(newVal, p);  // direct search 
                    if (found == null)
                    { 
                        // search for composed object
                        var decomp = p.Decomposes.FirstOrDefault();
                        
                        if (decomp?.RelatingObject is IIfcProduct) // 
                        {
                            found = FindUnderContainingSpace(newVal, (IIfcProduct)(decomp.RelatingObject));  // direct search of parent through containing space
                            if (found != null)
                                found = FindItemDepthFirst(found, newVal); // then search for the child
                        }
                        else
                        {
                            // do basic search
                            Select(newVal, false);
                        }
                        
                    }
                    if (found != null)
                    {
                        Highlight(found);
                        return;
                    }
                }
                // if optimised search fails revert to brute force expansion
                Select(newVal, false);
            }
            else
            {
                foreach (var item in HierarchySource.OfType<IXbimViewModel>())
                {
                    IXbimViewModel toSelect = FindItemDepthFirst(item, newVal);
                    if (toSelect != null)
                    {
                        Highlight(toSelect); 
                        return;
                    }
                }
            }
        }

        private IXbimViewModel FindUnderContainingSpace(IPersistEntity newVal, IIfcProduct p)
        {
            var containingSpace = p.IsContainedIn;
            if (containingSpace != null)
            {
                var containingSpaceView = FindItemBreadthFirst(containingSpace);
                if (containingSpaceView != null)
                {
                    var found = FindItemDepthFirst(containingSpaceView, newVal);
                    if (found != null)
                    {
                        return found;
                    }
                }
            }
            return null;
        }

        private void Highlight(IXbimViewModel toSelect)
        {
            UpdateLayout();
            ScrollIntoView(toSelect);
            toSelect.IsSelected = true;
            while (toSelect != null)
            {
                toSelect.IsExpanded = true;
                toSelect = toSelect.CreatingParent;
            }
        }

        public IXbimViewModel FindItemBreadthFirst(IPersistEntity entity)
        {
            Queue<IXbimViewModel> queue = new Queue<IXbimViewModel>();
            foreach (var item in HierarchySource.OfType<IXbimViewModel>())
            {
                queue.Enqueue(item);
            }
            IXbimViewModel current = queue.Dequeue();
            while (current != null)
            {
                if (IsMatch(current, entity))
                {
                    return current;
                }
                foreach (var item in current.Children)
                {
                    queue.Enqueue(item);
                }
                if (!queue.Any())
                    return null;
                current = queue.Dequeue();
            }
            return null;
        }

        public IXbimViewModel FindItemDepthFirst(IXbimViewModel node, IPersistEntity entity)
        {
            if (IsMatch(node, entity))
            {
                // node.IsExpanded = true; // commented because of new Highlighting mechanisms
                return node;
            }

            foreach (var child in node.Children)
            {
                IXbimViewModel res = FindItemDepthFirst(child, entity);
                if (res != null)
                {
                    // node.IsExpanded = true; //commented because of new Highlighting mechanisms
                    return res;
                }
            }
            return null;
        }

        // todo: bonghi: this function should be changed to match IXbimViewModel directly.
        // it should be possible in the redesign to build an IXbimViewModel from an IPersistEntity.
        private static bool IsMatch(IXbimViewModel node, IPersistEntity entity)
        {
            return Equals(node.Model, entity.Model) && node.EntityLabel == entity.EntityLabel;
        }

        public XbimViewType ViewDefinition
        {
            get { return (XbimViewType)GetValue(ViewDefinitionProperty); }
            set { SetValue(ViewDefinitionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ViewDefinition.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ViewDefinitionProperty =
            DependencyProperty.Register("ViewDefinition", typeof(XbimViewType), typeof(XbimTreeview), new UIPropertyMetadata(XbimViewType.SpatialStructure, OnViewDefinitionChanged));

        private static void OnViewDefinitionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

        }


       
        public IfcStore Model
        {
            get { return (IfcStore)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Model.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register("Model", typeof(IfcStore), typeof(XbimTreeview), new UIPropertyMetadata(null, OnModelChanged));

        private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            XbimTreeview tv = d as XbimTreeview;
            var model = e.NewValue as IfcStore;
            
            if (tv != null && model != null)
            {
                //TODO resolve reference models
                //model.ReferencedModels.CollectionChanged += tv.RefencedModels_CollectionChanged;
                switch (tv.ViewDefinition)
                {
                    case XbimViewType.SpatialStructure:
                        tv.ViewModel();
                        break;
                    case XbimViewType.Classification:
                        break;
                    case XbimViewType.Materials:
                        break;
                    case XbimViewType.IfcEntityType:
                        break;
                    case XbimViewType.Groups:
                        tv.ViewGroups();
                        break;
                }
            }
            else
            {
                if (tv != null) //unbind
                {
                    tv.UnselectAll();
                    tv.HierarchySource = Enumerable.Empty<XbimModelViewModel>();
                }
            }
        }

        private void RefencedModels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems.Count > 0)
            {  
                var refModel = e.NewItems[0] as XbimReferencedModel;
                var vm = HierarchySource.Cast<XbimModelViewModel>().FirstOrDefault();
                vm?.AddRefModel(new XbimRefModelViewModel(refModel, null));
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (XbimReferencedModel refModel in e.OldItems)
                {
                    XbimModelViewModel vm = HierarchySource.Cast<XbimModelViewModel>().FirstOrDefault();
                    if (vm != null)
                    {
                        var modelVm = vm.Children.FirstOrDefault(m =>
                        {
                            var xbimRefModelViewModel = m as XbimRefModelViewModel;
                            return xbimRefModelViewModel != null && xbimRefModelViewModel.RefModel == refModel;
                        }) as XbimRefModelViewModel;
                        vm.RemoveRefModel(modelVm);
                    }
                }
            }
        }

        protected void ViewSpatialStructure()
        {
            var project = Model.Instances.OfType<IIfcProject>().FirstOrDefault();
            if (project != null)
            {
                ChildrenPath="Children";
                List<SpatialViewModel> svList = new List<SpatialViewModel>();
                foreach (var item in project.SpatialStructuralElements)
                {
                    var sv = new SpatialViewModel(item, null);
                    svList.Add(sv); 
                }
                
                HierarchySource = svList;
                foreach (var child in svList)
                {
                    LazyLoadAll(child);
                }
            }
        }
        private void ViewModel()
        {
            var project = Model.Instances.OfType<IIfcProject>().FirstOrDefault();
            if (project != null)
            {
                ChildrenPath ="Children";
                ObservableCollection<XbimModelViewModel> svList = new ObservableCollection<XbimModelViewModel>();  
                svList.Add(new XbimModelViewModel(project, null));
                HierarchySource = svList;
            }
        }
        private void LazyLoadAll(IXbimViewModel parent)
        {
            foreach (var child in parent.Children)
            {
                LazyLoadAll(child);
            }
        }


        protected void Expand(IXbimViewModel treeitem)
        {
            treeitem.IsExpanded = true;
            foreach (var child in treeitem.Children)
            {
                Expand(child);
            }
        }

        protected void ViewClassification()
        {
            //IfcProject project = Model.IfcProject as IfcProject;
            //if (project != null)
            //{
            //    this.ChildrenBinding = new Binding("SubClassifications");
            //    List<ClassificationViewModel> sv = new List<ClassificationViewModel>();
            //    foreach (var item in Model.Instances.OfType<IfcClassification>())
            //    {
            //        sv.Add(new ClassificationViewModel(item));
            //    }
            //    this.HierarchySource = sv;
            //}
            //else //Load any spatialstructure
            //{
            //}
        }

        private void ViewGroups()
        {
            System.Collections.IEnumerable list = Enumerable.Empty<IfcGroupsViewModel>();
            if (Model != null && !string.IsNullOrEmpty(Model.FileName))
            {
                IfcGroupsViewModel v = new IfcGroupsViewModel(Model);
                if (v.Children.Any())
                {
                    ChildrenPath = "Children";
                    var glist = new List<IfcGroupsViewModel>();
                    glist.Add(v);
                    HierarchySource = glist;
                    return;
                }
            }
            HierarchySource = list;
        }

        public void Regenerate()
        {
            if (Model != null )
            {
               
               // Model.ReferencedModels.CollectionChanged += RefencedModels_CollectionChanged;
                switch (ViewDefinition)
                {
                    case XbimViewType.SpatialStructure:
                        ViewModel();
                        break;
                    case XbimViewType.Classification:
                        break;
                    case XbimViewType.Materials:
                        break;
                    case XbimViewType.IfcEntityType:
                        break;
                    case XbimViewType.Groups:
                        ViewGroups();
                        break;
                }
            }
        }
    }
}
