using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Xbim.Common.Geometry;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using System.Windows;
using Xbim.Ifc2x3.ProductExtension;

namespace Xbim.Presentation.LayerStylingV2
{
    public class SurfeceLayerStyler : ILayerStylerV2
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

            var scene = new XbimScene<WpfMeshGeometry3D, WpfMaterial>(model);

            if (context == null)
                return scene;

            //get a list of all the unique styles
            var styles = new Dictionary<int, WpfMaterial>();
            var shapeGeometries = new Dictionary<int, MeshGeometry3D>();
            var meshSets = new Dictionary<int, WpfMeshGeometry3D>();
            var opaques = new Model3DGroup();
            var transparents = new Model3DGroup();

            var sstyles = context.SurfaceStyles();
            if (sstyles == null)
                return scene;

            foreach (var style in sstyles)
            {
                var wpfMaterial = new WpfMaterial();
                wpfMaterial.CreateMaterial(style);
                styles.Add(style.DefinedObjectId, wpfMaterial);
                var mg = new WpfMeshGeometry3D(wpfMaterial, wpfMaterial);
                mg.WpfModel.SetValue(FrameworkElement.TagProperty, mg);
                meshSets.Add(style.DefinedObjectId, mg);
                if (style.IsTransparent)
                    transparents.Children.Add(mg);
                else
                    opaques.Children.Add(mg);
            }


            if (!styles.Any()) return scene; //this should always return something
            double metre = model.ModelFactors.OneMetre;
            Control.wcsTransform = XbimMatrix3D.CreateTranslation(Control._modelTranslation) * XbimMatrix3D.CreateScale(1 / metre);

            foreach (var shapeInstance in context.ShapeInstances()
                .Where(s => s.RepresentationType == XbimGeometryRepresentationType.OpeningsAndAdditionsIncluded &&
                            !typeof(IfcFeatureElement).IsAssignableFrom(IfcMetaData.GetType(s.IfcTypeId)) /*&&
                        !typeof(IfcSpace).IsAssignableFrom(IfcMetaData.GetType(s.IfcTypeId))*/))
            {
                var styleId = shapeInstance.StyleLabel > 0 ? shapeInstance.StyleLabel : shapeInstance.IfcTypeId * -1;

                //GET THE ACTUAL GEOMETRY 
                MeshGeometry3D wpfMesh;
                //see if we have already read it
                if (shapeGeometries.TryGetValue(shapeInstance.ShapeGeometryLabel, out wpfMesh))
                {
                    GeometryModel3D mg = new GeometryModel3D(wpfMesh, styles[styleId]);
                    mg.SetValue(FrameworkElement.TagProperty,
                        new XbimInstanceHandle(model, shapeInstance.IfcProductLabel, shapeInstance.IfcTypeId));
                    mg.BackMaterial = mg.Material;
                    mg.Transform =
                        XbimMatrix3D.Multiply(shapeInstance.Transformation, Control.wcsTransform).ToMatrixTransform3D();
                    if (styles[styleId].IsTransparent)
                        transparents.Children.Add(mg);
                    else
                        opaques.Children.Add(mg);
                }
                else //we need to get the shape geometry
                {

                    XbimShapeGeometry shapeGeom = context.ShapeGeometry(shapeInstance.ShapeGeometryLabel);
                    if (shapeGeom.ReferenceCount > 1) //only store if we are going to use again
                    {
                        wpfMesh = new MeshGeometry3D();
                        wpfMesh.Read(shapeGeom.ShapeData);
                        shapeGeometries.Add(shapeInstance.ShapeGeometryLabel, wpfMesh);
                        GeometryModel3D mg = new GeometryModel3D(wpfMesh, styles[styleId]);
                        mg.SetValue(FrameworkElement.TagProperty,
                            new XbimInstanceHandle(model, shapeInstance.IfcProductLabel, shapeInstance.IfcTypeId));
                        mg.BackMaterial = mg.Material;
                        mg.Transform =
                            XbimMatrix3D.Multiply(shapeInstance.Transformation, Control.wcsTransform).ToMatrixTransform3D();
                        if (styles[styleId].IsTransparent)
                            transparents.Children.Add(mg);
                        else
                            opaques.Children.Add(mg);
                    }
                    else //it is a one off, merge it with shapes of a similar material
                    {
                        WpfMeshGeometry3D mg = meshSets[styleId];
                        mg.Add(shapeGeom.ShapeData,
                            shapeInstance.IfcTypeId,
                            shapeInstance.IfcProductLabel,
                            shapeInstance.InstanceLabel,
                            XbimMatrix3D.Multiply(shapeInstance.Transformation, Control.wcsTransform), 0);
                    }
                }
            }

            //}
            if (opaques.Children.Any())
            {
                ModelVisual3D mv = new ModelVisual3D();
                mv.Content = opaques;
                Control.Opaques.Children.Add(mv);
                Control.ModelBounds = mv.Content.Bounds.ToXbimRect3D();
            }
            if (transparents.Children.Any())
            {
                ModelVisual3D mv = new ModelVisual3D();
                mv.Content = transparents;
                Control.Transparents.Children.Add(mv);
                if (Control.ModelBounds.IsEmpty) Control.ModelBounds = mv.Content.Bounds.ToXbimRect3D();
                else Control.ModelBounds.Union(mv.Content.Bounds.ToXbimRect3D());
            }
            return scene;
        }




        public DrawingControl3D Control { get; set; }


        public void SetFederationEnvironment(XbimReferencedModel refModel)
        {
            
        }
    }
}
