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
            var tmpChld = new ObservableCollection<ClassifiedAssetTypesViewModel>();
            // Dictionary<string> cats = 
            var tS = new Dictionary<Tuple<string, string>, ClassifiedAssetTypesViewModel>();
            foreach (var assetType in _model.AssetTypes)
            {
                if (assetType.Categories == null)
                    continue;
                var thisCat = assetType.Categories.FirstOrDefault();
                if (thisCat == null)
                    continue;
                
                var thisT = new Tuple<string, string>(thisCat.Classification, thisCat.Code);
                var thisCatVm = new ClassifiedAssetTypesViewModel(thisCat);
                if (!tS.ContainsKey(thisT))
                {
                    tS.Add(thisT, thisCatVm);
                }
                tS[thisT].AssetTypes.Add(new AssetTypeViewModel(assetType));

                // AssetTypes.Add(new AssetTypeViewModel(assetType));
            }
            AssetTypes = new ObservableCollection<ClassifiedAssetTypesViewModel>(tS.Values);
        }

        public string Title { get; set; }

        public ObservableCollection<ClassifiedAssetTypesViewModel> AssetTypes { get; set; }
    }
}
