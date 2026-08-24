using HelixToolkit.Wpf;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Xbim.Common;
using Xbim.Common.Configuration;
using Xbim.Common.Federation;
using Xbim.Common.Geometry;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace Xbim.Presentation.LayerStyling
{
	public class IndividualElementStyler : ILayerStyler, IProgressiveLayerStyler
	{
		public event ProgressChangedEventHandler ProgressChanged;

		readonly XbimColourMap _colourMap = new XbimColourMap();

		System.Windows.Threading.DispatcherTimer blinkTimer;

		public void SetAnimationTime(int milliseconds)
		{
			if (blinkTimer == null)
			{
				blinkTimer = new System.Windows.Threading.DispatcherTimer();
				blinkTimer.Tick += new EventHandler(animationEvent);
			}
			if (milliseconds > 0)
			{
				blinkTimer.Stop();
				animationEvent(null, null); // perform immediately, then schedule the next
				blinkTimer.Interval = new TimeSpan(0, 0, 0, 0, milliseconds);
				blinkTimer.Start();
			}
			else
				blinkTimer.Stop();
		}

		/// <summary>
		/// Used in the blinking functions to determine the required visual appearence
		/// </summary>
		public class ColorState
		{
			public ColorState()
			{
			}

			public ColorState(Color requiredColor)
			{
				Color = requiredColor;
			}

			public static ColorState Reset { get; } = new ColorState() { DefaultSurface = true };

			/// <summary>
			/// If this flag is set the model will take the default style
			/// </summary>
			public bool DefaultSurface { get; set; }
			/// <summary>
			/// Determines if the object is to retain its transparency value
			/// </summary>
			public bool PreserveAlpha { get; set; }
			/// <summary>
			/// If a specific color is desired, set it here
			/// </summary>
			public Color Color { get; set; }
		}

		/// <summary>
		/// Private class to determine toggle states of entities that we wish to blink
		/// </summary>
		private class BlinkStates
		{
			ColorState state1;
			ColorState state2;

			private int timerMultiplier = 1;
			int TimerMultiplier
			{
				get => timerMultiplier;
				set
				{
					// ignore negative and 0 values
					if (timerMultiplier >= 1)
						timerMultiplier = value;
				}
			}
			// tally of ticks to determine if the event needs to be fired.
			int multTally = 0;
			bool toggle = false;
			
			/// <summary>
			/// Increases the tick tally and determines if the state needs to be changed.
			/// </summary>
			/// <returns>true if the state needs to be changed</returns>
			internal bool Tick()
			{
				var change = multTally++ % TimerMultiplier == 0;
				if (change)
					toggle = !toggle;
				return change;
			}

			/// <summary>
			/// A colorsta
			/// </summary>
			/// <param name="state1"></param>
			/// <param name="state2"></param>
			/// <param name="baseTimerMultiplier"></param>
			public BlinkStates(ColorState state1, ColorState state2, int baseTimerMultiplier = 1)
			{
				this.state1 = state1;
				this.state2 = state2;
				TimerMultiplier = baseTimerMultiplier;
			}

			public BlinkStates(Color state1, Color state2, int baseTimerMultiplier = 1)
			{
				this.state1 = new ColorState(state1);
				this.state2 = new ColorState(state2);
				TimerMultiplier = baseTimerMultiplier;
			}

			internal ColorState Get()
			{
				// this could be done without keeping toggle state, with some math,
				// but we are trading off memory for speed.
				return toggle ? state1 : state2;
			}
		}

		private Dictionary<IPersistEntity, BlinkStates> entityStates = new Dictionary<IPersistEntity, BlinkStates>();
		private Dictionary<Type, BlinkStates> typeStates = new Dictionary<Type, BlinkStates>();
		IModel model;

		/// <summary>
		/// This event is triggered every time the blinking function happens, 
		/// but only if any objects have been toggled
		/// </summary>
		public event HandledEventHandler BlinkHandled;

		private void animationEvent(object sender, EventArgs e)
		{
			var cnt = 0;
			if (model == null)
				return;
			foreach (var ts in typeStates)
			{
				// skip any entities which have a specific state associated
				//
				var ents = GetEnts(ts.Key).Except(entityStates.Keys);
				foreach (var ent in ents)
				{
					if (ts.Value.Tick())
						SetColor(ent, ts.Value.Get());
					cnt++;
				}
			}
			foreach (var ts in entityStates)
			{
				if (ts.Value.Tick())
					SetColor(ts.Key, ts.Value.Get());
				cnt++;
			}
			if (cnt > 0)
				BlinkHandled?.Invoke(this, new HandledEventArgs());
		}

		protected ILogger Logger { get; private set; }

		public IndividualElementStyler(ILogger logger = null)
		{
			Logger = logger ?? XbimServices.Current.CreateLogger<SurfaceLayerStyler>();
		}

		ModelVisual3D op;
		ModelVisual3D tr;

		// each dictionay by model is a dictionary by entityLabel
		private Dictionary<IModel, Dictionary<int, Dictionary<int, WpfMeshGeometry3D>>> meshesByEntity;

		/// <summary>
		/// This version uses the new Geometry representation
		/// </summary>
		/// <param name="model"></param>
		/// <param name="modelTransform">The transform to place the models geometry in the right place</param>
		/// <param name="destinationOpaques"></param>
		/// <param name="destinationTransparents"></param>
		/// <param name="isolateInstances">List of instances to be isolated</param>
		/// <param name="hideInstances">List of instances to be hidden</param>
		/// <param name="excludeTypes">List of type to exclude, by default excplict openings and spaces are excluded if exclude = null</param>
		/// <param name="selectContexts">Context display selection</param>
		/// <returns></returns>
		public XbimScene<WpfMeshGeometry3D, WpfMaterial> BuildScene(IModel model, XbimMatrix3D modelTransform,
			ModelVisual3D destinationOpaques, ModelVisual3D destinationTransparents, List<IPersistEntity> isolateInstances = null,
			List<IPersistEntity> hideInstances = null, List<Type> excludeTypes = null,
			List<IIfcGeometricRepresentationContext> selectContexts = null
			)
		{
			entityStates = new Dictionary<IPersistEntity, BlinkStates>();
			typeStates = new Dictionary<Type, BlinkStates>();
			this.model = model;
			op = destinationOpaques;
			tr = destinationTransparents;
			Hidden = new HashSet<IPersistEntity>();

			var excludedTypes = model.DefaultExclusions(excludeTypes);
			var onlyInstances = isolateInstances?.Where(i => i.Model == model).ToDictionary(i => i.EntityLabel);
			var hiddenInstances = hideInstances?.Where(i => i.Model == model).ToDictionary(i => i.EntityLabel);
			var selectedContexts = selectContexts?.Where(i => i.Model == model).ToDictionary(i => i.EntityLabel);


			if (meshesByEntity == null)
				meshesByEntity = new Dictionary<IModel, Dictionary<int, Dictionary<int, WpfMeshGeometry3D>>>();

			if (meshesByEntity.ContainsKey(model))
			{ // this should never happen
				meshesByEntity.Remove(model);
			}
			var thisMeshesByEntity = new Dictionary<int, Dictionary<int, WpfMeshGeometry3D>>();
			meshesByEntity.Add(model, thisMeshesByEntity);

			var scene = new XbimScene<WpfMeshGeometry3D, WpfMaterial>(model);
			var timer = new Stopwatch();
			timer.Start();
			using (var geomStore = model.GeometryStore)
			using (var geomReader = geomStore.BeginRead())
			{
				var materialsByStyleId = new Dictionary<int, WpfMaterial>();
				var repeatedShapeGeometries = new Dictionary<int, MeshGeometry3D>();
				var tmpOpaquesGroup = new List<Model3D>();
				var tmpTransparentsGroup = new List<Model3D>();

				//get a list of all the unique style ids then build their style 
				var sstyleIds = geomReader.StyleIds;
				foreach (var styleId in sstyleIds)
				{
					var wpfMaterial = GetWpfMaterialFromStyle(model, styleId);
					materialsByStyleId.Add(styleId, wpfMaterial);
				}
				
				//// prepare a transparent one for debug purposes, give it label 1 and 2 (low risk of clash)
				//// this might later be used in the loop to assing arbitrary styles by entityLabel
				//try
				//{
				//	var wpfTransp = GetWpfMaterialByType(model, "IFCWINDOW");
				//	materialsByStyleId.Add(1, wpfTransp);
				//	var wpfTransp2 = GetWpfMaterialByType(model, "IFCWINDOW");
				//	materialsByStyleId.Add(2, wpfTransp2);
				//}
				//catch (Exception)
				//{

				//}
				

				// we are using an empty parameter so that we get all instance back, we will hide later.
				var shapeInstances = GetShapeInstancesToRender(geomReader, new HashSet<short>());
				var tot = 1;
				if (ProgressChanged != null)
				{
					// only enumerate if there's a need for progress update
					tot = shapeInstances.Count();
				}
				var prog = 0;
				var lastProgress = 0;

				foreach (var shapeInstance in shapeInstances.FilterShapes(onlyInstances, hiddenInstances, selectedContexts))
				{
					// we can identify what entity we are working with
					var entLabel = shapeInstance.IfcProductLabel;

					// logging 
					var currentProgress = 100 * prog++ / tot;
					if (currentProgress != lastProgress && ProgressChanged != null)
					{
						ProgressChanged(this, new ProgressChangedEventArgs(currentProgress, "Creating visuals"));
						lastProgress = currentProgress;
					}

					// work out style identity
					var styleId = shapeInstance.StyleLabel > 0
						? shapeInstance.StyleLabel
						: shapeInstance.IfcTypeId * -1;

					//// Keep for debug purposes, arbitrarily assing any style by entityLabel
					//if (shapeInstance.IfcProductLabel == 152)
					//{
					//	styleId = 1;
					//}
					//else
					//{
					//	styleId = 2;
					//}

					// if the style was not prepared, it means it was not defined for this entity
					// so we create one material from the type and add it to the dictionary to keep track of it
					if (!materialsByStyleId.TryGetValue(styleId, out var useMaterial)) // if the style is not available we build one by ExpressType
					{
						useMaterial = GetWpfMaterialByType(model, shapeInstance.IfcTypeId);
						materialsByStyleId.Add(styleId, useMaterial);
					}

					// we use the isHidden variable to determine whether to add the element ot one of the 
					// opaque or transparent collections and also to keep the Hidden collection meaningful
					//
					var isHidden = excludedTypes.Contains(shapeInstance.IfcTypeId);
					IXbimShapeGeometryData shapeGeom = geomReader.ShapeGeometry(shapeInstance.ShapeGeometryLabel);
					WpfMeshGeometry3D targetMesh;

					// get or make entityMeshes 
					//  - it's a dictionary, because we try to keep the mesh count small, reusing if
					//    they have the same looks.
					if (!thisMeshesByEntity.TryGetValue(entLabel, out var entityMeshes))
					{
						entityMeshes = new Dictionary<int, WpfMeshGeometry3D>();
						thisMeshesByEntity.Add(entLabel, entityMeshes);
					}
					// get or make the target mesh
					if (!entityMeshes.TryGetValue(styleId, out targetMesh))
					{
						targetMesh = GetNewMeshWithStyle(useMaterial, tmpTransparentsGroup, tmpOpaquesGroup, isHidden);
						entityMeshes.Add(styleId, targetMesh);
					}
					if (shapeGeom.Format != (byte)XbimGeometryType.PolyhedronBinary)
						continue;
					var transform = XbimMatrix3D.Multiply(shapeInstance.Transformation, modelTransform);
					targetMesh.Add(
						shapeGeom.ShapeData,
						shapeInstance.IfcTypeId,
						shapeInstance.IfcProductLabel,
						shapeInstance.InstanceLabel, transform,
						(short)model.UserDefinedId);
					if (isHidden)
					{
						var ent = model.Instances[entLabel];
						if (!Hidden.Contains(ent)) // we might encounter more than once, just do one.
						{
							_ = Hidden.Add(ent);
						}
					}
				}
				// end entity loop

				// now go through all the groups identified per each entity to finalise them
				foreach (var meshdic in thisMeshesByEntity.Values)
				{
					foreach (var wpfMeshGeometry3D in meshdic.Values)
					{
						wpfMeshGeometry3D.EndUpdate();
					}
				}

				// now move from the tmp to the final collection
				if (tmpOpaquesGroup.Any())
				{
					foreach (var item in tmpOpaquesGroup)
					{
						var mv = new ModelVisual3D() { Content = item };
						destinationOpaques.Children.Add(mv);
					}
				}
				if (tmpTransparentsGroup.Any())
				{
					foreach (var item in tmpTransparentsGroup)
					{
						var mv = new ModelVisual3D() { Content = item };
						destinationTransparents.Children.Add(mv);
					}
				}
			}
			Logger.LogDebug("Time to load visual components: {0:F3} seconds", timer.Elapsed.TotalSeconds);

			ProgressChanged?.Invoke(this, new ProgressChangedEventArgs(0, "Ready"));
			return scene;
		}

		protected IEnumerable<XbimShapeInstance> GetShapeInstancesToRender(IGeometryStoreReader geomReader, HashSet<short> excludedTypes)
		{
			var shapeInstances = geomReader.ShapeInstances
				.Where(s => s.RepresentationType == XbimGeometryRepresentationType.OpeningsAndAdditionsIncluded
							&&
							!excludedTypes.Contains(s.IfcTypeId));
			return shapeInstances;
		}


		protected static WpfMeshGeometry3D GetNewMeshWithStyle(
			WpfMaterial wpfMaterial,
			List<Model3D> tmpTransparentsGroup,
			List<Model3D> tmpOpaquesGroup,
			bool isHidden
			)
		{
			var mg = new WpfMeshGeometry3D(wpfMaterial, wpfMaterial);
			// set the tag of the child of mg to mg, so that it can be identified on click
			mg.WpfModel.SetValue(FrameworkElement.TagProperty, mg);
			mg.BeginUpdate();
			if (!isHidden)
			{
				if (wpfMaterial.IsTransparent)
				{
					tmpTransparentsGroup.Add(mg.WpfModel);
				}
				else
					tmpOpaquesGroup.Add(mg);
			}
			return mg;
		}

		protected WpfMaterial GetWpfMaterialFromStyle(IModel model, int styleId)
		{
			var sStyle = model.Instances[styleId] as IIfcSurfaceStyle;
			if (sStyle == null)
				return null;
			var texture = XbimTexture.Create(sStyle);
			if (texture.ColourMap.Count > 0)
			{
				if (texture.ColourMap[0].Alpha <= 0)
				{
					texture.ColourMap[0].Alpha = 0.5f;
					Logger.LogWarning("Fully transparent style #{styleId} forced to 50% opacity.", styleId);
				}
			}

			texture.DefinedObjectId = styleId;
			var wpfMaterial = new WpfMaterial();
			wpfMaterial.CreateMaterial(texture);
			return wpfMaterial;
		}

		protected WpfMaterial GetWpfMaterialByType(IModel model, short typeid)
		{
			var prodType = model.Metadata.ExpressType(typeid);
			var v = _colourMap[prodType.Name];
			var texture = XbimTexture.Create(v);
			var material2 = new WpfMaterial();
			material2.CreateMaterial(texture);
			return material2;
		}
		protected WpfMaterial GetWpfMaterialByType(IModel model, string type)
		{
			var prodType = model.Metadata.ExpressType(type.ToUpperInvariant());
			var v = _colourMap[prodType.Name];
			var texture = XbimTexture.Create(v);
			var material2 = new WpfMaterial();
			material2.CreateMaterial(texture);
			return material2;
		}


		public void SetFederationEnvironment(IReferencedModel refModel)
		{
			// nothing to do in this interface method
		}

		HashSet<IPersistEntity> Hidden;

		public void Show(IPersistEntity ent)
		{
			if (!Hidden.Contains(ent))
				return; // todo: should we load the geometry if it was never loaded?
						// find the right dictionary
			if (!meshesByEntity.TryGetValue(ent.Model, out var thisMeshesByEntity))
				return; // model not found

			if (thisMeshesByEntity.TryGetValue(ent.EntityLabel, out var dic))
			{
				foreach (var item in dic.Values)
				{
					// we need to determine if using opaques or transparents
					var opac = GetOpacity(item);
					if (opac == 1 || double.IsNaN(opac))
						Restore(op, item);
					else
						Restore(tr, item);
				}
				Hidden.Remove(ent);
			}
		}

		/// <summary>
		/// Gets the alpha of the item's brush
		/// </summary>
		/// <returns>values in range 0..1</returns>
		private static double GetOpacity(WpfMeshGeometry3D item)
		{
			var t = item.WpfModel.Material as DiffuseMaterial;
			if (t == null)
				return double.NaN;
			Debug.WriteLine(t.Brush.GetType());
			if (t.Brush is SolidColorBrush scb)
			{
				return scb.Color.ScA;
			}
			return t.Brush.Opacity;
		}

		public void Hide(IPersistEntity ent)
		{
			if (Hidden.Contains(ent))
				return;
			if (!meshesByEntity.TryGetValue(ent.Model, out var thisMeshesByEntity))
				return; // model not found
			
			if (thisMeshesByEntity.TryGetValue(ent.EntityLabel, out var dic))
			{
				foreach (var item in dic.Values)
				{
					Remove(op, item);
					Remove(tr, item);
				}
				Hidden.Add(ent);
			}
		}

		// todo: 2021: need to remove all references to Model3DGroup

		private void Remove(ModelVisual3D bucket, WpfMeshGeometry3D item)
		{
			var rem = bucket.Children.OfType<ModelVisual3D>().FirstOrDefault(x => x.Content == item.WpfModel);
			if (rem != null)
			{
				bucket.Children.Remove(rem);
			}
		}

		private void Restore(ModelVisual3D bucket, WpfMeshGeometry3D item)
		{
			var t = new ModelVisual3D() { Content = item};
			bucket.Children.Add(t);
		}

		public void SetAnimation(IPersistEntity ent, ColorState state1, ColorState state2, int BaseTimerMultiplier = 1)
		{
			BlinkStates bState = new BlinkStates(state1, state2, BaseTimerMultiplier);
			SetBlink(ent, bState);
		}
		public void SetAnimation(IPersistEntity ent, Color state1, Color state2, int BaseTimerMultiplier = 1)
		{
			BlinkStates bState = new BlinkStates(state1, state2, BaseTimerMultiplier);
			SetBlink(ent, bState);
		}

		public void SetAnimation(Type tp, ColorState state1, ColorState state2, int BaseTimerMultiplier = 1)
		{
			BlinkStates bState = new BlinkStates(state1, state2, BaseTimerMultiplier);
			SetBlink(tp, bState);
		}

		public void SetAnimation(Type tp, Color state1, Color state2, int BaseTimerMultiplier = 1)
		{
			BlinkStates bState = new BlinkStates(state1, state2, BaseTimerMultiplier);
			SetBlink(tp, bState);
		}

		private void SetBlink(IPersistEntity ent, BlinkStates bState)
		{
			if (entityStates.ContainsKey(ent))
				entityStates.Remove(ent);
			entityStates.Add(ent,
				bState
				);
		}

		private void SetBlink(Type tp, BlinkStates bState)
		{
			if (typeStates.ContainsKey(tp))
				typeStates.Remove(tp);
			typeStates.Add(tp,
				bState
				);
		}

		public void SetColor(Type tp, ColorState desiredState)
		{
			if (desiredState.DefaultSurface)
				ResetColor(tp);
			else
				SetColor(tp, desiredState.Color, desiredState.PreserveAlpha);
		}

		public void SetColor(IPersistEntity ent, ColorState desiredState)
		{
			if (desiredState.DefaultSurface)
				ResetColor(ent);
			else
				SetColor(ent, desiredState.Color, desiredState.PreserveAlpha);
		}

		public void SetColor(Type tp, Color newColor, bool preserveAlpha = true)
		{
			if (model == null)
				return;
			foreach (var ent in GetEnts(tp))
			{
				SetColor(ent, newColor, preserveAlpha);
			}
		}

		public void SetColor(IPersistEntity ent, Color newColor, bool preserveAlpha = true)
		{
			if (!meshesByEntity.TryGetValue(ent.Model, out var thisMeshesByEntity))
				return; // model not found
			if (!thisMeshesByEntity.TryGetValue(ent.EntityLabel, out var dic))
				return; // nothing to do

			newColor.Clamp();
			if (preserveAlpha)
			{
				// we will create a material for each mesh if we need to preserve alpha
				foreach (var mesh in dic.Values)
				{
					var alpha = GetOpacity(mesh);
					Debug.WriteLine(alpha);
					var name = $"#{Hex(newColor.ScR)}{Hex(newColor.ScB)}{Hex(newColor.ScB)}{Hex(alpha)}";
					XbimColour c = new XbimColour(name, newColor.ScR, newColor.ScG, newColor.ScB, alpha);
					var material = new WpfMaterial(c);
					mesh.WpfModel.Material = material;
					mesh.WpfModel.BackMaterial = material;
					// since we are preserving alpha no need to move bucket
				}
			}
			else
			{
				// we will create a single color if no need to preserve alpha
				var name = $"#{Hex(newColor.ScR)}{Hex(newColor.ScB)}{Hex(newColor.ScB)}{Hex(newColor.ScA)}";
				XbimColour c = new XbimColour(name, newColor.ScR, newColor.ScG, newColor.ScB, newColor.ScA);
				var material = new WpfMaterial(c);
				foreach (var mesh in dic.Values)
				{
					SetMaterial(mesh, material);
				}
			}
		}

		/// <summary>
		/// set color and adjust transparent/opaque bucket accordingly
		/// </summary>
		private void SetMaterial(WpfMeshGeometry3D mesh, WpfMaterial material)
		{
			var prevTransparent = GetOpacity(mesh) < 1;
			var needMove = prevTransparent != material.IsTransparent;
			mesh.WpfModel.Material = material;
			mesh.WpfModel.BackMaterial = material;
			if (needMove) // adjust bucket for alpha
				MoveBucket(mesh, material.IsTransparent);
		}

		private void MoveBucket(WpfMeshGeometry3D mesh, bool isTransparent)
		{
			if (isTransparent)
			{
				Remove(op, mesh);
				Restore(tr, mesh);
			}
			else
			{
				Remove(tr, mesh);
				Restore(op, mesh);
			}
		}

		private object Hex(double unityValue)
		{
			int intvalue = (int)(unityValue * 255);
			return intvalue.ToString("X2");
		}

		public void Hide(Type tp)
		{
			foreach (var ent in GetEnts(tp))
			{
				Hide(ent);
			}
		}

		public void Show(Type tp)
		{
			foreach (var ent in GetEnts(tp))
			{
				Show(ent);
			}
		}

		public void ResetColor(Type tp)
		{
			foreach (var ent in GetEnts(tp))
			{
				ResetColor(ent);
			}
		}

		/// <summary>
		/// Clears all animations previously set;
		/// </summary>
		public void RemoveAnimation(bool reset = false)
		{
			bool bRestart = false;
			if (reset)
			{
				if (blinkTimer.IsEnabled)
				{
					blinkTimer.Stop();
					bRestart = true;
				}
				foreach (var tp in typeStates.Keys)
				{
					ResetColor(tp);
				}
				foreach (var ent in entityStates.Keys)
				{
					ResetColor(ent);
				}
			}
			typeStates.Clear();
			entityStates.Clear();
			if (bRestart)            // the event will fire with no animations, but it will keep processing the queue
				blinkTimer.Start();  // if any is animation is added
		}

		public void RemoveAnimation(Type tp)
		{
			typeStates.Remove(tp);
		}

		public void RemoveAnimation(IPersistEntity ent)
		{
			entityStates.Remove(ent);
		}

		public IEnumerable<IPersistEntity> GetEnts(Type tp)
		{
			return model.Instances.OfType(tp.Name, false);
		}

		public void ResetColor(IPersistEntity ent)
		{
			if (!meshesByEntity.TryGetValue(ent.Model, out var thisMeshesByEntity))
				return; // model not found

			if (!thisMeshesByEntity.TryGetValue(ent.EntityLabel, out var dic))
				return; // nothing to do
			foreach (var item in dic)
			{
				var styleId = item.Key;
				WpfMaterial material;
				if (styleId < 0)
					material = GetWpfMaterialByType(model, (short)-styleId);				
				else
					material = GetWpfMaterialFromStyle(ent.Model, styleId);
				if (material == null)
					continue;
				var mesh = item.Value;
				SetMaterial(mesh, material);
			}
		}

		public void Clear()
		{
			blinkTimer?.Stop();
			typeStates?.Clear();
			entityStates?.Clear();
			model = null;
			meshesByEntity?.Clear();
			Hidden?.Clear();
		}
	}
}
