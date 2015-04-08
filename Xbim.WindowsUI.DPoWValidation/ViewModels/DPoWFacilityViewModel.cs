using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NPOI.SS.Formula.Functions;
using Xbim.COBieLiteUK;
using Xbim.WindowsUI.DPoWValidation.Commands;
using Xbim.WindowsUI.DPoWValidation.Properties;

namespace Xbim.WindowsUI.DPoWValidation.ViewModels
{
    internal class DPoWFacilityViewModel: INotifyPropertyChanged
    {
        private readonly Facility _model;

        public DPoWFacilityViewModel()
        { }

        public ObservableCollection<string> AvailableClassifications { get; private set; }

        private string _selectedClassification;

        public string SelectedClassification
        {
            get { return _selectedClassification; }
            set
            {
                _selectedClassification = value;
                if (PropertyChanged != null)
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(@"SelectedClassification"));
                RefreshAssetsTree();
            }
        }

        public FacilitySaveCommand SaveCommand { get; private set; }

        public DPoWFacilityViewModel(Facility model)
        {
            _model = model;

            // asset types
            //
            if (_model.AssetTypes != null)
            {
                // prepare Avalable classifications
                //
                var s = _model.AssetTypes.Where(at => at.Categories != null)
                    .SelectMany(x => x.Categories)
                    .Select(c => c.Classification)
                    .Distinct().ToList();
                AvailableClassifications = new ObservableCollection<string>(s);

                SelectedClassification =
                    s.Contains(Settings.Default.PreferredClassification)
                        ? Settings.Default.PreferredClassification
                        : AvailableClassifications.FirstOrDefault();
            }
            
            // documents
            //
            if (_model.Documents != null)
            {
                var lst = _model.Documents.Select(document => new DocumentViewModel(document)).ToList();
                Documents = new ObservableCollection<object>(lst);
            }
            else
                Documents = new ObservableCollection<object>();

            // command
            SaveCommand = new FacilitySaveCommand(_model);
        }

        private void RefreshAssetsTree()
        {
            if (_model.AssetTypes != null)
            {
                // setup
                var tS = new Dictionary<string, ClassifiedAssetTypesViewModel>();
                // look at classified asset types
                foreach (var assetType in _model.AssetTypes.Where(x => x.Categories != null))
                {
                    var thisCats = assetType.Categories.Where(x => x.Classification == SelectedClassification);

                    foreach (var thisCat in thisCats)
                    {
                        var thisT = thisCat.Code;
                        var thisCatVm = new ClassifiedAssetTypesViewModel(thisCat);
                        if (!tS.ContainsKey(thisT))
                        {
                            tS.Add(thisT, thisCatVm);
                        }
                        tS[thisT].AssetTypes.Add(new AssetTypeViewModel(assetType));    
                    }
                }

                var sorted  = new List<ClassifiedAssetTypesViewModel>(tS.Count);
                sorted.AddRange(tS.Keys.OrderBy(x => x).Select(key => tS[key]));

                AssetTypes = new ObservableCollection<ClassifiedAssetTypesViewModel>(sorted);
            }
            else
                AssetTypes = new ObservableCollection<ClassifiedAssetTypesViewModel>();

            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(@"AssetTypes"));
        }

        public ObservableCollection<object> Documents { get; set; }

        public string Title { get; set; }

        public ObservableCollection<ClassifiedAssetTypesViewModel> AssetTypes { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
