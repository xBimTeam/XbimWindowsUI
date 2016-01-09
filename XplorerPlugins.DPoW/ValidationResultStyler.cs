using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;
using Xbim.Common.Federation;
using Xbim.Common.Geometry;
using Xbim.Ifc;
using Xbim.Ifc2x3.IO;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.ModelGeometry.Scene;
using Xbim.Presentation;
using Xbim.Presentation.LayerStylingV2;

namespace XplorerPlugins.DPoW
{
    class ValidationResultStyler : ILayerStylerV2
    {
        private static WpfMeshGeometry3D PrepareMesh(XbimColour col)
        {
            var matRed = new WpfMaterial();
            matRed.CreateMaterial(col);
            var mg = new WpfMeshGeometry3D(matRed, matRed);
            return mg;
        }

        public XbimScene<WpfMeshGeometry3D, WpfMaterial> BuildScene(XbimModel model, Xbim3DModelContext context, List<Type> exclude = null)
        {
            var tmpOpaquesGroup = new Model3DGroup();

            var retScene = new XbimScene<WpfMeshGeometry3D, WpfMaterial>(model);
            
            var red = PrepareMesh(new XbimColour("Red", 1.0, 0.0, 0.0));
            var green = PrepareMesh(new XbimColour("Green", 0.0, 1.0, 0.0));
            var amber =PrepareMesh(new XbimColour("Amber", 0.0, 0.0, 1.0, 0.9));
            tmpOpaquesGroup.Children.Add(red);
            tmpOpaquesGroup.Children.Add(green);
            tmpOpaquesGroup.Children.Add(amber);


            var i = 0;

            foreach (var shapeInstance in context.ShapeInstances()
                .Where(s => s.RepresentationType == XbimGeometryRepresentationType.OpeningsAndAdditionsIncluded &&
                            !typeof (IfcFeatureElement).IsAssignableFrom(model.Metadata.GetType(s.IfcTypeId)) /*&&
                        !typeof(IfcSpace).IsAssignableFrom(IfcMetaData.GetType(s.IfcTypeId))*/))
            {
                IXbimShapeGeometryData shapeGeom = context.ShapeGeometry(shapeInstance.ShapeGeometryLabel);
                
                WpfMeshGeometry3D targetMergeMesh;
                switch (i++%3)
                {
                    case 0:
                        targetMergeMesh = red;
                        break;
                    case 1:
                        targetMergeMesh = green;
                        break;
                    default:
                        targetMergeMesh = amber;
                        break;
                }

                switch ((XbimGeometryType)shapeGeom.Format)
                {
                    case XbimGeometryType.Polyhedron:
                        var shapePoly = (XbimShapeGeometry)shapeGeom;
                        targetMergeMesh.Add(
                   shapePoly.ShapeData,
                   shapeInstance.IfcTypeId,
                   shapeInstance.IfcProductLabel,
                   shapeInstance.InstanceLabel,
                   XbimMatrix3D.Multiply(shapeInstance.Transformation, Control.ModelPositions[model].Transfrom),
                   (short)model.UserDefinedId
                   );
                        break;

                    case XbimGeometryType.PolyhedronBinary:
                        targetMergeMesh.Add(
                  shapeGeom.ShapeData,
                  shapeInstance.IfcTypeId,
                  shapeInstance.IfcProductLabel,
                  shapeInstance.InstanceLabel,
                  XbimMatrix3D.Multiply(shapeInstance.Transformation, Control.ModelPositions[model].Transfrom),
                  (short)model.UserDefinedId);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

               
                
            }

            var mv = new ModelVisual3D {Content = tmpOpaquesGroup};
            Control.OpaquesVisual3D.Children.Add(mv);
            Control.ModelBounds = mv.Content.Bounds.ToXbimRect3D();

            return retScene;
        }

        public DrawingControl3D Control { get; set; }
        public void SetFederationEnvironment(IReferencedModel refModel)
        {
            
        }


    }
}
