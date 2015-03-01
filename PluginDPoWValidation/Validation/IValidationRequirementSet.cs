using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xbim.COBieLite.Validation
{
    public interface IValidationRequirementSet
    {
        string Name { get; }
        IEnumerable<IValidationRequirementDetail> Details { get; }

        IEnumerable<ReportResult> Validate(Xbim.COBieLite.FacilityType modelFacilityType, int filterLabel = -1,
            StringBuilder reporter = null);
    }


}
