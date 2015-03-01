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
            const string undeterminedString = @"N/D";
            if (DataModel == null)
                return undeterminedString;
            return String.IsNullOrEmpty(DataModel.AssetTypeName) 
                ? undeterminedString 
                : DataModel.AssetTypeName;
        }

        internal AssetTypeInfoType DataModel;

    }
}
