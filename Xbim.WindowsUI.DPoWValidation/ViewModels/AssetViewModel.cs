using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Xbim.WindowsUI.DPoWValidation.ViewModels
{
    public class AssetViewModel
    {
        private readonly COBieLiteUK.Asset _asset;

        public int? EntityLabel
        {
            get
            {
                try
                {
                    return Convert.ToInt32(_asset.ExternalId);
                }
                catch (Exception)
                {

                    return null;
                }
            }
        }

        public AssetViewModel(COBieLiteUK.Asset asset)
        {
            this._asset = asset;
            if (_asset.Attributes == null)
            {
                RequirementResults = new ObservableCollection<RequirementViewModel>();
                return;
            }
            var lst = _asset.Attributes.Select(att => new RequirementViewModel(att)).ToList();
            RequirementResults = new ObservableCollection<RequirementViewModel>(lst);
        }

        public Visibility CircleVisibility
        {
            get
            {
                return CircleBrush.Equals(Brushes.Transparent)
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            }
        }

        public ObservableCollection<RequirementViewModel> RequirementResults { get; private set; }

        public string Name
        {
            get { return _asset.Name; }
        }

        public string Description
        {
            get { return _asset.Description; }
        }

        public Brush CircleBrush
        {
            get
            {
                if (_asset.Categories == null)
                    return Brushes.Transparent;
                var cat = _asset.Categories.FirstOrDefault(x => x.Classification == @"DPoW");
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
