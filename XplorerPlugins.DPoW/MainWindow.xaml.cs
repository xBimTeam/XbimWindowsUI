using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using log4net;
using Microsoft.Win32;
using Xbim.CobieLiteUK.Validation;
using Xbim.Common;
using Xbim.COBieLiteUK;
using Xbim.Ifc;
using Xbim.Presentation.XplorerPluginSystem;
using Xbim.WindowsUI.DPoWValidation.IO;
using Xbim.WindowsUI.DPoWValidation.ViewModels;

namespace XplorerPlugins.DPoW
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>S
    [XplorerUiElement(PluginWindowUiContainerEnum.LayoutAnchorable, PluginWindowActivation.OnMenu, "Digital Plan of Works")]
    public partial class MainWindow : IXbimXplorerPluginWindow 
    {
        private static readonly ILog Log = LogManager.GetLogger("XplorerPlugins.DPoWValidation.MainWindow");

        public MainWindow()
        {
            InitializeComponent();
            IsFileOpen = false;
        }
        // xml navigation sample at http://support.microsoft.com/kb/308333

        private void OpenFile(object sender, RoutedEventArgs e)
        {
            var supportedFiles = new []
            {
                "All supprted files|*.xlsx;*.xls;*.xml;*.json",
                "Validation requirement Excel|*.xlsx;*.xls",
                "Validation requirement XML|*.xml",
                "Validation requirement json|*.json"
            };

            var openFile = new OpenFileDialog {Filter = string.Join("|", supportedFiles)};

            var res = openFile.ShowDialog();

            if (res.HasValue && res.Value)
            {
                var r = new FacilityReader();
                ReqFacility = r.LoadFacility(openFile.FileName);
                TestValidation();
            }
        }

        private void SetFacility(Facility facility)
        {
            if (facility == null)
            {
                IsFileOpen = false;
                return;
            }
            ViewFacility = facility;
            // todo: initialise component viewmodel 
            // FacilityViewer.DataContext = new DPoWFacilityViewModel(ReqFacility);

            IsFileOpen = true;

            try
            {
                Classifications.ItemsSource = facility.AssetTypes.Where(at => at.Categories != null)
                    .SelectMany(x => x.Categories)
                    .Select(c => c.Code)
                    .Distinct().ToList();
                if (Classifications.Items.Count > 0)
                {
                    Classifications.SelectedItem = 0;
                }
            }
            catch
            {
                // ignored
            }
        }

        private bool IsFileOpen
        {
            get
            {
                return false;
            }
            set
            {
                // ReSharper disable once RedundantBoolCompare
                if (value == true)
                {
                    Ui.Visibility = Visibility.Visible;
                    OpenButton.Visibility = Visibility.Hidden;
                }
                else
                {
                    Ui.Visibility = Visibility.Hidden;
                    OpenButton.Visibility = Visibility.Visible;
                }
                //PropertyChanged.Invoke(this, new PropertyChangedEventArgs("OpenButtonVisibility"));
                //PropertyChanged.Invoke(this, new PropertyChangedEventArgs("UIVisibility"));
            }
        }

        public Visibility OpenButtonVisibility { get { return (IsFileOpen) ? Visibility.Hidden : Visibility.Visible; } }
        public Visibility UiVisibility { get { return (!IsFileOpen) ? Visibility.Hidden : Visibility.Visible; } }
        
       
        private IXbimXplorerPluginMasterWindow _xpWindow;

        // -----------------------------
        // plugin system related section
        //

        public void BindUi(IXbimXplorerPluginMasterWindow mainWindow)
        {
            _xpWindow = mainWindow;
            SetBinding(SelectedItemProperty, new Binding("SelectedItem") { Source = mainWindow, Mode = BindingMode.OneWay });
            SetBinding(ModelProperty, new Binding()); // whole datacontext binding, see http://stackoverflow.com/questions/8343928/how-can-i-create-a-binding-in-code-behind-that-doesnt-specify-a-path
        }

        // SelectedEntity
        public IPersistEntity SelectedEntity
        {
            get { return (IPersistEntity)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public static DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedEntity", typeof(IPersistEntity), typeof(MainWindow), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits,
                                                                      OnSelectedEntityChanged));


        private static void OnSelectedEntityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = d as MainWindow;
            if (ctrl == null) 
                return;
            switch (e.Property.Name)
            {
                case "Model":
                    var model = e.NewValue as IfcStore;
                    if (model != null)
                    {
                        try
                        {
                            ctrl.ModelFacility = FacilityFromIfcConverter.FacilityFromModel(model);
                        }
                        catch (Exception ex)
                        {
                            Log.Error( "Error in generating Facility from model " + model.FileName, ex);
                            ctrl.ModelFacility = null;
                        }
                    }
                    else
                    {
                        ctrl.ModelFacility = null;
                    }
                    ctrl.TestValidation();
                    break;
                case "SelectedEntity":
                    break;
            }
        }

        private void TestValidation()
        {
            if (ReqFacility == null || ModelFacility == null)
                return;
            var f = new FacilityValidator();
            ValFacility = f.Validate(ReqFacility, ModelFacility);
            SetFacility(ValFacility);
        }

        // Model
        public IModel Model
        {
            get { return (IModel)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        public static DependencyProperty ModelProperty =
            DependencyProperty.Register("Model", typeof(IModel), typeof(MainWindow), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits,
                                                                      OnSelectedEntityChanged));

        internal Facility ModelFacility;
        internal Facility ReqFacility;
        internal Facility ValFacility;
        internal Facility ViewFacility;

        public string WindowTitle
        {
            get { return "Digital Plan of Work"; }
        }
        
        private void TrafficLight(object sender, RoutedEventArgs e)
        {
            //var ls = new TrafficLightStyler((XbimModel)this.Model, this);
            //ls.UseAmber = UseAmber;
            //xpWindow.DrawingControl.LayerStyler = ls;

            //var newLayerStyler = new ValidationResultStyler();
            //xpWindow.DrawingControl.GeomSupport2LayerStyler = newLayerStyler;

            //xpWindow.DrawingControl.ReloadModel(/*Options: DrawingControl3D.ModelRefreshOptions.ViewPreserveAll*/);
        }

        private void CloseFile(object sender, RoutedEventArgs e)
        {
            ReqFacility = null;
            LstAssets.ItemsSource = null;
            Classifications.ItemsSource = null;
            IsFileOpen = false;
        }

        bool _useAmber = true;

        private void TranspToggle(object sender, MouseButtonEventArgs e)
        {
            UnMatched.Fill = _useAmber 
                ? Brushes.Transparent 
                : Brushes.Orange;

            _useAmber = !_useAmber;
        }

        private void UpdateList(object sender, SelectionChangedEventArgs e)
        {
            var selectedCode = Classifications.SelectedItem.ToString();
            var lst = new ObservableCollection<AssetViewModel>();

            if (ViewFacility.AssetTypes == null)
                return;
            foreach (var assetType in ViewFacility.AssetTypes.Where(x => x.Categories != null))
            {
                var valid = assetType.Categories.Any(x => x.Code == selectedCode);
                if (!valid)
                    continue;
                if (assetType.Assets == null)
                    continue;
                foreach (var asset in assetType.Assets)
                {
                    lst.Add(new AssetViewModel(asset));
                }               
            }
            LstAssets.ItemsSource = lst;    
        }

        private void GotoAsset(object sender, MouseButtonEventArgs e)
        {
            _xpWindow.DrawingControl.ZoomSelected();
        }

        private void SetSelectedAsset(object sender, SelectionChangedEventArgs e)
        {
            var avm = LstAssets.SelectedItem as AssetViewModel;
            if (avm == null)
                return;
            var selectedLabel = avm.EntityLabel;
            if (!selectedLabel.HasValue)
                return;
            _xpWindow.SelectedItem = Model.Instances[selectedLabel.Value];
        }
    }
}
