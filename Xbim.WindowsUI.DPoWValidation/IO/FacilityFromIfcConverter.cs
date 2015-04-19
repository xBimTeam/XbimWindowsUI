using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.COBieLiteUK;
using Xbim.IO;
using XbimExchanger.IfcToCOBieLiteUK;

namespace Xbim.WindowsUI.DPoWValidation.IO
{
    public class FacilityFromIfcConverter
    {
        public static Facility FacilityFromModel(XbimModel model)
        {
            var facilities = new List<Facility>();
            var ifcToCoBieLiteUkExchanger = new IfcToCOBieLiteUkExchanger(model, facilities);
            facilities = ifcToCoBieLiteUkExchanger.Convert();
            return facilities.FirstOrDefault();
        }
    }
}
