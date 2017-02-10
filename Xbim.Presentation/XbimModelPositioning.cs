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
        public XbimRegion SelectedRegion;
        public XbimMatrix3D Transform;
        private double _oneMeter = double.NaN;

        public double OneMeter
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

        public XbimRect3D GetSelectedRegionRectInMeters()
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

        public XbimModelPositioning(IModel model)
        {
            _model = model;
            var geomStore = model.GeometryStore;
            if (_model.GeometryStore.IsEmpty)
                return;
            using (var reader = geomStore.BeginRead())
            {
                var regions = reader.ContextRegions.Where(cr => cr.MostPopulated()!=null).Select(c=>c.MostPopulated());
                var rect = XbimRect3D.Empty;
                int pop = 0;
                foreach (var r in regions)
                {
                    pop += r.Population;
                    if (rect.IsEmpty) rect = r.ToXbimRect3D();
                    else rect.Union(r.ToXbimRect3D());
                }
                if(pop>0)
                    SelectedRegion = new XbimRegion("Largest", rect, pop);
                
            }
        }


        internal void SetCenterInMeters(XbimVector3D modelTranslation)
        {
            var translation = XbimMatrix3D.CreateTranslation(modelTranslation * OneMeter);
            var scaling = XbimMatrix3D.CreateScale(1/OneMeter);
            Transform =  translation * scaling;
        }
    }
}
