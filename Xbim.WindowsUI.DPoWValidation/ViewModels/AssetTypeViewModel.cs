using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using NPOI.OpenXml4Net.OPC.Internal;
using System.Collections.ObjectModel;
using Xbim.CobieLiteUk.Validation;

namespace Xbim.WindowsUI.DPoWValidation.ViewModels
{
    public class AssetTypeViewModel
    {
        private readonly CobieLiteUk.AssetType _assetType;

        public AssetTypeViewModel(CobieLiteUk.AssetType assetType)
        {
            _assetType = assetType;
            //var v = new AssetTypeValidator(_assetType);
            //if (v.HasRequirements)
            //{
            //    // display requirements instead of assets.
            //    Children = new ObservableCollection<object>(v.RequirementDetails.Select(x=> new RequirementViewModel(x.Attribute)));
            //    return;
            //}
            if (_assetType.Assets == null)
            {
                // no assets available
                Children = new ObservableCollection<object>();
                return;
            }
            // show available assets
            var l = _assetType.Assets.Select(asset => new AssetViewModel(asset)).ToList();
            Children = new ObservableCollection<object>(l);
        }

        public string Name
        {
            get { return _assetType.Name; }
        }

        public ObservableCollection<object> Children { get; set; }

        public Visibility CircleVisibility
        {
            get
            {
                return CircleBrush.Equals(Brushes.Transparent)
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            }
        }

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
