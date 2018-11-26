using Microsoft.Extensions.Logging;
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

namespace Xbim.Presentation.LayerStyling
{
    /// <summary>
    /// A class that just diplays bounding boxes of elements in the tree
    /// </summary>
    public class BoundingBoxStyler : ILayerStyler, IProgressiveLayerStyler
    {
        public event ProgressChangedEventHandler ProgressChanged;

        readonly XbimColourMap _colourMap = new XbimColourMap();

        protected ILogger Logger { get; private set; }

        public BoundingBoxStyler(ILogger logger = null)
        {
            Logger = logger ?? new LoggerFactory().CreateLogger<BoundingBoxStyler>();
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
                            : shapeInstance.IfcTypeId * -1;

                        if (!materialsByStyleId.ContainsKey(styleId))
                        {
                            // if the style is not available we build one by ExpressType
                            var material2 = GetWpfMaterialByType(model, shapeInstance.IfcTypeId);
                            materialsByStyleId.Add(styleId, material2);

                            var mg = GetNewStyleMesh(material2, tmpTransparentsGroup, tmpOpaquesGroup);
                            meshesByStyleId.Add(styleId, mg);
                        }
                        
                        var boxRep = GetSlicedBoxRepresentation(shapeInstance.BoundingBox);
                        var targetMergeMeshByStyle = meshesByStyleId[styleId];
                        
                        var transform = XbimMatrix3D.Multiply(shapeInstance.Transformation, modelTransform);
                        targetMergeMeshByStyle.Add(
                            boxRep,
                            shapeInstance.IfcTypeId,    
                            shapeInstance.IfcProductLabel,
                            shapeInstance.InstanceLabel, 
                            transform,
                            (short) model.UserDefinedId);
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

        /// <summary>
        /// this is just three triangles per boundinng box at the moment.
        /// </summary>
        /// <param name="BoundingBox"></param>
        /// <returns></returns>
        private byte[] GetSlicedBoxRepresentation(XbimRect3D BoundingBox)
        {
            var TriSize = 2;
            var pts = new List<XbimPoint3D>(4);
            pts.Add(new XbimPoint3D(BoundingBox.Min.X, BoundingBox.Min.Y, BoundingBox.Min.Z));
            pts.Add(new XbimPoint3D(BoundingBox.Min.X + BoundingBox.SizeX / TriSize, BoundingBox.Min.Y, BoundingBox.Min.Z));
            pts.Add(new XbimPoint3D(BoundingBox.Min.X, BoundingBox.Min.Y + BoundingBox.SizeY / TriSize, BoundingBox.Min.Z));
            pts.Add(new XbimPoint3D(BoundingBox.Min.X, BoundingBox.Min.Y, BoundingBox.Min.Z + BoundingBox.SizeZ / TriSize));

            pts.Add(new XbimPoint3D(BoundingBox.Max.X, BoundingBox.Max.Y, BoundingBox.Max.Z));
            pts.Add(new XbimPoint3D(BoundingBox.Max.X - BoundingBox.SizeX / TriSize, BoundingBox.Max.Y, BoundingBox.Max.Z));
            pts.Add(new XbimPoint3D(BoundingBox.Max.X, BoundingBox.Max.Y - BoundingBox.SizeY / TriSize, BoundingBox.Max.Z));
            pts.Add(new XbimPoint3D(BoundingBox.Max.X, BoundingBox.Max.Y, BoundingBox.Max.Z - BoundingBox.SizeZ / TriSize));

            var n1 = new XbimPackedNormal(0, 0, 1);
            var n2 = new XbimPackedNormal(0, 1, 0);
            var n3 = new XbimPackedNormal(1, 0, 0);

            var n4 = new XbimPackedNormal(0, 0, -1);
            var n5 = new XbimPackedNormal(0, -1, 0);
            var n6 = new XbimPackedNormal(-1, 0, 0);

            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write((byte) 1); // version
                    writer.Write((int)pts.Count); // points
                    writer.Write((int)6); // total triangles

                    // write the points
                    foreach (var pt in pts)
                    {
                        writer.Write((float)pt.X);
                        writer.Write((float)pt.Y);
                        writer.Write((float)pt.Z);
                    }

                    writer.Write((int)6); // faces

                    // face 1
                    // 
                    writer.Write((int)1); // 1 triangle
                    n1.Write(writer); // the normal
                    writer.Write((byte) 0); // small indices are stored in a single byte
                    writer.Write((byte) 1);
                    writer.Write((byte) 2);

                    // face 2
                    // 
                    writer.Write((int)1); // 1 triangle
                    n2.Write(writer); // the normal
                    writer.Write((byte)0); // small indices are stored in a single byte
                    writer.Write((byte)1);
                    writer.Write((byte)3);

                    // face 3
                    // 
                    writer.Write((int)1); // 1 triangle
                    n3.Write(writer); // the normal
                    writer.Write((byte)0); // small indices are stored in a single byte
                    writer.Write((byte)2);
                    writer.Write((byte)3);

                    // face 4
                    // 
                    writer.Write((int)1); // 1 triangle
                    n4.Write(writer); // the normal
                    writer.Write((byte)4); // small indices are stored in a single byte
                    writer.Write((byte)6);
                    writer.Write((byte)5);

                    // face 5
                    // 
                    writer.Write((int)1); // 1 triangle
                    n5.Write(writer); // the normal
                    writer.Write((byte)5); // small indices are stored in a single byte
                    writer.Write((byte)4);
                    writer.Write((byte)7);

                    // face 6
                    // 
                    writer.Write((int)1); // 1 triangle
                    n6.Write(writer); // the normal
                    writer.Write((byte)6); // small indices are stored in a single byte
                    writer.Write((byte)4);
                    writer.Write((byte)7);
                }
                stream.Flush();
                byte[] bytes = stream.GetBuffer();
                return bytes;
            }
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

        protected static WpfMaterial GetWpfMaterial(IModel model, int styleId)
        {
            var sStyle = model.Instances[styleId] as IIfcSurfaceStyle;
            var texture = XbimTexture.Create(sStyle);
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
