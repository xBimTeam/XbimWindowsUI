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
        public XbimMatrix3D Transfrom;
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

        private static XbimRegion GetLargestRegion(XbimModel model)
        {
            var project = model.IfcProject;
            var projectId = 0;
            if (project != null) projectId = project.EntityLabel;
            var regionData = model.GetGeometryData(projectId, XbimGeometryType.Region).FirstOrDefault();
            //get the region data should only be one

            if (regionData == null)
                return null;
            var regions = XbimRegionCollection.FromArray(regionData.ShapeData);
            return regions.MostPopulated();
        }

        internal void SetCenter(XbimVector3D modelTranslation)
        {
            var metre = _model.ModelFactors.OneMetre;
            Transfrom = XbimMatrix3D.CreateTranslation(modelTranslation) * XbimMatrix3D.CreateScale((float)(1 / metre));
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

        internal void SetCenter(XbimVector3D ModelTranslation)
        {
            foreach (var model in _collection.Values)
            {
                model.SetCenter(ModelTranslation);
            }
        }
    }
}
