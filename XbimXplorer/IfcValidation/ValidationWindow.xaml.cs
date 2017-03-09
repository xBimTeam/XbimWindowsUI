using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Xbim.Common;
using Xbim.Common.Enumerations;
using Xbim.Common.ExpressValidation;
using Xbim.Essentials.Tests;
using Xbim.Ifc.Validation;
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

        private void txtCommand_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl)))
                return;
            ExecuteCommand();
            e.Handled = true;
        }

        private void ExecuteCommand()
        {
            var validator = new IfcValidator()
            {
                CreateEntityHierarchy = true,
                ValidateLevel = ValidationFlags.All
            };

            var ret = validator.Validate(Model.Instances);
            Report(ret);
        }

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
            var validator = new IfcValidator()
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
            foreach (var validationResult in new ValidationReporter(ret))
            {
                sb.AppendLine(validationResult);
                issues++;
            }
            Results.Text = issues == 0 
                ? $"No issues found.\r\n{DateTime.Now.ToLongTimeString()}." 
                : sb.ToString();
        }
    }
}
