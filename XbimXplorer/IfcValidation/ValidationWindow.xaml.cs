using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Xbim.Common;
using Xbim.Common.Enumerations;
using Xbim.Common.ExpressValidation;
using Xbim.Ifc.Validation;
using Xbim.Presentation;
using Xbim.Presentation.XplorerPluginSystem;


namespace XbimXplorer.IfcValidation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [XplorerUiElement(PluginWindowUiContainerEnum.LayoutAnchorable, PluginWindowActivation.OnMenu, "View/Developer/IFC Validation")]
    public partial class ValidationWindow : IXbimXplorerPluginWindow
    {
        public ValidationWindow()
        {
            InitializeComponent();
        }

        public string WindowTitle => "IFC Validation";


        private IXbimXplorerPluginMasterWindow _xpWindow;

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
            DependencyProperty.Register("SelectedEntity", typeof(IPersistEntity), typeof(ValidationWindow), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits,
                                                                      OnSelectedEntityChanged));

        // Model
        public IModel Model
        {
            get { return (IModel)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        public static DependencyProperty ModelProperty =
            DependencyProperty.Register("Model", typeof(IModel), typeof(ValidationWindow), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits,
                                                                      OnSelectedEntityChanged));
        
        private static void OnSelectedEntityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // if any UI event should happen it needs to be specified here
            var ctrl = d as ValidationWindow;
            if (ctrl == null)
                return;
            if (e.Property.Name != "SelectedEntity")
                return;
            var newValue = e.NewValue as IPersistEntity;
            if (newValue == null)
                return;
            if (ctrl.IgnoreNextSelectionChange)
            {
                ctrl.IgnoreNextSelectionChange = false;
                return;
            }
            var validator = new Validator()
            {
                CreateEntityHierarchy = true,
                ValidateLevel = ValidationFlags.All
            };

            var ret = validator.Validate(newValue);
            ctrl.Report(ret);
        }

        private void Report(IEnumerable<ValidationResult> ret)
        {
            var sb = new StringBuilder();
            var issues = 0;
            foreach (var validationResult in new IfcValidationReporter(ret))
            {
                sb.AppendLine(validationResult);
                issues++;
            }
            Results.Text = issues == 0 
                ? $"No issues found.\r\n{DateTime.Now.ToLongTimeString()}." 
                : sb.ToString();
        }

        private void ValidateModel(object sender, RoutedEventArgs e)
        {
            using (var cursor = new WaitCursor())
            {
                var validator = new Validator()
                {
                    CreateEntityHierarchy = true,
                    ValidateLevel = ValidationFlags.All
                };

                var ret = validator.Validate(Model.Instances);
                Report(ret);
            }
        }

        public bool IgnoreNextSelectionChange { get; set; }

        private void Results_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var ln  = Results.GetLineIndexFromCharacterIndex(Results.CaretIndex);
            var lineStr = Results.GetLineText(ln);
            var r = new Regex("#(\\d+)");
            var m = r.Match(lineStr);
            if (m.Success)
            {
                var elS = m.Groups[1].Value;
                var el = Convert.ToInt32(elS);
                IgnoreNextSelectionChange = true;
                _xpWindow.SelectedItem = Model.Instances[el];
            }
        }
    }
}
