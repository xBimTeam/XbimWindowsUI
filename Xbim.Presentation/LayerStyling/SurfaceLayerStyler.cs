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
    public class SurfaceLayerStyler : ILayerStyler, IProgressiveLayerStyler
    {
       

        public event ProgressChangedEventHandler ProgressChanged;

        readonly XbimColourMap _colourMap = new XbimColourMap();

        public bool UseMaps = false;

        protected ILogger Logger { get; private set; }

        public SurfaceLayerStyler(ILogger logger = null)
        {
            Logger = logger ?? new LoggerFactory().CreateLogger<SurfaceLayerStyler>();
        }

        /// <summary>
        /// This version uses the new Geometry representation
        /// </summary>
        /// <param name="model"></param>
        /// <param name="modelTransform">The transform to place the models geometry in the right place</param>
        /// <param name="opaqueShapes"></param>
        /// <param name="transparentShapes"></param>
        /// <param name="exclude">List of type to exclude, by default excplict openings and spaces are excluded if exclude = null</param>
        /// <returns></returns>
        public XbimScene<WpfMeshGeometry3D, WpfMaterial> BuildScene(IModel model, XbimMatrix3D modelTransform, ModelVisual3D opaqueShapes, ModelVisual3D transparentShapes,
            List<Type> exclude = null)
        {
            var excludedTypes = model.DefaultExclusions(exclude);

            var scene = new XbimScene<WpfMeshGeometry3D, WpfMaterial>(model);
            var timer = new Stopwatch();
            timer.Start();
            using (var geomStore = model.GeometryStore)
            {
                using (var geomReader = geomStore.BeginRead())
                {
                    var materialsByStyleId = new Dictionary<int, WpfMaterial>();
                    var repeatedShapeGeometries = new Dictionary<int, MeshGeometry3D>();
                    var meshesByStyleId = new Dictionary<int, WpfMeshGeometry3D>();
                    var tmpOpaquesGroup = new Model3DGroup();
                    var tmpTransparentsGroup = new Model3DGroup();

                    //get a list of all the unique style ids then build their style and mesh
                    var sstyleIds = geomReader.StyleIds;
                    foreach (var styleId in sstyleIds)
                    {
                        var wpfMaterial = GetWpfMaterial(model, styleId);
                        materialsByStyleId.Add(styleId, wpfMaterial);
                        
                        var mg = GetNewStyleMesh(wpfMaterial, tmpTransparentsGroup, tmpOpaquesGroup);
                        meshesByStyleId.Add(styleId, mg);
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
                    
                    // !typeof (IfcFeatureElement).IsAssignableFrom(IfcMetaData.GetType(s.IfcTypeId)) /*&&
                    // !typeof(IfcSpace).IsAssignableFrom(IfcMetaData.GetType(s.IfcTypeId))*/);
                    foreach (var shapeInstance in shapeInstances)
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
                            : shapeInstance.IfcTypeId*-1;
                        
                        if (!materialsByStyleId.ContainsKey(styleId)) 
                        {
                            // if the style is not available we build one by ExpressType
                            var material2 = GetWpfMaterialByType(model, shapeInstance.IfcTypeId);
                            materialsByStyleId.Add(styleId, material2);

                            var mg = GetNewStyleMesh(material2, tmpTransparentsGroup, tmpOpaquesGroup);
                            meshesByStyleId.Add(styleId, mg);
                        }

                        //GET THE ACTUAL GEOMETRY 
                        MeshGeometry3D wpfMesh;
                        //see if we have already read it
                        if (UseMaps && repeatedShapeGeometries.TryGetValue(shapeInstance.ShapeGeometryLabel, out wpfMesh))
                        {
                            var mg = new GeometryModel3D(wpfMesh, materialsByStyleId[styleId]);
                            mg.SetValue(FrameworkElement.TagProperty,
                                new XbimInstanceHandle(model, shapeInstance.IfcProductLabel, shapeInstance.IfcTypeId));
                            mg.BackMaterial = mg.Material;
                            mg.Transform =
                                XbimMatrix3D.Multiply(shapeInstance.Transformation,
                                    modelTransform)
                                    .ToMatrixTransform3D();
                            if (materialsByStyleId[styleId].IsTransparent)
                                tmpTransparentsGroup.Children.Add(mg);
                            else
                                tmpOpaquesGroup.Children.Add(mg);
                        }
                        else //we need to get the shape geometry
                        {
                            IXbimShapeGeometryData shapeGeom = geomReader.ShapeGeometry(shapeInstance.ShapeGeometryLabel);

                            if (UseMaps && shapeGeom.ReferenceCount > 1) //only store if we are going to use again
                            {
                                wpfMesh = new MeshGeometry3D();
                                switch ((XbimGeometryType) shapeGeom.Format)
                                {
                                    case XbimGeometryType.PolyhedronBinary:
                                        wpfMesh.Read(shapeGeom.ShapeData);
                                        break;
                                    case XbimGeometryType.Polyhedron:
                                        wpfMesh.Read(((XbimShapeGeometry) shapeGeom).ShapeData);
                                        break;
                                }
                                repeatedShapeGeometries.Add(shapeInstance.ShapeGeometryLabel, wpfMesh);
                                var mg = new GeometryModel3D(wpfMesh, materialsByStyleId[styleId]);
                                mg.SetValue(FrameworkElement.TagProperty,
                                    new XbimInstanceHandle(model, shapeInstance.IfcProductLabel, shapeInstance.IfcTypeId));
                                mg.BackMaterial = mg.Material;
                                mg.Transform = XbimMatrix3D.Multiply(shapeInstance.Transformation, modelTransform).ToMatrixTransform3D();
                                if (materialsByStyleId[styleId].IsTransparent)
                                    tmpTransparentsGroup.Children.Add(mg);
                                else
                                    tmpOpaquesGroup.Children.Add(mg);
                            }
                            else //it is a one off, merge it with shapes of same style
                            {
                                var targetMergeMeshByStyle = meshesByStyleId[styleId];

                                // replace target mesh beyond suggested size
                                // https://docs.microsoft.com/en-us/dotnet/framework/wpf/graphics-multimedia/maximize-wpf-3d-performance
                                // 
                                if (targetMergeMeshByStyle.PositionCount > 20000
                                    ||
                                    targetMergeMeshByStyle.TriangleIndexCount > 60000
                                )
                                {
                                    targetMergeMeshByStyle.EndUpdate();
                                    var replace = GetNewStyleMesh(materialsByStyleId[styleId], tmpTransparentsGroup, tmpOpaquesGroup);
                                    meshesByStyleId[styleId] = replace;
                                    targetMergeMeshByStyle = replace;
                                }
                                // end replace

                                if (shapeGeom.Format != (byte) XbimGeometryType.PolyhedronBinary) 
                                    continue;
                                var transform = XbimMatrix3D.Multiply(shapeInstance.Transformation, modelTransform);
                                targetMergeMeshByStyle.Add(
                                    shapeGeom.ShapeData,
                                    shapeInstance.IfcTypeId,
                                    shapeInstance.IfcProductLabel,
                                    shapeInstance.InstanceLabel, transform,
                                    (short) model.UserDefinedId);
                            }
                        }
                    }

                    foreach (var wpfMeshGeometry3D in meshesByStyleId.Values)
                    {
                        wpfMeshGeometry3D.EndUpdate();
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


        protected static WpfMeshGeometry3D GetNewStyleMesh(WpfMaterial wpfMaterial, Model3DGroup tmpTransparentsGroup,
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


        public void SetFederationEnvironment(IReferencedModel refModel)
        {
            
        }
    }
}
