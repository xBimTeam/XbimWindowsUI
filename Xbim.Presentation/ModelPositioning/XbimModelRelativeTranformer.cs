using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xbim.Common;
using Xbim.Common.Geometry;

namespace Xbim.Presentation.Modelpositioning
{
    /// <summary>
    /// This class determines the relative positioning of models (e.g. for federation visualisation porposes)
    /// Eventually, it should be placed in a dll and namespace that all visualisation toolkits can use.
    /// For the time being there are redundant copies in WindowsUI and in the fast OSG viewer.
    /// </summary>
    public class XbimModelRelativeTranformer
    {
        IModel _baseModel;

        XbimPoint3D _firstModelCentralCoordinates;
        XbimMatrix3D _firstModelWcsAdjustment;

        // the idea of the class determines the centering on the first model to be 
        // central to coordinate system of the viewer.
        // The initial region can be determined by different modes.
        // then each other model relative to that.

        public XbimMatrix3D SetBaseModel(IModel model, XbimRegion startRegion)
        {
            _baseModel = model;
            _firstModelCentralCoordinates = new XbimPoint3D(0, 0, 0);

            if (startRegion != null)
            {
                _firstModelCentralCoordinates = new XbimPoint3D(
                    startRegion.Centre.X,
                    startRegion.Centre.Y,
                    startRegion.Centre.Z - startRegion.Size.Z / 2); // the z is arranged so that the bottom of the model is placed at 0 height
            }

            // for wcsAdjusted models centralcoords is likely going to be small or zero, 
            // while it could be large for non adjusted
            //
            // adapt to meters
            _firstModelCentralCoordinates = new XbimPoint3D(
                _firstModelCentralCoordinates.X / model.ModelFactors.OneMeter,
                _firstModelCentralCoordinates.Y / model.ModelFactors.OneMeter,
                _firstModelCentralCoordinates.Z / model.ModelFactors.OneMeter
                );
            
            // centralCoordinats works to center the first model, but then subsequent models need to be 
            // centered in consideration of the the conceptual location of such model in absolute World coordinates
            // If all models have adjustedWcs = false then the same matrix should work, but otherwise we need to know how to adapt
            var dtctor = new StoreMeshingModeDetector(model);
            _firstModelWcsAdjustment = dtctor.WcsMatrix;

            // the offset needs to be adapted to meters
            _firstModelWcsAdjustment.OffsetX /= model.ModelFactors.OneMeter;
            _firstModelWcsAdjustment.OffsetY /= model.ModelFactors.OneMeter;
            _firstModelWcsAdjustment.OffsetZ /= model.ModelFactors.OneMeter;
            _firstModelWcsAdjustment.Invert();
            //Debug.WriteLine("_firstModelWcsAdjustment: " + _firstModelWcsAdjustment);


            var modelTranslation =  XbimMatrix3D.CreateTranslation(
                -_firstModelCentralCoordinates.X,
                -_firstModelCentralCoordinates.Y,
                -_firstModelCentralCoordinates.Z
                );

            var modelScale = XbimMatrix3D.CreateScale(1 / model.ModelFactors.OneMeter);

            //Debug.WriteLine("_firstModelCentralCoordinates: " + _firstModelCentralCoordinates);
            var ret = XbimMatrix3D.Multiply(modelScale, modelTranslation);
            // ret = XbimMatrix3D.Multiply(modelTranslation, modelScale);

            //Debug.WriteLine($"BaseModelWorld: {_firstModelWcsAdjustment}");
            //Debug.WriteLine($"BaseModelMatrix: {ret}");
            return ret;
        }

        public XbimMatrix3D GetRelativeMatrix(IModel forModel)
        {
            // get the absolute of this model against the world
            // then compute the relative
            //
            var thisScale = XbimMatrix3D.CreateScale(1 / forModel.ModelFactors.OneMeter);
            var dtctor = new StoreMeshingModeDetector(forModel);

            var w = dtctor.WcsMatrix;
            w.OffsetX /= forModel.ModelFactors.OneMeter;
            w.OffsetY /= forModel.ModelFactors.OneMeter;
            w.OffsetZ /= forModel.ModelFactors.OneMeter;

            var tmp = XbimMatrix3D.Multiply(thisScale, w);

            var transAdj = XbimMatrix3D.CreateTranslation(
               _firstModelCentralCoordinates.X,
               _firstModelCentralCoordinates.Y,
               _firstModelCentralCoordinates.Z
               );
            transAdj.Invert();
   
            tmp = XbimMatrix3D.Multiply(tmp, _firstModelWcsAdjustment);
            tmp = XbimMatrix3D.Multiply(tmp, transAdj);

            //Debug.WriteLine($"RelativelWorld: {dtctor.WcsMatrix}");
            //Debug.WriteLine($"BaseModelMatrix: {tmp}");
            return tmp;
        }

        public XbimMatrix3D GetAbsoluteMatrix()
        {
            XbimMatrix3D tmp = XbimMatrix3D.Identity;
            var transAdj = XbimMatrix3D.CreateTranslation(
               _firstModelCentralCoordinates.X,
               _firstModelCentralCoordinates.Y,
               _firstModelCentralCoordinates.Z
               );
            transAdj.Invert();
            tmp = XbimMatrix3D.Multiply(tmp, _firstModelWcsAdjustment);
            tmp = XbimMatrix3D.Multiply(tmp, transAdj);
            return tmp;
        }


        static private double GetDistance(XbimPoint3D point1, XbimPoint3D point2)
        {
            return Math.Sqrt(
                Math.Pow(point1.X - point2.X, 2) +
                Math.Pow(point1.Y - point2.Y, 2) +
                Math.Pow(point1.Z - point2.Z, 2)
            );
        }

        static public XbimRegion GetExpandedMostPopulated(IModel model, double thresholdFromMostPopulated)
        {
            var _model = model;
            var geomStore = model.GeometryStore;
            if (_model.GeometryStore.IsEmpty)
                return null;
            
            using (var reader = geomStore.BeginRead())
            {
                // ContextRegions is a collection of ContextRegions, which is also a collection.
                // we get the most populated from each
                var name = "MostPopulated";
                var regions = reader.ContextRegions.Where(cr => cr.MostPopulated() != null).Select(c => c.MostPopulated());
                var rect = XbimRect3D.Empty;
                var pop = 0;
                var mergedRegions = new List<XbimRegion>();
                // then perform their union
                foreach (var r in regions)
                {
                    mergedRegions.Add(r);
                    pop += r.Population;
                    if (rect.IsEmpty)
                    {
                        rect = r.ToXbimRect3D();
                        name = r.Name;
                    }
                    else
                    {
                        rect.Union(r.ToXbimRect3D());
                    }
                }

                if (pop <= 0)
                    return null;
                // look at expanding the region to any other that might be visible in the viewspace
                //
                var selectedRad = rect.Radius() * thresholdFromMostPopulated;
                var testOtherRegions = true;
                while (testOtherRegions)
                {
                    testOtherRegions = false;
                    foreach (var contextRegion in reader.ContextRegions)
                    {
                        foreach (var otherRegion in contextRegion.Where(x => !mergedRegions.Contains(x)))
                        {
                            var otherRad = otherRegion.Size.Length / 2;
                            var centreDistance = GetDistance(otherRegion.Centre, rect.Centroid());
                            if (otherRegion.Population > 50)
                                otherRad *= otherRegion.Population / 50;
                            if (otherRad + selectedRad > centreDistance)
                            {
                                mergedRegions.Add(otherRegion);
                                pop += otherRegion.Population;
                                rect.Union(otherRegion.ToXbimRect3D());
                                testOtherRegions = true;
                                selectedRad = rect.Radius() * thresholdFromMostPopulated;
                            }
                        }
                    }
                }
                var SelectedRegion = new XbimRegion(name, rect, pop, XbimMatrix3D.Identity);
                return SelectedRegion;
            }
        }
    }
}
