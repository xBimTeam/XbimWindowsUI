using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;
using NPOI.SS.Formula.Functions;
using Xbim.Common.Geometry;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using Xbim.Presentation;
using Xbim.Presentation.LayerStylingV2;
using XbimGeometry.Interfaces;

namespace XbimXplorer.Plugins.DPoWValidation
{
    class ValidationResultStyler : ILayerStylerV2
    {
        private WpfMeshGeometry3D prepareMesh(XbimColour col)
        {
            var matRed = new WpfMaterial();
            matRed.CreateMaterial(col);
            var mg = new WpfMeshGeometry3D(matRed, matRed);
            return mg;
        }

        public XbimScene<WpfMeshGeometry3D, WpfMaterial> BuildScene(Xbim.IO.XbimModel model, Xbim3DModelContext context, List<Type> exclude = null)
        {
            var tmpOpaquesGroup = new Model3DGroup();

            var retScene = new XbimScene<WpfMeshGeometry3D, WpfMaterial>(model);
            
            var Red = prepareMesh(new XbimColour("Red", 1.0, 0.0, 0.0));
            var Green = prepareMesh(new XbimColour("Green", 0.0, 1.0, 0.0));
            var Amber =prepareMesh(new XbimColour("Amber", 0.0, 0.0, 1.0, 0.9));
            tmpOpaquesGroup.Children.Add(Red);
            tmpOpaquesGroup.Children.Add(Green);
            tmpOpaquesGroup.Children.Add(Amber);


            int i = 0;

            foreach (var shapeInstance in context.ShapeInstances()
                .Where(s => s.RepresentationType == XbimGeometryRepresentationType.OpeningsAndAdditionsIncluded &&
                            !typeof (IfcFeatureElement).IsAssignableFrom(IfcMetaData.GetType(s.IfcTypeId)) /*&&
                        !typeof(IfcSpace).IsAssignableFrom(IfcMetaData.GetType(s.IfcTypeId))*/))
            {
                IXbimShapeGeometryData shapeGeom = context.ShapeGeometry(shapeInstance.ShapeGeometryLabel);
                
                WpfMeshGeometry3D targetMergeMesh;
                switch (i++%3)
                {
                    case 0:
                        targetMergeMesh = Red;
                        break;
                    case 1:
                        targetMergeMesh = Green;
                        break;
                    default:
                        targetMergeMesh = Amber;
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
                   XbimMatrix3D.Multiply(shapeInstance.Transformation, Control.WcsTransform));
                        break;

                    case XbimGeometryType.PolyhedronBinary:
                        targetMergeMesh.Add(
                  shapeGeom.ShapeData,
                  shapeInstance.IfcTypeId,
                  shapeInstance.IfcProductLabel,
                  shapeInstance.InstanceLabel,
                  XbimMatrix3D.Multiply(shapeInstance.Transformation, Control.WcsTransform));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

               
                
            }

            var mv = new ModelVisual3D {Content = tmpOpaquesGroup};
            Control.Opaques.Children.Add(mv);
            Control.ModelBounds = mv.Content.Bounds.ToXbimRect3D();

            return retScene;
        }

        public Xbim.Presentation.DrawingControl3D Control { get; set; }
        

        public void SetFederationEnvironment(Xbim.IO.XbimReferencedModel refModel)
        {
            
        }
    }
}
