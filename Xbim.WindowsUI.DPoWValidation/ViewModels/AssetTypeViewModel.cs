using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using NPOI.OpenXml4Net.OPC.Internal;
using System.Collections.ObjectModel;

namespace Xbim.WindowsUI.DPoWValidation.ViewModels
{
    public class AssetTypeViewModel
    {
        private readonly COBieLiteUK.AssetType _assetType;

        public AssetTypeViewModel(COBieLiteUK.AssetType assetType)
        {
            _assetType = assetType;
            if (_assetType.Assets == null)
            {
                Assets = new ObservableCollection<AssetViewModel>();
                return;
            }
            var l = _assetType.Assets.Select(asset => new AssetViewModel(asset)).ToList();
            Assets = new ObservableCollection<AssetViewModel>(l);
        }

        public string Name
        {
            get { return _assetType.Name; }
        }

        public ObservableCollection<AssetViewModel> Assets { get; set; }

        public Brush CircleBrush
        {
            get
            {
                var cat = _assetType.Categories.FirstOrDefault(x => x.Classification == @"DPoW");
                if (cat == null)
                    return Brushes.Transparent;

                switch (cat.Code)
                {
                    case @"Passed":
                        return Brushes.Green;
                    case @"Failed":
                        return Brushes.Red;
                    default:
                        return Brushes.Blue;
                }
            }
        }
    }
}
