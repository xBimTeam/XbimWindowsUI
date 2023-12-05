using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media.Media3D;
using Xbim.Common;
using Xbim.Common.Configuration;
using Xbim.Common.Federation;
using Xbim.Common.Geometry;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace Xbim.Presentation.LayerStyling
{
    public class RandomColorStyler : ILayerStyler, IProgressiveLayerStyler
    {
        public event ProgressChangedEventHandler ProgressChanged;

        readonly XbimColourMap _colourMap = new XbimColourMap();

        protected ILogger Logger { get; private set; }

        public RandomColorStyler(ILogger logger = null)
        {
            Logger = logger ?? XbimServices.Current.CreateLogger<RandomColorStyler>();
        }

        /// <summary>
        /// This version uses the new Geometry representation
        /// </summary>
        /// <param name="model"></param>
        /// <param name="modelTransform">The transform to place the models geometry in the right place</param>
        /// <param name="opaqueShapes"></param>
        /// <param name="transparentShapes"></param>
        /// <param name="isolateInstances">List of instances to be isolated</param>
        /// <param name="hideInstances">List of instances to be hidden</param>
        /// <param name="excludeTypes">List of type to exclude, by default excplict openings and spaces are excluded if exclude = null</param>
        /// <param name="selectContexts">Context display selection</param>
        /// <returns></returns>
        public XbimScene<WpfMeshGeometry3D, WpfMaterial> BuildScene(IModel model, XbimMatrix3D modelTransform, 
            ModelVisual3D opaqueShapes, ModelVisual3D transparentShapes, List<IPersistEntity> isolateInstances = null, List<IPersistEntity> hideInstances = null, List<Type> excludeTypes = null,
            List<IIfcGeometricRepresentationContext> selectContexts = null
            )
        {
            var excludedTypes = model.DefaultExclusions(excludeTypes);
            var onlyInstances = isolateInstances?.Where(i => i.Model == model).ToDictionary(i => i.EntityLabel);
            var hiddenInstances = hideInstances?.Where(i => i.Model == model).ToDictionary(i => i.EntityLabel);
            var selectedContexts = selectContexts?.Where(i => i.Model == model).ToDictionary(i => i.EntityLabel);

            var scene = new XbimScene<WpfMeshGeometry3D, WpfMaterial>(model);
            var timer = new Stopwatch();
            timer.Start();
            using (var geomStore = model.GeometryStore)
            {
                using (var geomReader = geomStore.BeginRead())
                {
                    // we are keeping the style to decide if a new element needs to be transparent.
                    //
                    var materialsByStyleId = new Dictionary<int, WpfMaterial>();
                    var repeatedShapeGeometries = new Dictionary<int, MeshGeometry3D>();
                    var tmpOpaquesGroup = new Model3DGroup();
                    var tmpTransparentsGroup = new Model3DGroup();

                    //get a list of all the unique style ids then build their style and mesh
                    //
                    var sstyleIds = geomReader.StyleIds;
                    foreach (var styleId in sstyleIds)
                    {
                        var wpfMaterial = GetWpfMaterial(model, styleId);
                        materialsByStyleId.Add(styleId, wpfMaterial);
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
                    foreach (var shapeInstance in shapeInstances.FilterShapes(onlyInstances, hiddenInstances, selectedContexts))
                    {
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

                        WpfMaterial selectedMaterial;
                        if (!materialsByStyleId.TryGetValue(styleId, out selectedMaterial))
                        {
                            // if the style is not available we build one by ExpressType
                            selectedMaterial = GetWpfMaterialByType(model, shapeInstance.IfcTypeId);
                            materialsByStyleId.Add(styleId, selectedMaterial);
                        }
                        selectedMaterial = GetRandomColorRetainTransparency(selectedMaterial);

                        //GET THE ACTUAL GEOMETRY 
                        MeshGeometry3D wpfMesh;
                        //see if we have already read it
                        if (!repeatedShapeGeometries.TryGetValue(shapeInstance.ShapeGeometryLabel, out wpfMesh))
                        {
                            IXbimShapeGeometryData shapeGeom = geomReader.ShapeGeometry(shapeInstance.ShapeGeometryLabel);
                            if ((XbimGeometryType)shapeGeom.Format != XbimGeometryType.PolyhedronBinary)
                                continue;
                            wpfMesh = new MeshGeometry3D();
                            wpfMesh.Read(shapeGeom.ShapeData);
                            if (shapeGeom.ReferenceCount > 1) //only store if we are going to use again
                            {
                                repeatedShapeGeometries.Add(shapeInstance.ShapeGeometryLabel, wpfMesh);
                            }
                        }
                        var mg = new GeometryModel3D(wpfMesh, selectedMaterial);
                        mg.BackMaterial = mg.Material;

                        mg.SetValue(FrameworkElement.TagProperty,
                            new XbimInstanceHandle(model, shapeInstance.IfcProductLabel, shapeInstance.IfcTypeId));
                        mg.Transform = XbimMatrix3D.Multiply(shapeInstance.Transformation, modelTransform).ToMatrixTransform3D();
                        if (materialsByStyleId[styleId].IsTransparent)
                            tmpTransparentsGroup.Children.Add(mg);
                        else
                            tmpOpaquesGroup.Children.Add(mg);
                    }
                    
                    if (tmpOpaquesGroup.Children.Any())
                    {
                        var mv = new ModelVisual3D {Content = tmpOpaquesGroup};
                        opaqueShapes.Children.Add(mv);
                    }
                    if (tmpTransparentsGroup.Children.Any())
                    {
                        var mv = new ModelVisual3D {Content = tmpTransparentsGroup};
                        transparentShapes.Children.Add(mv);
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


        protected static WpfMeshGeometry3D GetNewMesh(WpfMaterial wpfMaterial, Model3DGroup tmpTransparentsGroup,
            Model3DGroup tmpOpaquesGroup)
        {
            var mg = new WpfMeshGeometry3D(wpfMaterial, wpfMaterial);
            
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

        Random r = new Random();

        private WpfMaterial GetRandomColorRetainTransparency(WpfMaterial selectedMaterial)
        {
            var col = new XbimColour("Some random color",
                r.NextDouble(),
                r.NextDouble(),
                r.NextDouble(),
                selectedMaterial.IsTransparent ? 0.5 : 1.0
                );
            return new WpfMaterial(col);
        }


        public void SetFederationEnvironment(IReferencedModel refModel)
        {
            
        }

        public void Clear()
        {
            // nothing to do for clearing this style
        }
    }
}
