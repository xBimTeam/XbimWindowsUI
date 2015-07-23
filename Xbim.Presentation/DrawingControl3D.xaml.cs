#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Presentation
// Filename:    DrawingControl3D.xaml.cs
// Published:   01, 2012
// Last Edited: 9:05 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

// #define DOPARALLEL

#region Directives

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using HelixToolkit.Wpf;
using Xbim.Common.Geometry;
using Xbim.Ifc2x3;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.ExternalReferenceResource;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.SharedComponentElements;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using Xbim.Presentation.LayerStyling;
using Xbim.Presentation.LayerStylingV2;
using Xbim.Presentation.ModelGeomInfo;
using Xbim.XbimExtensions.Interfaces;
using XbimGeometry.Interfaces;

#endregion



namespace Xbim.Presentation
{
    /// <summary>
    ///   Interaction logic for DrawingControl3D.xaml
    /// </summary>
    public partial class DrawingControl3D
    {   
        public DrawingControl3D()
        {
            InitializeComponent();
            Highlighted.PropertyChanged += Highlighted_PropertyChanged;
            Viewport = Canvas;
            Canvas.MouseDown += Canvas_MouseDown;
            Canvas.MouseWheel += Canvas_MouseWheel;
            Loaded += DrawingControl3D_Loaded;
            _federationColours = new XbimColourMap(StandardColourMaps.Federation);
            Viewport.CameraChanged += UpdatefrustumPlanes;
            ClearGraphics();
            MouseModifierKeyBehaviour.Add(ModifierKeys.Control, XbimMouseClickActions.Toggle);
            MouseModifierKeyBehaviour.Add(ModifierKeys.Alt, XbimMouseClickActions.Measure);
            MouseModifierKeyBehaviour.Add(ModifierKeys.Shift, XbimMouseClickActions.SetClip);            
        }

        /// <summary>
        /// this method keeps meshes for TransHighlighted and Highlighted items in sync.
        /// </summary>
        void Highlighted_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var pInfo = typeof(MeshVisual3D).GetProperty(e.PropertyName);
            var sourceValue = pInfo.GetValue(Highlighted, null);
            pInfo.SetValue(TransHighlighted, sourceValue, null);
        }

        CombinedManipulator _clipHandler;

        public bool LayerStylerForceVersion1 { get; set; }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            var plane = GetCutPlane();
            if (e.Key == Key.LeftShift && _clipHandler == null && plane != null)
                ClipPlaneHandlesShow();
            else if (e.Key == Key.Delete &&
                ((e.KeyboardDevice.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift) // shift is pressed
                )
            {
                if (plane != null)
                {
                    ClearCutPlane();
                }
                ClipPlaneHandlesHide();
                _clipHandler = null;
            }
            base.OnPreviewKeyDown(e);
        }

        private void ClipPlaneHandlesPlace(Point3D pos)
        {
            var m = Matrix3D.Identity;
            m.Translate(new Vector3D(
                pos.X, pos.Y, pos.Z)
                );
            Extras.Transform = new MatrixTransform3D(m);
            // ClipPlaneHandlesShow();
        }

        private void ClipPlaneHandlesShow()
        {
            _clipHandler = new CombinedManipulator();
            Extras.Children.Add(_clipHandler);
        }

        protected override void OnPreviewKeyUp(KeyEventArgs e)
        {
            // dealing with cutting plane update
            //
            if (e.Key == Key.LeftShift && _clipHandler != null)
            {
                var m1 = Extras.Transform.Value;
                var m2 = _clipHandler.Transform.Value;

                ClipPlaneHandlesHide();

                var newMatrix = Matrix3D.Multiply(m2, m1);
                Extras.Transform = new MatrixTransform3D(newMatrix);

                var p = new Point3D(newMatrix.OffsetX, newMatrix.OffsetY, newMatrix.OffsetZ);
                var n = newMatrix.Transform(new Vector3D(0, 0, -1));
                ClearCutPlane();
                SetCutPlane(p.X, p.Y, p.Z, n.X, n.Y, n.Z);
            }
            base.OnPreviewKeyUp(e);
        }

        private void ClipPlaneHandlesHide()
        {
            Extras.Children.Clear();
            _clipHandler = null;
        }


        // elements associated with vector polygons drafted interactively on the model by the user
        //
       
        private List<Type> _exclude;
           
        private LinesVisual3D _userModeledDimLines;
        private PointsVisual3D _userModeledDimPoints;
        public PolylineGeomInfo UserModeledDimension = new PolylineGeomInfo();

        private void FirePrevPointsChanged()
        {
            if (!UserModeledDimension.IsEmpty)
            {
                // enable the loop that updates the drawing geometry
                CompositionTarget.Rendering += OnCompositionTargetRendering;
            }
            if (UserModeledDimensionChangedEvent != null)
                UserModeledDimensionChangedEvent(this, UserModeledDimension);

        }

        void OnCompositionTargetRendering(object sender, EventArgs e)
        {
            var doShow = !UserModeledDimension.IsEmpty;
            // lines
            var depthoff = 0.001;
            if (doShow && _userModeledDimLines == null)
            {
                _userModeledDimLines = new LinesVisual3D { 
                    Color = Colors.Yellow, 
                    Thickness = 3,
                    DepthOffset = depthoff
                };
                Canvas.Children.Add(_userModeledDimLines);
            }
            if (!doShow && _userModeledDimLines != null)
            {
                _userModeledDimLines.IsRendering = false;
                Canvas.Children.Remove(_userModeledDimLines);
                _userModeledDimLines = null;
            }
            // points 
            if (doShow && _userModeledDimPoints == null)
            {
                _userModeledDimPoints = new PointsVisual3D { 
                    Color = Colors.Orange, 
                    Size = 5,
                    DepthOffset = depthoff
                };
                Canvas.Children.Add(_userModeledDimPoints);
            }
            if (!doShow && _userModeledDimPoints != null)
            {
                _userModeledDimPoints.IsRendering = false;
                Canvas.Children.Remove(_userModeledDimPoints);
                _userModeledDimPoints = null;
            }
            if (!doShow)
            {
                // if not needed the hook can be removed until a new measure is made by the user
                CompositionTarget.Rendering -= OnCompositionTargetRendering;
            }

            // geometry prep
            if (_userModeledDimLines != null)
                _userModeledDimLines.Points = UserModeledDimension.VisualPoints;
            if (_userModeledDimPoints != null)
                _userModeledDimPoints.Points = UserModeledDimension.VisualPoints;
        }

        void UpdatefrustumPlanes(object sender, RoutedEventArgs e)
        {
            var snd = sender as HelixViewport3D;
            if (snd == null)
                return;

            var middlePoint = _viewBounds.Centroid();
            var centralDistance = Math.Sqrt(
                    Math.Pow(snd.Camera.Position.X, 2) + Math.Pow(middlePoint.X, 2) +
                    Math.Pow(snd.Camera.Position.Y, 2) + Math.Pow(middlePoint.Y, 2) +
                    Math.Pow(snd.Camera.Position.Z, 2) + Math.Pow(middlePoint.Z, 2)
                    );

            double diag = 40;
            if (_viewBounds.Length() > 0)
            {
                diag = _viewBounds.Length();
            }
            var farPlaneDistance = centralDistance + 1.5 * diag;
            var nearPlaneDistance = centralDistance - 1.5 * diag;

            const double nearLimit = 0.125;
            nearPlaneDistance = Math.Max(nearPlaneDistance, nearLimit);

// ReSharper disable once RedundantCheckBeforeAssignment
            if (Math.Abs(snd.Camera.NearPlaneDistance - nearPlaneDistance) > 0)
            {
                snd.Camera.NearPlaneDistance = nearPlaneDistance;  // Debug.WriteLine("Near: " + NearPlane);
            }
// ReSharper disable once RedundantCheckBeforeAssignment
            if (Math.Abs(snd.Camera.FarPlaneDistance - farPlaneDistance) > 0)
            {
                snd.Camera.FarPlaneDistance = farPlaneDistance;    // Debug.WriteLine("Far: " + FarPlane);
            }
            }

        void DrawingControl3D_Loaded(object sender, RoutedEventArgs e)
        {
            ShowSpaces = false; 
        }

        #region Fields
        public List<XbimScene<WpfMeshGeometry3D, WpfMaterial>> Scenes = new List<XbimScene<WpfMeshGeometry3D, WpfMaterial>>();
        private readonly XbimColourMap _federationColours;

        // protected RayMeshGeometry3DHitTestResult _hitResult;
       
        public XbimRect3D ModelBounds;
        private XbimRect3D _viewBounds;
        // private int? _currentProduct;
        private readonly List<Material> _materials = new List<Material>();
        private readonly Dictionary<Material, double> _opacities = new Dictionary<Material, double>();
        /// <summary>
        /// Gets or sets the model.
        /// </summary>
        /// <value>The model.</value>
        public Model3D Model3D { get; set; }

        public Plane3D GetCutPlane()
        {
            var p = FindName("cuttingGroup");
            var cpg = p as CuttingPlaneGroup;
            if (cpg == null || cpg.IsEnabled == false) 
                return null;
            return cpg.CuttingPlanes.Count == 1 
                ? cpg.CuttingPlanes[0] 
                : null;
        }

        public void SetCutPlane(double posX, double posY, double posZ, double nrmX, double nrmY, double nrmZ)
        {   
            SetNamedCutPlane(posX, posY, posZ, nrmX, nrmY, nrmZ, "cuttingGroup");
            SetNamedCutPlane(posX, posY, posZ, nrmX, nrmY, nrmZ, "cuttingGroupT");
        }

        private void SetNamedCutPlane(double posX, double posY, double posZ, double nrmX, double nrmY, double nrmZ, string cuttingGroupName)
        {
            var p = FindName(cuttingGroupName);
            var cpg = p as CuttingPlaneGroup;
            if (cpg == null) 
                return;
            cpg.IsEnabled = false;
            cpg.CuttingPlanes.Clear();
            cpg.CuttingPlanes.Add(
                new Plane3D(
                    new Point3D(posX, posY, posZ),
                    new Vector3D(nrmX, nrmY, nrmZ)
                    ));
            cpg.IsEnabled = true;
        }

        public void ClearCutPlane()
        {
            ClearNamedCutPlane("cuttingGroup");
            ClearNamedCutPlane("cuttingGroupT");
        }

        private void ClearNamedCutPlane(string name)
        {
            var p = FindName(name);
            var cpg = p as CuttingPlaneGroup;
            if (cpg != null)
            {
                cpg.IsEnabled = false;
            }
        }

        #endregion

        #region Events

        public new static readonly RoutedEvent LoadedEvent =
            EventManager.RegisterRoutedEvent("LoadedEvent", RoutingStrategy.Bubble, typeof(RoutedEventHandler),
                                             typeof(DrawingControl3D));

        public new event RoutedEventHandler Loaded
        {
            add { AddHandler(LoadedEvent, value); }
            remove { RemoveHandler(LoadedEvent, value); }
        }

        public Dictionary<ModifierKeys, XbimMouseClickActions> MouseModifierKeyBehaviour = new Dictionary<ModifierKeys, XbimMouseClickActions>();

        private void SelectionDrivenSelectedEntityChange(IPersistIfcEntity entity)
        {
            _selectedEntityChangeTriggedBySelectionChange = true;
            if (SelectedEntity == null && entity == null)
            {
                // OnSelectedEntityChanged(this, new DependencyPropertyChangedEventArgs(SelectedEntityProperty, null, null));
                HighlighSelected(null);
            }
            SelectedEntity = entity;
            
            _selectedEntityChangeTriggedBySelectionChange = false;
        }

        public event UserModeledDimensionChanged UserModeledDimensionChangedEvent;
        public delegate void UserModeledDimensionChanged(DrawingControl3D m, PolylineGeomInfo e);

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {    
            var pos = e.GetPosition(Canvas);
            var hit = FindHit(pos);
         
            if (hit == null || hit.ModelHit == null)
            {
                Selection.Clear();
                HighlighSelected(null);
                return;
            }

            var hitObject = hit.ModelHit.GetValue(TagProperty);
            IPersistIfcEntity thisSelectedEntity=null;
            if (hitObject is XbimInstanceHandle)
            {
                var selhandle = (XbimInstanceHandle)hitObject;
                thisSelectedEntity = selhandle.GetEntity();
            }
            else if (hitObject is WpfMeshGeometry3D)
            {
                var mesh = hitObject as WpfMeshGeometry3D;
                var frag = mesh.Meshes.Find(hit.VertexIndex1);
                var modelId = frag.ModelId;
                XbimModel modelHit = null; // default to not hit
                if (modelId == 0) 
                    modelHit = Model;
                else
                {
                    foreach (var refModel in Model.ReferencedModels)
                    {
                        if (refModel.Model.UserDefinedId != modelId) 
                            continue;
                        modelHit = refModel.Model;
                        break;
                    }
                }
                if (modelHit != null)
                {
                    if (frag.IsEmpty)
                        frag = mesh.Meshes.Find(hit.VertexIndex2);
                    if (frag.IsEmpty)
                        frag = mesh.Meshes.Find(hit.VertexIndex3);
                    if (!frag.IsEmpty)
                    {
                        thisSelectedEntity = modelHit.Instances[frag.EntityLabel];
                    }
                }
            }
            else if (hitObject is XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial>)
            {
                thisSelectedEntity = GetClickedEntity(hit);
            }
            else
            {
                Selection.Clear();
                HighlighSelected(null);
            }
            
            if (SelectionBehaviour == SelectionBehaviours.MultipleSelection)
            {
                // default behaviour is single selection
                var mc = XbimMouseClickActions.Single;
                if (MouseModifierKeyBehaviour.ContainsKey(Keyboard.Modifiers))
                    mc = MouseModifierKeyBehaviour[Keyboard.Modifiers];
                if (mc != XbimMouseClickActions.Measure)
                {
                    // drop the geometry for the measure visualization
                    // FurtherGeometries.Content = null;
                    UserModeledDimension.Clear(); 
                    FirePrevPointsChanged();
                }

                if (thisSelectedEntity == null)
                { // regardless of selection mode an empty selection clears the current selection
                    Selection.Clear();
                    HighlighSelected(null);
                }
                else
                {
                    switch (mc)
                    {
                        case XbimMouseClickActions.Add:
                            Selection.Add(thisSelectedEntity);
                            SelectionDrivenSelectedEntityChange(thisSelectedEntity);
                            break;
                        case XbimMouseClickActions.Remove:
                            Selection.Remove(thisSelectedEntity);
                            SelectionDrivenSelectedEntityChange(null);
                            break;
                        case XbimMouseClickActions.Toggle:
                            var bAdded = Selection.Toggle(thisSelectedEntity);
                            if (bAdded)
                                SelectionDrivenSelectedEntityChange(thisSelectedEntity);
                            else
                                SelectionDrivenSelectedEntityChange(null);
                            break;
                        case XbimMouseClickActions.Single:
                            Selection.Clear();
                            Selection.Add(thisSelectedEntity);
                            SelectionDrivenSelectedEntityChange(thisSelectedEntity);
                            break;
                        case XbimMouseClickActions.Measure:
                            var p = GetClosestPoint(hit);
                            if (UserModeledDimension.Last3DPoint.HasValue && UserModeledDimension.Last3DPoint.Value == p.Point)
                                UserModeledDimension.RemoveLast();
                            else
                                UserModeledDimension.Add(p); 
                            Debug.WriteLine(UserModeledDimension.ToString());
                            FirePrevPointsChanged();
                            break;
                        case XbimMouseClickActions.SetClip:
                            SetCutPlane(hit.PointHit.X, hit.PointHit.Y, hit.PointHit.Z, 0, 0, -1);
                            ClipPlaneHandlesPlace(hit.PointHit);
                            break;
                    }
                }
            }
            else
            {
                SelectedEntity = thisSelectedEntity;
            }
        }

        private PointGeomInfo GetClosestPoint(RayMeshGeometry3DHitTestResult hit)
        {
            var pts = new[] {
                hit.VertexIndex1,
                hit.VertexIndex2,
                hit.VertexIndex3
            };

            var pHit = new PointGeomInfo
            {
                Entity = GetClickedEntity(hit),
                Point = hit.PointHit
            };

            var minDist = double.PositiveInfinity;
            var iClosest = -1;
            for (var i = 0; i < 3; i++)
            {
                var iPtMesh = pts[i];
                
                var dist = hit.PointHit.DistanceTo(hit.MeshHit.Positions[iPtMesh]);
                if (dist < minDist)
                {
                    minDist = dist;
                    iClosest = iPtMesh;
                }
            }

            var pRet = new PointGeomInfo
            {
                Entity = pHit.Entity,
                Point = hit.MeshHit.Positions[iClosest]
            };

            return pRet;
        }

        private  IPersistIfcEntity GetClickedEntity(RayMeshGeometry3DHitTestResult hit)
        {
            if (hit == null) 
                return null;
            
            var layer = hit.ModelHit.GetValue(TagProperty) as XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial>; //get the fragments
            if (layer == null) 
                return null;
            
            var frag = layer.Visible.Meshes.Find(hit.VertexIndex1);
            var modelId = frag.ModelId;
            XbimModel modelHit =  null; //default to not hit
            if (modelId == 0) modelHit = Model;
            else
            {
                foreach (var refModel in Model.ReferencedModels)
                {
                    if (refModel.Model.UserDefinedId != modelId) 
                        continue;
                    modelHit = refModel.Model;
                    break;
                }
            }
            if (modelHit == null) 
                return null;
            if (frag.IsEmpty)
                frag = layer.Visible.Meshes.Find(hit.VertexIndex2);
            if (frag.IsEmpty)
                frag = layer.Visible.Meshes.Find(hit.VertexIndex3);
            return frag.IsEmpty
                ? null
                : (modelHit.Instances[frag.EntityLabel]);
        }

        #endregion

        #region Dependency Properties

        public double ModelOpacity
        {
            get { return (double)GetValue(ModelOpacityProperty); }
            set { SetValue(ModelOpacityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ModelOpacity.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ModelOpacityProperty =
            DependencyProperty.Register("ModelOpacity", typeof(double), typeof(DrawingControl3D), new UIPropertyMetadata(1.0, OnModelOpacityChanged));

        private static void OnModelOpacityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
             var d3D = d as DrawingControl3D;
             if (d3D != null && e.NewValue !=null)
             {
                 d3D.SetOpacity((double)e.NewValue);
             }
        }

        private void SetOpacity( double opacityPercent)
        {
            var opacity = Math.Min(1, opacityPercent);
            opacity = Math.Max(0, opacity); //bound opacity factor
            
            foreach (var material in _materials)
            {
                SetOpacityPercent(material, opacity);
            }
        }

        private void SetOpacityPercent(Material material,  double opacity)
        {
            var g = material as MaterialGroup;
            if (g != null)
            {
                foreach (var item in g.Children)
                {
                    SetOpacityPercent(item, opacity);
                }
                return;
            }

            var dm = material as DiffuseMaterial;
            if (dm != null)
            {
                double oldValue;
                if (!_opacities.TryGetValue(dm, out oldValue))
                {
                    oldValue = dm.Brush.Opacity;
                    _opacities.Add(dm, oldValue);
                }
                dm.Brush.Opacity = oldValue * opacity;
            }
            var sm = material as SpecularMaterial;
            if (sm != null)
            {
                double oldValue;
                if (!_opacities.TryGetValue(sm, out oldValue))
                {
                    oldValue = sm.Brush.Opacity;
                    _opacities.Add(sm, oldValue);
                }
                sm.Brush.Opacity = oldValue * opacity;
            }
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

        private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var d3D = d as DrawingControl3D;
            if (d3D != null)
            {
                // XbimModel model = e.NewValue as XbimModel;
                d3D.ReloadModel();
            }
        }

        public void ReloadModel(ModelRefreshOptions options = ModelRefreshOptions.None)
        {
            LoadGeometry((XbimModel)GetValue(ModelProperty),
                options: options
                );
            SetValue(LayerSetProperty, LayerSetRefresh());
        }

        public bool ForceRenderBothSides
        {
            get { return (bool)GetValue(ForceRenderBothSidesProperty); }
            set { SetValue(ForceRenderBothSidesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ForceRenderBothSides.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ForceRenderBothSidesProperty =
            DependencyProperty.Register("ForceRenderBothSides", typeof(bool), typeof(DrawingControl3D), new PropertyMetadata(true));

        #region Selection

        public enum SelectionHighlightModes
        {
            WholeMesh,
            Normals,
            WireFrame
        }
        public SelectionHighlightModes SelectionHighlightMode = SelectionHighlightModes.WholeMesh;

        public enum SelectionBehaviours
        {
            SingleSelection,
            MultipleSelection
        }

#if DEBUG
        public SelectionBehaviours SelectionBehaviour = SelectionBehaviours.MultipleSelection;
#else
        public SelectionBehaviours SelectionBehaviour = SelectionBehaviours.SingleSelection;
#endif

        public EntitySelection Selection
        {
            get { return (EntitySelection)GetValue(SelectionProperty); }
            set { SetValue(SelectionProperty, value); }
        }

        public static readonly DependencyProperty SelectionProperty = DependencyProperty.Register("Selection", typeof(EntitySelection), typeof(DrawingControl3D), new PropertyMetadata(OnSelectionChanged));
        private static void OnSelectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var d3D = d as DrawingControl3D;
            if (d3D == null) 
                return;
            var newVal = e.NewValue as EntitySelection;
            d3D.ReplaceSelection(newVal);
        }

        private void ReplaceSelection(EntitySelection newVal)
        {
            if (newVal.Count() < 2)
            {
                SelectionDrivenSelectedEntityChange(newVal.FirstOrDefault());
            }
            HighlighSelected(null);
        }

        public static readonly RoutedEvent SelectedEntityChangedEvent = EventManager.RegisterRoutedEvent("SelectedEntityChangedEvent", RoutingStrategy.Bubble, typeof(SelectionChangedEventHandler), typeof(DrawingControl3D));

        public event SelectionChangedEventHandler SelectedEntityChanged
        {
            add { AddHandler(SelectedEntityChangedEvent, value); }
            remove { RemoveHandler(SelectedEntityChangedEvent, value); }
        }

        // this events are is not involved when SelectedEntityProperty is changed.
        public IPersistIfcEntity SelectedEntity
        {
            get { return (IPersistIfcEntity)GetValue(SelectedEntityProperty); }
            set { SetValue(SelectedEntityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedEntity.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedEntityProperty =
            DependencyProperty.Register("SelectedEntity", typeof(IPersistIfcEntity), typeof(DrawingControl3D), new PropertyMetadata(OnSelectedEntityChanged));

        /// <summary>
        /// _SelectedEntityChangeTriggedBySelectionChange is introduced a temporary fix to allow the multiple selection mode to continue working and propagating the SelectedEntityChanged event.
        /// When selectedEntity is changed externally (value is false) then the Selection property is also impacted.
        /// </summary>
        private bool _selectedEntityChangeTriggedBySelectionChange;
        private static void OnSelectedEntityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var d3D = d as DrawingControl3D;
            if (d3D == null) 
                return;
            var newVal = e.NewValue as IPersistIfcEntity;
            if (!d3D._selectedEntityChangeTriggedBySelectionChange)
            {
                d3D.Selection.Clear();
                if (newVal != null)
                    d3D.Selection.Add(newVal);
            }
            d3D.HighlighSelected(newVal);
        }

        /// <summary>
        /// Executed when a new entity is selected
        /// </summary>
        /// <param name="newVal"></param>
        private void HighlighSelected(IPersistIfcEntity newVal)
        {
            var m = new MeshGeometry3D();

            // 1. get the geometry first
            if (SelectionBehaviour == SelectionBehaviours.MultipleSelection)
            {
                foreach (var item in Selection)
                {
                    var fromModel = item.ModelOf as XbimModel;
                    if (fromModel != null && item is IfcProduct)
                    {
                        if (fromModel.GeometrySupportLevel == 2)
                        {
                            var metre = fromModel.ModelFactors.OneMetre;
                            WcsTransform = XbimMatrix3D.CreateTranslation(ModelTranslation)*
                                           XbimMatrix3D.CreateScale((float) (1/metre));

                            var context = new Xbim3DModelContext(fromModel);

                            var productShape =
                                context.ShapeInstancesOf((IfcProduct) item)
                                    .Where(
                                        s =>
                                            s.RepresentationType !=
                                            XbimGeometryRepresentationType.OpeningsAndAdditionsExcluded)
                                    .ToList();
                            if (productShape.Any())
                            {

                                foreach (var shapeInstance in productShape)
                                {
                                    IXbimShapeGeometryData shapeGeom =
                                        context.ShapeGeometry(shapeInstance.ShapeGeometryLabel);
                                    switch ((XbimGeometryType) shapeGeom.Format)
                                    {
                                        case XbimGeometryType.PolyhedronBinary:
                                            m.Read(shapeGeom.ShapeData,
                                                XbimMatrix3D.Multiply(shapeInstance.Transformation, WcsTransform));
                                            break;
                                        case XbimGeometryType.Polyhedron:
                                            m.Read(((XbimShapeGeometry) shapeGeom).ShapeData,
                                                XbimMatrix3D.Multiply(shapeInstance.Transformation, WcsTransform));
                                            break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            var xm3d = new XbimMeshGeometry3D();
                            var geomDataSet = fromModel.GetGeometryData(item.EntityLabel, XbimGeometryType.TriangulatedMesh);
                            foreach (var geomData in geomDataSet)
                            {
                                var gd = geomData.TransformBy(WcsTransform);
                                xm3d.Add(gd);
                            }
                            m.Add(xm3d);
                        }
                    }
                }
            }
            else if (newVal != null)
            {
                var fromModel = newVal.ModelOf as XbimModel;

                if (fromModel != null && newVal is IfcProduct)
                {
                    var context = new Xbim3DModelContext(fromModel);
                    var metre = fromModel.ModelFactors.OneMetre;
                    WcsTransform = XbimMatrix3D.CreateTranslation(ModelTranslation)*
                                   XbimMatrix3D.CreateScale((float) (1/metre));

                    var productShape =
                        context.ShapeInstancesOf((IfcProduct) newVal)
                            .Where(
                                s => s.RepresentationType != XbimGeometryRepresentationType.OpeningsAndAdditionsExcluded)
                            .ToList();
                    if (productShape.Any())
                    {

                        foreach (var shapeInstance in productShape)
                        {
                            IXbimShapeGeometryData shapeGeom = context.ShapeGeometry(shapeInstance.ShapeGeometryLabel);
                            switch ((XbimGeometryType)shapeGeom.Format)
                            {
                                case XbimGeometryType.PolyhedronBinary:
                                    m.Read(shapeGeom.ShapeData, XbimMatrix3D.Multiply(shapeInstance.Transformation, WcsTransform));
                                    break;
                                case XbimGeometryType.Polyhedron:
                                    m.Read(((XbimShapeGeometry)shapeGeom).ShapeData, XbimMatrix3D.Multiply(shapeInstance.Transformation, WcsTransform));
                                    break;
                            }
                        }
                    }
                }
            }

            // 2. then determine how to highlight it
            //
            if (SelectionHighlightMode == SelectionHighlightModes.WholeMesh)
            {              
                // Highlighted is defined in the XAML of drawingcontrol3d
                Highlighted.Mesh = new Mesh3D(m.Positions, m.TriangleIndices);           
            }
            else if (SelectionHighlightMode == SelectionHighlightModes.Normals)
            {
                // prepares the normals to faces (or points)
                var axesMeshBuilder = new MeshBuilder();
                for (var i = 0; i < m.TriangleIndices.Count; i += 3)
                {
                    var p1 = m.TriangleIndices[i];
                    var p2 = m.TriangleIndices[i + 1];
                    var p3 = m.TriangleIndices[i + 2];

                    if (m.Normals[p1] == m.Normals[p2] && m.Normals[p1] == m.Normals[p3]) // same normals
                    {
                        var cnt = FindCentroid(new[] {m.Positions[p1], m.Positions[p2], m.Positions[p3]});
                        CreateNormal(cnt, m.Normals[p1], axesMeshBuilder);
                    }
                    else
                    {
                        CreateNormal(m.Positions[p1], m.Normals[p1], axesMeshBuilder);
                        CreateNormal(m.Positions[p2], m.Normals[p2], axesMeshBuilder);
                        CreateNormal(m.Positions[p3], m.Normals[p3], axesMeshBuilder);
                    }
                }
                Highlighted.Content = new GeometryModel3D(axesMeshBuilder.ToMesh(), Materials.Yellow);
            }
            else
            {
                var axesMeshBuilder = new MeshBuilder();
                if (newVal != null)
                {
                    var box = XbimRect3D.Empty;
                    for (var i = 0; i < m.TriangleIndices.Count; i += 3)
                    {
                        var p1 = m.TriangleIndices[i];
                        var p2 = m.TriangleIndices[i + 1];
                        var p3 = m.TriangleIndices[i + 2];

                        // box evaluation
                        box.Union(new XbimPoint3D(m.Positions[p1].X, m.Positions[p1].Y, m.Positions[p1].Z));
                        box.Union(new XbimPoint3D(m.Positions[p2].X, m.Positions[p2].Y, m.Positions[p2].Z));
                        box.Union(new XbimPoint3D(m.Positions[p3].X, m.Positions[p3].Y, m.Positions[p3].Z));
                    }

                    var bl = box.Length();
                    var lineThickness = bl / 1000; // 0.01;

                    for (var i = 0; i < m.TriangleIndices.Count; i += 3)
                    {
                        var p1 = m.TriangleIndices[i];
                        var p2 = m.TriangleIndices[i + 1];
                        var p3 = m.TriangleIndices[i + 2];
                        

                        var path = new List<Point3D>
                        {
                            new Point3D(m.Positions[p1].X, m.Positions[p1].Y, m.Positions[p1].Z),
                            new Point3D(m.Positions[p2].X, m.Positions[p2].Y, m.Positions[p2].Z),
                            new Point3D(m.Positions[p3].X, m.Positions[p3].Y, m.Positions[p3].Z)
                        };


                        axesMeshBuilder.AddTube(path, lineThickness, 9, true);

                    }
                    
                    HideAll();
                }
                else
                {
                    ShowAll();
                }
                Highlighted.Content = new GeometryModel3D(axesMeshBuilder.ToMesh(), Materials.Yellow);
            }
        }

        #endregion

        #region TypesShowHide

        public bool ShowSpaces
        {
            get { return (bool)GetValue(ShowSpacesProperty); }
            set { SetValue(ShowSpacesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShowWalls.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowSpacesProperty =
            DependencyProperty.Register("ShowSpaces", typeof(bool), typeof(DrawingControl3D), new UIPropertyMetadata(true, OnShowSpacesChanged));

        private static void OnShowSpacesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var d3D = d as DrawingControl3D;
            if (d3D != null)
            {
                if (e.NewValue is bool)
                {
                    if ((bool)e.NewValue)
                        d3D.Show<IfcSpace>();
                    else
                        d3D.Hide<IfcSpace>();
                }
            }
        }

        public bool ShowWalls
        {
            get { return (bool)GetValue(ShowWallsProperty); }
            set { SetValue(ShowWallsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShowWalls.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowWallsProperty =
            DependencyProperty.Register("ShowWalls", typeof(bool), typeof(DrawingControl3D), new UIPropertyMetadata(true, OnShowWallsChanged));

        private static void OnShowWallsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var d3D = d as DrawingControl3D;
            if (d3D != null)
            {
                if (e.NewValue is bool)
                {
                    var on = (bool)e.NewValue;
                    if (on)
                        d3D.ShowAll();
                    else
                        d3D.HideAll();
                }
            }
        }

        public bool ShowDoors
        {
            get { return (bool)GetValue(ShowDoorsProperty); }
            set { SetValue(ShowDoorsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShowWalls.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowDoorsProperty =
            DependencyProperty.Register("ShowDoors", typeof(bool), typeof(DrawingControl3D), new UIPropertyMetadata(true, OnShowDoorsChanged));

        private static void OnShowDoorsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var d3D = d as DrawingControl3D;
            if (d3D != null)
            {
                if (e.NewValue is bool)
                {
                    var on = (bool)e.NewValue;
                    if (on)
                        d3D.Show<IfcDoor>();
                    else
                        d3D.Hide<IfcDoor>();
                }
            }
        }

        public bool ShowWindows
        {
            get { return (bool)GetValue(ShowWindowsProperty); }
            set { SetValue(ShowWindowsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShowWalls.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowWindowsProperty =
            DependencyProperty.Register("ShowWindows", typeof(bool), typeof(DrawingControl3D), new UIPropertyMetadata(true, OnShowWindowsChanged));

        private static void OnShowWindowsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var d3D = d as DrawingControl3D;
            if (d3D != null)
            {
                if (e.NewValue is bool)
                {
                    if ((bool)e.NewValue)
                        d3D.Show<IfcWindow>();
                    else
                        d3D.Hide<IfcWindow>();
                }
            }
        }

        public bool ShowSlabs
        {
            get { return (bool)GetValue(ShowSlabsProperty); }
            set { SetValue(ShowSlabsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShowWalls.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowSlabsProperty =
            DependencyProperty.Register("ShowSlabs", typeof(bool), typeof(DrawingControl3D), new UIPropertyMetadata(true, OnShowSlabsChanged));

        private static void OnShowSlabsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var d3D = d as DrawingControl3D;
            if (d3D != null)
            {
                if (e.NewValue is bool)
                {
                    if ((bool)e.NewValue)
                        d3D.Show<IfcSlab>();
                    else
                        d3D.Hide<IfcSlab>();
                }
            }
        }
        public bool ShowFurniture
        {
            get { return (bool)GetValue(ShowFurnitureProperty); }
            set { SetValue(ShowFurnitureProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShowWalls.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowFurnitureProperty =
            DependencyProperty.Register("ShowFurniture", typeof(bool), typeof(DrawingControl3D), new UIPropertyMetadata(true, OnShowFurnitureChanged));

        private static void OnShowFurnitureChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var d3D = d as DrawingControl3D;
            if (d3D != null)
            {
                if (e.NewValue is bool)
                {
                    if ((bool)e.NewValue)
                        d3D.Show<IfcFurnishingElement>();
                    else
                        d3D.Hide<IfcFurnishingElement>();
                }
            }
        }
        #endregion

        public bool ShowGridLines
        {
            get { return (bool)GetValue(ShowGridLinesProperty); }
            set { SetValue(ShowGridLinesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShowWalls.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowGridLinesProperty =
            DependencyProperty.Register("ShowGridLines", typeof(bool), typeof(DrawingControl3D), new UIPropertyMetadata(true, OnShowGridLinesChanged));

        private static void OnShowGridLinesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var d3D = d as DrawingControl3D;
            if (d3D != null)
            {
                if (e.NewValue is bool)
                {
                    if ((bool)e.NewValue)
                        d3D.Viewport.Children.Insert(0, d3D.GridLines);
                    else
                        d3D.Viewport.Children.Remove( d3D.GridLines);
                }
            }
        }
        

        public HelixViewport3D Viewport
        {
            get { return (HelixViewport3D)GetValue(ViewportProperty); }
            set { SetValue(ViewportProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Viewport.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ViewportProperty =
            DependencyProperty.Register("Viewport", typeof(HelixViewport3D), typeof(DrawingControl3D), new PropertyMetadata(null));

        public Point3D FindCentroid(Point3D[] p)
        {
            double x = 0;
            double y = 0;
            double z = 0;
            var n = 0;
            foreach (var item in p)
	        {
		         x += item.X;
                 y += item.Y;
                 z += item.Z;
                 n++;
	        }
            if (n > 0)
            {
                x /= n;
                y /= n;
                z /= n;
            }
            return new Point3D(x, y, z);
        }

        private void CreateNormal(Point3D cnt, Vector3D vector3D, MeshBuilder axesMeshBuilder)
        {
            var path = new List<Point3D>();
            path.Add(cnt);
            const double nrmRatio = .2;
            path.Add(
                new Point3D(
                cnt.X + vector3D.X * nrmRatio,
                cnt.Y + vector3D.Y * nrmRatio,
                cnt.Z + vector3D.Z * nrmRatio
                ));

            const double lineThickness = 0.001;
            axesMeshBuilder.AddTube(path, lineThickness, 9, false);
        }

        private RayMeshGeometry3DHitTestResult FindHit(Point position)
        {
            RayMeshGeometry3DHitTestResult result = null;
            HitTestFilterCallback hitFilterCallback = oFilter =>
            {
                // Test for the object value you want to filter. 
                if (oFilter.GetType() == typeof(MeshVisual3D))
                    return HitTestFilterBehavior.ContinueSkipSelfAndChildren;
                // Debug.WriteLine(oFilter.GetType());
                return HitTestFilterBehavior.Continue;
            };

            HitTestResultCallback hitTestCallback = hit =>
            {
                var rayHit = hit as RayMeshGeometry3DHitTestResult;
                if (rayHit != null)
                {
                    if (rayHit.MeshHit != null)
                    {
                        result = rayHit;
                        return HitTestResultBehavior.Stop;
                    }
                }
                return HitTestResultBehavior.Continue;
            };

            var hitParams = new PointHitTestParameters(position);
            VisualTreeHelper.HitTest(Viewport.Viewport, hitFilterCallback, hitTestCallback, hitParams);
            return result;
        }
        #endregion

        public double PercentageLoaded
        {
            get { return (double)GetValue(PercentageLoadedProperty); }
            set { SetValue(PercentageLoadedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PercentageLoaded.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PercentageLoadedProperty =
            DependencyProperty.Register("PercentageLoaded", typeof(double), typeof(DrawingControl3D),
                                        new UIPropertyMetadata(0.0));
        public XbimVector3D ModelTranslation;
        public XbimMatrix3D WcsTransform;


        private void ClearGraphics(ModelRefreshOptions options = ModelRefreshOptions.None)
        {
            PercentageLoaded = 0;

            if (!((options & ModelRefreshOptions.ViewPreserveSelection) == ModelRefreshOptions.ViewPreserveSelection))
            {
            Selection = new EntitySelection();
                Highlighted.Mesh = null;
            }
            
            UserModeledDimension.Clear();

            _materials.Clear();
            _opacities.Clear();

            Opaques.Children.Clear();
            Transparents.Children.Clear();
            Extras.Children.Clear();

            if (!((options & ModelRefreshOptions.ViewPreserveCuttingPlane) == ModelRefreshOptions.ViewPreserveCuttingPlane))
            ClearCutPlane();

            ModelBounds = XbimRect3D.Empty;
            _viewBounds = new XbimRect3D(0, 0, 0, 10, 10, 5);    
            Scenes = new List<XbimScene<WpfMeshGeometry3D, WpfMaterial>>();
            if ((options & ModelRefreshOptions.ViewPreserveCameraPosition) != ModelRefreshOptions.ViewPreserveCameraPosition)
            Viewport.ResetCamera();
        }

        [Flags]
        public enum ModelRefreshOptions
        {
            None = 0,
            ViewPreserveCameraPosition = 1,
            ViewPreserveSelection = 2,
            ViewPreserveCuttingPlane = 4,
            ViewPreserveAll = 7
        }

        public ILayerStylerV2 GeomSupport2LayerStyler;

        /// <summary>
        /// Clears the current graphics and initiates the cascade of events that result in viewing the scene.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="entityLabels">If null loads the whole model, otherwise only elements listed in the enumerable</param>
        /// <param name="options"></param>
        public void LoadGeometry(XbimModel model, IEnumerable<int> entityLabels = null, ModelRefreshOptions options = ModelRefreshOptions.None)
        {
            // AddLayerToDrawingControl is the function that actually populates the geometry in the viewer.
            // AddLayerToDrawingControl is triggered by BuildRefModelScene and BuildScene below here when layers get ready.

            //reset all the visuals
            ClearGraphics(options);
            short userDefinedId = 0;
            if (model == null) 
                return; //nothing to show
            model.UserDefinedId = userDefinedId;
            var geometrySupportLevel = model.GeometrySupportLevel;
            var context = new Xbim3DModelContext(model);
            XbimRegion largest;
            largest = 
                geometrySupportLevel == 1 
                ? GetLargestRegion(model) 
                : context.GetLargestRegion();
            var bb = XbimRect3D.Empty;
            if (largest != null)
                bb = new XbimRect3D(largest.Centre, largest.Centre);

            foreach (var refModel in model.ReferencedModels)
            {
                XbimRegion r;
                refModel.Model.UserDefinedId = ++userDefinedId;
                if (geometrySupportLevel == 1)
                    r = GetLargestRegion(refModel.Model);
                else  //assume we are the latest level (2)
                {
                    var refContext = new Xbim3DModelContext(refModel.Model);
                    r = refContext.GetLargestRegion();
                }
                if (r != null)
                {
                    if (bb.IsEmpty)
                        bb = new XbimRect3D(r.Centre, r.Centre);
                    else
                        bb.Union(r.Centre);
                }
            }
            var p = bb.Centroid();
            ModelTranslation = new XbimVector3D(-p.X, -p.Y, -p.Z);

            // model scaling
            var metre = model.ModelFactors.OneMetre;
            WcsTransform = XbimMatrix3D.CreateTranslation(ModelTranslation) * XbimMatrix3D.CreateScale(1 / metre);


            model.ReferencedModels.CollectionChanged += ReferencedModels_CollectionChanged;

            // prepare grouping and layering behaviours
            if (LayerStyler == null)
                LayerStyler = new LayerStylerTypeAndIfcStyle();
            LayerStyler.SetFederationEnvironment(null);
            //build the geometric scene and render as we go
            XbimScene<WpfMeshGeometry3D, WpfMaterial> scene;



            if (LayerStylerForceVersion1 || geometrySupportLevel == 1)
                scene = BuildScene(model, entityLabels, LayerStyler);
            else //assume we are the latest level (GeometrySupportLevel == 2)
            {
                if (GeomSupport2LayerStyler == null)
                    GeomSupport2LayerStyler = new SurfaceLayerStyler();
                GeomSupport2LayerStyler.Control = this;
                GeomSupport2LayerStyler.SetFederationEnvironment(null);
                scene = GeomSupport2LayerStyler.BuildScene(model, context, _exclude);
            }
            if(scene.Layers.Any())
                Scenes.Add(scene);

            // now look at all referenced models.
            foreach (var refModel in model.ReferencedModels)
            {
                if (LayerStylerForceVersion1 || refModel.Model.GeometrySupportLevel == 1)
                {
                    Scenes.Add(BuildRefModelScene(refModel.Model, refModel.DocumentInformation));
                }
                else //assume we are the latest level (GeometrySupportLevel == 2)
                {
                    GeomSupport2LayerStyler.SetFederationEnvironment(refModel);
                    var refContext = new Xbim3DModelContext(refModel.Model);
                    Scenes.Add(GeomSupport2LayerStyler.BuildScene(refModel.Model, refContext, _exclude));
                }
            }
            ShowSpaces = false;
            RecalculateView();
        }

        private XbimRegion GetLargestRegion(XbimModel model)
        {
            var project = model.IfcProject;
            var projectId = 0;
            if (project != null) projectId = project.EntityLabel;
            var regionData = model.GetGeometryData(projectId, XbimGeometryType.Region).FirstOrDefault();
            //get the region data should only be one

            if (regionData == null) 
                return null;
            var regions = XbimRegionCollection.FromArray(regionData.ShapeData);
            return regions.MostPopulated();
        }

        private void RecalculateView(ModelRefreshOptions options = ModelRefreshOptions.None)
        {
            if (!ModelBounds.IsEmpty) //we have  geometry so create view box
                _viewBounds = ModelBounds;
          
            // Assumes a NearPlaneDistance of 1/8 of meter.
            //all models are now in metres
            UpdatefrustumPlanes(null, null);

            //get bounding box for the whole scene and adapt gridlines to the model units
            //
            var widthModelUnits = _viewBounds.SizeY;
            var lengthModelUnits = _viewBounds.SizeX;
            var gridWidth = Convert.ToInt64(widthModelUnits /  10);
            var gridLen = Convert.ToInt64(lengthModelUnits / 10);
            if (gridWidth > 10 || gridLen > 10)
                GridLines.MinorDistance = 10;
            else
                GridLines.MinorDistance = 1;
            GridLines.Width = (gridWidth + 1) * 10;
            GridLines.Length = (gridLen + 1) * 10;

            GridLines.MajorDistance =  10;
            GridLines.Thickness = 0.01;
            var p3D = _viewBounds.Centroid();
            var t3D = new TranslateTransform3D(p3D.X, p3D.Y, _viewBounds.Z);
            GridLines.Transform = t3D;
           
            //make sure whole scene is visible
            if ((options & ModelRefreshOptions.ViewPreserveCameraPosition) != ModelRefreshOptions.ViewPreserveCameraPosition)
            ViewHome();   
        }

        private void ReferencedModels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Add || e.NewItems.Count <= 0) 
                return;
            var refModel = e.NewItems[0] as XbimReferencedModel;
            if (Scenes.Count == 0) //need to calculate extents
            {
                if (refModel != null)
                {
                    var largest = GetLargestRegion(refModel.Model);
                    var bb = XbimRect3D.Empty;
                    if (largest != null)
                        bb = new XbimRect3D(largest.Centre, largest.Centre);
                    var p = bb.Centroid();
                    ModelTranslation = new XbimVector3D(-p.X, -p.Y, -p.Z);
                }
            }
            if (refModel != null)
            {
                var scene = BuildRefModelScene(refModel.Model, refModel.DocumentInformation);
                Scenes.Add(scene);
            }
            RecalculateView();
        }

        public void ReportData(StringBuilder sb, IModel model, int entityLabel)
        {
            var m = model as XbimModel;
            if (m == null) 
                return;
            foreach (var scene in Scenes)
            {
                var mesh = scene.GetMeshGeometry3D(model.Instances[entityLabel], m.UserDefinedId);
                mesh.ReportGeometryTo(sb);
            }
        }

        private XbimScene<WpfMeshGeometry3D, WpfMaterial> BuildRefModelScene(XbimModel model, IfcDocumentInformation docInfo)
        {
            var scene = new XbimScene<WpfMeshGeometry3D, WpfMaterial>(model);
            var handles = new XbimGeometryHandleCollection(model.GetGeometryHandles()
                                                       .Exclude(IfcEntityNameEnum.IFCFEATUREELEMENT)); // ifcSpaces added to the geometry

            var colour = _federationColours[docInfo.DocumentOwner.RoleName()];
            var metre = model.ModelFactors.OneMetre;
            WcsTransform = XbimMatrix3D.CreateTranslation(ModelTranslation) * XbimMatrix3D.CreateScale(1 / (float)metre);
                
            var layer = new XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial>(model, colour) { Name = "All" };
            //add all content initially into the hidden field
            foreach (var geomData in model.GetGeometryData(handles))
            {
#pragma warning disable 618
                layer.AddToHidden(geomData.TransformBy(WcsTransform), model);
#pragma warning restore 618
            }

            Dispatcher.BeginInvoke(new Action(() => { AddLayerToDrawingControl(layer, true); }), DispatcherPriority.Background);
            lock (scene)
            {
                scene.Add(layer);

                if (ModelBounds.IsEmpty) 
                    ModelBounds = layer.BoundingBoxHidden();
                else 
                    ModelBounds.Union(layer.BoundingBoxHidden());
            }

            Dispatcher.BeginInvoke(new Action(Hide<IfcSpace>), DispatcherPriority.Background);
            return scene;
        }
        
        /// <summary>
        /// Provides a mechanism to define colouring schemes for elements in DrawingControl3D.
        /// After setting a new LayerStyler issue a ReloadModel (<see cref="Xbim.Presentation.DrawingControl3D.ReloadModel(ModelRefreshOptions)"/>). 
        /// </summary>
        public ILayerStyler LayerStyler;
        /// <summary>
        /// Provides a mechanism to define colouring schemes for elements in DrawingControl3D.
        /// After setting a new LayerStyler issue a ReloadModel (<see cref="Xbim.Presentation.DrawingControl3D.ReloadModel(ModelRefreshOptions)"/>). 
        /// </summary>
        public ILayerStyler FederationLayerStyler = null;

        

        private XbimScene<WpfMeshGeometry3D, WpfMaterial> BuildScene(XbimModel model, IEnumerable<int> loadLabels, ILayerStyler layerStyler)
        {
            // spaces are not excluded from the model to make the ShowSpaces property meaningful
            var scene = new XbimScene<WpfMeshGeometry3D, WpfMaterial>(model);
            scene.LayerColourMap.SetProductTypeColourMap();

            var project = model.IfcProject;
            if (project == null)
                return scene;

            XbimGeometryHandleCollection handles; 
                    // = new XbimGeometryHandleCollection(model.GetGeometryHandles().Exclude(IfcEntityNameEnum.IFCFEATUREELEMENT));
                    // .Exclude(IfcEntityNameEnum.IFCFEATUREELEMENT | IfcEntityNameEnum.IFCSPACE));

            Xbim3DModelContext ctx = null;
            
            var suppLevel = model.GeometrySupportLevel;
            if (suppLevel == 1)
            {
                var metre = model.ModelFactors.OneMetre;
                WcsTransform = XbimMatrix3D.CreateTranslation(ModelTranslation)*
                               XbimMatrix3D.CreateScale((float) (1/metre));

                handles = loadLabels == null
                    ? new XbimGeometryHandleCollection(
                        model.GetGeometryHandles().Exclude(IfcEntityNameEnum.IFCFEATUREELEMENT))
                    : new XbimGeometryHandleCollection(
                        model.GetGeometryHandles().Where(t => loadLabels.Contains(t.ProductLabel)));

                // geometry engine version 1
            var groupedHandlers = layerStyler.GroupLayers(handles);
#if DOPARALLEL
            Parallel.ForEach(groupedHandlers.Keys, layerName =>
#else
            foreach (var layerName in groupedHandlers.Keys)
#endif
            {
                var layer = layerStyler.GetLayer(layerName, model, scene);

                    GeometryModel3D m3D = (WpfMeshGeometry3D)layer.Visible;
                    m3D.SetValue(TagProperty, layer);
                    
                    var isLayerVisible = layerStyler.IsVisibleLayer(layerName);
                    var geomColl = model.GetGeometryData(groupedHandlers[layerName]);
                    // initially add all content into the hidden field (underlying geometry info)
                    // it will later be moved to the visible WPF implementation by AddLayerToDrawingControl
                    foreach (var geomData in geomColl)
                    {
#pragma warning disable 618
                        var gd = geomData.TransformBy(WcsTransform);
#pragma warning restore 618
                        if (LayerStyler.UseIfcSubStyles)
                            layer.AddToHidden(gd, model);
                        else
                            layer.AddToHidden(gd);
                    }

                    Dispatcher.BeginInvoke(new Action(() => { AddLayerToDrawingControl(layer, true, isLayerVisible); }), DispatcherPriority.Background);
                    lock (scene)
                    {

                        scene.Add(layer);

                        if (ModelBounds.IsEmpty)
                            ModelBounds = layer.BoundingBoxHidden();
                        else
                            ModelBounds.Union(layer.BoundingBoxHidden());

                        if (ModelBounds.IsEmpty)
                            ModelBounds = layer.BoundingBoxVisible();
                        else
                            ModelBounds.Union(layer.BoundingBoxVisible());
                    }
                    }
#if DOPARALLEL
            );
#endif
                   
                }
                else
                {
                ctx = new Xbim3DModelContext(model);
                handles = ctx.GetApproximateGeometryHandles();

                // geometry engine version 2
                var groupedHandlers = layerStyler.GroupLayers(handles);

                foreach (var layerName in groupedHandlers.Keys)
                {
                    var layer = layerStyler.GetLayer(layerName, model, scene);
                    var isLayerVisible = layerStyler.IsVisibleLayer(layerName);
                    var targetMergeMeshByStyle = ((WpfMeshGeometry3D) layer.Visible);

                    targetMergeMeshByStyle.BeginUpdate();
                    targetMergeMeshByStyle.WpfModel.SetValue(TagProperty, targetMergeMeshByStyle);
                    // v2 handles
                    var hndls = groupedHandlers[layerName];
                    foreach (var handle in hndls)
                    {
                        var shapeInstance =
                            ctx.ShapeInstances().FirstOrDefault(si => si.InstanceLabel == handle.GeometryLabel);
                        IXbimShapeGeometryData shapeGeom = ctx.ShapeGeometry(shapeInstance.ShapeGeometryLabel);

                        // var targetMergeMeshByStyle = styleMeshSets[styleId];
                        switch ((XbimGeometryType) shapeGeom.Format)
                        {
                            case XbimGeometryType.Polyhedron:
                                var shapePoly = (XbimShapeGeometry) shapeGeom;
                                targetMergeMeshByStyle.Add(
                                   shapePoly.ShapeData,
                                   shapeInstance.IfcTypeId,
                                   shapeInstance.IfcProductLabel,
                                   shapeInstance.InstanceLabel,
                                   XbimMatrix3D.Multiply(shapeInstance.Transformation, WcsTransform),
                                   model.UserDefinedId);
                                break;

                            case XbimGeometryType.PolyhedronBinary:
                                targetMergeMeshByStyle.Add(
                                  shapeGeom.ShapeData,
                                  shapeInstance.IfcTypeId,
                                  shapeInstance.IfcProductLabel,
                                  shapeInstance.InstanceLabel,
                                  XbimMatrix3D.Multiply(shapeInstance.Transformation, WcsTransform),
                                  model.UserDefinedId);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    targetMergeMeshByStyle.EndUpdate();

                    Dispatcher.BeginInvoke(new Action(() => { AddLayerToDrawingControl(layer, false, isLayerVisible); }), null);
                lock (scene)
                {
                    scene.Add(layer);

                        if (ModelBounds.IsEmpty)
                            ModelBounds = layer.BoundingBoxHidden();
                        else
                            ModelBounds.Union(layer.BoundingBoxHidden());

                        if (ModelBounds.IsEmpty)
                            ModelBounds = layer.BoundingBoxVisible();
                        else
                            ModelBounds.Union(layer.BoundingBoxVisible());
                    }
                }
            }
            Dispatcher.BeginInvoke(new Action(Hide<IfcSpace>), DispatcherPriority.Background);
            return scene;
        }

        /// <summary>
        /// Function that actually populates the geometry from the layer into the viewer meshes.
        /// If the <paramref name="isLayerVisible"/> is set to false layer becomes hidden.
        /// </summary>
        private void AddLayerToDrawingControl(XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial> layer, bool addTagProperty, bool isLayerVisible)
        {
            AddLayerToDrawingControl(layer, addTagProperty);
            if (!isLayerVisible)
                layer.HideAll();
        }

        /// <summary>
        /// function that actually populates the geometry from the layer into the viewer meshes.
        /// </summary>
        private void AddLayerToDrawingControl(XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial> layer, bool addTagProperty) // Formerly called DrawLayer
        {
            layer.Show();
            GeometryModel3D m3D = (WpfMeshGeometry3D)layer.Visible;
            if (addTagProperty)
                m3D.SetValue(TagProperty, layer);

            // sort out materials and bind
            if (layer.Style.RenderBothFaces)
                m3D.BackMaterial = m3D.Material = (WpfMaterial)layer.Material;
            else if (layer.Style.SwitchFrontAndRearFaces)
                m3D.BackMaterial = (WpfMaterial)layer.Material;
            else
                m3D.Material = (WpfMaterial)layer.Material;
            if (ForceRenderBothSides) m3D.BackMaterial = m3D.Material;
            _materials.Add(m3D.Material);
            // SetOpacityPercent(m3d.Material, ModelOpacity);
            var mv = new ModelVisual3D();
            mv.Content = m3D;
            if (layer.Style.IsTransparent)
                Transparents.Children.Add(mv);
            else
                Opaques.Children.Add(mv);

            foreach (var subLayer in layer.SubLayers)
                AddLayerToDrawingControl(subLayer, addTagProperty);
        }

        /// <summary>
        /// Returns the list of nested visual elements.
        /// </summary>
        /// <param name="ofItem">Valid names are for instance: Opaques, Transparents, BuildingModel, cuttingGroup...</param>
        /// <returns>IEnumerable names</returns>
        public IEnumerable<string> ListItems(string ofItem)
        {
            foreach (var scene in Scenes)
                foreach (var layer in scene.SubLayers) //go over top level layers 
                {
                    yield return layer.Name;
                }
        }

        /// <summary>
        /// Useful for analysis and debugging purposes (invoked by Querying interface)
        /// </summary>
        /// <returns>A string tree of layers in scenes</returns>
        public IEnumerable<string> LayersTree()
        {
            foreach (var scene in Scenes)
                foreach (var layer in scene.SubLayers) //go over top level layers 
                {
                    foreach (var item in layer.LayersTree(0))
                    {
                        yield return item;    
                    }
                }   
        }


        public void SetVisibility(string layerName, bool visibility)
        {
            foreach (var scene in Scenes)
            {
                foreach (var layer in scene.SubLayers) //go over top level layers only
                {
                    if (layer.Name == layerName)
                    {
                        if (visibility)
                            layer.ShowAll();
                        else
                            layer.HideAll();
                    }
                }
            }
        }

        /// <summary>
        ///   Hides all instances of the specified type
        /// </summary>
        public void Hide<T>()
        {
            var ifcType = IfcMetaData.IfcType(typeof(T));
            var toHide = ifcType.Name + ";";
            foreach (var subType in ifcType.NonAbstractSubTypes)
                toHide += subType.Name + ";";
            foreach (var scene in Scenes)
                foreach (var layer in scene.SubLayers) //go over top level layers only
                    if (toHide.Contains(layer.Name + ";"))
                        layer.HideAll();
        }
       
        public static readonly DependencyProperty LayerSetProperty =
            DependencyProperty.Register("LayerSet", typeof(List<LayerViewModel>), typeof(DrawingControl3D));

        public List<LayerViewModel> LayerSet
        {
            get { return (List<LayerViewModel>)GetValue(LayerSetProperty); }
            // set { SetValue(ShowSpacesProperty, value); }
        }

        private List<LayerViewModel> LayerSetRefresh()
        {
            var ret = new List<LayerViewModel>();
            ret.Add(new LayerViewModel("All"));
            foreach (var scene in Scenes)
                foreach (var layer in scene.SubLayers) // go over top level layers only
                    ret.Add(new LayerViewModel(layer.Name));
            return ret;
        }

        public void Hide(int hideProduct)
        {
            //ModelVisual3D item;
            //if (_items.TryGetValue(hideProduct, out item))
            //{
            //    ModelVisual3D parent = VisualTreeHelper.GetParent(item) as ModelVisual3D;
            //    if (parent != null)
            //    {
            //        _hidden.Add(item, parent);
            //        parent.Children.Remove(item);
            //    }
            //    return;
            //}
        }

        private void Show<T>()
        {
            var ifcType = IfcMetaData.IfcType(typeof(T));
            var toShow = ifcType.Name + ";";
            foreach (var subType in ifcType.NonAbstractSubTypes)
                toShow += subType.Name + ";";
            foreach (var scene in Scenes)
                foreach (var layer in scene.SubLayers) //go over top level layers only
                    if (toShow.Contains(layer.Name + ";"))
                        layer.ShowAll();
        }

        public void ShowAll()
        {
            _exclude = new List<Type>();
            _exclude.Add(typeof(IfcFeatureElement));
            _exclude.Add(typeof(IfcSpace));
           // ReloadModel(false);
  
        }

        public void HideAll()
        {
            _exclude = new List<Type>();
            _exclude.Add(typeof(IfcFeatureElement));
            _exclude.Add(typeof(IfcSpace));
            _exclude.Add(typeof(IfcFastener));
            _exclude.Add(typeof(IfcPlate));
            _exclude.Add(typeof(IfcMember));
           // ReloadModel(false);
            
        }

        public void SetCamera(ProjectionCamera cam)
        {
            Viewport.Camera = cam;
        }

        public void ViewHome()
        {
            if (Viewport.CameraController != null) Viewport.CameraController.ResetCamera();
            var r3D = new Rect3D(_viewBounds.X, _viewBounds.Y, _viewBounds.Z, _viewBounds.SizeX, _viewBounds.SizeY, _viewBounds.SizeZ);
            Viewport.ZoomExtents(r3D);
        }

        public void ZoomSelected()
        {
            if (SelectedEntity != null && Highlighted != null && Highlighted.Mesh != null)
            {
                var r3D = Highlighted.Mesh.GetBounds();
                ZoomTo(r3D);
            }
        }


        /// <summary>
        /// This functions sets a cutting plane at a distance of delta over the base of the selected element.
        /// It is useful when the selected element is obscured by elements surrounding it.
        /// </summary>
        /// <param name="delta">positive distance of the cutting plane above the base of the selected element.</param>
        public void ClipBaseSelected(double delta)
        {
            if (SelectedEntity != null && Highlighted != null && Highlighted.Mesh != null)
            {
                var r3D = Highlighted.Mesh.GetBounds();
                if (!r3D.IsEmpty)
                SetCutPlane(
                    r3D.X, r3D.Y, r3D.Z + delta, 
                    0, 0, -1
                    );
            }
        }

        /// <summary>
        /// Zooms to a selected portion of the space.
        /// </summary>
        /// <param name="r3D">The box to be zoomed to</param>
        /// <param name="doubleRectSize">Effectively doubles the size of the bounding box so to fit more space around it.</param>
        private void ZoomTo(Rect3D r3D, bool doubleRectSize = true)
        {
            if (!r3D.IsEmpty)
            {
                var bounds = new Rect3D(
                    _viewBounds.X, _viewBounds.Y, _viewBounds.Z, 
                    _viewBounds.SizeX, _viewBounds.SizeY, _viewBounds.SizeZ
                    );
                if (doubleRectSize)
                {
                    r3D.Offset(-r3D.SizeX / 2, -r3D.SizeY / 2, -r3D.SizeZ / 2);
                    r3D.SizeX *= 2;
                    r3D.SizeY *= 2;
                    r3D.SizeZ *= 2;
                }
                if (!r3D.IsEmpty)
                {
                    if (r3D.Contains(bounds)) // if bigger than bounds zoom bounds
                        Viewport.ZoomExtents(bounds, 200);
                    else
                        Viewport.ZoomExtents(r3D, 200);
                }
            }
        }

        public void ZoomTo(XbimRect3D r3D)
        {
            ZoomTo(new Rect3D(
                        new Point3D(r3D.X, r3D.Y, r3D.Z),
                        new Size3D(r3D.SizeX, r3D.SizeY, r3D.SizeZ)
                        ),
                   false);
        }



        public void ShowOctree(XbimOctree<IfcProduct> octree, int specificLevel = -1, bool onlyWithContent = false)
        {
            if (Viewport.Children.Contains(_octreeVisualization))
                Viewport.Children.Remove(_octreeVisualization);
            _octreeVisualization.Children.Clear();
            ShowOctree<IfcProduct>(octree, specificLevel, onlyWithContent);
            Viewport.Children.Add(_octreeVisualization);
            
        }

        ModelVisual3D _octreeVisualization = new ModelVisual3D();


        private void ShowOctree<T>(XbimOctree<T> octree, int specificLevel = -1, bool onlyWithContent = false)
        {
            //prepare action
            Action show = () => {
                var rect = new WpfXbimRectangle3D(octree.Bounds);

                //create transformation
                var scale = 1f / Model.ModelFactors.OneMetre;
                var transformation = Transform3DHelper.CombineTransform(
                    new TranslateTransform3D(ModelTranslation.X, ModelTranslation.Y, ModelTranslation.Z),
                    new ScaleTransform3D(scale, scale, scale)
                    );

                //Add octree geometry
                _octreeVisualization.Children.Add(new ModelVisual3D { Content = rect.Geometry, Transform = transformation });
            };

            if (specificLevel == -1 || specificLevel == octree.Depth)
            {
                if (onlyWithContent && octree.Content().Any())
                    show();
                else if (!onlyWithContent)
                    show();
            }

            if (specificLevel == -1 || specificLevel > octree.Depth)
                foreach (var child in octree.Children)
                {
                    ShowOctree(child, specificLevel, onlyWithContent);
                }
        }

        private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Console.WriteLine(e.Delta);
        }

        /// <summary>
        /// create a bitmap image of the required size and saves to the specificed file. Title is printed if specified
        /// </summary>
        /// <param name="model"></param>
        /// <param name="bmpFileName"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="title"></param>
        static public void CreateThumbnail(XbimModel model,string bmpFileName, int width, int height, string title=null)
        {
            var d3D = new DrawingControl3D();
            // Create the container
            var container = new Border
            {
                Child = d3D,
                Background = Brushes.White,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                Width=width,
                Height=height
            };

            // Measure and arrange the container
            container.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            container.Arrange(new Rect(container.DesiredSize));

            // Temporarily add a PresentationSource if none exists
// ReSharper disable once UnusedVariable
            using (var temporaryPresentationSource = new HwndSource(new HwndSourceParameters()) { RootVisual = (VisualTreeHelper.GetParent(container) == null ? container : null) })
            {
                
                d3D.Dispatcher.Invoke(DispatcherPriority.SystemIdle, new Action(() => { }));
                d3D.LoadGeometry(model);
                d3D.Viewport.ShowViewCube = false;
                d3D.Viewport.ShowFieldOfView = false;
                d3D.Viewport.ShowCoordinateSystem = false;
                if (!string.IsNullOrEmpty(title))
                {
                    d3D.Viewport.Title = title;
                   
                }
                d3D.ShowGridLines = false;
                d3D.Viewport.ZoomExtents();
                // Render to bitmap
                var rtb = new RenderTargetBitmap((int)container.ActualWidth, (int)container.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(container);

                var aEncoder = new PngBitmapEncoder();
                aEncoder.Frames.Add(BitmapFrame.Create(rtb));

                using (Stream stm = File.Create(bmpFileName))
                {
                    aEncoder.Save(stm);
                }
            }
        }
    }
}
