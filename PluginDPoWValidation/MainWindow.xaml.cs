using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Diagnostics;
using Xbim.Presentation.XplorerPluginSystem;
using Xbim.WindowsUI.DPoWValidation.IO;
using Xbim.WindowsUI.DPoWValidation.ViewModels;
using Xbim.XbimExtensions.Interfaces;
using System.IO;
using Microsoft.Win32;
using Xbim.COBieLiteUK;
using Xbim.IO;

namespace Validation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : UserControl, IXbimXplorerPluginWindow 
    {
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

            var openFile = new OpenFileDialog();
            openFile.Filter = string.Join("|", supportedFiles);
                    
            var res = openFile.ShowDialog();

            if (res.HasValue && res.Value)
            {
                var r = new FacilityReader();
                var t = r.LoadFacility(openFile.FileName);
                SetFacility(t);
                
            }
        }

        private void SetFacility(Facility facility)
        {
            if (facility == null)
            {
                IsFileOpen = false;
                return;
            }
            ReqFacility = facility;
            // todo: initialise component viewmodel 
            IsFileOpen = true;
        }

        private bool IsFileOpen
        {
            get
            {
                // return (Doc != null);
                return false;
            }
            set
            {
                if (value == true)
                {
                    UI.Visibility = System.Windows.Visibility.Visible;
                    OpenButton.Visibility = System.Windows.Visibility.Hidden;
                }
                else
                {
                    UI.Visibility = System.Windows.Visibility.Hidden;
                    OpenButton.Visibility = System.Windows.Visibility.Visible;
                }
                //PropertyChanged.Invoke(this, new PropertyChangedEventArgs("OpenButtonVisibility"));
                //PropertyChanged.Invoke(this, new PropertyChangedEventArgs("UIVisibility"));
            }
        }

        public Visibility OpenButtonVisibility { get { return (IsFileOpen) ? System.Windows.Visibility.Hidden : System.Windows.Visibility.Visible; } }
        public Visibility UIVisibility { get { return (!IsFileOpen) ? System.Windows.Visibility.Hidden : System.Windows.Visibility.Visible; } }
        
       
        private IXbimXplorerPluginMasterWindow xpWindow;

        // ---------------------------
        // plugin system related stuff
        //

        public void BindUI(IXbimXplorerPluginMasterWindow mainWindow)
        {
            xpWindow = mainWindow;
            this.SetBinding(SelectedItemProperty, new Binding("SelectedItem") { Source = mainWindow, Mode = BindingMode.OneWay });
            this.SetBinding(ModelProperty, new Binding()); // whole datacontext binding, see http://stackoverflow.com/questions/8343928/how-can-i-create-a-binding-in-code-behind-that-doesnt-specify-a-path
        }

        // SelectedEntity
        public IPersistIfcEntity SelectedEntity
        {
            get { return (IPersistIfcEntity)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public static DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedEntity", typeof(IPersistIfcEntity), typeof(MainWindow), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits,
                                                                      new PropertyChangedCallback(OnSelectedEntityChanged)));


        private static void OnSelectedEntityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = d as MainWindow;
            if (ctrl == null) 
                return;
            switch (e.Property.Name)
            {
                case "Model":
                {
                    var model = e.NewValue as XbimModel;
                    if (model != null)
                    {
                        try
                        {
                            ctrl.ModelFacility = FacilityFromIfcConverter.FacilityFromModel(model);
                        }
                        catch (Exception ex)
                        {
                            ctrl.ModelFacility = null;
                        }
                    }
                    else
                        ctrl.ModelFacility = null;
                    ctrl.FacilityViewer.DataContext = new DPoWFacilityViewModel(ctrl.ModelFacility);
                    
                }
                    break;
                case "SelectedEntity":
                    break;
            }
        }

        // Model
        public IModel Model
        {
            get { return (IModel)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        public static DependencyProperty ModelProperty =
            DependencyProperty.Register("Model", typeof(IModel), typeof(MainWindow), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits,
                                                                      new PropertyChangedCallback(OnSelectedEntityChanged)));

        internal Facility ModelFacility = null;
        internal Facility ReqFacility = null;

        
       
        
        public string MenuText
        {
            get { return "DPoW Validation"; }
        }

        public string WindowTitle
        {
            get { return "DPoW Validation"; }
        }

        private void ModelInfo(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine(Model != null);
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


        public PluginWindowDefaultUIShow DefaultUIActivation
        { get { return PluginWindowDefaultUIShow.onLoad; } }

        public PluginWindowDefaultUIContainerEnum DefaultUIContainer
        { get { return PluginWindowDefaultUIContainerEnum.LayoutAnchorable; } }

        private void CloseFile(object sender, RoutedEventArgs e)
        {
            //Doc = null;
            //lstClassifications.ItemsSource = null;
            //lstProject.ItemsSource = null;
            //IsFileOpen = false;
        }

      

        private void AddComment(object sender, RoutedEventArgs e)
        {
            //Hyperlink h = sender as Hyperlink;
            //if (h != null)
            //{
            //    ReportResult res = null;
            //    string title = "";
            //    if (h.ToolTip.ToString() == "Create ER comment" && LstProjectResults.SelectedItem != null)
            //    {
            //        res = LstProjectResults.SelectedItem as ReportResult;
            //        title = LstProjectResults.SelectedItem.ToString() + " ";
            //        title += (res.BoolResult) ? "validation passed" : "validation failed";
            //    }
            //    else if (h.ToolTip.ToString() == "Create Class comment" && lstClassResults.SelectedItem != null)
            //    {                    
            //        res = lstClassResults.SelectedItem as ReportResult;   
            //        title = (res.BoolResult) ? "Validation passed" : "Validation failed";
            //    }
            //    if (res != null)
            //    {
            //        var MessageData = new Dictionary<string, object>();
            //        string VerbalStatus = (res.BoolResult) ? "Information" : "Error";
            //        string commentText =
            //            string.Format("Entity {0} ({1}) {2} request {3}",
            //                "Element " + res.EntityLabel,
            //                res.EntityLabel,
            //                (res.BoolResult) ? "passed" : "did not pass",
            //                res.ConceptName
            //                );

            //        commentText += "\r\n\r\n" + res.Notes;

            //        MessageData.Add("InstanceTitle", title);
            //        MessageData.Add("DestinationEmail", "claudio.benghi@gmail.com");
            //        MessageData.Add("CommentVerbalStatus", VerbalStatus);
            //        MessageData.Add("CommentAuthor", "CB");
            //        MessageData.Add("CommentText", commentText);
            //        // MessageData.Add("DestinationEmail", res.Requirement.Author);
            //        xpWindow.BroadCastMessage(this, "BcfAddInstance", MessageData);
            //    }
            //}
        }

        bool _useAmber = true;

        private void TranspToggle(object sender, MouseButtonEventArgs e)
        {
            UnMatched.Fill = _useAmber 
                ? Brushes.Transparent 
                : Brushes.Orange;

            _useAmber = !_useAmber;
        }

        private void LstClassResults_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            xpWindow.DrawingControl.ZoomSelected();
        }
    }
}
