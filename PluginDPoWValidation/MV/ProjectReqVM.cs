using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.COBieLite;

namespace XbimXplorer.Plugins.DPoWValidation.MV
{
    public class ProjectReqVM
    {
        public ProjectReqVM(FacilityType Init)
        {
            DataModel = Init;
        }

        public override string ToString()
        {
            return "Project";
        }

        internal FacilityType DataModel;
    }
}
