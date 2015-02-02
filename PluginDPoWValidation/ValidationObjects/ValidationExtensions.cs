using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Validation.ValidationObjects;
using Xbim.COBieLite;
using Xbim.Ifc2x3.Kernel;


namespace XbimXplorer.Plugins.DPoWValidation.ValidationObjects
{
    static class ValidationExtensions
    {
        public static bool Validates(this IfcProduct entity, FacilityType requirements)
        {
            //var spec =
            //            ctrl.ModelFacility.AssetTypes.AssetType.FirstOrDefault(
            //                x => x.externalID == type.EntityLabel.ToString());

            //var req =
            //    ctrl.ReqFacility.AssetTypes.AssetType.FirstOrDefault(x => spec != null && x.AssetTypeCategory == spec.AssetTypeCategory);

            //if (req != null)
            //{
            //    var creq = new CobieAssetTypeRequirement(req);
            //    StringBuilder b = new StringBuilder();
            //    creq.Validate(ctrl.ModelFacility, selectedEnt.EntityLabel, b);
            //    var rep = b.ToString();
            //    ctrl.report.Text = rep;
            //}


            return false;
        }
    }
}
