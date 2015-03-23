using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NPOI.SS.Formula.Functions;
using Xbim.COBieLiteUK;

namespace Xbim.WindowsUI.DPoWValidation.ViewModels
{
    internal class DPoWFacilityViewModel
    {
        private Facility _model;

        public DPoWFacilityViewModel()
        { }

        public DPoWFacilityViewModel(Facility model)
        {
            _model = model;
            AssetTypes = new ObservableCollection<AssetType>(model.AssetTypes);

        }

        public string Title { get; set; }

        public ObservableCollection<AssetType> AssetTypes { get; set; }
    }
}
