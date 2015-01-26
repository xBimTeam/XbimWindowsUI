using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.COBieLite;

namespace Validation.MV
{
    class AssetTypeInfoTypeVM
    {
        internal AssetTypeInfoTypeVM(AssetTypeInfoType init)
        {
            DataModel = init;
        }

        public override string ToString()
        {
            if (DataModel == null)
                return @"N/D";
            return DataModel.AssetTypeCategory;
        }

        internal AssetTypeInfoType DataModel;

    }
}
