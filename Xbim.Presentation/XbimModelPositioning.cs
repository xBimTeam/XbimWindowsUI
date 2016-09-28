using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Common;
using Xbim.Common.Geometry;
using Xbim.Ifc;
using Xbim.ModelGeometry.Scene;

namespace Xbim.Presentation
{
    public class XbimModelPositioning
    {
        public XbimRegion LargestRegion;
        public Xbim3DModelContext Context;
        public XbimMatrix3D Transform;
        
        private double _oneMeter = Double.NaN;

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

        public XbimRect3D GetLargestRegionRectInMeters()
        {
            if (LargestRegion == null)
                return XbimRect3D.Empty;
            var pts = MinMaxPoints(LargestRegion, OneMeter);
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
                    LargestRegion = new XbimRegion("Largest", rect, pop);
                
            }
        }


        internal void SetCenterInMeters(XbimVector3D modelTranslation)
        {
            var translation = XbimMatrix3D.CreateTranslation(modelTranslation * OneMeter);
            var scaling = XbimMatrix3D.CreateScale(1/OneMeter);
            Transform =  translation * scaling;
        }
    }
    
    public class XbimModelPositioningCollection
    {
        public XbimModelPositioning this[IModel modelKey]
        {
            get
            {
                XbimModelPositioning returnValue;
                if (_collection.TryGetValue(modelKey, out returnValue))
                    return returnValue;
                return null;
            }
            set { _collection[modelKey] = value; }
        }

        private readonly Dictionary<IModel, XbimModelPositioning> _collection;

        public void AddModel(IModel model)
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
            foreach (var r in _collection.Values.Select(positioning => positioning.LargestRegion).Where(r => r != null))
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

        internal void SetCenterInMeters(XbimVector3D modelTranslation)
        {
            foreach (var model in _collection.Values)
            {
                model.SetCenterInMeters(modelTranslation);
            }
        }
    }
}
