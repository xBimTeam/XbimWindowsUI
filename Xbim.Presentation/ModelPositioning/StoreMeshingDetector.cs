using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Common;
using Xbim.Common.Geometry;
using Xbim.Geometry.Engine.Interop;
using Xbim.Ifc4.Interfaces;
using Xbim.ModelGeometry.Scene;
using Xbim.Tessellator;

namespace Xbim.Presentation.Modelpositioning
{
    /// <summary>
    /// Xbim models can be meshed with adjustWcs true or false
    /// This class helps determines what mode was used and provides information to consider that 
    /// when positioning models, e.g. relative to each other in federations.
    /// 
    /// Eventually, it should be placed in a dll and namespace that all visualisation toolkits can use.
    /// For the time being there are redundant copies in WindowsUI and in the fast OSG viewer.
    /// </summary>
    public class StoreMeshingModeDetector
    {
        IModel _model;

        private static readonly ILog Log = LogManager.GetLogger("Xbim.Presentation.Modelpositioning.StoreMeshingModeDetector");

        public StoreMeshingModeDetector(IModel model)
        {
            _model = model;
        }

        private bool _attempted = false;
        private XbimMatrix3D _wcsMatrix = XbimMatrix3D.Identity;

        public enum WcsAdjustmentStatusEnum
        {
            Adjusted,
            NonAdjusted,
            Error
        }

        public XbimMatrix3D WcsMatrix
        {
            get
            {
                if (!_attempted)
                    AttemptResolution();
                return _wcsMatrix;
            }
        }

        private XbimGeometryEngine _engine;

        private XbimGeometryEngine Engine
        {
            get { return _engine ?? (_engine = new XbimGeometryEngine()); }
        }

        private void AttemptResolution()
        {
            _attempted = true;
            var placementTree = new XbimPlacementTree(_model, false);
            using (var geomStore = _model.GeometryStore)
            using (var geomReader = geomStore.BeginRead())
            {

                foreach (var instance in geomReader.ShapeInstances)
                {
                    var product = _model.Instances[instance.IfcProductLabel] as IIfcProduct;
                    if (product == null)
                        continue;
                    var placement = product.ObjectPlacement as IIfcLocalPlacement;
                    if (placement == null)
                        continue;
                    var nonAdjustedTransform = placementTree[product.ObjectPlacement.EntityLabel];
                    var storedTransform = instance.Transformation;

                    IXbimShapeGeometryData shapegeom = geomReader.ShapeGeometry(instance.ShapeGeometryLabel);
                    if (shapegeom.Format != (byte)XbimGeometryType.PolyhedronBinary)
                        continue;
                    var shapeEntityLabel = shapegeom.IfcShapeLabel;
                    IIfcGeometricRepresentationItem repItem = _model.Instances[shapeEntityLabel] as IIfcGeometricRepresentationItem;
                    if (repItem == null)
                        continue;


                    // some geometries are not meshed by engine, but by essential's tessellator; 
                    // todo: there should be a simplified entry for this, in geometry
                    //
                    var xbimTessellator = new XbimTessellator(_model, XbimGeometryType.PolyhedronBinary);
                    xbimTessellator.MoveMinToOrigin = true;
                    XbimShapeGeometry shapeGeom = null;
                    if (xbimTessellator.CanMesh(repItem))
                    {
                        shapeGeom = xbimTessellator.Mesh(repItem);
                    }
                    else
                    {
                        var recreated = Engine.Create(repItem);
                        shapeGeom = Engine.CreateShapeGeometry(recreated, _model.ModelFactors.Precision, _model.ModelFactors.DeflectionTolerance, _model.ModelFactors.DeflectionAngle, XbimGeometryType.PolyhedronBinary);
                    }

                    var shapeOffset = shapeGeom.TempOriginDisplacement;
                    var adjustedForShape = XbimMatrix3D.Multiply(
                               XbimMatrix3D.CreateTranslation((XbimVector3D)shapeOffset),
                               nonAdjustedTransform);
                    
                    if (XbimMatrix3D.Equal(storedTransform, adjustedForShape))
                    {
                        _WcsAdjustmentStatus = WcsAdjustmentStatusEnum.NonAdjusted;
                    }
                    else
                    {
                        _WcsAdjustmentStatus = WcsAdjustmentStatusEnum.Adjusted;
                        try
                        {
                            _wcsMatrix = placementTree.RootNodes[0].Matrix;
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Error in StoreMeshingMode detection.", ex);
                        }
                    }
                    return;
                }
            }
        }

        private WcsAdjustmentStatusEnum _WcsAdjustmentStatus = WcsAdjustmentStatusEnum.Error;

        public WcsAdjustmentStatusEnum WcsAdjustmentStatus
        {
            get
            {
                if (!_attempted)
                    AttemptResolution();
                return _WcsAdjustmentStatus;
            }
        }
    }
}
