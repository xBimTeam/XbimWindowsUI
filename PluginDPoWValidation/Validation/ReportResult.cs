using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Xbim.XbimExtensions.Interfaces;

namespace Xbim.COBieLite.Validation
{
    public class ReportResult
    {
        public IValidationRequirementSet Requirement;
        public int EntityLabel;
        public bool BoolResult;
        public string Notes;

        public ReportResult(IValidationRequirementSet requirement, int entityLabel, bool passed, string notes)
        {
            Requirement = requirement;
            EntityLabel = entityLabel;
            BoolResult = passed;
            Notes = notes;
        }

        public string EntityDesc 
        {
            get { return @"Entity " + EntityLabel; }
        }

        public string ResultSummary
        {
            get
            {
                return string.Format("{0}Passed", (BoolResult) ? "" : "Not ");
            }
        }

        public string ConceptName
        {
            get
            {
                return Requirement.Name;
            }
        }

        public Brush CircleBrush
        {
            get
            {
                if (BoolResult)
                    return Brushes.Green;
                else
                    return Brushes.Red;
            }
        }

    }
}
