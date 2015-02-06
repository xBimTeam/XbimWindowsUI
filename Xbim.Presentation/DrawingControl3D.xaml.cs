#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Presentation
// Filename:    DrawingControl3D.xaml.cs
// Published:   01, 2012
// Last Edited: 9:05 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.ModelGeometry;
using Xbim.ModelGeometry.Scene;
using Xbim.XbimExtensions;
using Xbim.Ifc2x3.SharedComponentElements;
using Xbim.XbimExtensions.Interfaces;
using Xbim.IO;
using System.Diagnostics;
using System.Windows.Markup;
using Xbim.Common.Exceptions;
using System.Threading;
using Xbim.Ifc2x3;
using HelixToolkit.Wpf;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Xbim.Common.Geometry;
using Xbim.Ifc2x3.ExternalReferenceResource;
using System.Text;
using Xbim.Presentation.ModelGeomInfo;
using Xbim.ModelGeometry.Converter;
using Xbim.Ifc2x3.PresentationAppearanceResource;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Interop;
using System.Windows.Threading;
using XbimGeometry.Interfaces;
using Xbim.Presentation.LayerStyling;
using Xbim.Presentation.LayerStylingV2;

#endregion

namespace Xbim.Presentation
{
    /// <summary>
    ///   Interaction logic for DrawingControl3D.xaml
    /// </summary>
    public partial class DrawingControl3D : UserControl
    {   
        public DrawingControl3D()
        {
            InitializeComponent();
            Highlighted.PropertyChanged += Highlighted_PropertyChanged;
            Viewport = Canvas;
            Canvas.MouseDown += Canvas_MouseDown;
            Canvas.MouseWheel += Canvas_MouseWheel;
            this.Loaded += DrawingControl3D_Loaded;
            federationColours = new XbimColourMap(StandardColourMaps.Federation);
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

        CombinedManipulator ClipHandler = null;

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            var plane = GetCutPlane();
            if (e.Key == Key.LeftShift && ClipHandler == null && plane != null)
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
                ClipHandler = null;
            }
            base.OnPreviewKeyDown(e);
        }

        private void ClipPlaneHandlesPlace(Point3D pos)
        {
            Matrix3D m = Matrix3D.Identity;
            m.Translate(new Vector3D(
                pos.X, pos.Y, pos.Z)
                );
            Extras.Transform = new MatrixTransform3D(m);
            // ClipPlaneHandlesShow();
        }

        private void ClipPlaneHandlesShow()
        {
            ClipHandler = new CombinedManipulator();
            Extras.Children.Add(ClipHandler);
        }

        protected override void OnPreviewKeyUp(KeyEventArgs e)
        {
            // dealing with cutting plane update
            //
            if (e.Key == Key.LeftShift && ClipHandler != null)
            {
                var m1 = Extras.Transform.Value;
                var m2 = ClipHandler.Transform.Value;

                ClipPlaneHandlesHide();

                var newMatrix = Matrix3D.Multiply(m2, m1);
                Extras.Transform = new MatrixTransform3D(newMatrix);

                Point3D p = new Point3D(newMatrix.OffsetX, newMatrix.OffsetY, newMatrix.OffsetZ);
                var n = newMatrix.Transform(new Vector3D(0, 0, -1));
                ClearCutPlane();
                SetCutPlane(p.X, p.Y, p.Z, n.X, n.Y, n.Z);
            }
            base.OnPreviewKeyUp(e);
        }

        private void ClipPlaneHandlesHide()
        {
            Extras.Children.Clear();
            ClipHandler = null;
        }


        // elements associated with vector polygons drafted interactively on the model by the user
        //
       
        private List<Type> _exclude;
           
        private LinesVisual3D _UserModeledDimLines;
        private PointsVisual3D _UserModeledDimPoints;
        public PolylineGeomInfo UserModeledDimension = new PolylineGeomInfo();

        private void FirePrevPointsChanged()
        {
            if (!UserModeledDimension.IsEmpty)
            {
                // enable the loop that updates the drawing geometry
                CompositionTarget.Rendering += this.OnCompositionTargetRendering;
            }
            if (UserModeledDimensionChangedEvent != null)
                UserModeledDimensionChangedEvent(this, UserModeledDimension);

        }

        void OnCompositionTargetRendering(object sender, EventArgs e)
        {
            bool doShow = !UserModeledDimension.IsEmpty;
            // lines
            double depthoff = 0.001;
            if (doShow && _UserModeledDimLines == null)
            {
                _UserModeledDimLines = new LinesVisual3D { 
                    Color = Colors.Yellow, 
                    Thickness = 3,
                    DepthOffset = depthoff
                };
                Canvas.Children.Add(_UserModeledDimLines);
            }
            if (!doShow && _UserModeledDimLines != null)
            {
                _UserModeledDimLines.IsRendering = false;
                Canvas.Children.Remove(_UserModeledDimLines);
                _UserModeledDimLines = null;
            }
            // points 
            if (doShow && _UserModeledDimPoints == null)
            {
                _UserModeledDimPoints = new PointsVisual3D { 
                    Color = Colors.Orange, 
                    Size = 5,
                    DepthOffset = depthoff
                };
                Canvas.Children.Add(_UserModeledDimPoints);
            }
            if (!doShow && _UserModeledDimPoints != null)
            {
                _UserModeledDimPoints.IsRendering = false;
                Canvas.Children.Remove(_UserModeledDimPoints);
                _UserModeledDimPoints = null;
            }
            if (!doShow)
            {
                // if not needed the hook can be removed until a new measure is made by the user
                CompositionTarget.Rendering -= this.OnCompositionTargetRendering;
            }

            // geometry prep
            if (_UserModeledDimLines != null)
                _UserModeledDimLines.Points = UserModeledDimension.VisualPoints;
            if (_UserModeledDimPoints != null)
                _UserModeledDimPoints.Points = UserModeledDimension.VisualPoints;
        }

        void UpdatefrustumPlanes(object sender, RoutedEventArgs e)
        {
            HelixViewport3D snd = sender as HelixViewport3D;
            if (snd == null)
                return;

            var middlePoint = viewBounds.Centroid();
            double CentralDistance = Math.Sqrt(
                    Math.Pow(snd.Camera.Position.X, 2) + Math.Pow(middlePoint.X, 2) +
                    Math.Pow(snd.Camera.Position.Y, 2) + Math.Pow(middlePoint.Y, 2) +
                    Math.Pow(snd.Camera.Position.Z, 2) + Math.Pow(middlePoint.Z, 2)
                    );

            double diag = 40;
            if (viewBounds.Length() > 0)
            {
                diag = viewBounds.Length();
            }
            double FarPlaneDistance = CentralDistance + 1.5 * diag;
            double NearPlaneDistance = CentralDistance - 1.5 * diag;

            const double nearLimit = 0.125;
            NearPlaneDistance = Math.Max(NearPlaneDistance, nearLimit);

            if (snd.Camera.NearPlaneDistance != NearPlaneDistance)
            {
                snd.Camera.NearPlaneDistance = NearPlaneDistance;  // Debug.WriteLine("Near: " + NearPlane);
            }
            if (snd.Camera.FarPlaneDistance != FarPlaneDistance)
            {
                snd.Camera.FarPlaneDistance = FarPlaneDistance;    // Debug.WriteLine("Far: " + FarPlane);
            }
            }

        void DrawingControl3D_Loaded(object sender, RoutedEventArgs e)
        {
            ShowSpaces = false; 
        }

        #region Fields
        public List<XbimScene<WpfMeshGeometry3D, WpfMaterial>> scenes = new List<XbimScene<WpfMeshGeometry3D, WpfMaterial>>();
        private XbimColourMap federationColours;

        // protected RayMeshGeometry3DHitTestResult _hitResult;
       
        public XbimRect3D ModelBounds;
        private XbimRect3D viewBounds;
        // private int? _currentProduct;
        private List<Material> _materials = new List<Material>();
        private Dictionary<Material, double> _opacities = new Dictionary<Material, double>();
        /// <summary>
        /// Gets or sets the model.
        /// </summary>
        /// <value>The model.</value>
        public Model3D Model3d { get; set; }

        public Plane3D GetCutPlane()
        {
            object p = this.FindName("cuttingGroup");
            var cpg = p as CuttingPlaneGroup;
            if (cpg == null || cpg.IsEnabled == false) 
                return null;
            if (cpg.CuttingPlanes.Count == 1)
                return cpg.CuttingPlanes[0];
            return null;
        }

        public void SetCutPlane(double PosX, double PosY, double PosZ, double NrmX, double NrmY, double NrmZ)
        {   
            SetNamedCutPlane(PosX, PosY, PosZ, NrmX, NrmY, NrmZ, "cuttingGroup");
            SetNamedCutPlane(PosX, PosY, PosZ, NrmX, NrmY, NrmZ, "cuttingGroupT");
        }

        private void SetNamedCutPlane(double PosX, double PosY, double PosZ, double NrmX, double NrmY, double NrmZ, string cuttingGroupName)
        {
            object p = this.FindName(cuttingGroupName);
            CuttingPlaneGroup cpg = p as CuttingPlaneGroup;
            if (cpg != null)
            {
                cpg.IsEnabled = false;
                cpg.CuttingPlanes.Clear();
                cpg.CuttingPlanes.Add(
                    new Plane3D(
                        new Point3D(PosX, PosY, PosZ),
                        new Vector3D(NrmX, NrmY, NrmZ)
                        ));
                cpg.IsEnabled = true;
            }
        }

        public void ClearCutPlane()
        {
            ClearNamedCutPlane("cuttingGroup");
            ClearNamedCutPlane("cuttingGroupT");
        }

        private void ClearNamedCutPlane(string name)
        {
            object p = this.FindName(name);
            CuttingPlaneGroup cpg = p as CuttingPlaneGroup;
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
            _SelectedEntityChangeTriggedBySelectionChange = true;
            if (this.SelectedEntity == null && entity == null)
            {
                // OnSelectedEntityChanged(this, new DependencyPropertyChangedEventArgs(SelectedEntityProperty, null, null));
                this.HighlighSelected(null);
            }
            this.SelectedEntity = entity;
            
            _SelectedEntityChangeTriggedBySelectionChange = false;
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
            };

            object hitObject = hit.ModelHit.GetValue(TagProperty);
            IPersistIfcEntity thisSelectedEntity=null;
            if (hitObject is XbimInstanceHandle)
            {
                XbimInstanceHandle selhandle = (XbimInstanceHandle)hitObject;
                thisSelectedEntity = selhandle.GetEntity();
            }
            else if (hitObject is WpfMeshGeometry3D)
            {
                WpfMeshGeometry3D mesh = hitObject as WpfMeshGeometry3D;
                var frag = mesh.Meshes.Find(hit.VertexIndex1);
                short modelId = frag.ModelId;
                XbimModel modelHit = null; //default to not hit
                if (modelId == 0) modelHit = this.Model;
                else
                {
                    foreach (var refModel in this.Model.ReferencedModels)
                    {
                        if (refModel.Model.UserDefinedId == modelId)
                        {
                            modelHit = refModel.Model;
                            break;
                        }
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
            else
            {
                Selection.Clear();
                HighlighSelected(null);
            }
            
            if (SelectionBehaviour == SelectionBehaviours.MultipleSelection)
            {
                // default behaviour is single selection
                XbimMouseClickActions mc = XbimMouseClickActions.Single;
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
                            bool bAdded = Selection.Toggle(thisSelectedEntity);
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
                            
                            // UserModeledDimension.SetToVisual(FurtherGeometries); 
                            break;
                        case XbimMouseClickActions.SetClip:
                            SetCutPlane(
                                hit.PointHit.X, hit.PointHit.Y, hit.PointHit.Z,
                                0, 0, -1);
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

        private XbimMeshFragment GetClickedMeshFragment(RayMeshGeometry3DHitTestResult hit)
        {
        
            if (hit != null)
            {
                XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial> layer = hit.ModelHit.GetValue(TagProperty) as XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial>; //get the fragments
                if (layer != null)
                {
                    var frag = layer.Visible.Meshes.Find(hit.VertexIndex1);
                    if (frag.IsEmpty)
                        frag = layer.Visible.Meshes.Find(hit.VertexIndex2);
                    if (frag.IsEmpty)
                        frag = layer.Visible.Meshes.Find(hit.VertexIndex3);
                    if (!frag.IsEmpty)
                    {
                        return frag;
                    }
                }
            }
            return default(XbimMeshFragment);
        }
        
        
        
        private PointGeomInfo GetClosestPoint(RayMeshGeometry3DHitTestResult hit)
        {
            int[] pts = new int[] {
                hit.VertexIndex1,
                hit.VertexIndex2,
                hit.VertexIndex3
            };

            PointGeomInfo pHit = new PointGeomInfo();
            pHit.Entity = GetClickedEntity(hit);
            pHit.Point = hit.PointHit;

            double minDist = double.PositiveInfinity;
            int iClosest = -1;
            for (int i = 0; i < 3; i++)
            {
                
                int iPtMesh = pts[i];

                PointGeomInfo pRetI = new PointGeomInfo();
                pRetI.Entity = pHit.Entity;
                pRetI.Point = hit.MeshHit.Positions[iPtMesh];

                double dist = hit.PointHit.DistanceTo(hit.MeshHit.Positions[iPtMesh]);
                if (dist < minDist)
                {
                    minDist = dist;
                    iClosest = iPtMesh;
                }
            }

            PointGeomInfo pRet = new PointGeomInfo();
            pRet.Entity = pHit.Entity;
            pRet.Point = hit.MeshHit.Positions[iClosest];

            return pRet;
        }

        private  IPersistIfcEntity GetClickedEntity(RayMeshGeometry3DHitTestResult hit)
        {
            IPersistIfcEntity clicked = null;
            if (hit != null)
            {
                XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial> layer = hit.ModelHit.GetValue(TagProperty) as XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial>; //get the fragments
                if (layer != null)
                {
                    var frag = layer.Visible.Meshes.Find(hit.VertexIndex1);
                    short modelId = frag.ModelId;
                    XbimModel modelHit =  null; //default to not hit
                    if (modelId == 0) modelHit = this.Model;
                    else
                    {
                        foreach (var refModel in this.Model.ReferencedModels)
                        {
                            if (refModel.Model.UserDefinedId == modelId)
                            {
                                modelHit = refModel.Model;
                                break;
                            }
                        }
                    }
                    if (modelHit != null)
                    {
                    if (frag.IsEmpty)
                        frag = layer.Visible.Meshes.Find(hit.VertexIndex2);
                    if (frag.IsEmpty)
                        frag = layer.Visible.Meshes.Find(hit.VertexIndex3);
                    if (!frag.IsEmpty)
                    {
                            clicked = modelHit.Instances[frag.EntityLabel];
                    }
                }
            }
            }
            return clicked;
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
             DrawingControl3D d3d = d as DrawingControl3D;
             if (d3d != null && e.NewValue !=null)
             {
                 d3d.SetOpacity((double)e.NewValue);
             }
        }

        private void SetOpacity( double opacityPercent)
        {
            double opacity = Math.Min(1, opacityPercent);
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
                                                                      new PropertyChangedCallback(OnModelChanged)));

        private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DrawingControl3D d3d = d as DrawingControl3D;
            if (d3d != null)
            {
                // XbimModel model = e.NewValue as XbimModel;
                d3d.ReloadModel();
            }
        }

        public void ReloadModel(ModelRefreshOptions Options = ModelRefreshOptions.None)
        {
            LoadGeometry(
                model: (XbimModel)this.GetValue(ModelProperty),
                Options: Options
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
            if (d is DrawingControl3D)
            {
                DrawingControl3D d3d = d as DrawingControl3D;
                EntitySelection oldVal = e.OldValue as EntitySelection;
                EntitySelection newVal = e.NewValue as EntitySelection;
                d3d.ReplaceSelection(newVal, oldVal);
            }
        }

        private void ReplaceSelection(EntitySelection newVal, EntitySelection oldVal)
        {
            if (newVal.Count() < 2)
            {
                SelectionDrivenSelectedEntityChange(newVal.FirstOrDefault());
            }
            this.HighlighSelected(null);
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
        private bool _SelectedEntityChangeTriggedBySelectionChange = false;
        private static void OnSelectedEntityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DrawingControl3D)
            {
                DrawingControl3D d3d = d as DrawingControl3D;
                // IPersistIfcEntity oldVal = e.OldValue as IPersistIfcEntity;
                // if (oldVal != null)
                // {
                //    d3d.Deselect(oldVal);
                // }
                IPersistIfcEntity newVal = e.NewValue as IPersistIfcEntity;
                if (!d3d._SelectedEntityChangeTriggedBySelectionChange)
                {
                    d3d.Selection.Clear();
                    if (newVal != null)
                        d3d.Selection.Add(newVal);
                }
                d3d.HighlighSelected(newVal);
            }
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
                    if (fromModel != null && newVal is IfcProduct)
                    {
                        short modelId = fromModel.UserDefinedId;
                        double metre = fromModel.ModelFactors.OneMetre;
                        wcsTransform = XbimMatrix3D.CreateTranslation(_modelTranslation) * XbimMatrix3D.CreateScale((float)(1 / metre));
           
                        var context = new Xbim3DModelContext(fromModel);
                        
                        List<XbimShapeInstance> productShape = context.ShapeInstancesOf((IfcProduct)newVal).Where(s => s.RepresentationType != XbimGeometryRepresentationType.OpeningsAndAdditionsExcluded).ToList();
                        if (productShape.Any())
                        {

                            foreach (var shapeInstance in productShape)
                            {
                                IXbimShapeGeometryData shapeGeom = context.ShapeGeometry(shapeInstance.ShapeGeometryLabel);
                                m.Read(shapeGeom.ShapeData, XbimMatrix3D.Multiply(shapeInstance.Transformation,wcsTransform));
                        }
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
                    short modelId = fromModel.UserDefinedId;
                    double metre = fromModel.ModelFactors.OneMetre;
                    wcsTransform = XbimMatrix3D.CreateTranslation(_modelTranslation) * XbimMatrix3D.CreateScale((float)(1 / metre));
           
                    List<XbimShapeInstance> productShape = context.ShapeInstancesOf((IfcProduct)newVal).Where(s=>s.RepresentationType!=XbimGeometryRepresentationType.OpeningsAndAdditionsExcluded).ToList();
                    if (productShape.Any())
                {

                        foreach (var shapeInstance in productShape)
                    {
                            XbimShapeGeometry shapeGeom = context.ShapeGeometry(shapeInstance.ShapeGeometryLabel);
                            m.Read(shapeGeom.ShapeData, XbimMatrix3D.Multiply(shapeInstance.Transformation,wcsTransform));
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
                for (int i = 0; i < m.TriangleIndices.Count; i += 3)
                {
                    int p1 = m.TriangleIndices[i];
                    int p2 = m.TriangleIndices[i + 1];
                    int p3 = m.TriangleIndices[i + 2];

                    if (m.Normals[p1] == m.Normals[p2] && m.Normals[p1] == m.Normals[p3]) // same normals
                    {
                        var cnt = FindCentroid(new [] { m.Positions[p1], m.Positions[p2], m.Positions[p3] });
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
                    XbimRect3D box = XbimRect3D.Empty;
                    for (int i = 0; i < m.TriangleIndices.Count; i += 3)
                    {
                        int p1 = m.TriangleIndices[i];
                        int p2 = m.TriangleIndices[i + 1];
                        int p3 = m.TriangleIndices[i + 2];

                        // box evaluation
                        box.Union(new XbimPoint3D(m.Positions[p1].X, m.Positions[p1].Y, m.Positions[p1].Z));
                        box.Union(new XbimPoint3D(m.Positions[p2].X, m.Positions[p2].Y, m.Positions[p2].Z));
                        box.Union(new XbimPoint3D(m.Positions[p3].X, m.Positions[p3].Y, m.Positions[p3].Z));
                    }

                    double bl = box.Length();
                    double lineThickness = bl / 1000; // 0.01;

                    for (int i = 0; i < m.TriangleIndices.Count; i += 3)
                    {
                        int p1 = m.TriangleIndices[i];
                        int p2 = m.TriangleIndices[i + 1];
                        int p3 = m.TriangleIndices[i + 2];
                        

                        var path = new List<Point3D>();
                        path.Add(new Point3D(m.Positions[p1].X, m.Positions[p1].Y, m.Positions[p1].Z));
                        path.Add(new Point3D(m.Positions[p2].X, m.Positions[p2].Y, m.Positions[p2].Z));
                        path.Add(new Point3D(m.Positions[p3].X, m.Positions[p3].Y, m.Positions[p3].Z));

                        
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
            DrawingControl3D d3d = d as DrawingControl3D;
            if (d3d != null)
            {
                if (e.NewValue is bool)
                {
                    if ((bool)e.NewValue)
                        d3d.Show<IfcSpace>();
                    else
                        d3d.Hide<IfcSpace>();
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
            DrawingControl3D d3d = d as DrawingControl3D;
            if (d3d != null)
            {
                if (e.NewValue is bool)
                {
                    bool on = (bool)e.NewValue;
                    if (on)
                        d3d.ShowAll();
                    else
                        d3d.HideAll();
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
            DrawingControl3D d3d = d as DrawingControl3D;
            if (d3d != null)
            {
                if (e.NewValue is bool)
                {
                    bool on = (bool)e.NewValue;
                    if (on)
                        d3d.Show<IfcDoor>();
                    else
                        d3d.Hide<IfcDoor>();
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
            DrawingControl3D d3d = d as DrawingControl3D;
            if (d3d != null)
            {
                if (e.NewValue is bool)
                {
                    if ((bool)e.NewValue)
                        d3d.Show<IfcWindow>();
                    else
                        d3d.Hide<IfcWindow>();
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
            DrawingControl3D d3d = d as DrawingControl3D;
            if (d3d != null)
            {
                if (e.NewValue is bool)
                {
                    if ((bool)e.NewValue)
                        d3d.Show<IfcSlab>();
                    else
                        d3d.Hide<IfcSlab>();
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
            DrawingControl3D d3d = d as DrawingControl3D;
            if (d3d != null)
            {
                if (e.NewValue is bool)
                {
                    if ((bool)e.NewValue)
                        d3d.Show<IfcFurnishingElement>();
                    else
                        d3d.Hide<IfcFurnishingElement>();
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
            DrawingControl3D d3d = d as DrawingControl3D;
            if (d3d != null)
            {
                if (e.NewValue is bool)
                {
                    if ((bool)e.NewValue)
                        d3d.Viewport.Children.Insert(0, d3d.GridLines);
                    else
                        d3d.Viewport.Children.Remove( d3d.GridLines);
                }
            }
        }
        

        public HelixToolkit.Wpf.HelixViewport3D Viewport
        {
            get { return (HelixToolkit.Wpf.HelixViewport3D)GetValue(ViewportProperty); }
            set { SetValue(ViewportProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Viewport.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ViewportProperty =
            DependencyProperty.Register("Viewport", typeof(HelixToolkit.Wpf.HelixViewport3D), typeof(DrawingControl3D), new PropertyMetadata(null));

        public Point3D FindCentroid(Point3D[] p)
        {
            double x = 0;
            double y = 0;
            double z = 0;
            int n = 0;
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

            const double lineThickness = 0.01;
            axesMeshBuilder.AddTube(path, lineThickness, 9, false);
            return;
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
        public XbimVector3D _modelTranslation;
        public XbimMatrix3D wcsTransform;


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
            this.ClearCutPlane();

            ModelBounds = XbimRect3D.Empty;
            viewBounds = new XbimRect3D(0, 0, 0, 10, 10, 5);    
            scenes = new List<XbimScene<WpfMeshGeometry3D, WpfMaterial>>();
            if (!((options & ModelRefreshOptions.ViewPreserveCameraPosition) == ModelRefreshOptions.ViewPreserveCameraPosition))
            Viewport.ResetCamera();
        }

        private XbimRect3D GetModelBounds(XbimModel model)
        {
            XbimRect3D box = new XbimRect3D();
            if (model == null) return box;
            bool first = true;
            foreach (XbimGeometryData shape in model.GetGeometryData(XbimGeometryType.BoundingBox))
            {
                XbimMatrix3D matrix3d = XbimMatrix3D.FromArray(shape.DataArray2);
                XbimRect3D bb = XbimRect3D.FromArray(shape.ShapeData);
                bb = XbimRect3D.TransformBy(bb, matrix3d);  
                if (first) { box = bb; first = false; }
                else box.Union(bb);
            }
            return box;
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

        public ILayerStylerV2 GeomSupport2LayerStyler = null;

        /// <summary>
        /// Clears the current graphics and initiates the cascade of events that result in viewing the scene.
        /// </summary>
        /// <param name="EntityLabels">If null loads the whole model, otherwise only elements listed in the enumerable</param>
        public void LoadGeometry(XbimModel model, IEnumerable<int> EntityLabels = null, ModelRefreshOptions Options = ModelRefreshOptions.None)
        {
            // AddLayerToDrawingControl is the function that actually populates the geometry in the viewer.
            // AddLayerToDrawingControl is triggered by BuildRefModelScene and BuildScene below here when layers get ready.

            //reset all the visuals
            ClearGraphics(Options);
            short userDefinedId = 0;
            if (model == null) 
                return; //nothing to show
            model.UserDefinedId = userDefinedId;
            int geometrySupportLevel = model.GeometrySupportLevel;
            Xbim3DModelContext context = new Xbim3DModelContext(model);
            XbimRegion largest;
            if (geometrySupportLevel == 1)
                largest = GetLargestRegion(model);
            else //assume we are the latest level (2)
                largest = context.GetLargestRegion();
            XbimPoint3D c = new XbimPoint3D(0, 0, 0);
            XbimRect3D bb = XbimRect3D.Empty;
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
                    Xbim3DModelContext refContext = new Xbim3DModelContext(refModel.Model);
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
            XbimPoint3D p = bb.Centroid();
            _modelTranslation = new XbimVector3D(-p.X, -p.Y, -p.Z);

            // model scaling
            double metre = model.ModelFactors.OneMetre;
            this.wcsTransform = XbimMatrix3D.CreateTranslation(this._modelTranslation) * XbimMatrix3D.CreateScale(1 / metre);


            model.ReferencedModels.CollectionChanged += ReferencedModels_CollectionChanged;

            // prepare grouping and layering behaviours
            if (LayerStyler == null)
                LayerStyler = new LayerStyling.LayerStylerTypeAndIFCStyle();
            LayerStyler.SetFederationEnvironment(null);
            //build the geometric scene and render as we go
            XbimScene<WpfMeshGeometry3D, WpfMaterial> scene;

            

            if (geometrySupportLevel == 1)
                scene = BuildScene(model, EntityLabels, LayerStyler);
            else //assume we are the latest level (2)
            {
                if (GeomSupport2LayerStyler == null)
                    GeomSupport2LayerStyler = new SurfaceLayerStyler();
                GeomSupport2LayerStyler.Control = this;
                GeomSupport2LayerStyler.SetFederationEnvironment(null);
                scene = GeomSupport2LayerStyler.BuildScene(model, context, _exclude);
            }

            if(scene.Layers.Count() > 0)
                scenes.Add(scene);
            foreach (var refModel in model.ReferencedModels)
            {
                if (refModel.Model.GeometrySupportLevel == 1)
                    scenes.Add(BuildRefModelScene(refModel.Model, refModel.DocumentInformation));
                else  //assume we are the latest level (2)
                {
                    GeomSupport2LayerStyler.SetFederationEnvironment(refModel);
                    Xbim3DModelContext refContext = new Xbim3DModelContext(refModel.Model);
                    scenes.Add(GeomSupport2LayerStyler.BuildScene(refModel.Model, refContext, _exclude));
                }
            }
            ShowSpaces = false;
            RecalculateView(model);
        }

        private XbimRegion GetLargestRegion(XbimModel model)
        {
            IfcProject project = model.IfcProject;
            int projectId = 0;
            if (project != null) projectId = project.EntityLabel;
            XbimGeometryData regionData = model.GetGeometryData(projectId, XbimGeometryType.Region).FirstOrDefault(); //get the region data should only be one
            
            if (regionData != null)
            {
                XbimRegionCollection regions = XbimRegionCollection.FromArray(regionData.ShapeData);
                return regions.MostPopulated();
            }
            else
                return null;
        }

        private void RecalculateView(XbimModel model, ModelRefreshOptions options = ModelRefreshOptions.None)
        {
            if (!ModelBounds.IsEmpty) //we have  geometry so create view box
                viewBounds = ModelBounds;
          
            // Assumes a NearPlaneDistance of 1/8 of meter.
            //all models are now in metres
            UpdatefrustumPlanes(null, null);

            //get bounding box for the whole scene and adapt gridlines to the model units
            //
            double widthModelUnits = viewBounds.SizeY;
            double lengthModelUnits = viewBounds.SizeX;
            long gridWidth = Convert.ToInt64(widthModelUnits /  10);
            long gridLen = Convert.ToInt64(lengthModelUnits / 10);
            if (gridWidth > 10 || gridLen > 10)
                this.GridLines.MinorDistance = 10;
            else
                this.GridLines.MinorDistance = 1;
            this.GridLines.Width = (gridWidth + 1) * 10;
            this.GridLines.Length = (gridLen + 1) * 10;

            this.GridLines.MajorDistance =  10;
            this.GridLines.Thickness = 0.01;
            XbimPoint3D p3d = viewBounds.Centroid();
            TranslateTransform3D t3d = new TranslateTransform3D(p3d.X, p3d.Y, viewBounds.Z);
            this.GridLines.Transform = t3d;
           
            //make sure whole scene is visible
            if (!((options & ModelRefreshOptions.ViewPreserveCameraPosition) == ModelRefreshOptions.ViewPreserveCameraPosition))
            ViewHome();   
        }

        void ReferencedModels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems.Count > 0)
            {
                XbimReferencedModel refModel = e.NewItems[0] as XbimReferencedModel;
                if (scenes.Count == 0) //need to calculate extents
                {
                    XbimRegion largest = GetLargestRegion(refModel.Model);
                    XbimPoint3D c = new XbimPoint3D(0, 0, 0);
                    XbimRect3D bb = XbimRect3D.Empty;
                    if (largest != null)
                        bb = new XbimRect3D(largest.Centre, largest.Centre);
                    XbimPoint3D p = bb.Centroid();
                    _modelTranslation = new XbimVector3D(-p.X, -p.Y, -p.Z);
                }
                XbimScene<WpfMeshGeometry3D, WpfMaterial> scene = BuildRefModelScene(refModel.Model, refModel.DocumentInformation);
                scenes.Add(scene);
                RecalculateView(refModel.Model);
            }
        }
       

        public void ReportData(StringBuilder sb, IModel model, int entityLabel)
        {
            XbimModel m = model as XbimModel;
            if (m != null)
            {
            foreach (var scene in scenes)
            {
                    IXbimMeshGeometry3D mesh = scene.GetMeshGeometry3D(model.Instances[entityLabel], m.UserDefinedId);
                mesh.ReportGeometryTo(sb);
            }
            }
        }

       


        private XbimScene<WpfMeshGeometry3D, WpfMaterial> BuildRefModelScene(XbimModel model, IfcDocumentInformation docInfo)
        {
            XbimScene<WpfMeshGeometry3D, WpfMaterial> scene = new XbimScene<WpfMeshGeometry3D, WpfMaterial>(model);
            XbimGeometryHandleCollection handles = new XbimGeometryHandleCollection(model.GetGeometryHandles()
                                                       .Exclude(IfcEntityNameEnum.IFCFEATUREELEMENT)); // ifcSpaces added to the geometry
            double total = handles.Count;
            double processed = 0;

            XbimColour colour = federationColours[docInfo.DocumentOwner.RoleName()];
            double metre = model.ModelFactors.OneMetre;
            wcsTransform = XbimMatrix3D.CreateTranslation(_modelTranslation) * XbimMatrix3D.CreateScale(1 / (float)metre);
                
            XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial> layer = new XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial>(model, colour) { Name = "All" };
            //add all content initially into the hidden field
            foreach (var geomData in model.GetGeometryData(handles))
            {
                layer.AddToHidden(geomData.TransformBy(wcsTransform));
                processed++;
                int progress = Convert.ToInt32(100.0 * processed / total);
            }

            this.Dispatcher.BeginInvoke(new Action(() => { AddLayerToDrawingControl(layer); }), System.Windows.Threading.DispatcherPriority.Background);
            lock (scene)
            {
                scene.Add(layer);

                if (ModelBounds.IsEmpty) ModelBounds = layer.BoundingBoxHidden();
                else ModelBounds.Union(layer.BoundingBoxHidden());

            }

            this.Dispatcher.BeginInvoke(new Action(() => { Hide<IfcSpace>(); }), System.Windows.Threading.DispatcherPriority.Background);
            return scene;
        }
        
        /// <summary>
        /// Provides a mechanism to define colouring schemes for elements in DrawingControl3D.
        /// After setting a new LayerStyler issue a ReloadModel (<see cref="Xbim.Presentation.DrawingControl3D.ReloadModel()"/>). 
        /// </summary>
        public ILayerStyler LayerStyler = null;
        /// <summary>
        /// Provides a mechanism to define colouring schemes for elements in DrawingControl3D.
        /// After setting a new LayerStyler issue a ReloadModel (<see cref="Xbim.Presentation.DrawingControl3D.ReloadModel()"/>). 
        /// </summary>
        public ILayerStyler FederationLayerStyler = null;

        

        private XbimScene<WpfMeshGeometry3D, WpfMaterial> BuildScene(XbimModel model, IEnumerable<int> LoadLabels, ILayerStyler layerStyler)
        {

            // spaces are not excluded from the model to make the ShowSpaces property meaningful
            XbimScene<WpfMeshGeometry3D, WpfMaterial> scene = new XbimScene<WpfMeshGeometry3D, WpfMaterial>(model);
            scene.LayerColourMap.SetProductTypeColourMap();
            XbimGeometryHandleCollection handles; 
                    // = new XbimGeometryHandleCollection(model.GetGeometryHandles().Exclude(IfcEntityNameEnum.IFCFEATUREELEMENT));
                    // .Exclude(IfcEntityNameEnum.IFCFEATUREELEMENT | IfcEntityNameEnum.IFCSPACE));
            if (LoadLabels == null)
                handles = new XbimGeometryHandleCollection(model.GetGeometryHandles().Exclude(IfcEntityNameEnum.IFCFEATUREELEMENT));
            else 
                handles = new XbimGeometryHandleCollection(model.GetGeometryHandles().Where(t => LoadLabels.Contains(t.ProductLabel)));

            double total = handles.Count;
            double processed = 0;

            IfcProject project = model.IfcProject;
            int projectId = 0;
            if (project != null) projectId = project.EntityLabel;
            double metre = model.ModelFactors.OneMetre;
            wcsTransform = XbimMatrix3D.CreateTranslation(_modelTranslation) * XbimMatrix3D.CreateScale((float)(1 / metre));

            

            Dictionary<string, XbimGeometryHandleCollection> GroupedHandlers = layerStyler.GroupLayers(handles);

            Parallel.ForEach<string>(GroupedHandlers.Keys, LayerName =>
            {
                XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial> layer = layerStyler.GetLayer(LayerName, model, scene);
                IEnumerable<XbimGeometryData> geomColl = model.GetGeometryData(GroupedHandlers[LayerName]);
                bool IsLayerVisible = layerStyler.IsVisibleLayer(LayerName);

                // initially add all content into the hidden field (underlying geometry info)
                // it will later be moved to the visible WPF implementation by AddLayerToDrawingControl
                foreach (var geomData in geomColl)
                {
                    XbimGeometryData gd  = geomData.TransformBy(wcsTransform);

                    if (LayerStyler.UseIfcSubStyles)
                        layer.AddToHidden(gd, model);
                    else
                        layer.AddToHidden(gd, null);
                    processed++;
                    int progress = Convert.ToInt32(100.0 * processed / total);
                }

                this.Dispatcher.BeginInvoke(new Action(() => { AddLayerToDrawingControl(layer, IsLayerVisible); }), System.Windows.Threading.DispatcherPriority.Background);
                lock (scene)
                {
                    scene.Add(layer);

                    if (ModelBounds.IsEmpty) ModelBounds = layer.BoundingBoxHidden();
                    else ModelBounds.Union(layer.BoundingBoxHidden());
                }
            }
            );
            this.Dispatcher.BeginInvoke(new Action(() => { Hide<IfcSpace>(); }), System.Windows.Threading.DispatcherPriority.Background);

            return scene;
        }

        /// <summary>
        /// Function that actually populates the geometry from the layer into the viewer meshes.
        /// If the <paramref name="isLayerVisible"/> is set to false layer becomes hidden.
        /// </summary>
        private void AddLayerToDrawingControl(XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial> layer, bool isLayerVisible)
        {
            AddLayerToDrawingControl(layer);
            if (!isLayerVisible)
                layer.HideAll();
        }

        /// <summary>
        /// function that actually populates the geometry from the layer into the viewer meshes.
        /// </summary>
        private void AddLayerToDrawingControl(XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial> layer) // Formerly called DrawLayer
        {
            layer.Show();

            GeometryModel3D m3d = (WpfMeshGeometry3D)layer.Visible;
            m3d.SetValue(TagProperty, layer);
            // sort out materials and bind
            if (layer.Style.RenderBothFaces)
                m3d.BackMaterial = m3d.Material = (WpfMaterial)layer.Material;
            else if (layer.Style.SwitchFrontAndRearFaces)
                m3d.BackMaterial = (WpfMaterial)layer.Material;
            else
                m3d.Material = (WpfMaterial)layer.Material;
            if (ForceRenderBothSides) m3d.BackMaterial = m3d.Material;
            _materials.Add(m3d.Material);
            // SetOpacityPercent(m3d.Material, ModelOpacity);
            ModelVisual3D mv = new ModelVisual3D();
            mv.Content = m3d;
            if (layer.Style.IsTransparent)
                Transparents.Children.Add(mv);
            else
                Opaques.Children.Add(mv);

            foreach (var subLayer in layer.SubLayers)
                AddLayerToDrawingControl(subLayer);
        }

        /// <summary>
        /// Returns the list of nested visual elements.
        /// </summary>
        /// <param name="OfItem">Valid names are for instance: Opaques, Transparents, BuildingModel, cuttingGroup...</param>
        /// <returns>IEnumerable names</returns>
        public IEnumerable<string> ListItems(string OfItem)
        {
            foreach (var scene in scenes)
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
            foreach (var scene in scenes)
                foreach (var layer in scene.SubLayers) //go over top level layers 
                {
                    foreach (var item in layer.LayersTree(0))
                    {
                        yield return item;    
                    }
                }   
        }


        public void SetVisibility(string LayerName, bool visibility)
        {
            foreach (var scene in scenes)
            {
                foreach (var layer in scene.SubLayers) //go over top level layers only
                {
                    if (layer.Name == LayerName)
                    {
                        if (visibility == true)
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
            IfcType ifcType = IfcMetaData.IfcType(typeof(T));
            string toHide = ifcType.Name + ";";
            foreach (var subType in ifcType.NonAbstractSubTypes)
                toHide += subType.Name + ";";
            foreach (var scene in scenes)
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
            ret.Add(new LayerViewModel("All", this));
            foreach (var scene in scenes)
                foreach (var layer in scene.SubLayers) // go over top level layers only
                    ret.Add(new LayerViewModel(layer.Name, this));
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
            IfcType ifcType = IfcMetaData.IfcType(typeof(T));
            string toShow = ifcType.Name + ";";
            foreach (var subType in ifcType.NonAbstractSubTypes)
                toShow += subType.Name + ";";
            foreach (var scene in scenes)
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

        public void SetCamera(ProjectionCamera Cam)
        {
            Viewport.Camera = Cam;
        }

        public void ViewHome()
        {
            XbimPoint3D c = viewBounds.Centroid();
            Point3D p = new Point3D(c.X, c.Y, c.Z);
            if (Viewport.CameraController != null) Viewport.CameraController.ResetCamera();
            Rect3D r3d = new Rect3D(viewBounds.X, viewBounds.Y, viewBounds.Z, viewBounds.SizeX, viewBounds.SizeY, viewBounds.SizeZ);
            Viewport.ZoomExtents(r3d);
        }

        public void ZoomSelected()
        {
            if (SelectedEntity != null && Highlighted != null && Highlighted.Mesh != null)
            {
                Rect3D r3d = Highlighted.Mesh.GetBounds();
                ZoomTo(r3d);
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
                Rect3D r3d = Highlighted.Mesh.GetBounds();
                if (!r3d.IsEmpty)
                SetCutPlane(
                    r3d.X, r3d.Y, r3d.Z + delta, 
                    0, 0, -1
                    );
            }
        }

        /// <summary>
        /// Zooms to a selected portion of the space.
        /// </summary>
        /// <param name="r3d">The box to be zoomed to</param>
        /// <param name="DoubleRectSize">Effectively doubles the size of the bounding box so to fit more space around it.</param>
        private void ZoomTo(Rect3D r3d, bool DoubleRectSize = true)
        {
            if (!r3d.IsEmpty)
            {
                Rect3D bounds = new Rect3D(
                    viewBounds.X, viewBounds.Y, viewBounds.Z, 
                    viewBounds.SizeX, viewBounds.SizeY, viewBounds.SizeZ
                    );
                if (DoubleRectSize)
                {
                    r3d.Offset(-r3d.SizeX / 2, -r3d.SizeY / 2, -r3d.SizeZ / 2);
                    r3d.SizeX *= 2;
                    r3d.SizeY *= 2;
                    r3d.SizeZ *= 2;
                }
                if (!r3d.IsEmpty)
                {
                    if (r3d.Contains(bounds)) // if bigger than bounds zoom bounds
                        Viewport.ZoomExtents(bounds, 200);
                    else
                        Viewport.ZoomExtents(r3d, 200);
                }
            }
        }

        public void ZoomTo(XbimRect3D r3d)
        {
            ZoomTo(new Rect3D(
                        new Point3D(r3d.X, r3d.Y, r3d.Z),
                        new Size3D(r3d.SizeX, r3d.SizeY, r3d.SizeZ)
                        ),
                   false);
        }



        public void ShowOctree(XbimOctree<IfcProduct> octree, int specificLevel = -1, bool onlyWithContent = false)
        {
            if (Viewport.Children.Contains(OctreeVisualization))
                Viewport.Children.Remove(OctreeVisualization);
            OctreeVisualization.Children.Clear();
            ShowOctree<IfcProduct>(octree, specificLevel, onlyWithContent);
            Viewport.Children.Add(OctreeVisualization);
            
        }

        ModelVisual3D OctreeVisualization = new ModelVisual3D();


        private void ShowOctree<T>(XbimOctree<T> octree, int specificLevel = -1, bool onlyWithContent = false)
        {
            //prepare action
            Action show = () => {
                WpfXbimRectangle3D rect = new WpfXbimRectangle3D(octree.Bounds);

                //create transformation
                var scale = 1f / Model.ModelFactors.OneMetre;
                var transformation = Transform3DHelper.CombineTransform(
                    new TranslateTransform3D(_modelTranslation.X, _modelTranslation.Y, _modelTranslation.Z),
                    new ScaleTransform3D(scale, scale, scale)
                    );

                //Add octree geometry
                OctreeVisualization.Children.Add(new ModelVisual3D() { Content = rect.Geometry, Transform = transformation });
            };

            if (specificLevel == -1 || specificLevel == octree.Depth)
            {
                if (onlyWithContent && octree.Content().FirstOrDefault() != null)
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
        static public void CreateThumbnail(XbimModel model,string bmpFileName, int width, int height, string title=null)
        {
            DrawingControl3D d3d = new DrawingControl3D();
            // Create the container
            var container = new Border
            {
                Child = d3d,
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
            using (var temporaryPresentationSource = new HwndSource(new HwndSourceParameters()) { RootVisual = (VisualTreeHelper.GetParent(container) == null ? container : null) })
            {
                
                d3d.Dispatcher.Invoke(DispatcherPriority.SystemIdle, new Action(() => { }));
                d3d.LoadGeometry(model);
                d3d.Viewport.ShowViewCube = false;
                d3d.Viewport.ShowFieldOfView = false;
                d3d.Viewport.ShowCoordinateSystem = false;
                if (!string.IsNullOrEmpty(title))
                {
                    d3d.Viewport.Title = title;
                   
                }
                d3d.ShowGridLines = false;
                d3d.Viewport.ZoomExtents();
                // Render to bitmap
                var rtb = new RenderTargetBitmap((int)container.ActualWidth, (int)container.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(container);

                PngBitmapEncoder aEncoder = new PngBitmapEncoder();
                aEncoder.Frames.Add(BitmapFrame.Create(rtb));

                using (Stream stm = File.Create(bmpFileName))
                {
                    aEncoder.Save(stm);
                }
            }
        }
    }
}
