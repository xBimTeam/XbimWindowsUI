using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media.Media3D;
using Xbim.Common;
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

        protected ILogger Logger { get; private set; }

        public IndividualElementStyler(ILogger logger = null)
        {
            Logger = logger ?? XbimLogging.CreateLogger<SurfaceLayerStyler>();
        }

        ModelVisual3D op;
        ModelVisual3D tr;

        private Dictionary<IPersistEntity, Dictionary<int, WpfMeshGeometry3D>> meshesByEntity;

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
        /// <returns></returns>
        public XbimScene<WpfMeshGeometry3D, WpfMaterial> BuildScene(IModel model, XbimMatrix3D modelTransform,
            ModelVisual3D destinationOpaques, ModelVisual3D destinationTransparents, List<IPersistEntity> isolateInstances = null, 
            List<IPersistEntity> hideInstances = null, List<Type> excludeTypes = null)
        {
			op = destinationOpaques;
            tr = destinationTransparents;
            Hidden = new HashSet<IPersistEntity>();

            var excludedTypes = model.DefaultExclusions(excludeTypes);
            var onlyInstances = isolateInstances?.Where(i => i.Model == model).ToDictionary(i => i.EntityLabel);
            var hiddenInstances = hideInstances?.Where(i => i.Model == model).ToDictionary(i => i.EntityLabel);

            meshesByEntity = new Dictionary<IPersistEntity, Dictionary<int, WpfMeshGeometry3D>>();

            var scene = new XbimScene<WpfMeshGeometry3D, WpfMaterial>(model);
            var timer = new Stopwatch();
            timer.Start();
            using (var geomStore = model.GeometryStore)
            using (var geomReader = geomStore.BeginRead())
            {
                var materialsByStyleId = new Dictionary<int, WpfMaterial>();
                var repeatedShapeGeometries = new Dictionary<int, MeshGeometry3D>();
                var tmpOpaquesGroup = new Model3DGroup();
                var tmpTransparentsGroup = new Model3DGroup();

                //get a list of all the unique style ids then build their style and mesh
                var sstyleIds = geomReader.StyleIds;
                foreach (var styleId in sstyleIds)
                {
                    var wpfMaterial = GetWpfMaterial(model, styleId);
                    materialsByStyleId.Add(styleId, wpfMaterial);
                    // var mg = GetNewStyleMesh(wpfMaterial, tmpTransparentsGroup, tmpOpaquesGroup);
                    // meshesByStyleId.Add(styleId, mg);
                }

                var shapeInstances = GetShapeInstancesToRender(geomReader, excludedTypes);
                var tot = 1;
                if (ProgressChanged != null)
                {
                    // only enumerate if there's a need for progress update
                    tot = shapeInstances.Count();
                }
                var prog = 0;
                var lastProgress = 0;

                foreach (var shapeInstance in shapeInstances
                    .Where(s => null == onlyInstances || onlyInstances.Count == 0 || onlyInstances.Keys.Contains(s.IfcProductLabel))
                    .Where(s => null == hiddenInstances || hiddenInstances.Count == 0 || !hiddenInstances.Keys.Contains(s.IfcProductLabel)))
                {
                    // we can identify what entity we are working with
                    var ent = model.Instances[shapeInstance.IfcProductLabel];

                    // logging 
                    var currentProgress = 100 * prog++ / tot;
                    if (currentProgress != lastProgress && ProgressChanged != null)
                    {
                        ProgressChanged(this, new ProgressChangedEventArgs(currentProgress, "Creating visuals"));
                        lastProgress = currentProgress;
                    }

                    // work out style
                    var styleId = shapeInstance.StyleLabel > 0
                        ? shapeInstance.StyleLabel
                        : shapeInstance.IfcTypeId * -1;

                    if (!materialsByStyleId.ContainsKey(styleId)) // if the style is not available we build one by ExpressType
                    {
                        var material2 = GetWpfMaterialByType(model, shapeInstance.IfcTypeId);
                        materialsByStyleId.Add(styleId, material2);
                    }

                    IXbimShapeGeometryData shapeGeom = geomReader.ShapeGeometry(shapeInstance.ShapeGeometryLabel);
                    WpfMeshGeometry3D targetMesh;
                    if (!meshesByEntity.TryGetValue(ent, out var meshesByStyleId))
                    {
						targetMesh = GetNewStyleMesh(materialsByStyleId[styleId], tmpTransparentsGroup, tmpOpaquesGroup);
                        meshesByStyleId = new Dictionary<int, WpfMeshGeometry3D>();
                        meshesByStyleId.Add(styleId, targetMesh);
                        meshesByEntity.Add(ent, meshesByStyleId);
                    }
                    else if (!meshesByStyleId.TryGetValue(styleId, out targetMesh))
					{
                        targetMesh = GetNewStyleMesh(materialsByStyleId[styleId], tmpTransparentsGroup, tmpOpaquesGroup);
                        meshesByStyleId.Add(styleId, targetMesh);
                    }
                    // otherwise the targetmesh is already identified


                    if (shapeGeom.Format != (byte)XbimGeometryType.PolyhedronBinary)
                        continue;
                    var transform = XbimMatrix3D.Multiply(shapeInstance.Transformation, modelTransform);
                    targetMesh.Add(
                        shapeGeom.ShapeData,
                        shapeInstance.IfcTypeId,
                        shapeInstance.IfcProductLabel,
                        shapeInstance.InstanceLabel, transform,
                        (short)model.UserDefinedId);

                }
                // now go through all the groups identified per each entity to finalise them
				foreach (var meshdic in meshesByEntity.Values)
				{
                    foreach (var wpfMeshGeometry3D in meshdic.Values)
                    {
                        wpfMeshGeometry3D.EndUpdate();
                    }
                }

                // now move from the tmp to the final repository
                if (tmpOpaquesGroup.Children.Any())
                {
                    var mv = new ModelVisual3D { Content = tmpOpaquesGroup };
                    destinationOpaques.Children.Add(mv);
                }
                if (tmpTransparentsGroup.Children.Any())
                {
                    var mv = new ModelVisual3D { Content = tmpTransparentsGroup };
                    destinationTransparents.Children.Add(mv);
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


        protected static WpfMeshGeometry3D GetNewStyleMesh(WpfMaterial wpfMaterial, Model3DGroup tmpTransparentsGroup,
            Model3DGroup tmpOpaquesGroup)
        {
            var mg = new WpfMeshGeometry3D(wpfMaterial, wpfMaterial);
            // set the tag of the child of mg to mg, so that it can be identified on click
            mg.WpfModel.SetValue(FrameworkElement.TagProperty, mg);
            mg.BeginUpdate();
            if (wpfMaterial.IsTransparent)
                tmpTransparentsGroup.Children.Add(mg);
            else
                tmpOpaquesGroup.Children.Add(mg);
            return mg;
        }

        protected WpfMaterial GetWpfMaterial(IModel model, int styleId)
        {
            var sStyle = model.Instances[styleId] as IIfcSurfaceStyle;
            var texture = XbimTexture.Create(sStyle);
            if(texture.ColourMap.Count > 0)
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


        public void SetFederationEnvironment(IReferencedModel refModel)
        {
            
        }

        HashSet<IPersistEntity> Hidden;

		public void Show(IPersistEntity ent)
		{
            if (!Hidden.Contains(ent))
                return;
            if (meshesByEntity.TryGetValue(ent, out var dic))
            {
				foreach (var item in dic.Values)
				{
                    // we need to determine if using opaques or transparents
                    var t = item.WpfModel.Material as DiffuseMaterial;
                    if (t == null)
                        continue;
                    Debug.Write(t.Brush.Opacity);
                    if (t.Brush.Opacity == 1)
                        Restore(op, item);
                    else
                        Restore(tr, item);

                }
                Hidden.Remove(ent);
			}
        }


		public void Hide(IPersistEntity ent)
        {
            if (Hidden.Contains(ent))
                return;
            if (meshesByEntity.TryGetValue(ent, out var dic))
            {
                foreach (var item in dic.Values)
                {
                    Remove(op, item);
                    Remove(tr, item);
                }
                Hidden.Add(ent);
            }
        }

        private void Remove(ModelVisual3D op, WpfMeshGeometry3D item)
		{
            foreach (var mv in op.Children.OfType<ModelVisual3D>())
            {
                // Debug.Write(mv.Content.GetType());
                var g = mv.Content as Model3DGroup;
                if (g == null)
                    continue;
                g.Children.Remove(item);
            }
        }

        private void Restore(ModelVisual3D op, WpfMeshGeometry3D item)
        {
            var dest = op.Children.OfType<ModelVisual3D>().FirstOrDefault();
            var cont = dest.Content as Model3DGroup;
            cont.Children.Add(item);
        }
    }
}
