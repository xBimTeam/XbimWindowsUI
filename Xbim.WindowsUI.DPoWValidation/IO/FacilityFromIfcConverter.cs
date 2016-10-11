using System.Collections.Generic;
using System.Linq;
using Xbim.CobieLiteUk;
using Xbim.Ifc;
using XbimExchanger.IfcToCOBieLiteUK;

namespace Xbim.WindowsUI.DPoWValidation.IO
{
    public class FacilityFromIfcConverter
    {
        public static Facility FacilityFromModel(IfcStore model)
        {
            var facilities = new List<Facility>();
            var ifcToCoBieLiteUkExchanger = new IfcToCOBieLiteUkExchanger(model, facilities);
            facilities = ifcToCoBieLiteUkExchanger.Convert();
            return facilities.FirstOrDefault();
        }
    }
}
