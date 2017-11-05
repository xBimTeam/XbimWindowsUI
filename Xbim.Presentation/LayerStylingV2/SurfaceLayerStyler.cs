using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Media3D;
using Xbim.Common.Geometry;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using XbimGeometry.Interfaces;

namespace Xbim.Presentation.LayerStylingV2
{
    public class SurfaceLayerStyler : ILayerStylerV2
    {
        /// <summary>
        /// Looking into efficiency of WPF loading loops it looks like the styler is much more efficient when it ignores the instancing of maps 
        /// and merges new geometries in the large meshes instead. 
        /// </summary>
        public bool UseMaps = false;

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
                var ifcT = IfcMetaData.IfcType(excludedT);
                foreach (var exIfcType in ifcT.NonAbstractSubTypes.Select(IfcMetaData.IfcType))
                {
                    excludedTypes.Add(exIfcType.TypeId);
                }
            }

            var scene = new XbimScene<WpfMeshGeometry3D, WpfMaterial>(model);

            if (context == null)
                return scene;

            //get a list of all the unique styles
            var materialsByStyleId = new Dictionary<int, WpfMaterial>();
            var repeatedShapeGeometries = new Dictionary<int, MeshGeometry3D>();
            var meshesByStyleId = new Dictionary<int, WpfMeshGeometry3D>();
            var tmpOpaquesGroup = new Model3DGroup();
            var tmpTransparentsGroup = new Model3DGroup();

            // get a list of all the unique style ids then build their style and mesh
            var sstyles = context.SurfaceStyles();
            foreach (var style in sstyles)
            {
                WpfMaterial wpfMaterial = GetWpfMaterial(style);
                materialsByStyleId.Add(style.DefinedObjectId, wpfMaterial);

                WpfMeshGeometry3D mg = GetNewStyleMesh(wpfMaterial, tmpOpaquesGroup, tmpTransparentsGroup);
                meshesByStyleId.Add(style.DefinedObjectId, mg);
            }

            if (!materialsByStyleId.Any()) return scene; //this should always return something
            var shapeInstances = context.ShapeInstances()
                .Where(s => s.RepresentationType == XbimGeometryRepresentationType.OpeningsAndAdditionsIncluded
                            &&
                            !excludedTypes.Contains(s.IfcTypeId));
                            // !typeof (IfcFeatureElement).IsAssignableFrom(IfcMetaData.GetType(s.IfcTypeId)) /*&&
                            // !typeof(IfcSpace).IsAssignableFrom(IfcMetaData.GetType(s.IfcTypeId))*/);
            foreach (var shapeInstance in shapeInstances)
            {
                
                var styleId = shapeInstance.StyleLabel > 0 ? shapeInstance.StyleLabel : shapeInstance.IfcTypeId * -1;

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
                        XbimMatrix3D.Multiply(shapeInstance.Transformation, Control.ModelPositions[model].Transfrom).ToMatrixTransform3D();
                    if (materialsByStyleId[styleId].IsTransparent)
                        tmpTransparentsGroup.Children.Add(mg);
                    else
                        tmpOpaquesGroup.Children.Add(mg);
                }
                else //we need to get the shape geometry
                {
                    IXbimShapeGeometryData shapeGeom = context.ShapeGeometry(shapeInstance.ShapeGeometryLabel);

                    if (UseMaps && shapeGeom.ReferenceCount > 1) //only store if we are going to use again
                    {
                        wpfMesh = new MeshGeometry3D();
                        switch ((XbimGeometryType)shapeGeom.Format)
                        {
                            case XbimGeometryType.PolyhedronBinary:
                                wpfMesh.Read(shapeGeom.ShapeData);
                                break;
                            case XbimGeometryType.Polyhedron:
                                wpfMesh.Read(((XbimShapeGeometry)shapeGeom).ShapeData);
                                break;
                        }
                       
                        repeatedShapeGeometries.Add(shapeInstance.ShapeGeometryLabel, wpfMesh);
                        var mg = new GeometryModel3D(wpfMesh, materialsByStyleId[styleId]);
                        mg.SetValue(FrameworkElement.TagProperty,
                            new XbimInstanceHandle(model, shapeInstance.IfcProductLabel, shapeInstance.IfcTypeId));
                        mg.BackMaterial = mg.Material;
                        mg.Transform =
                            XbimMatrix3D.Multiply(shapeInstance.Transformation, Control.ModelPositions[model].Transfrom).ToMatrixTransform3D();
                        if (materialsByStyleId[styleId].IsTransparent)
                            tmpTransparentsGroup.Children.Add(mg);
                        else
                            tmpOpaquesGroup.Children.Add(mg);
                    }
                    else //it is a one off, merge it with shapes of a similar material
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

                            var mat = materialsByStyleId[styleId];
                            var replace = GetNewStyleMesh(mat, tmpOpaquesGroup, tmpTransparentsGroup);
                            meshesByStyleId[styleId] = replace;
                            targetMergeMeshByStyle = replace;
                        }
                        // end replace

                        switch ((XbimGeometryType)shapeGeom.Format)
                        {
                            case XbimGeometryType.Polyhedron:
                                // var shapePoly = (XbimShapeGeometry)shapeGeom;
                                var asString = Encoding.UTF8.GetString(shapeGeom.ShapeData.ToArray());
                                targetMergeMeshByStyle.Add(
                                    asString,
                                    shapeInstance.IfcTypeId,
                                    shapeInstance.IfcProductLabel,
                                    shapeInstance.InstanceLabel,
                                    XbimMatrix3D.Multiply(shapeInstance.Transformation, Control.ModelPositions[model].Transfrom),
                                    model.UserDefinedId);
                                break;

                            case XbimGeometryType.PolyhedronBinary:
                                var transform = XbimMatrix3D.Multiply(shapeInstance.Transformation, Control.ModelPositions[model].Transfrom);
                                targetMergeMeshByStyle.Add(
                                    shapeGeom.ShapeData,
                                    shapeInstance.IfcTypeId,
                                    shapeInstance.IfcProductLabel,
                                    shapeInstance.InstanceLabel, transform,
                                    model.UserDefinedId);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
            }
            foreach (var wpfMeshGeometry3D in meshesByStyleId.Values)
            {
                wpfMeshGeometry3D.EndUpdate();
            }
            //}
            if (tmpOpaquesGroup.Children.Any())
            {
                var mv = new ModelVisual3D();
                mv.Content = tmpOpaquesGroup;
                Control.OpaquesVisual3D.Children.Add(mv);
            }
            if (tmpTransparentsGroup.Children.Any())
            {
                var mv = new ModelVisual3D();
                mv.Content = tmpTransparentsGroup;
                Control.TransparentsVisual3D.Children.Add(mv);
            }
            return scene;
        }

        private static WpfMeshGeometry3D GetNewStyleMesh(WpfMaterial wpfMaterial, Model3DGroup tmpOpaquesGroup, Model3DGroup tmpTransparentsGroup)
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

        private static WpfMaterial GetWpfMaterial(XbimTexture style)
        {
            var wpfMaterial = new WpfMaterial();
            wpfMaterial.CreateMaterial(style);
            return wpfMaterial;
        }

        public DrawingControl3D Control { get; set; }


        public void SetFederationEnvironment(XbimReferencedModel refModel)
        {
            
        }
    }
}
