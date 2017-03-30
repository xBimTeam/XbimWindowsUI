using System;
using System.Linq;
using Xbim.Common;
using Xbim.Common.Geometry;
using Xbim.Ifc;
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
        /// <returns>true if the region has ben found and set, false otherwise</returns>
        public bool SetSelectedRegionByName(string name)
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
                    SelectedRegion = reg;
                    return true;
                }
            }
            return false;
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
            using (var reader = geomStore.BeginRead())
            {
                // ContextRegions is a collection of ContextRegions, which is also a collection.
                // we get the most populated from each
                var name = "MostPopulated";
                var regions = reader.ContextRegions.Where(cr => cr.MostPopulated() != null).Select(c => c.MostPopulated());
                var rect = XbimRect3D.Empty;
                var pop = 0;
                // then perform their union
                foreach (var r in regions)
                {
                    pop += r.Population;
                    if (rect.IsEmpty)
                    {
                        rect = r.ToXbimRect3D();
                        name = r.Name;
                    }
                    else
                    {
                        rect.Union(r.ToXbimRect3D());
                        name = "MostPopulatedMerge";
                    }
                }
                if (pop > 0)
                    SelectedRegion = new XbimRegion(name, rect, pop);
            }
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
