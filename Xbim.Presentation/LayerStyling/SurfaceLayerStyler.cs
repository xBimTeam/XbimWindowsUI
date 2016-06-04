using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Media3D;
using Xbim.Common;
using Xbim.Common.Federation;
using Xbim.Common.Geometry;
using Xbim.Common.Metadata;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace Xbim.Presentation.LayerStyling
{
    public class SurfaceLayerStyler : ILayerStyler
    {
        // ReSharper disable once CollectionNeverUpdated.Local
        readonly XbimColourMap _colourMap = new XbimColourMap();

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
            
            var excludedTypes = new HashSet<short>();
            if (exclude == null)
                exclude = new List<Type>()
                {
                    typeof(IIfcSpace)
                    // , typeof(IfcFeatureElement)
                };
            foreach (var excludedT in exclude)
            {
                ExpressType ifcT;
                if (excludedT.IsInterface && excludedT.Name.StartsWith("IIfc"))
                {
                    var concreteTypename = excludedT.Name.Substring(1).ToUpper();
                    ifcT = model.Metadata.ExpressType(concreteTypename);
                }
                else
                    ifcT = model.Metadata.ExpressType(excludedT);
                foreach (var exIfcType in ifcT.NonAbstractSubTypes)
                {
                    excludedTypes.Add(exIfcType.TypeId);
                }
            }

            var scene = new XbimScene<WpfMeshGeometry3D, WpfMaterial>(model);

            using (var geomStore = model.GeometryStore)
            {
                using (var geomReader = geomStore.BeginRead())
                {
                    //get a list of all the unique styles
                    var styles = new Dictionary<int, WpfMaterial>();
                    var repeatedShapeGeometries = new Dictionary<int, MeshGeometry3D>();
                    var styleMeshSets = new Dictionary<int, WpfMeshGeometry3D>();
                    var tmpOpaquesGroup = new Model3DGroup();
                    var tmpTransparentsGroup = new Model3DGroup();

                    var sstyles = geomReader.StyleIds;
                   
                    foreach (var style in sstyles)
                    {
                        
                        var sStyle = model.Instances[style] as IIfcSurfaceStyle;
                        var texture = XbimTexture.Create(sStyle);
                        
                        texture.DefinedObjectId = style;
                        var wpfMaterial = new WpfMaterial();
                        wpfMaterial.CreateMaterial(texture);
                        styles.Add(style, wpfMaterial);
                        var mg = new WpfMeshGeometry3D(wpfMaterial, wpfMaterial);
                        mg.WpfModel.SetValue(FrameworkElement.TagProperty, mg);
                        styleMeshSets.Add(style, mg);
                        mg.BeginUpdate();
                        if (texture.IsTransparent)
                            tmpTransparentsGroup.Children.Add(mg);
                        else
                            tmpOpaquesGroup.Children.Add(mg);
                    }

                   
                    var shapeInstances = geomReader.ShapeInstances
                        .Where(s => s.RepresentationType == XbimGeometryRepresentationType.OpeningsAndAdditionsIncluded
                                    &&
                                    !excludedTypes.Contains(s.IfcTypeId));
                    // !typeof (IfcFeatureElement).IsAssignableFrom(IfcMetaData.GetType(s.IfcTypeId)) /*&&
                    // !typeof(IfcSpace).IsAssignableFrom(IfcMetaData.GetType(s.IfcTypeId))*/);
                    foreach (var shapeInstance in shapeInstances)
                    {

                        var styleId = shapeInstance.StyleLabel > 0
                            ? shapeInstance.StyleLabel
                            : shapeInstance.IfcTypeId*-1;
                        WpfMaterial material;
                        if (!styles.TryGetValue(styleId, out material)) //get the ifc style and build it
                        {
                            var prodType = model.Metadata.ExpressType(shapeInstance.IfcTypeId);
                            var v = _colourMap[prodType.Name];
                            var texture = XbimTexture.Create(v);
                            material = new WpfMaterial();
                            material.CreateMaterial(texture);
                            styles.Add(styleId,material);
                            var mg = new WpfMeshGeometry3D(material, material);
                            mg.WpfModel.SetValue(FrameworkElement.TagProperty, mg);
                            styleMeshSets.Add(styleId, mg);
                            mg.BeginUpdate();
                            if (texture.IsTransparent)
                                tmpTransparentsGroup.Children.Add(mg);
                            else
                                tmpOpaquesGroup.Children.Add(mg);
                        }
                        //GET THE ACTUAL GEOMETRY 
                        MeshGeometry3D wpfMesh;
                        //see if we have already read it
                        if (repeatedShapeGeometries.TryGetValue(shapeInstance.ShapeGeometryLabel, out wpfMesh))
                        {
                            var mg = new GeometryModel3D(wpfMesh, styles[styleId]);
                            mg.SetValue(FrameworkElement.TagProperty,
                                new XbimInstanceHandle(model, shapeInstance.IfcProductLabel, shapeInstance.IfcTypeId));
                            mg.BackMaterial = mg.Material;
                            mg.Transform =
                                XbimMatrix3D.Multiply(shapeInstance.Transformation,
                                    modelTransform)
                                    .ToMatrixTransform3D();
                            if (styles[styleId].IsTransparent)
                                tmpTransparentsGroup.Children.Add(mg);
                            else
                                tmpOpaquesGroup.Children.Add(mg);
                        }
                        else //we need to get the shape geometry
                        {
                            IXbimShapeGeometryData shapeGeom = geomReader.ShapeGeometry(shapeInstance.ShapeGeometryLabel);

                            if (shapeGeom.ReferenceCount > 1) //only store if we are going to use again
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
                                var mg = new GeometryModel3D(wpfMesh, styles[styleId]);
                                mg.SetValue(FrameworkElement.TagProperty,
                                    new XbimInstanceHandle(model, shapeInstance.IfcProductLabel, shapeInstance.IfcTypeId));
                                mg.BackMaterial = mg.Material;
                                mg.Transform =
                                    XbimMatrix3D.Multiply(shapeInstance.Transformation,
                                        modelTransform)
                                        .ToMatrixTransform3D();
                                if (styles[styleId].IsTransparent)
                                    tmpTransparentsGroup.Children.Add(mg);
                                else
                                    tmpOpaquesGroup.Children.Add(mg);
                            }
                            else //it is a one off, merge it with shapes of a similar material
                            {
                                var targetMergeMeshByStyle = styleMeshSets[styleId];
                                if (shapeGeom.Format == (byte)XbimGeometryType.PolyhedronBinary)
                                {
                                    var transform = XbimMatrix3D.Multiply(shapeInstance.Transformation,
                                        modelTransform);
                                    targetMergeMeshByStyle.Add(
                                        shapeGeom.ShapeData,
                                        shapeInstance.IfcTypeId,
                                        shapeInstance.IfcProductLabel,
                                        shapeInstance.InstanceLabel, transform,
                                        (short) model.UserDefinedId);
                                }                           
                            }
                        }
                    }

                    foreach (var wpfMeshGeometry3D in styleMeshSets.Values)
                    {
                        wpfMeshGeometry3D.EndUpdate();
                    }
                    //}
                    if (tmpOpaquesGroup.Children.Any())
                    {
                        var mv = new ModelVisual3D();
                        mv.Content = tmpOpaquesGroup;
                        opaqueShapes.Children.Add(mv);
                        // Control.ModelBounds = mv.Content.Bounds.ToXbimRect3D();
                    }
                    if (tmpTransparentsGroup.Children.Any())
                    {
                        var mv = new ModelVisual3D {Content = tmpTransparentsGroup};
                        transparentShapes.Children.Add(mv);
                        //if (Control.ModelBounds.IsEmpty) Control.ModelBounds = mv.Content.Bounds.ToXbimRect3D();
                        //else Control.ModelBounds.Union(mv.Content.Bounds.ToXbimRect3D());
                    }
                }
            }
            return scene;
        }


        public void SetFederationEnvironment(IReferencedModel refModel)
        {
            
        }
    }
}
