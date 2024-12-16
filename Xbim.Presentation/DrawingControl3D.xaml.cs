//#define DOPARALLEL
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
using Xbim.Common;
using Xbim.Common.Configuration;
using Xbim.Common.Federation;
using Xbim.Common.Geometry;
using Xbim.Common.Metadata;
using Xbim.Geometry.Engine.Interop;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.ModelGeometry.Scene;
using Xbim.Presentation.Extensions;
using Xbim.Presentation.LayerStyling;
using Xbim.Presentation.ModelGeomInfo;
using Xbim.Presentation.Overlay;

namespace Xbim.Presentation
{
	// See /Themes/Generic.xaml for the definition of the style of DrawingControl3D

	/// <summary>
	///   Interaction logic for DrawingControl3D.xaml
	/// </summary>
	[TemplatePart(Name = TemplateCanvas, Type = typeof(HelixViewport3D))]
	[TemplatePart(Name = TemplateTransHighlighted, Type = typeof(MeshVisual3D))]
	[TemplatePart(Name = TemplateCuttingGroup, Type = typeof(CuttingPlaneGroup))]
	[TemplatePart(Name = TemplateOpaques, Type = typeof(ModelVisual3D))]
	[TemplatePart(Name = TemplateHighlighted, Type = typeof(ObservableMeshVisual3D))]
	[TemplatePart(Name = TemplateCuttingGroupT, Type = typeof(CuttingPlaneGroup))]
	[TemplatePart(Name = TemplateTransparents, Type = typeof(SortingVisual3D))]
	[TemplatePart(Name = TemplateExtras, Type = typeof(ModelVisual3D))]
	[TemplatePart(Name = TemplateOverlays, Type = typeof(ModelVisual3D))]
	[TemplatePart(Name = TemplateGridLines, Type = typeof(GridLinesVisual3D))]
	public class DrawingControl3D : UserControl
	{
		#region Template defined variables

		private const string TemplateCanvas = "Canvas";
		private const string TemplateTransHighlighted = "TransHighlighted";
		private const string TemplateCuttingGroup = "CuttingGroup";
		private const string TemplateOpaques = "Opaques";
		private const string TemplateHighlighted = "Highlighted";
		private const string TemplateCuttingGroupT = "CuttingGroupT";
		private const string TemplateTransparents = "Transparents";
		private const string TemplateExtras = "Extras";
		private const string TemplateOverlays = "Overlays";
		private const string TemplateGridLines = "GridLines";

		protected HelixViewport3D Canvas;
		protected MeshVisual3D TransHighlighted;
		protected CuttingPlaneGroup CuttingGroup;
		protected ModelVisual3D Opaques;
		protected ObservableMeshVisual3D Highlighted;
		protected CuttingPlaneGroup CuttingGroupT;
		protected SortingVisual3D Transparents;
				protected ModelVisual3D Extras;
		protected ModelVisual3D Overlays;
		protected GridLinesVisual3D GridLines;

		public ModelVisual3D OpaquesVisual3D => Opaques;

		public SortingVisual3D TransparentsVisual3D => Transparents;

		public ObservableMeshVisual3D HighlightedVisual => Highlighted;

		#endregion

		protected HashSet<Material> Materials { get; } = new HashSet<Material>();

		// protected Dictionary<Material, double> Opacities { get; } = new Dictionary<Material, double>();

		protected CombinedManipulator ClipHandler;

		static DrawingControl3D()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(DrawingControl3D),
				new FrameworkPropertyMetadata(typeof(DrawingControl3D)));
		}

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			Canvas = (HelixViewport3D)GetTemplateChild(TemplateCanvas);
			TransHighlighted = (MeshVisual3D)GetTemplateChild(TemplateTransHighlighted);
			CuttingGroup = (CuttingPlaneGroup)GetTemplateChild(TemplateCuttingGroup);
			Opaques = (ModelVisual3D)GetTemplateChild(TemplateOpaques);
			Highlighted = (ObservableMeshVisual3D)GetTemplateChild(TemplateHighlighted);
			CuttingGroupT = (CuttingPlaneGroup)GetTemplateChild(TemplateCuttingGroupT);
			Transparents = (SortingVisual3D)GetTemplateChild(TemplateTransparents);
			Extras = (ModelVisual3D)GetTemplateChild(TemplateExtras);
			Overlays = (ModelVisual3D)GetTemplateChild(TemplateOverlays);
			GridLines = (GridLinesVisual3D)GetTemplateChild(TemplateGridLines);

			if (Highlighted != null)
				Highlighted.PropertyChanged += Highlighted_PropertyChanged;
			Viewport = Canvas;
			if (Canvas != null)
			{
				Canvas.MouseDown += Canvas_MouseDown;
				Canvas.MouseMove += Canvas_MouseMove;
				Canvas.MouseWheel += Canvas_MouseWheel;
				// Canvas.ShowFrameRate = true;
				var p = RenderOptions.GetEdgeMode((DependencyObject)Canvas);
			}
			Loaded += DrawingControl3D_Loaded;

			if (Viewport != null)
				Viewport.CameraChanged += UpdatefrustumPlanes;
			ClearGraphics();
			MouseModifierKeyBehaviour.Add(ModifierKeys.Control, XbimMouseClickActions.Toggle);
			MouseModifierKeyBehaviour.Add(ModifierKeys.Alt, XbimMouseClickActions.Measure);
			MouseModifierKeyBehaviour.Add(ModifierKeys.Shift, XbimMouseClickActions.SetClip);
		}

		/// <summary>
		/// this method keeps meshes for TransHighlighted and Highlighted items in sync.
		/// </summary>
		private void Highlighted_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			var pInfo = typeof(MeshVisual3D).GetProperty(e.PropertyName);
			var sourceValue = pInfo.GetValue(Highlighted, null);
			pInfo.SetValue(TransHighlighted, sourceValue, null);
		}

		public bool ShowFps
		{
			get { return Canvas.ShowFrameRate; }
			set { Canvas.ShowFrameRate = value; }
		}

		public bool HighSpeed
		{
			get
			{
				return Canvas.ClipToBounds == false;
			}
			set
			{
				if (value == true)
				{
					Canvas.ClipToBounds = false;
					RenderOptions.SetEdgeMode((DependencyObject)Canvas, EdgeMode.Aliased);
				}
				else
				{
					Canvas.ClipToBounds = true;
					RenderOptions.SetEdgeMode((DependencyObject)Canvas, EdgeMode.Unspecified);
				}
			}
		}

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

		protected void ClipPlaneHandlesPlace(Point3D pos)
		{
			var m = Matrix3D.Identity;
			m.Translate(new Vector3D(
				pos.X, pos.Y, pos.Z)
				);
			Extras.Transform = new MatrixTransform3D(m);
		}

		protected void ClipPlaneHandlesShow()
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

				var p = new Point3D(newMatrix.OffsetX, newMatrix.OffsetY, newMatrix.OffsetZ);
				var n = newMatrix.Transform(new Vector3D(0, 0, -1));
				ClearCutPlane();
				SetCutPlane(p.X, p.Y, p.Z, n.X, n.Y, n.Z);
			}
			base.OnPreviewKeyUp(e);
		}

		protected void ClipPlaneHandlesHide()
		{
			Extras.Children.Clear();
			ClipHandler = null;
		}

		public static List<Type> DefaultExcludedTypes = new List<Type>()
		{
			typeof(Ifc2x3.ProductExtension.IfcSpace),
			typeof(Ifc4.ProductExtension.IfcSpace),
            typeof(Ifc4x3.ProductExtension.IfcSpace),

            typeof(Ifc2x3.ProductExtension.IfcFeatureElement),
			typeof(Ifc4.ProductExtension.IfcFeatureElement),
            typeof(Ifc4x3.ProductExtension.IfcFeatureElement)
        };

		/// <summary>
		/// The list of types that the engine will not consider in the generation of the scene, the exclusion code needs to be correctly implemented in the 
		/// configued ILayerStyler for the exclusion to work.
		/// </summary>
		public List<Type> ExcludedTypes = new List<Type>(DefaultExcludedTypes);

		public List<IPersistEntity> IsolateInstances = null;

		public List<IPersistEntity> HiddenInstances = null;

        public List<IIfcGeometricRepresentationContext> SelectedContexts = null;

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
			UserModeledDimensionChangedEvent?.Invoke(this, UserModeledDimension);
		}

		private void OnCompositionTargetRendering(object sender, EventArgs e)
		{
			var doShow = !UserModeledDimension.IsEmpty;
			// lines
			var depthoff = 0.001;
			if (doShow && _userModeledDimLines == null)
			{
				_userModeledDimLines = new LinesVisual3D
				{
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
				_userModeledDimPoints = new PointsVisual3D
				{
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

		private void UpdatefrustumPlanes(object sender, RoutedEventArgs e)
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

			if (Math.Abs(snd.Camera.NearPlaneDistance - nearPlaneDistance) > 0)
			{
				snd.Camera.NearPlaneDistance = nearPlaneDistance; // Debug.WriteLine("Near: " + NearPlane);
			}
			if (Math.Abs(snd.Camera.FarPlaneDistance - farPlaneDistance) > 0)
			{
				snd.Camera.FarPlaneDistance = farPlaneDistance; // Debug.WriteLine("Far: " + FarPlane);
			}
		}

		protected virtual void DrawingControl3D_Loaded(object sender, RoutedEventArgs e)
		{

		}

		#region Fields

		public List<XbimScene<WpfMeshGeometry3D, WpfMaterial>> Scenes =
			new List<XbimScene<WpfMeshGeometry3D, WpfMaterial>>();

		/// <summary>
		/// Represent the extents of what is considered model. This depends on the selected region.
		/// _viewBounds is transformed depending on ModelBounds and _modelTranslation. 
		/// </summary>
		private XbimRect3D _viewBounds
		{
			get
			{
				return ModelPositions.ViewSpaceBounds;
			}
		}



		/// <summary>
		/// Gets or sets the model.
		/// </summary>
		/// <value>The model.</value>
		public Model3D Model3D { get; set; }

		public Plane3D GetCutPlane()
		{
			if (CuttingGroup == null || CuttingGroup.IsEnabled == false)
				return null;
			return CuttingGroup.CuttingPlanes.Count == 1
				? CuttingGroup.CuttingPlanes[0]
				: null;
		}

		public void SetCutPlane(double posX, double posY, double posZ, double nrmX, double nrmY, double nrmZ)
		{
			SetNamedCutPlane(posX, posY, posZ, nrmX, nrmY, nrmZ, CuttingGroup);
			SetNamedCutPlane(posX, posY, posZ, nrmX, nrmY, nrmZ, CuttingGroupT);
		}

		private static void SetNamedCutPlane(double posX, double posY, double posZ, double nrmX, double nrmY,
			double nrmZ, CuttingPlaneGroup cpg)
		{
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
			ClearNamedCutPlane(CuttingGroup);
			ClearNamedCutPlane(CuttingGroupT);
		}

		private static void ClearNamedCutPlane(CuttingPlaneGroup cpg)
		{
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

		public Dictionary<ModifierKeys, XbimMouseClickActions> MouseModifierKeyBehaviour =
			new Dictionary<ModifierKeys, XbimMouseClickActions>();

		protected void MouseDrivenSelectedEntityChange(IPersistEntity entity = null)
		{
			_selectedEntityChangeTriggedBySelectionChange = true;
			if (SelectedEntity == null && entity == null)
				HighlighSelected(null);

			SelectedEntity = entity;

			_selectedEntityChangeTriggedBySelectionChange = false;
		}

		public event UserModeledDimensionChanged UserModeledDimensionChangedEvent;

		public delegate void UserModeledDimensionChanged(DrawingControl3D m, PolylineGeomInfo e);

		protected virtual void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
		{
			var pos = e.GetPosition(Canvas);
			var hit = FindHit(pos);

			var hitObject = hit?.ModelHit?.GetValue(TagProperty);
			if (hitObject == null)
			{
				Selection.Clear();
				SelectedEntity = null;
				if (UserModeledDimension.IsEmpty)
					return;
				// drop the geometry that holds the visualization of the measure
				UserModeledDimension.Clear();
				FirePrevPointsChanged();
				return;
			}

			IPersistEntity thisSelectedEntity = null;
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
				IModel modelHit = null; // default to not hit
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
				if (mc != XbimMouseClickActions.Measure && !UserModeledDimension.IsEmpty)
				{
					// drop the geometry that holds the visualization of the measure
					// FurtherGeometries.Content = null;
					UserModeledDimension.Clear();
					FirePrevPointsChanged();
				}

				if (thisSelectedEntity == null)
				{
					// regardless of selection mode an empty selection clears the current selection
					Selection.Clear();
					HighlighSelected(null);
				}
				else
				{
					switch (mc)
					{
						case XbimMouseClickActions.Add:
							Selection.Add(thisSelectedEntity);
							MouseDrivenSelectedEntityChange(thisSelectedEntity);
							break;
						case XbimMouseClickActions.Remove:
							Selection.Remove(thisSelectedEntity);
							MouseDrivenSelectedEntityChange();
							break;
						case XbimMouseClickActions.Toggle:
							var bAdded = Selection.Toggle(thisSelectedEntity);
							MouseDrivenSelectedEntityChange(bAdded ? thisSelectedEntity : null);
							break;
						case XbimMouseClickActions.Single:
							Selection.Clear();
							Selection.Add(thisSelectedEntity);
							MouseDrivenSelectedEntityChange(thisSelectedEntity);
							break;
						case XbimMouseClickActions.Measure:
							var p = GetClosestPoint(hit);
							if (UserModeledDimension.Last3DPoint.HasValue &&
								UserModeledDimension.Last3DPoint.Value == p.Point)
								UserModeledDimension.RemoveLast();
							else
							{
								var t = ModelPositions.GetPointInverse(new XbimPoint3D(
									p.Point.X,
									p.Point.Y,
									p.Point.Z
									));
								Debug.WriteLine(t);
								UserModeledDimension.Add(p);
							}
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
				// this triggers the highlighting in case of single selection mode.
				SelectedEntity = thisSelectedEntity;
			}
		}

		protected virtual void Canvas_MouseMove(object sender, MouseEventArgs e)
		{
		}

		protected virtual void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
		{
		}

		private PointGeomInfo GetClosestPoint(RayMeshGeometry3DHitTestResult hit)
		{
			var pts = new[]
			{
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
				if (!(dist < minDist))
					continue;
				minDist = dist;
				iClosest = iPtMesh;
			}

			var pRet = new PointGeomInfo
			{
				Entity = pHit.Entity,
				ModelReferencePoint = -1 * ModelPositions[pHit.Entity?.Model ?? Model.Model].Transform.Translation,
				Point = hit.MeshHit.Positions[iClosest]
			};

			return pRet;
		}

		private IPersistEntity GetClickedEntity(RayMeshGeometry3DHitTestResult hit)
		{
			var layer = hit?.ModelHit.GetValue(TagProperty) as XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial>;
			if (layer == null)
				return null;

			var frag = layer.Visible.Meshes.Find(hit.VertexIndex1);
			var modelId = frag.ModelId;
			IModel modelHit = null; //default to not hit
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
			DependencyProperty.Register("ModelOpacity", typeof(double), typeof(DrawingControl3D),
				new UIPropertyMetadata(1.0, OnModelOpacityChanged));

		private static void OnModelOpacityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var d3D = d as DrawingControl3D;
			if (d3D != null && e.NewValue != null)
			{
				d3D.SetOpacity((double)e.NewValue);
			}
		}

		/// <summary>
		/// Iterates all the materials to set the required opacity.
		/// </summary>
		public void SetOpacity(double opacityPercent)
		{
			var opacity = Math.Min(1, opacityPercent);
			opacity = Math.Max(0, opacity); //bound opacity factor
			foreach (var item in OpaquesVisual3D.Children.OfType<ModelVisual3D>())
			{
				SetOp(item, opacityPercent);
			}
			foreach (var item in TransparentsVisual3D.Children.OfType<ModelVisual3D>())
			{
				SetOp(item, opacityPercent);
			}

			foreach (var material in Materials)
			{
				SetOpacityPercent(material, opacity);
			}
		}

		private void SetOp(ModelVisual3D mv3d, double opacity)
		{
			if (mv3d.Content is Model3DGroup)
				SetOp((Model3DGroup)mv3d.Content, opacity);

			if (mv3d.Content != null)
			{
				var as3D = mv3d.Content as GeometryModel3D;
				SetOp(as3D, opacity);
			}
			foreach (var visualElementChild in mv3d.Children.OfType<ModelVisual3D>())
			{
				SetOp(visualElementChild, opacity);
			}
		}

		private void SetOp(Model3DGroup content, double opacity)
		{
			foreach (var child in content.Children.OfType<GeometryModel3D>())
			{
				SetOp(child, opacity);
			}
		}

		protected Dictionary<GeometryModel3D, double> OriginalOpacities { get; } = new Dictionary<GeometryModel3D, double>();

		private void SetOp(GeometryModel3D as3D, double opacity)
		{
			if (as3D == null)
				return;

			double origOp = 1;
			if (!OriginalOpacities.TryGetValue(as3D, out origOp))
			{
				// needs storing original opacity
				origOp = getOpacity(as3D.Material);
				OriginalOpacities.Add(as3D, origOp);
			}

			var ret = SetOpacityPercent(as3D.Material, opacity * origOp);
			if (ret != null)
			{
				as3D.Material = ret;
				as3D.BackMaterial = ret;
			}
		}

		private double getOpacity(Material material)
		{
			if (material is DiffuseMaterial)
			{
				var dm = material as DiffuseMaterial;
				return dm.Brush.Opacity;
			}
			if (material is SpecularMaterial)
			{
				var sm = material as DiffuseMaterial;
				return sm.Brush.Opacity;
			}
			return 1;
		}

		private Material SetOpacityPercent(Material material, double opacity)
		{
			var g = material as MaterialGroup;
			if (g != null)
			{
				foreach (var item in g.Children)
				{
					SetOpacityPercent(item, opacity);
				}
				return null;
			}

			var dm = material as DiffuseMaterial;
			if (dm != null)
			{
				if (dm.IsFrozen)
				{
					dm = dm.Clone();
				}
				if (!dm.Brush.IsFrozen)
					dm.Brush.Opacity = opacity;
				else
				{
					var b = dm.Brush.Clone();
					b.Opacity = opacity;
					dm.Brush = b;
				}
				return dm;
			}
			var sm = material as SpecularMaterial;
			if (sm != null)
			{
				sm.Brush.Opacity = opacity;
			}
			return null;
		}

		public void AddImageOverlay(ImageOverlay imageOverlay)
		{
			imageOverlay.UpdatePosition(ModelPositions);
			Overlays.Children.Add(imageOverlay.GraphicsItem);
		}
		public bool RemoveImageOverlay(ImageOverlay imageOverlay)
		{
			return Overlays.Children.Remove(imageOverlay.GraphicsItem);
		}

		

		public void AddTextOverlay(TextOverlay text)
		{
			var container = text.Style.GraphicsItem;
			if (!Overlays.Children.Contains(container))
			{
				Overlays.Children.Add(container);
			}
			var items = container.Items?.ToList();
			if (items == null)
			{
				items = new List<BillboardTextItem>();
			}
			text.UpdatePosition(ModelPositions);
			items.Add(text.GraphicsItem);
			container.Items = items; // if items is set early it does not work.. 
		}

		public bool RemoveTextOverlay(TextOverlay toBeRemoved)
		{
			var conts = Overlays.Children.OfType<BillboardTextGroupVisual3D>();
			foreach (var cont in conts)
			{
				var found = cont.Items.FirstOrDefault(x => x == toBeRemoved.GraphicsItem);
				if (found == null)
					continue;
				var copy = cont.Items.ToList();
				copy.Remove(toBeRemoved.GraphicsItem);
				cont.Items = copy;
				return true;
			}
			return false;
		}

		

		public IfcStore Model
		{
			get { return (IfcStore)GetValue(ModelProperty); }
			set { SetValue(ModelProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Model.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ModelProperty =
			DependencyProperty.Register("Model", typeof(IfcStore), typeof(DrawingControl3D),
				new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits,
					OnModelChanged));

		private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var d3D = d as DrawingControl3D;
			d3D?.ReloadModel();
		}

		public void ReloadModel(ModelRefreshOptions options = ModelRefreshOptions.None)
		{
			LoadGeometry((IfcStore)GetValue(ModelProperty), options: options);
			SetValue(LayerSetProperty, LayerSetRefresh());
		}

		public bool ForceRenderBothSides
		{
			get { return (bool)GetValue(ForceRenderBothSidesProperty); }
			set { SetValue(ForceRenderBothSidesProperty, value); }
		}

		// Using a DependencyProperty as the backing store for ForceRenderBothSides.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ForceRenderBothSidesProperty =
			DependencyProperty.Register("ForceRenderBothSides", typeof(bool), typeof(DrawingControl3D),
				new PropertyMetadata(true));

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

		public SelectionBehaviours SelectionBehaviour = SelectionBehaviours.MultipleSelection;

		public EntitySelection Selection
		{
			get { return (EntitySelection)GetValue(SelectionProperty); }
			set { SetValue(SelectionProperty, value); }
		}


		public static readonly DependencyProperty SelectionProperty = DependencyProperty.Register("Selection",
			typeof(EntitySelection), typeof(DrawingControl3D), new PropertyMetadata(OnSelectionChanged));

		private static void OnSelectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var d3D = d as DrawingControl3D;
			if (d3D == null)
				return;


			var newVal = e.NewValue as EntitySelection;


			d3D.ReplaceSelection(newVal);
		}

		private static void FireSelectionChanged(DrawingControl3D d3D, EntitySelection newVal, EntitySelection oldVal)
		{
			if (oldVal == null)
				oldVal = new EntitySelection();

			var a = new SelectionChangedEventArgs(
				SelectedEntityChangedEvent,
				oldVal.Except(newVal).ToList(),
				newVal.Except(oldVal).ToList());
			d3D.RaiseEvent(a);

		}

		private void ReplaceSelection(EntitySelection newVal)
		{
			if (newVal.Count() < 2)
			{
				MouseDrivenSelectedEntityChange(newVal.FirstOrDefault());
			}
			else
			{
				HighlighSelected(null);
				foreach (var item in newVal)
				{
					HighlighSelected(item);
				}
			}
		}

		public static readonly RoutedEvent SelectedEntityChangedEvent =
			EventManager.RegisterRoutedEvent("SelectedEntityChangedEvent", RoutingStrategy.Bubble,
				typeof(SelectionChangedEventHandler), typeof(DrawingControl3D));

		public event SelectionChangedEventHandler SelectedEntityChanged
		{
			add { AddHandler(SelectedEntityChangedEvent, value); }
			remove { RemoveHandler(SelectedEntityChangedEvent, value); }
		}

		// this events are is not involved when SelectedEntityProperty is changed.
		public IPersistEntity SelectedEntity
		{
			get { return (IPersistEntity)GetValue(SelectedEntityProperty); }
			set
			{
				SelectionChangedEventArgs a = new SelectionChangedEventArgs(
					SelectedEntityChangedEvent,
					new[] { SelectedEntityProperty },
					new[] { value }
					);
				RaiseEvent(a);
				SetValue(SelectedEntityProperty, value);
			}
		}

		// Using a DependencyProperty as the backing store for SelectedEntity.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty SelectedEntityProperty =
			DependencyProperty.Register("SelectedEntity", typeof(IPersistEntity), typeof(DrawingControl3D),
				new PropertyMetadata(OnSelectedEntityChanged));

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
			var newVal = e.NewValue as IPersistEntity;
			if (!d3D._selectedEntityChangeTriggedBySelectionChange)
			{
				d3D.Selection.Clear();
				if (newVal != null)
					d3D.Selection.Add(newVal);
			}
			d3D.HighlighSelected(newVal);
		}

		[Flags]
		public enum ComponentSelectionMode
		{
			None = 0,
			IsDecomposedBy = 1,
			All = 1
		}

		private Color selectionColor = Colors.Blue;
		public Color SelectionColor
		{
			get => selectionColor;
			set
			{
				selectionColor = value;
				HighlighSelected(null);
			}
		}

		/// <summary>
		/// Executed when a new entity is selected
		/// </summary>
		/// <param name="newVal">If null represents the current selection if the multiple selection is enabled.</param>
		protected virtual void HighlighSelected(IPersistEntity newVal)
		{
			// 0. prepare
			var mat = new WpfMaterial(
				new XbimColour(
					"Selection", SelectionColor.ScR, SelectionColor.ScG, SelectionColor.ScB, SelectionColor.ScA)
				);

			// 1. get the geometry first -> this gets the entire selection (not just newVal) if no special cases are triggered
			//
			var m = GetSelectionGeometry(newVal, mat);
			// 2. then determine how to highlight it
			//
			if (SelectionHighlightMode == SelectionHighlightModes.WholeMesh)
			{
				// Highlighted is defined in the XAML of drawingcontrol3d
				//
				Highlighted.Content = m;
			}
			else if (SelectionHighlightMode == SelectionHighlightModes.Normals)
			{
				// prepares the normals to faces (or points)
				//
				var axesMeshBuilder = new MeshBuilder();
				var pos = m.Positions.ToArray();
				var nor = m.Normals.ToArray();

				for (var i = 0; i < m.TriangleIndices.Count; i += 3)
				{
					var p1 = m.TriangleIndices[i];
					var p2 = m.TriangleIndices[i + 1];
					var p3 = m.TriangleIndices[i + 2];

					if (nor[p1] == nor[p2] && nor[p1] == nor[p3]) // same normals
					{
						var cnt = FindCentroid(new[] { pos[p1], pos[p2], pos[p3] });
						CreateNormal(cnt, nor[p1], axesMeshBuilder);
					}
					else
					{
						CreateNormal(pos[p1], nor[p1], axesMeshBuilder);
						CreateNormal(pos[p2], nor[p2], axesMeshBuilder);
						CreateNormal(pos[p3], nor[p3], axesMeshBuilder);
					}
				}
				Highlighted.Content = new GeometryModel3D(axesMeshBuilder.ToMesh(), HelixToolkit.Wpf.Materials.Yellow);
			}
			else if (SelectionHighlightMode == SelectionHighlightModes.WireFrame)
			{
				var axesMeshBuilder = new MeshBuilder();
				var pos = m.Positions.ToArray();
				if (newVal != null)
				{
					var box = XbimRect3D.Empty;
					for (var i = 0; i < m.TriangleIndices.Count; i += 3)
					{
						var p1 = m.TriangleIndices[i];
						var p2 = m.TriangleIndices[i + 1];
						var p3 = m.TriangleIndices[i + 2];

						// box evaluation
						box.Union(pos[p1]);
						box.Union(pos[p2]);
						box.Union(pos[p3]);
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
							new Point3D(pos[p1].X, pos[p1].Y, pos[p1].Z),
							new Point3D(pos[p2].X, pos[p2].Y, pos[p2].Z),
							new Point3D(pos[p3].X, pos[p3].Y, pos[p3].Z)
						};
						axesMeshBuilder.AddTube(path, lineThickness, 9, true);
					}
					HideAll();
				}
				else
				{
					ShowAll();
				}
				Highlighted.Content = new GeometryModel3D(axesMeshBuilder.ToMesh(), HelixToolkit.Wpf.Materials.Yellow);
			}
		}

		IIfcProduct _lastSelectedProduct = null;

		public bool WcsAdjusted { get; set; } = true;

		private WpfMeshGeometry3D GetSelectionGeometry(IPersistEntity newVal, WpfMaterial mat)
		{
			if (newVal is IIfcProduct)
			{
				_lastSelectedProduct = newVal as IIfcProduct;
			}
			if(newVal == null)
			{
				return new WpfMeshGeometry3D();
			}
			var engine = new XbimGeometryEngine(Model, XbimServices.Current.GetLoggerFactory());
			WpfMeshGeometry3D m;
			if (newVal is IIfcRepresentationItem)
			{
				if (_lastSelectedProduct == null)
					m = new WpfMeshGeometry3D();
				else
				{
					var productContexts = new List<IIfcProduct>() { _lastSelectedProduct };
					var representationLabels = new List<int>() { newVal.EntityLabel };
					var selModel = _lastSelectedProduct.Model;
					var modelTransform = ModelPositions[selModel].Transform;

					
					m = WpfMeshGeometry3D.GetRepresentationGeometry(engine, mat, productContexts, representationLabels, selModel, modelTransform, WcsAdjusted);
					if (m.PositionCount == 0)
					{
						var gri = newVal as IIfcGeometricRepresentationItem;
						if (gri != null)
						{
							var solid = engine.Create(gri, null);
							if (solid != null)
							{
								var shape = engine.CreateShapeGeometry(solid, selModel.ModelFactors.Precision, Model.ModelFactors.OneMetre / 20, selModel.ModelFactors.DeflectionAngle, XbimGeometryType.PolyhedronBinary, null);
								m = WpfMeshGeometry3D.GetRepresentationGeometry2(engine, mat, representationLabels, selModel, modelTransform, WcsAdjusted, shape, _lastSelectedProduct);
							}
						}
					}
				}
			}
			else if (newVal is IIfcShapeRepresentation)
			{
				m = WpfMeshGeometry3D.GetGeometry(engine, (IIfcShapeRepresentation)newVal, ModelPositions, mat, WcsAdjusted);
			}
			else if (newVal is IIfcRelVoidsElement)
			{
				var vd = newVal as IIfcRelVoidsElement;
				var rep = vd.RelatedOpeningElement.Representation.Representations.OfType<IIfcShapeRepresentation>()
					.FirstOrDefault();
				if (rep != null)
					m = WpfMeshGeometry3D.GetGeometry(engine, (IIfcShapeRepresentation)rep, ModelPositions, mat, WcsAdjusted);
				else
				{
					m = new WpfMeshGeometry3D();
				}
			}
			else
			{
				if (SelectionBehaviour == SelectionBehaviours.MultipleSelection)
				{
					m = WpfMeshGeometry3D.GetGeometry(Selection, ModelPositions, mat);
				}
				else // single element selection, requires the newval to get the model
				{
					m = WpfMeshGeometry3D.GetGeometry(newVal, ModelPositions[newVal.Model].Transform, mat);
				}
				
			}
			return m;
		}

		public ComponentSelectionMode ComponentSelectionDisplay = ComponentSelectionMode.All;

		// todo: remove after having restored the ComponentSelectionDisplay function

		/*
				private void AddElement(MeshGeometry3D m, IPersistEntity item)
				{
					var mod = ModelPositions[item.Model];
					if (mod != null)
						m.AddElements(item, mod.Transform);
					if (ComponentSelectionDisplay == ComponentSelectionMode.None)
						return;
					if ((ComponentSelectionDisplay & ComponentSelectionMode.IsDecomposedBy) ==
						ComponentSelectionMode.IsDecomposedBy && item is IIfcObjectDefinition)
					{
						foreach (var relation in ((IIfcObjectDefinition)item).IsDecomposedBy)
						{
							foreach (var relObject in relation.RelatedObjects)
							{
								AddElement(m, relObject);
							}
						}
					}
				}
		*/

		#endregion

		public bool ShowGridLines
		{
			get { return (bool)GetValue(ShowGridLinesProperty); }
			set { SetValue(ShowGridLinesProperty, value); }
		}

		// Using a DependencyProperty as the backing store for ShowWalls.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ShowGridLinesProperty =
			DependencyProperty.Register("ShowGridLines", typeof(bool), typeof(DrawingControl3D),
				new UIPropertyMetadata(true, OnShowGridLinesChanged));

		private static void OnShowGridLinesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var d3D = d as DrawingControl3D;
			if (d3D == null)
				return;
			if (!(e.NewValue is bool))
				return;
			if ((bool)e.NewValue)
				d3D.Viewport.Children.Insert(0, d3D.GridLines);
			else
				d3D.Viewport.Children.Remove(d3D.GridLines);
		}


		public HelixViewport3D Viewport
		{
			get { return (HelixViewport3D)GetValue(ViewportProperty); }
			set { SetValue(ViewportProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Viewport.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ViewportProperty =
			DependencyProperty.Register("Viewport", typeof(HelixViewport3D), typeof(DrawingControl3D),
				new PropertyMetadata(null));

		public static XbimPoint3D FindCentroid(XbimPoint3D[] p)
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
			if (n <= 0)
				return new XbimPoint3D(x, y, z);
			x /= n;
			y /= n;
			z /= n;
			return new XbimPoint3D(x, y, z);
		}

		private static void CreateNormal(XbimPoint3D pnt, XbimVector3D vector3D, MeshBuilder axesMeshBuilder)
		{
			var cnt = new Point3D() { X = pnt.X, Y = pnt.Y, Z = pnt.Z };
			var path = new List<Point3D> { cnt };
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

		protected virtual RayMeshGeometry3DHitTestResult FindHit(Point position)
		{
			RayMeshGeometry3DHitTestResult result = null;
			HitTestFilterCallback hitFilterCallback = oFilter =>
			{
				// Test for the object value you want to filter. 
				if (oFilter.GetType() == typeof(MeshVisual3D))
					return HitTestFilterBehavior.ContinueSkipSelfAndChildren;
				return HitTestFilterBehavior.Continue;
			};

			HitTestResultCallback hitTestCallback = hit =>
			{
				var rayHit = hit as RayMeshGeometry3DHitTestResult;
				if (rayHit?.MeshHit == null)
					return HitTestResultBehavior.Continue;
				var tagObject = rayHit.ModelHit.GetValue(TagProperty);
				if (tagObject == null)
					return HitTestResultBehavior.Continue;

				result = rayHit;
				return HitTestResultBehavior.Stop;
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

		// public XbimVector3D ModelTranslation;
		// public XbimMatrix3D WcsTransform;
		// private XbimVector3D _modelTranslation;

		private void ClearGraphics(ModelRefreshOptions options = ModelRefreshOptions.None)
		{
			if (DefaultLayerStyler != null)
				DefaultLayerStyler.Clear();
			PercentageLoaded = 0;
			UserModeledDimension.Clear();
			Materials.Clear();
			OriginalOpacities.Clear();

			if(Opaques != null)
				Opaques.Children.Clear();
			if(Transparents != null)
				Transparents.Children.Clear();
			if(Extras != null)
			Extras.Children.Clear();
			if(Overlays != null)
				Overlays.Children.Clear();

			if (!options.HasFlag(ModelRefreshOptions.ViewPreserveSelection))
			{
				Selection = new EntitySelection();
				Highlighted.Content = null;
			}

			if (!options.HasFlag(ModelRefreshOptions.ViewPreserveCuttingPlane))
				ClearCutPlane();

			if (!options.HasFlag(ModelRefreshOptions.ViewPreserveSelectedRegion))
				ModelPositions = new XbimModelPositioningCollection();

			Scenes = new List<XbimScene<WpfMeshGeometry3D, WpfMaterial>>();
			//if ((options & ModelRefreshOptions.ViewPreserveCameraPosition) != ModelRefreshOptions.ViewPreserveCameraPosition)
			//    Viewport.ResetCamera();
		}

		[Flags]
		public enum ModelRefreshOptions
		{
			None = 0,
			ViewPreserveCameraPosition = 1,
			ViewPreserveSelection = 2,
			ViewPreserveCuttingPlane = 4,
			ViewPreserveSelectedRegion = 4,
			ViewPreserveAll = ~None
		}

		public XbimModelPositioningCollection ModelPositions = new XbimModelPositioningCollection();

		private ILayerStyler defaultLayerStyler;
		public ILayerStyler DefaultLayerStyler
		{
			get => defaultLayerStyler;
			set
			{
				if (defaultLayerStyler != null)
					defaultLayerStyler.Clear();
				defaultLayerStyler = value;
			}
		}

		//TODO resolve issues with reference models

		private void ReferencedModels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action != NotifyCollectionChangedAction.Add || e.NewItems.Count <= 0)
				return;
			var refModel = e.NewItems[0] as XbimReferencedModel;
			if (refModel != null)
			{
				// adding and updating the model to positioning database
				ModelPositions.AddModel(refModel.Model);
				// _modelTranslation is not recalculated unless there are no models in the scene 
				// becayse it's burnt into the other models already
				if (Scenes.Count == 0)
				{
					//can recalculate extents and _modelTranslation
					// DefineModelTranslation();
				}
				//ModelPositions.SetCenterInMeters(_modelTranslation);
				//ModelBounds = ModelPositions.GetEnvelopeInMeters();

				// actually load the model geometry
				LoadReferencedModel(refModel);
			}
			RecalculateView();
		}


		/// <summary>
		/// Clears the current graphics and initiates the cascade of events that result in viewing the scene.
		/// </summary>
		/// <param name="model">The model</param>
		/// <param name="options">The options (None by default)</param>
		public void LoadGeometry(IfcStore model, ModelRefreshOptions options = ModelRefreshOptions.None)
		{
			// AddLayerToDrawingControl is the function that actually populates the geometry in the viewer.
			// AddLayerToDrawingControl is triggered by BuildRefModelScene and BuildScene below here when layers get ready.

			// reset all the visuals
			// - ModelPositions is emptied in here
			if (DefaultLayerStyler == null)
				DefaultLayerStyler = new SurfaceLayerStyler();
			ClearGraphics(options);

			if (model == null)
			{

				RecalculateView(options);
				return; //nothing to show
			}
			_hasModelGrid = model.Instances.OfType<IIfcGrid>().Any();

			// ensure a unique userDefinedId
			short userDefinedId = 0;
			model.UserDefinedId = userDefinedId;
			if (model.IsFederation)
			{
				foreach (var refModel in model.ReferencedModels)
				{
					refModel.Model.UserDefinedId = ++userDefinedId;
				}
				// todo: model federation needs to be enabled back
				// fedModel.ReferencedModels.CollectionChanged += ReferencedModels_CollectionChanged;
			}

			// model positioning routine
			if (!options.HasFlag(ModelRefreshOptions.ViewPreserveSelectedRegion))
			{
				// model scaling is determined on all federated models
				ModelPositions.AddModel(model.ReferencingModel); //add in the model that holds the main entities
																 //now add in any referenced models
				if (model.IsFederation)
				{
					foreach (var refModel in model.ReferencedModels)
					{

						var v = refModel.Model as IfcStore;
						if (v != null)
							ModelPositions.AddModel(v.ReferencingModel); //add in the referenced models
					}
				}
			}
			ModelPositions.ComputeViewBoundsTransform();


			// build the geometric scene and render as we go
			// loading the main model
			DefaultLayerStyler.SetFederationEnvironment(null);


			// load the geometry in the direct model
			XbimScene<WpfMeshGeometry3D, WpfMaterial> scene = null;
			if (!Model.GeometryStore.IsEmpty)
				scene = DefaultLayerStyler.BuildScene(model.ReferencingModel, ModelPositions[model.ReferencingModel].Transform, Opaques, Transparents,
					IsolateInstances, HiddenInstances, ExcludedTypes, SelectedContexts);

			if (scene != null && scene.Layers.Any())
			{
				Scenes.Add(scene);
			}

			if (model.IsFederation)
			{
				// loading all referenced models.
				foreach (var refModel in model.ReferencedModels)
				{
					LoadReferencedModel(refModel);
				}
			}

			RecalculateView(options);
		}

		private void LoadReferencedModel(IReferencedModel refModel)
		{
			if (refModel.Model == null)
				return;

			DefaultLayerStyler.SetFederationEnvironment(refModel);
			var mod = refModel.Model as IfcStore;
			if (mod == null)
				return;
			var pos = ModelPositions[mod.ReferencingModel].Transform;

			XbimScene<WpfMeshGeometry3D, WpfMaterial> scene = null;
			if (!mod.GeometryStore.IsEmpty)
                scene = DefaultLayerStyler.BuildScene(refModel.Model, pos, Opaques, Transparents, IsolateInstances, HiddenInstances, ExcludedTypes, SelectedContexts);
			if (scene != null && scene.Layers.Any())
			{
				Scenes.Add(scene);
			}
		}

		private void RecalculateView(ModelRefreshOptions options = ModelRefreshOptions.None)
		{
			// Assumes a NearPlaneDistance of 1/8 of meter.
			// all models are now in metres
			UpdatefrustumPlanes(Canvas, null);
			UpdateGrid();

			// make sure whole scene is visible, if ViewPreserveCameraPosition is not specified
			if (!options.HasFlag(ModelRefreshOptions.ViewPreserveCameraPosition))
				ViewHome();
		}

		/// <summary>
		/// get bounding box for the whole scene and adapt gridlines to the model units
		/// </summary>
		private void UpdateGrid()
		{
			// Disable while updating
			GridLines.Visible = false;

			var gridSizeX = Math.Ceiling(_viewBounds.SizeX / 10) * 10;
			var gridSizeY = Math.Ceiling(_viewBounds.SizeY / 10) * 10;

			GridLines.Length = 1;
			double gridLevel = Math.Ceiling(Math.Max(gridSizeX, gridSizeY) / 1000);

			if (gridSizeX > 10 || gridSizeY > 10)
			{
				// Being adaptive beyond 10x10
				GridLines.MinorDistance = gridLevel * 5;
				GridLines.MajorDistance = gridLevel * 50;
				GridLines.Thickness = gridLevel * 0.05;
			}
			else
			{
				// Otherwise static
				GridLines.MinorDistance = 1;
				GridLines.MajorDistance = 10;
				GridLines.Thickness = 0.01;
			}

			// Set extent finally
			GridLines.Length = gridSizeX;
			GridLines.Width = gridSizeY;

			var p3D = _viewBounds.Centroid();
			var t3D = new TranslateTransform3D(p3D.X, p3D.Y, _viewBounds.Z);

			GridLines.Transform = t3D;

			// Show if needed
			GridLines.Visible = !_hasModelGrid;
		}

		public void ReportData(StringBuilder sb, IModel model, int entityLabel)
		{
			if (model == null)
				return;
			foreach (var scene in Scenes)
			{
				var mesh = scene.GetMeshGeometry3D(model.Instances[entityLabel], (short)model.UserDefinedId);
				mesh.ReportGeometryTo(sb);
			}
		}


		/// <summary>
		/// function that actually populates the geometry from the layer into the viewer meshes.
		/// </summary>
		protected void AddLayerToDrawingControl(XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial> layer, bool addTagProperty)
		// Formerly called DrawLayer
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
			Materials.Add(m3D.Material);
			SetOpacityPercent(m3D.Material, ModelOpacity);
			var mv = new ModelVisual3D { Content = m3D };
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
			return from scene in Scenes from layer in scene.SubLayers select layer.Name;
		}

		/// <summary>
		/// Useful for analysis and debugging purposes (invoked by Querying interface)
		/// </summary>
		/// <returns>A string tree of layers in scenes</returns>
		public IEnumerable<string> LayersTree()
		{
			return from scene in Scenes from layer in scene.SubLayers from item in layer.LayersTree(0) select item;
		}


		public void SetVisibility(string layerName, bool visibility)
		{
			foreach (
				var layer in
					from scene in Scenes from layer in scene.SubLayers where layer.Name == layerName select layer)
			{
				if (visibility)
					layer.ShowAll();
				else
					layer.HideAll();
			}
		}

		// todo: verify that hideafterload is still in use, otherwise remove 
		public List<string> HideAfterLoad { get; set; } = new List<string>() { "IfcSpace", "IfcOpeningElement" };

		public static readonly DependencyProperty LayerSetProperty =
			DependencyProperty.Register("LayerSet", typeof(List<LayerViewModel>), typeof(DrawingControl3D));

		public List<LayerViewModel> LayerSet => (List<LayerViewModel>)GetValue(LayerSetProperty);

		private List<LayerViewModel> LayerSetRefresh()
		{
			var ret = new List<LayerViewModel> { new LayerViewModel("All") };
			ret.AddRange(from scene in Scenes from layer in scene.SubLayers select new LayerViewModel(layer.Name));
			return ret;
		}


		/// This mechanism for showing/hiding layers is not available any more, layerStylers have to be used instead
		[Obsolete]
		public void Hide(int hideProduct)
		{
		}

		/// <summary>
		/// This mechanism for showing/hiding layers is not reliable any more.
		/// It might produce odd behaviours, as it relies on conventions that are not enforced in layerstylers.
		/// </summary>
		[Obsolete]
		protected void Show(ExpressType ifcType)
		{
			var toShow = ifcType.Name + ";";
			toShow = ifcType.NonAbstractSubTypes.Aggregate(toShow, (current, subType) => current + (subType.Name + ";"));
			foreach (var eachSublayerInScenes in from scene in Scenes from eachSublayerInScenes in scene.SubLayers where toShow.Contains(eachSublayerInScenes.Name + ";") select eachSublayerInScenes)
				eachSublayerInScenes.ShowAll();
		}

		public void ShowAll()
		{
			ExcludedTypes = new List<Type>
			{
				typeof (IIfcFeatureElement),
				typeof (IIfcSpace)
			};
		}

		public void HideAll()
		{
			ExcludedTypes = new List<Type>
			{
				typeof (IIfcFeatureElement),
				typeof (IIfcSpace),
				typeof (IIfcFastener),
				typeof (IIfcPlate),
				typeof (IIfcMember)
			};
		}

		public void SetCamera(ProjectionCamera cam)
		{
			Viewport.Camera = cam;
		}

		/// <summary>
		/// Zooms to the full scale of the model.
		/// This fuction is fired when the reset of the camera is invoked.
		/// It is called from RecalculateView if the parameters of RecalculateView don't exclude it.
		/// It is also called from the WindowsUI Xplorer menu.
		/// </summary>
		public virtual void ViewHome()
		{
			Viewport.CameraController?.ResetCamera();
			var r3D = new Rect3D(
				_viewBounds.X, _viewBounds.Y, _viewBounds.Z,
				_viewBounds.SizeX, _viewBounds.SizeY, _viewBounds.SizeZ
				);
			Viewport.ZoomExtents(r3D);
		}

		public void ZoomSelected()
		{
			if (SelectedEntity == null || Highlighted.Content == null || Highlighted.Content.Bounds.IsEmpty)
				return;
			var r3D = Highlighted.Content.Bounds;
			ZoomTo(r3D);
		}

		/// <summary>
		/// This functions sets a cutting plane at a distance of delta over the base of the selected element.
		/// It is useful when the selected element is obscured by elements surrounding it.
		/// </summary>
		/// <param name="delta">positive distance of the cutting plane above the base of the selected element.</param>
		public void ClipBaseSelected(double delta)
		{
			if (SelectedEntity == null || Highlighted.Content == null || Highlighted.Content.Bounds.IsEmpty)
				return;
			var r3D = Highlighted.Content.Bounds;
			if (r3D.IsEmpty)
				return;
			SetCutPlane(
				r3D.X, r3D.Y, r3D.Z + delta,
				0, 0, -1
				);
		}

		/// <summary>
		/// Zooms to a selected portion of the space.
		/// </summary>
		/// <param name="r3D">The box to be zoomed to</param>
		/// <param name="doubleRectSize">Effectively doubles the size of the bounding box so to fit more space around it.</param>
		private void ZoomTo(Rect3D r3D, bool doubleRectSize = true)
		{
			if (r3D.IsEmpty)
				return;
			var bounds = new Rect3D(
				_viewBounds.X, _viewBounds.Y, _viewBounds.Z,
				_viewBounds.SizeX, _viewBounds.SizeY, _viewBounds.SizeZ
				);

			if (doubleRectSize)
			{
				// move the corner and double the size.
				r3D.Offset(-r3D.SizeX / 2, -r3D.SizeY / 2, -r3D.SizeZ / 2);
				r3D.SizeX *= 2;
				r3D.SizeY *= 2;
				r3D.SizeZ *= 2;
			}
			if (r3D.IsEmpty)
				return;
			// if the zoom area is bigger than bounds, zoom to bounds instead
			var actualZoomBounds = r3D.Contains(bounds) ? bounds : r3D;
			// Viewport is helix component
			Viewport.ZoomExtents(actualZoomBounds, 200);
		}

		public void ZoomTo(XbimRect3D r3D)
		{
			ZoomTo(new Rect3D(
				new Point3D(r3D.X, r3D.Y, r3D.Z),
				new Size3D(r3D.SizeX, r3D.SizeY, r3D.SizeZ)
				), false);
		}

		public void ShowOctree(XbimOctree<IIfcProduct> octree, int specificLevel = -1, bool onlyWithContent = false)
		{
			if (Viewport.Children.Contains(_octreeVisualization))
				Viewport.Children.Remove(_octreeVisualization);
			_octreeVisualization.Children.Clear();
			ShowOctree<IIfcProduct>(octree, specificLevel, onlyWithContent);
			Viewport.Children.Add(_octreeVisualization);
		}

		private readonly ModelVisual3D _octreeVisualization = new ModelVisual3D();
		private bool _hasModelGrid;
		

		private void ShowOctree<T>(XbimOctree<T> octree, int specificLevel = -1, bool onlyWithContent = false)
		{
			//prepare action
			Action show = () =>
			{
				var rect = new WpfXbimRectangle3D(octree.Bounds);
				//create transformation

				var transformation = Transform3D.Identity;

				// todo: restore scaling if needed
				// var scale = 1f / Model.ModelFactors.OneMetre;
				//Transform3DHelper.CombineTransform(
				//new TranslateTransform3D(ModelTranslation.X, ModelTranslation.Y, ModelTranslation.Z),
				//new ScaleTransform3D(scale, scale, scale)
				//);

				//Add octree geometry
				_octreeVisualization.Children.Add(new ModelVisual3D
				{
					Content = rect.Geometry,
					Transform = transformation
				});
			};

			if (specificLevel == -1 || specificLevel == octree.Depth)
			{
				if (onlyWithContent && octree.Content().Any())
					show();
				else if (!onlyWithContent)
					show();
			}

			if (specificLevel != -1 && specificLevel <= octree.Depth)
				return;
			foreach (var child in octree.Children)
			{
				ShowOctree(child, specificLevel, onlyWithContent);
			}
		}

		/// <summary>
		/// create a bitmap image of the required size and saves to the specificed file. Title is printed if specified
		/// </summary>
		/// <param name="model"></param>
		/// <param name="bmpFileName"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="title"></param>
		public static void CreateThumbnail(IfcStore model, string bmpFileName, int width, int height,
			string title = null)
		{
			var d3D = new DrawingControl3D();
			// Create the container
			var container = new Border
			{
				Child = d3D,
				Background = Brushes.White,
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(1),
				Width = width,
				Height = height
			};

			// Measure and arrange the container
			container.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			container.Arrange(new Rect(container.DesiredSize));

			// Temporarily add a PresentationSource if none exists
			using (
				var temporaryPresentationSource = new HwndSource(new HwndSourceParameters())
				{
					RootVisual = (VisualTreeHelper.GetParent(container) == null ? container : null)
				})
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
				var rtb = new RenderTargetBitmap((int)container.ActualWidth, (int)container.ActualHeight, 96, 96,
					PixelFormats.Pbgra32);
				rtb.Render(container);

				var aEncoder = new PngBitmapEncoder();
				aEncoder.Frames.Add(BitmapFrame.Create(rtb));

				using (Stream stm = File.Create(bmpFileName))
				{
					aEncoder.Save(stm);
				}
			}
		}
		/// <summary>
		/// Sets the reguion to be displayed to the relevant area.
		/// </summary>
		/// <param name="rName"></param>
		/// <param name="add"></param>
		/// <returns>true if the region has ben found and set, false otherwise</returns>
		public bool SetRegion(string rName, bool add)
		{
			var ret = ModelPositions.SetSelectedRegionByName(rName, add);
			ReloadModel(ModelRefreshOptions.ViewPreserveSelectedRegion);
			return ret;
		}
	}
}