using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NPOI.HPSF;
using XbimExchanger.COBieLiteHelpers;

namespace Xbim.COBieLite.Validation
{
    class CobieProjectRequirementSet : IValidationRequirementSet
    {
        private FacilityType requirementFacilityType;

        public CobieProjectRequirementSet(FacilityType facilityType)
        {
            
            this.requirementFacilityType = facilityType;
        }

        public string Name
        {
            get { return requirementFacilityType.ProjectAssignment.ProjectName; }
        }


        public IEnumerable<IValidationRequirementDetail> Details
        {
            get { throw new NotImplementedException(); }
        }


        public IEnumerable<ReportResult> Validate(FacilityType modelFacilityType, int filterLabel = -1, StringBuilder reporter = null)
        {
            var el = -1;
            Int32.TryParse(modelFacilityType.ProjectAssignment.externalID, out el);

            var pass = (modelFacilityType.ProjectAssignment.ProjectName == requirementFacilityType.ProjectAssignment.ProjectName);
            if (pass)
            {
                yield return new ReportResult(this, el, true, @"Project name is correct");
            }
            else
            {
                yield return
                    new ReportResult(this, el, false,
                        string.Format("Incorrect project name (expected: {0})",
                            requirementFacilityType.ProjectAssignment.ProjectName));
            }

            var aProp = requirementFacilityType.FacilityAttributes.FirstOrDefault(at => at.AttributeName == "Area");
            if (aProp != null)
            {
                // todo: trying to get the value of the area here, but cant' find a way.
                //var p2 = aProp.AttributeValue.GetType().ToString();
                //var p = aProp.AttributeValue.Item.ToString();
            }

        }
    }
}
