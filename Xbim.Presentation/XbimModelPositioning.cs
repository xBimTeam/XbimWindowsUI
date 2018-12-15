using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Common;
using Xbim.Common.Geometry;
using Xbim.ModelGeometry.Scene;

namespace Xbim.Presentation
{
    public class XbimModelPositioning
    {
        public XbimRegion SelectedRegion { get; private set; }

        internal XbimRect3D SelectedRegionInMeters
        {
            get
            {
                if (SelectedRegion == null)
                    return XbimRect3D.Empty;
                var pts = MinMaxPoints(SelectedRegion, OneMeter);
                return new XbimRect3D(pts[0], pts[1]);
            }
        }

        public XbimMatrix3D Transform { get; private set; }

        private double _oneMeter = double.NaN;

        private double OneMeter
        {
            get
            {
                if (double.IsNaN(_oneMeter))
                {
                    _oneMeter = _model.ModelFactors.OneMetre;
                }
                return _oneMeter;
            }
        }
        
        private XbimPoint3D[] MinMaxPoints(XbimRegion rect, double oneMeter =  1.0)
        {
            var pMin = new XbimPoint3D(
                (rect.Centre.X - (rect.Size.X / 2)) / oneMeter,
                (rect.Centre.Y - (rect.Size.Y / 2)) / oneMeter,
                (rect.Centre.Z - (rect.Size.Z / 2)) / oneMeter
                );

            var pMax = new XbimPoint3D(
                (rect.Centre.X + (rect.Size.X / 2)) / oneMeter,
                (rect.Centre.Y + (rect.Size.Y / 2)) / oneMeter,
                (rect.Centre.Z + (rect.Size.Z / 2)) / oneMeter
                );

            return new[] { pMin, pMax };
        }

        private readonly IModel _model;


        /// <summary>
        /// Sets the region specified by name as selected.
        /// </summary>
        /// <param name="name">the region name to match</param>
        /// <param name="add"></param>
        /// <returns>true if the region has ben found and set, false otherwise</returns>
        public bool SetSelectedRegionByName(string name, bool add)
        {
            var geomStore = _model.GeometryStore;
            if (_model.GeometryStore.IsEmpty)
                return false;
            using (var reader = geomStore.BeginRead())
            {
                foreach (var readerContextRegion in reader.ContextRegions)
                {
                    if (!readerContextRegion.Any())
                        continue;
                    var reg = readerContextRegion.FirstOrDefault(x => x.Name == name);
                    if (reg == null)
                        continue;

                    if (!add)
                        SelectedRegion = reg;
                    else
                    {
                        SelectedRegion = Merge(SelectedRegion, reg);
                    }
                    return true;
                }
            }
            return false;
        }

        private XbimRegion Merge(XbimRegion reg1, XbimRegion reg2)
        {
            // todo: needs to review the merge function to consider region's WorldCoordinateSystem
            //
            var s1 = MinMaxPoints(reg1);
            var s2 = MinMaxPoints(reg2);
            var r1 = new XbimRect3D(s1[0], s1[1]);
            var r2 = new XbimRect3D(s2[0], s2[1]);
            r1.Union(r2);
            var merged = new XbimRegion(
                "Merged", 
                r1,
                reg1.Population + reg2.Population,
                reg1.WorldCoordinateSystem
                );
            return merged;
        }

        /// <summary>
        /// Initialises the position class and 
        /// </summary>
        /// <param name="model"></param>
        public XbimModelPositioning(IModel model)
        {
            _model = model;
            var geomStore = model.GeometryStore;
            if (_model.GeometryStore.IsEmpty)
                return;
            var gc = new Xbim3DModelContext(_model);
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
                    return;
                // look at expanding the region to any other that might be visible in the viewspace
                //
                var threshold = 5;
                var selectedRad = rect.Radius() * threshold;
                var testOtherRegions = true;
                while (testOtherRegions)
                {
                    testOtherRegions = false;
                    foreach (var contextRegion in reader.ContextRegions)
                    {
                        foreach (var otherRegion in contextRegion.Where(x => !mergedRegions.Contains(x)))
                        {
                            var otherRad = otherRegion.Size.Length/2;
                            var centreDistance = GetDistance(otherRegion.Centre, rect.Centroid());
                            if (otherRad + selectedRad > centreDistance)
                            {
                                mergedRegions.Add(otherRegion);
                                pop += otherRegion.Population;
                                rect.Union(otherRegion.ToXbimRect3D());
                                testOtherRegions = true;
                                selectedRad = rect.Radius() * threshold;
                            }
                        }
                    }
                }
                // todo: the identity matrix should be replaced with a correct model matrix.
                //
                SelectedRegion = new XbimRegion(name, rect, pop, XbimMatrix3D.Identity);
            }
        }

        // todo: create distance function in XbimPoint3D?
        private double GetDistance(XbimPoint3D point1, XbimPoint3D point2)
        {
            return Math.Sqrt(
                Math.Pow(point1.X - point2.X, 2) +
                Math.Pow(point1.Y - point2.Y, 2) +
                Math.Pow(point1.Z - point2.Z, 2)
            );
        }

        /// <summary>
        /// Creates the transform based on the model dimensional unit (oneMeter)
        /// </summary>
        /// <param name="modelTranslation">The translation is expressed in meters.</param>
        internal void PrepareTransform(XbimVector3D modelTranslation)
        {
            var translation = XbimMatrix3D.CreateTranslation(modelTranslation * OneMeter);
            var scaling = XbimMatrix3D.CreateScale(1/OneMeter);
            Transform =  translation * scaling;
        }
    }
}
