using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Xbim.XbimExtensions.Interfaces;

namespace Validation.ValidationObjects
{
    class ReportResult
    {
        public IValidationRequirement Requirement;
        public int EntityLabel;
        public bool BoolResult;
        string Report;

        public ReportResult(IValidationRequirement requirement, int entityLabel, bool bPassed)
        {
            this.Requirement = requirement;
            this.EntityLabel = entityLabel;
            this.BoolResult = bPassed;
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
