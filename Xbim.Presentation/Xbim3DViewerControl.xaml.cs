using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using Xbim.Common.Geometry;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;

namespace Xbim.Presentation
{
    /// <summary>
    /// Interaction logic for Xbim3DViewerControl.xaml
    /// </summary>
    public partial class Xbim3DViewerControl : UserControl
    {
        #region Fields

        public Dictionary<ModifierKeys, XbimMouseClickActions> MouseModifierKeyBehaviour = new Dictionary<ModifierKeys, XbimMouseClickActions>();
        private XbimRect3D _modelBounds;
        private XbimRect3D _viewBounds;
        private XbimVector3D _modelTranslation;
        private EntitySelection _selection;
        #endregion

        #region Events and dependency properties

        public new static readonly RoutedEvent LoadedEvent =
            EventManager.RegisterRoutedEvent("LoadedEvent", RoutingStrategy.Bubble, typeof(RoutedEventHandler),
                                             typeof(Xbim3DViewerControl));

        public new event RoutedEventHandler Loaded
        {
            add { AddHandler(LoadedEvent, value); }
            remove { RemoveHandler(LoadedEvent, value); }
        }

        public XbimModel Model
        {
            get { return (XbimModel)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Model.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register("Model", typeof(XbimModel), typeof(DrawingControl3D), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits,
                                                                      OnModelChanged));



        public HelixViewport3D Viewport
        {
            get { return (HelixViewport3D)GetValue(ViewportProperty); }
            set { SetValue(ViewportProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Viewport.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ViewportProperty =
            DependencyProperty.Register("Viewport", typeof(HelixViewport3D), typeof(DrawingControl3D), new PropertyMetadata(null));
       
        #endregion

        #region Handlers

        private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DrawingControl3D d3D = d as DrawingControl3D;
            if (d3D != null) d3D.ReloadModel();
        }


        public void ReloadModel(bool recalcView = true)
        {
            LoadGeometry((XbimModel)GetValue(ModelProperty), recalcView);
           
        }

        #endregion


        #region Initialisation functions
        
        /// <summary>
        /// Clears the current graphics and initiates the cascade of events that result in viewing the scene.
        /// </summary>
        /// <param name="EntityLabels">If null loads the whole model, otherwise only elements listed in the enumerable</param>
        public void LoadGeometry(XbimModel model, bool recalcView = true)
        {
            // AddLayerToDrawingControl is the function that actually populates the geometry in the viewer.
            // AddLayerToDrawingControl is triggered by BuildRefModelScene and BuildScene below here when layers get ready.

            //reset all the visuals
            ClearGraphics(recalcView);
            short userDefinedId = 0;
            if (model == null)
                return; //nothing to show
            model.UserDefinedId = userDefinedId;
            Xbim3DModelContext context = new Xbim3DModelContext(model);
            XbimRegion largest = context.GetLargestRegion();
            XbimPoint3D c = new XbimPoint3D(0, 0, 0);
            XbimRect3D bb = XbimRect3D.Empty;
            if (largest != null)
                bb = new XbimRect3D(largest.Centre, largest.Centre);

            foreach (var refModel in model.ReferencedModels)
            {

                XbimRegion r;
                refModel.Model.UserDefinedId = ++userDefinedId;

                Xbim3DModelContext refContext = new Xbim3DModelContext(refModel.Model);
                r = refContext.GetLargestRegion();

                if (r != null)
                {
                    if (bb.IsEmpty)
                        bb = new XbimRect3D(r.Centre, r.Centre);
                    else
                        bb.Union(r.Centre);
                }
            }
            XbimPoint3D p = bb.Centroid();
            _modelTranslation = new XbimVector3D(-p.X, -p.Y, -p.Z);
            model.ReferencedModels.CollectionChanged += RefencedModels_CollectionChanged;
            //build the geometric scene and render as we go
            BuildScene(context);
            foreach (var refModel in model.ReferencedModels)
            {
                Xbim3DModelContext refContext = new Xbim3DModelContext(refModel.Model);
                BuildScene(refContext);
            }
            if (recalcView) RecalculateView(model);
        }

        private void RecalculateView(XbimModel model)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This version uses the new Geometry representation
        /// </summary>
        /// <param name="model"></param>
        /// <param name="context"></param>
        /// <param name="exclude">List of type to exclude, by default excplict openings and spaces are excluded if exclude = null</param>
        /// <returns></returns>
        private void BuildScene(Xbim3DModelContext context)
        {
           
            //get a list of all the unique styles
            Dictionary<int, WpfMaterial> styles = new Dictionary<int, WpfMaterial>();
            Dictionary<int, MeshGeometry3D> shapeGeometries = new Dictionary<int, MeshGeometry3D>();
            Dictionary<int, WpfMeshGeometry3D> meshSets = new Dictionary<int, WpfMeshGeometry3D>();
            Model3DGroup opaques = new Model3DGroup();
            Model3DGroup transparents = new Model3DGroup();

            foreach (var shapeGeom in context.ShapeGeometries())
            {
                MeshGeometry3D wpfMesh = new MeshGeometry3D();
                wpfMesh.SetValue(TagProperty, shapeGeom); //don't read the geometry into the mesh yet as we do not know where the instance is located
                shapeGeometries.Add(shapeGeom.ShapeLabel, wpfMesh);
            }


            foreach (var style in context.SurfaceStyles())
            {
                WpfMaterial wpfMaterial = new WpfMaterial();
                wpfMaterial.CreateMaterial(style);
                styles.Add(style.DefinedObjectId, wpfMaterial);
                WpfMeshGeometry3D mg = new WpfMeshGeometry3D(wpfMaterial, wpfMaterial);
                meshSets.Add(style.DefinedObjectId, mg);
                if (style.IsTransparent)
                    transparents.Children.Add(mg);
                else
                    opaques.Children.Add(mg);

            }

            ////if (!styles.Any()) return ; //this should always have something unless the model is empty
            ////double metre = model.ModelFactors.OneMetre;
            ////XbimMatrix3D wcsTransform = XbimMatrix3D.CreateTranslation(_modelTranslation) * XbimMatrix3D.CreateScale((float)(1 / metre));

            ////foreach (var shapeInstance in context.ShapeInstances()
            ////        .Where(s => s.RepresentationType == XbimGeometryRepresentationType.OpeningsAndAdditionsIncluded &&
            ////            !typeof(IfcFeatureElement).IsAssignableFrom(IfcMetaData.GetType(s.IfcTypeId)) &&
            ////            !typeof(IfcSpace).IsAssignableFrom(IfcMetaData.GetType(s.IfcTypeId))))
            ////{
            ////    int styleId = shapeInstance.StyleLabel > 0 ? shapeInstance.StyleLabel : shapeInstance.IfcTypeId * -1;

            ////    //GET THE ACTUAL GEOMETRY 
            ////    MeshGeometry3D wpfMesh;
            ////    //see if we have already read it
            ////    if (shapeGeometries.TryGetValue(shapeInstance.ShapeGeometryLabel, out wpfMesh))
            ////    {
            ////        GeometryModel3D mg = new GeometryModel3D(wpfMesh, styles[styleId]);
            ////        mg.SetValue(TagProperty, new XbimInstanceHandle(model, shapeInstance.IfcProductLabel, shapeInstance.IfcTypeId));
            ////        mg.BackMaterial = mg.Material;
            ////        mg.Transform = XbimMatrix3D.Multiply(shapeInstance.Transformation, wcsTransform).ToMatrixTransform3D();
            ////        if (styles[styleId].IsTransparent)
            ////            transparents.Children.Add(mg);
            ////        else
            ////            opaques.Children.Add(mg);
            ////    }
            ////    else //we need to get the shape geometry
            ////    {
            ////        XbimShapeGeometry shapeGeom = context.ShapeGeometry(shapeInstance.ShapeGeometryLabel);
            ////        if (shapeGeom.ReferenceCount > 1) //only store if we are going to use again
            ////        {
            ////            wpfMesh = new MeshGeometry3D();
            ////            wpfMesh.Read(shapeGeom.ShapeData);
            ////            shapeGeometries.Add(shapeInstance.ShapeGeometryLabel, wpfMesh);
            ////            GeometryModel3D mg = new GeometryModel3D(wpfMesh, styles[styleId]);
            ////            mg.SetValue(TagProperty, new XbimInstanceHandle(model, shapeInstance.IfcProductLabel, shapeInstance.IfcTypeId));
            ////            mg.BackMaterial = mg.Material;
            ////            mg.Transform = XbimMatrix3D.Multiply(shapeInstance.Transformation, wcsTransform).ToMatrixTransform3D();
            ////            if (styles[styleId].IsTransparent)
            ////                transparents.Children.Add(mg);
            ////            else
            ////                opaques.Children.Add(mg);
            ////        }
            ////        else //it is a one off, merge it with shapes of a similar material
            ////        {
            ////            WpfMeshGeometry3D mg = meshSets[styleId];
            ////            mg.Add(shapeGeom.ShapeData,
            ////                shapeInstance.IfcTypeId,
            ////                shapeInstance.IfcProductLabel,
            ////                shapeInstance.InstanceLabel,
            ////                XbimMatrix3D.Multiply(shapeInstance.Transformation, wcsTransform), 0);
            ////        }
            ////    }

            ////}

            if (opaques.Children.Any())
            {
                ModelVisual3D mv = new ModelVisual3D();
                mv.Content = opaques;
                Opaques.Children.Add(mv);
                if (_modelBounds.IsEmpty) _modelBounds = mv.Content.Bounds.ToXbimRect3D();
                else _modelBounds.Union(mv.Content.Bounds.ToXbimRect3D());
            }
            if (transparents.Children.Any())
            {
                ModelVisual3D mv = new ModelVisual3D();
                mv.Content = transparents;
                Transparents.Children.Add(mv);
                if (_modelBounds.IsEmpty) _modelBounds = mv.Content.Bounds.ToXbimRect3D();
                else _modelBounds.Union(mv.Content.Bounds.ToXbimRect3D());
            }

        }

        private void ClearGraphics(bool recalcView = true)
        {
           
            _selection = new EntitySelection();
         //   UserModeledDimension.Clear();

            Opaques.Children.Clear();
            Transparents.Children.Clear();
            Extras.Children.Clear();
           // this.ClearCutPlane();
            Highlighted.Mesh = null;

            if (recalcView)
            {
                _modelBounds = XbimRect3D.Empty;
                _viewBounds = new XbimRect3D(0, 0, 0, 10, 10, 5);
                Viewport.ResetCamera();
            }

        }

        void RefencedModels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            //if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems.Count > 0)
            //{
            //    XbimReferencedModel refModel = e.NewItems[0] as XbimReferencedModel;
            //    if (scenes.Count == 0) //need to calculate extents
            //    {
            //        XbimRegion largest = GetLargestRegion(refModel.Model);
            //        XbimPoint3D c = new XbimPoint3D(0, 0, 0);
            //        XbimRect3D bb = XbimRect3D.Empty;
            //        if (largest != null)
            //            bb = new XbimRect3D(largest.Centre, largest.Centre);
            //        XbimPoint3D p = bb.Centroid();
            //        _modelTranslation = new XbimVector3D(-p.X, -p.Y, -p.Z);
            //    }
            //    XbimScene<WpfMeshGeometry3D, WpfMaterial> scene = BuildRefModelScene(refModel.Model, refModel.DocumentInformation);
            //    scenes.Add(scene);
            //    RecalculateView(refModel.Model);
            //}
        }

        #endregion

        public Xbim3DViewerControl()
        {
            InitializeComponent();
        }
    }
}
