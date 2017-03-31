using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Common;
using Xbim.Common.Geometry;

namespace Xbim.Presentation
{
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

        /// <summary>
        /// The union of selected regions from all models
        /// </summary>
        /// <returns></returns>
        private XbimRect3D GetSelectedRegionsEnvelopeInMeters()
        {
            var bb = XbimRect3D.Empty;
            var modelBoundaries = _collection.Values.Select(positioning => positioning.SelectedRegionInMeters);
            foreach (var r in modelBoundaries)
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

        public XbimRect3D GetEnvelopOfCentres()
        {
            var bb = XbimRect3D.Empty;
            foreach (var r in _collection.Values.Select(positioning => positioning.SelectedRegion).Where(r => r != null)
            )
            {
                if (bb.IsEmpty)
                    bb = new XbimRect3D(r.Centre, r.Centre);
                else
                    bb.Union(r.Centre);
            }
            return bb;
        }

        internal XbimRect3D ModelSpaceBounds
        {
            get
            {
                if (_collection == null || !_collection.Any())
                    return XbimRect3D.Empty;
                return GetSelectedRegionsEnvelopeInMeters();
            }
        }

        private XbimVector3D _viewSpaceTranslation;

        public XbimRect3D ViewSpaceBounds
        {
            get
            {
                return (ModelSpaceBounds.IsEmpty)
                    ? new XbimRect3D(0, 0, 0, 10, 10, 5)
                    : ModelSpaceBounds.Transform(XbimMatrix3D.CreateTranslation(_viewSpaceTranslation));
            }
        }

        internal void ComputeViewBoundsTransform()
        {
            var p = ModelSpaceBounds.Centroid();
            _viewSpaceTranslation = new XbimVector3D(-p.X, -p.Y, -p.Z);
            SetCenterInMeters(_viewSpaceTranslation);
        }

        public XbimModelPositioningCollection()
        {
            _collection = new Dictionary<IModel, XbimModelPositioning>();
            _viewSpaceTranslation = new XbimVector3D(0, 0, 0);
        }

        internal void SetCenterInMeters(XbimVector3D modelTranslation)
        {
            _viewSpaceTranslation = modelTranslation;
            foreach (var model in _collection.Values)
            {
                // each item in the collection stores a matrix that depends on the units of measure of the model.
                //
                model.PrepareTransform(modelTranslation);
            }
        }

        /// <summary>
        /// Sets the region specified by name as selected.
        /// </summary>
        /// <param name="name">the region name to match</param>
        /// <returns>true if the region has ben found and set, false otherwise</returns>
        public bool SetSelectedRegionByName(string name, bool add)
        {
            foreach (var xbimModelPositioning in _collection.Values)
            {
                if (xbimModelPositioning.SetSelectedRegionByName(name, add))
                    return true;
            }
            return false;
        }

        public string Report()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Model space bounds: {ModelSpaceBounds}");
            sb.AppendLine($"View space bounds: {ViewSpaceBounds}");
            sb.AppendLine($"{_viewSpaceTranslation}");
            return sb.ToString();
        }
    }
}