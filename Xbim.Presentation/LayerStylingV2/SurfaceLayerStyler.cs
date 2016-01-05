using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Media3D;
using Xbim.Common.Federation;
using Xbim.Common.Geometry;
using Xbim.Ifc;
using Xbim.Ifc2x3.IO;
using Xbim.Ifc2x3.PresentationAppearanceResource;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.IO.Esent;
using Xbim.ModelGeometry.Scene;

namespace Xbim.Presentation.LayerStylingV2
{
    public class SurfaceLayerStyler : ILayerStylerV2
    {

        /// <summary>
        /// This version uses the new Geometry representation
        /// </summary>
        /// <param name="model"></param>
        /// <param name="context"></param>
        /// <param name="exclude">List of type to exclude, by default excplict openings and spaces are excluded if exclude = null</param>
        /// <returns></returns>
        public XbimScene<WpfMeshGeometry3D, WpfMaterial> BuildScene(XbimModel model, Xbim3DModelContext context,
            List<Type> exclude = null)
        {
            var excludedTypes = new HashSet<short>();
            if (exclude == null)
                exclude = new List<Type>()
                {
                    typeof(IfcSpace)
                    // , typeof(IfcFeatureElement)
                };
            foreach (var excludedT in exclude)
            {
                var ifcT = model.Metadata.ExpressType(excludedT);
                foreach (var exIfcType in ifcT.NonAbstractSubTypes.Select(model.Metadata.ExpressType))
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
                    var colourMap = new XbimColourMap();
                    foreach (var style in sstyles)
                    {
                        XbimTexture texture;
                        if (style > 0) //get the ifc style and build it
                        {
                            var sStyle = model.Instances[style] as IfcSurfaceStyle;
                            texture = XbimTexture.Create(sStyle);
                        }
                        else
                        {
                            var prodType = model.Metadata.ExpressType((short) Math.Abs(style));
                            var v = colourMap[prodType.Name];
                            texture = XbimTexture.Create(v);
                        }
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

                    if (!styles.Any()) return scene; //this should always return something
                    var shapeInstances = context.ShapeInstances()
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
                                    Control.ModelPositions[model].Transfrom)
                                    .ToMatrixTransform3D();
                            if (styles[styleId].IsTransparent)
                                tmpTransparentsGroup.Children.Add(mg);
                            else
                                tmpOpaquesGroup.Children.Add(mg);
                        }
                        else //we need to get the shape geometry
                        {
                            IXbimShapeGeometryData shapeGeom = context.ShapeGeometry(shapeInstance.ShapeGeometryLabel);

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
                                        Control.ModelPositions[model].Transfrom)
                                        .ToMatrixTransform3D();
                                if (styles[styleId].IsTransparent)
                                    tmpTransparentsGroup.Children.Add(mg);
                                else
                                    tmpOpaquesGroup.Children.Add(mg);
                            }
                            else //it is a one off, merge it with shapes of a similar material
                            {
                                var targetMergeMeshByStyle = styleMeshSets[styleId];

                                switch ((XbimGeometryType) shapeGeom.Format)
                                {
                                    case XbimGeometryType.Polyhedron:
                                        // var shapePoly = (XbimShapeGeometry)shapeGeom;
                                        var asString = Encoding.UTF8.GetString(shapeGeom.ShapeData.ToArray());
                                        targetMergeMeshByStyle.Add(
                                            asString,
                                            shapeInstance.IfcTypeId,
                                            shapeInstance.IfcProductLabel,
                                            shapeInstance.InstanceLabel,
                                            XbimMatrix3D.Multiply(shapeInstance.Transformation,
                                                Control.ModelPositions[model].Transfrom),
                                           (short) model.UserDefinedId);
                                        break;

                                    case XbimGeometryType.PolyhedronBinary:
                                        var transform = XbimMatrix3D.Multiply(shapeInstance.Transformation,
                                            Control.ModelPositions[model].Transfrom);
                                        targetMergeMeshByStyle.Add(
                                            shapeGeom.ShapeData,
                                            shapeInstance.IfcTypeId,
                                            shapeInstance.IfcProductLabel,
                                            shapeInstance.InstanceLabel, transform,
                                           (short)model.UserDefinedId);
                                        break;
                                    default:
                                        throw new ArgumentOutOfRangeException();
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
                        Control.OpaquesVisual3D.Children.Add(mv);
                        // Control.ModelBounds = mv.Content.Bounds.ToXbimRect3D();
                    }
                    if (tmpTransparentsGroup.Children.Any())
                    {
                        var mv = new ModelVisual3D();
                        mv.Content = tmpTransparentsGroup;
                        Control.TransparentsVisual3D.Children.Add(mv);
                        //if (Control.ModelBounds.IsEmpty) Control.ModelBounds = mv.Content.Bounds.ToXbimRect3D();
                        //else Control.ModelBounds.Union(mv.Content.Bounds.ToXbimRect3D());
                    }
                }
            }
            return scene;
        }




        public DrawingControl3D Control { get; set; }


        public void SetFederationEnvironment(IReferencedModel refModel)
        {
            
        }
    }
}
