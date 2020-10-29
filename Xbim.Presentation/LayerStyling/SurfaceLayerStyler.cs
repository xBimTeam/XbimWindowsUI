﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Media3D;
using Xbim.Common;
using Xbim.Common.Federation;
using Xbim.Common.Geometry;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.Presentation.Texturing;

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
            Logger = logger ?? XbimLogging.CreateLogger<SurfaceLayerStyler>();
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
        /// <param name="selectContexts">Contexts for displaying</param>
        /// <returns></returns>
        public XbimScene<WpfMeshGeometry3D, WpfMaterial> BuildScene(IModel model, XbimMatrix3D modelTransform, 
            ModelVisual3D opaqueShapes, ModelVisual3D transparentShapes, List<IPersistEntity> isolateInstances = null, 
            List<IPersistEntity> hideInstances = null, List<IIfcGeometricRepresentationContext> selectContexts = null,
            List <Type> excludeTypes = null)
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
                    foreach (var shapeInstance in shapeInstances
                        .Where(s => null == onlyInstances || onlyInstances.Count == 0 || onlyInstances.Keys.Contains(s.IfcProductLabel) )
                        .Where(s => null == hiddenInstances || hiddenInstances.Count == 0 || !hiddenInstances.Keys.Contains(s.IfcProductLabel)) 
                        .Where(s => null == selectedContexts || selectedContexts.Count == 0 || selectedContexts.Keys.Contains(s.RepresentationContext)))
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

                                //manual Texturemapping
                                if (materialsByStyleId[styleId].HasTexture
                                    && mg.Geometry is MeshGeometry3D mesh3D)
                                {
                                    ITextureMapping tMapping;
                                    if (materialsByStyleId[styleId].IfcTextureCoordinate != null)
                                    {
                                        tMapping = TextureMappingFactory.CreateTextureMapping(materialsByStyleId[styleId].IfcTextureCoordinate);
                                    }
                                    else
                                    {
                                        Logger.LogWarning(0, "No IfcTextureCoordinate is defined for style " + styleId + ". Spherical mapping is used.");
                                        tMapping = new SphericalTextureMap();
                                    }
                                    mesh3D.TextureCoordinates.Concat(tMapping.GetTextureMap(mesh3D.Positions, mesh3D.Normals, mesh3D.TriangleIndices));
                                }

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
                                ITextureMapping textureMethod = null;
                                if (materialsByStyleId[styleId].HasTexture)
                                {
                                    if (materialsByStyleId[styleId].IfcTextureCoordinate != null)
                                    {
                                        textureMethod = TextureMappingFactory.CreateTextureMapping(materialsByStyleId[styleId].IfcTextureCoordinate);
                                    }
                                    else
                                    {
                                        Logger.LogWarning(0, "No texture mapping method defined for style " + styleId + ". Spherical mapping is used.");
                                        textureMethod = new SphericalTextureMap();
                                    }
                                }
                                targetMergeMeshByStyle.Add(
                                    shapeGeom.ShapeData,
                                    shapeInstance.IfcTypeId,
                                    shapeInstance.IfcProductLabel,
                                    shapeInstance.InstanceLabel, transform,
                                    (short) model.UserDefinedId,
                                    textureMethod);

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
            var mg = new WpfMeshGeometry3D(wpfMaterial, wpfMaterial, wpfMaterial.IfcTextureCoordinate);
            
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
            var wpfMaterial = new WpfMaterial();
            
            //The style contains a texture
            bool isTexture = false;
            if (sStyle.Styles.Any(x => x is IIfcSurfaceStyleWithTextures))
            {
                IIfcSurfaceStyleWithTextures surfaceStyleWithTexture = (IIfcSurfaceStyleWithTextures)sStyle.Styles.First(x => x is IIfcSurfaceStyleWithTextures);
                if (surfaceStyleWithTexture.Textures.Any(x => x is IIfcImageTexture))
                {
                    IIfcImageTexture imageTexture = surfaceStyleWithTexture.Textures.First(x => x is IIfcImageTexture) as IIfcImageTexture;
                    //generate the correct path
                    Uri imageUri;
                    if (Uri.TryCreate(imageTexture.URLReference, UriKind.Absolute, out imageUri))
                    {
                        wpfMaterial.WpfMaterialFromImageTexture(imageUri);
                    }
                    else if (Uri.TryCreate(imageTexture.URLReference, UriKind.Relative, out imageUri))
                    {
                        Uri modelFileUri = new Uri(model.Header.FileName.Name);
                        Uri absolutFileUri = new Uri(modelFileUri, imageTexture.URLReference);
                        wpfMaterial.WpfMaterialFromImageTexture(absolutFileUri);
                    }
                    else
                    {
                        Logger.LogWarning(0, "Invalid Uri " + imageTexture.URLReference + " (bad formatted or file not found).", imageTexture);
                    }

                    if (imageTexture.IsMappedBy != null)
                    {
                        wpfMaterial.IfcTextureCoordinate = imageTexture.IsMappedBy.FirstOrDefault();
                    }
                    isTexture = true;
                }
            }
            
            //The style doesn't contain a texture
            if (isTexture == false)
            {
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
                wpfMaterial.CreateMaterial(texture);
            }
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
