using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Common.Geometry;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using Xbim.XbimExtensions.Interfaces;
using XbimGeometry.Interfaces;

namespace Xbim.Presentation
{
    public class XbimModelPositioning
    {
        public XbimRegion SelectedRegion;
        public Xbim3DModelContext Context;
        // todo: rename this
        public XbimMatrix3D Transfrom;
        
        private double _OneMeter = Double.NaN;

        public double OneMeter
        {
            get
            {
                if (double.IsNaN(_OneMeter))
                {
                    _OneMeter = _model.ModelFactors.OneMetre;
                }
                return _OneMeter;
            }
        }

        public XbimRect3D GetLargestRegionRectInMeters()
        {
            if (SelectedRegion == null)
                return XbimRect3D.Empty;
            var pts = MinMaxPoints(SelectedRegion, OneMeter);
            return new XbimRect3D(pts[0], pts[1]);
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

        public XbimModelPositioning(XbimModel model)
        {
            _model = model;
            Context = new Xbim3DModelContext(model);
            var supportLevel = model.GeometrySupportLevel;
          
            switch (supportLevel)
            {
                case 1:
                    SelectedRegion = GetLargestRegion(model);
                    break;
                case 2:
                    // SelectedRegion = Context.GetLargestRegion();
                    SelectedRegion = GetView(Context.GetRegions());
                    break;
            }
        }

        private XbimRegion GetView(IEnumerable<XbimRegion> enumRegions)
        {
            var arrRegions = enumRegions.ToArray();
            var name = "";
            if (!arrRegions.Any())
                return null;
            var MaxPopulation = arrRegions.Max(r => r.Population);

            var mostPopulated = arrRegions.Where(cr => cr.Population == MaxPopulation);
            var rect = XbimRect3D.Empty;
            var pop = 0;
            var mergedRegions = new List<XbimRegion>();
            // then perform their union
            foreach (var r in mostPopulated)
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

            if (pop > 0)
            {
                // look at expandind the region to any othe that might be visible in the viewspace
                //
                var selectedRad = rect.Radius()*2;
                var testOtherRegions = true;
                while (testOtherRegions)
                {
                    testOtherRegions = false;

                    foreach (var otherRegion in arrRegions.Where(x => !mergedRegions.Contains(x)))
                    {
                        var otherRad = otherRegion.Size.Length/2;
                        var centreDistance = GetDistance(otherRegion.Centre, rect.Centroid());
                        if (otherRad + selectedRad > centreDistance)
                        {
                            mergedRegions.Add(otherRegion);
                            pop += otherRegion.Population;
                            rect.Union(otherRegion.ToXbimRect3D());
                            testOtherRegions = true;
                            selectedRad = rect.Radius()*2;
                        }
                    }

                }
            }
            return new XbimRegion(name, rect, pop);
        }

        private double GetDistance(XbimPoint3D point1, XbimPoint3D point2)
        {
            return Math.Sqrt(
                Math.Pow(point1.X - point2.X, 2) +
                Math.Pow(point1.Y - point2.Y, 2) +
                Math.Pow(point1.Z - point2.Z, 2)
            );
        }

        /// <summary>
        /// Works only on models version 1.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private static XbimRegion GetLargestRegion(XbimModel model)
        {
            //get the region data should only be one
            var project = model.IfcProject;
            var projectId = 0;
            if (project != null) projectId = project.EntityLabel;
            // in version 1.0 there should be only 1 record in the database for the project (storing multiple regions).
            var regionData = model.GetGeometryData(projectId, XbimGeometryType.Region).FirstOrDefault();

            if (regionData == null)
                return null;
            var regions = XbimRegionCollection.FromArray(regionData.ShapeData);
            return regions.MostPopulated(); // this then returns 
        }

        internal void SetCenterInMeters(XbimVector3D modelTranslation)
        {
            var translation = XbimMatrix3D.CreateTranslation(modelTranslation * OneMeter);
            var scaling = XbimMatrix3D.CreateScale(1/OneMeter);
            Transfrom =  translation * scaling;
        }
    }

    public class XbimModelPositioningCollection
    {
        public XbimModelPositioning this[IModel i]
        {
            get { return _collection[i]; }
            set { _collection[i] = value; }
        }

        private readonly Dictionary<IModel, XbimModelPositioning> _collection;

        public void AddModel(XbimModel model)
        {
            var tmp = new XbimModelPositioning(model);
            _collection.Add(model, tmp);
        }

        public XbimRect3D GetEnvelopeInMeters()
        {
            var bb = XbimRect3D.Empty;
            foreach (var r in _collection.Values.Select(positioning => positioning.GetLargestRegionRectInMeters()))
            {
                if (r.IsEmpty)
                    continue;
                if (bb.IsEmpty)
                    bb = r;
                else
                {
                    bb.Union(r);
                }
            }
            return bb;
        }

        public XbimRect3D GetEnvelopOfCentes()
        {
            var bb = XbimRect3D.Empty;
            foreach (var r in _collection.Values.Select(positioning => positioning.SelectedRegion).Where(r => r != null))
            {
                if (bb.IsEmpty)
                    bb = new XbimRect3D(r.Centre, r.Centre);
                else
                    bb.Union(r.Centre);
            }
            return bb;
        }

        public XbimModelPositioningCollection()
        {
            _collection = new Dictionary<IModel, XbimModelPositioning>();
        }

        internal void SetCenterInMeters(XbimVector3D ModelTranslation)
        {
            foreach (var model in _collection.Values)
            {
                model.SetCenterInMeters(ModelTranslation);
            }
        }
    }
}
