using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PropertyTools.Wpf;
using Xbim.IO;
using System.Windows;
using Xbim.Ifc2x3.Kernel;
using System.Windows.Data;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.ExternalReferenceResource;
using System.Windows.Controls.Primitives;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Specialized;
using Xbim.XbimExtensions.Interfaces;
using System.Collections.ObjectModel;
using Xbim.IO.ViewModels;

namespace Xbim.Presentation
{
    public class XbimTreeview : TreeListBox
    {

        public XbimTreeview()
        {
            SelectionMode = System.Windows.Controls.SelectionMode.Single; //always use single selection mode
        }

        protected override void OnSelectionChanged(System.Windows.Controls.SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);
            if (e.AddedItems.Count > 0)
            {
                IPersistIfcEntity p = ((IXbimViewModel)(e.AddedItems[0])).Entity;
                IPersistIfcEntity p2 = SelectedEntity;
                if (p2 == null)
                    SelectedEntity = p;
                else if (!(p.ModelOf == p2.ModelOf && p.EntityLabel==p2.EntityLabel)) 
                    SelectedEntity = p;
            }
        }



        public IPersistIfcEntity SelectedEntity
        {
            get { return (IPersistIfcEntity)GetValue(SelectedEntityProperty); }
            set { SetValue(SelectedEntityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedEntity.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedEntityProperty =
            DependencyProperty.Register("SelectedEntity", typeof(IPersistIfcEntity), typeof(XbimTreeview), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits,
                                                                      new PropertyChangedCallback(OnSelectedEntityChanged)));

        private static void OnSelectedEntityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            XbimTreeview view = d as XbimTreeview;
            if (view != null && e.NewValue is IPersistIfcEntity)
            {
                view.UnselectAll();
                IPersistIfcEntity newVal = (IPersistIfcEntity)(e.NewValue);
                if (newVal != null) view.Select(newVal);
                return;
            }
        }

        // todo: bonghi: this one is too slow on Architettonico_def.xBIM, so I'm patching it for a specific hierarchy, but it needs serious redesign for efficiency
        private void Select(IPersistIfcEntity newVal, bool tryOptimise = true)
        {
            if (this.ViewDefinition == XbimViewType.SpatialStructure && tryOptimise)
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
                todo: bonghi: this is still not optimal, because it can only point to simple IPersistIfcEntity and not intermediate IXbimViewModels.
 
                */
                IfcProduct p = newVal as IfcProduct;
                if (p != null)
                {
                    var found = FindUnderContainingSpace(newVal, p);  // direct search 
                    if (found == null)
                    { 
                        // search for composed object
                        IfcRelDecomposes decomp = p.Decomposes.FirstOrDefault();
                        
                        if (decomp!=null && decomp.RelatingObject is IfcProduct) // 
                        {
                            found = FindUnderContainingSpace(newVal, (IfcProduct)(decomp.RelatingObject));  // direct search of parent through containing space
                            if (found != null)
                                found = FindItemDepthFirst(found, newVal); // then search for the child
                        }
                        else
                        {
                            // do basic search
                            this.Select(newVal, false);
                        }
                        
                    }
                    if (found != null)
                    {
                        Highlight(found);
                        return;
                    }
                }
                // if optimised search fails revert to brute force expansion
                this.Select(newVal, false);
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

        private IXbimViewModel FindUnderContainingSpace(IPersistIfcEntity newVal, IfcProduct p)
        {
            var ContainingSpace = p.IsContainedIn().FirstOrDefault();
            if (ContainingSpace != null)
            {
                var ContainingSpaceView = FindItemBreadthFirst(ContainingSpace);
                if (ContainingSpaceView != null)
                {
                    var found = FindItemDepthFirst(ContainingSpaceView, newVal);
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

        public IXbimViewModel FindItemBreadthFirst(IPersistIfcEntity entity)
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
                current = queue.Dequeue();
            }
            return null;
        }

        public IXbimViewModel FindItemDepthFirst(IXbimViewModel node, IPersistIfcEntity entity)
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
        // it should be possible in the redesign to build an IXbimViewModel from an IPersistIfcEntity.
        private static bool IsMatch(IXbimViewModel node, IPersistIfcEntity entity)
        {
            return node.Model == entity.ModelOf && node.EntityLabel == entity.EntityLabel;
        }

        public XbimViewType ViewDefinition
        {
            get { return (XbimViewType)GetValue(ViewDefinitionProperty); }
            set { SetValue(ViewDefinitionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ViewDefinition.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ViewDefinitionProperty =
            DependencyProperty.Register("ViewDefinition", typeof(XbimViewType), typeof(XbimTreeview), new UIPropertyMetadata(XbimViewType.SpatialStructure, new PropertyChangedCallback(OnViewDefinitionChanged)));

        private static void OnViewDefinitionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

        }


       
        public XbimModel Model
        {
            get { return (XbimModel)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Model.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register("Model", typeof(XbimModel), typeof(XbimTreeview), new UIPropertyMetadata(null, new PropertyChangedCallback(OnModelChanged)));

        private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            XbimTreeview tv = d as XbimTreeview;
            XbimModel model = e.NewValue as XbimModel;
            
            if (tv != null && model != null)
            {
                model.ReferencedModels.CollectionChanged += tv.RefencedModels_CollectionChanged;
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
                    default:
                        break;
                }
            }
            else
            {
                if (tv != null) //unbind
                {
                    tv.HierarchySource = Enumerable.Empty<XbimModelViewModel>();
                }
            }
        }

        void RefencedModels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems.Count > 0)
            {  
                XbimReferencedModel refModel = e.NewItems[0] as XbimReferencedModel;
                XbimModelViewModel vm = HierarchySource.Cast<XbimModelViewModel>().FirstOrDefault();
                if(vm!=null)
                {
                    vm.AddRefModel(new XbimRefModelViewModel(refModel, null));
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (XbimReferencedModel refModel in e.OldItems)
                {
                    XbimModelViewModel vm = HierarchySource.Cast<XbimModelViewModel>().FirstOrDefault();
                    if (vm != null)
                    {
                        var modelVM = vm.Children.FirstOrDefault(m => (m as XbimRefModelViewModel).RefModel == refModel) as XbimRefModelViewModel;
                        vm.RemoveRefModel(modelVM);
                    }
                }
            }
        }

        private void ViewSpatialStructure()
        {
            IfcProject project = Model.IfcProject as IfcProject;
            if (project != null)
            {
                this.ChildrenBinding = new Binding("Children");
                List<SpatialViewModel> svList = new List<SpatialViewModel>();
                foreach (var item in project.GetSpatialStructuralElements())
                {
                    var sv = new SpatialViewModel(item, null);
                    svList.Add(sv); 
                }
                
                this.HierarchySource = svList;
                foreach (var child in svList)
                {
                    LazyLoadAll(child);
                }
            }
            else //Load any spatialstructure
            {
            }
        }
        private void ViewModel()
        {
            IfcProject project = Model.IfcProject as IfcProject;
            if (project != null)
            {
              
                this.ChildrenBinding = new Binding("Children");
                ObservableCollection<XbimModelViewModel> svList = new ObservableCollection<XbimModelViewModel>();  
                svList.Add(new XbimModelViewModel(project, null));
                this.HierarchySource = svList;
            }
        }
        private void LazyLoadAll(IXbimViewModel parent)
        {

            foreach (var child in parent.Children)
            {
                LazyLoadAll(child);
            }
            
        }


        private void Expand(IXbimViewModel treeitem)
        {
            treeitem.IsExpanded = true;
            foreach (var child in treeitem.Children)
            {
                Expand(child);
            }
        }

        private void ViewClassification()
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
            if (Model != null)
            {
                this.ChildrenBinding = new Binding("Children");
                List<GroupViewModel> modelList = new List<GroupViewModel>();
                
                var groups = Model.Instances.OfType<IfcGroup>();
                var groupedObjects = new List<IfcRoot>();
                foreach (var obj in Model.Instances.OfType<IfcRelAssignsToGroup>())
                {
                    groupedObjects.AddRange(obj.RelatedObjects.ToList());
                }

                foreach (var item in groups)
                {
                    if(!groupedObjects.Contains(item))
                        modelList.Add(new GroupViewModel(item, null)); //add only root groups/systems
                }
                this.HierarchySource = modelList;
                //foreach (var child in modelList)
                //{
                //    LazyLoadAll(child); //why to do this?
                //}
            }
        }

        public void Regenerate()
        {
            if (Model != null )
            {
                Model.ReferencedModels.CollectionChanged += RefencedModels_CollectionChanged;
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
                    default:
                        break;
                }
            }
        }
    }
}
