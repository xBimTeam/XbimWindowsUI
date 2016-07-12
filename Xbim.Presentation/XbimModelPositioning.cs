using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NPOI.POIFS.Storage;
using Xbim.Common.Geometry;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using Xbim.XbimExtensions.Interfaces;
using XbimGeometry.Interfaces;

namespace Xbim.Presentation
{
    public class XbimModelPositioning
    {
        public XbimRegion LargestRegion;
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

        public XbimModelPositioning(XbimModel model)
        {
            _model = model;
            Context = new Xbim3DModelContext(model);
            var supportLevel = model.GeometrySupportLevel;
          
            switch (supportLevel)
            {
                case 1:
                    LargestRegion = GetLargestRegion(model);
                    break;
                case 2:
                    LargestRegion = Context.GetLargestRegion();
                    break;
            }
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

        internal void SetCenterInMeters(XbimVector3D ModelTranslation)
        {
            foreach (var model in _collection.Values)
            {
                model.SetCenterInMeters(ModelTranslation);
            }
        }
    }
}
