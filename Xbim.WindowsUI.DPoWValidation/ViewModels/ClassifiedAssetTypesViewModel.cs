using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.COBieLiteUK;

namespace Xbim.WindowsUI.DPoWValidation.ViewModels
{
    public class ClassifiedAssetTypesViewModel
    {
        public string CategoryCode { get; private set; }
        public string CategoryDescription { get; private set; }

        public string CategoryClassification { get; private set; }

        public ObservableCollection<AssetTypeViewModel> AssetTypes { get; set; }

        public ClassifiedAssetTypesViewModel(Category initCategory)
        {
            CategoryCode = initCategory.Code;
            CategoryDescription = initCategory.Description;
            CategoryClassification = initCategory.Classification;
            AssetTypes = new ObservableCollection<AssetTypeViewModel>();
        }
    }
}
