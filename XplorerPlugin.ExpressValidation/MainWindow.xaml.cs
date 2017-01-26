using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Xbim.Common;
using Xbim.Common.Enumerations;
//using Xbim.Essentials.Tests;
//using Xbim.Ifc.Validation;
using Xbim.Presentation.XplorerPluginSystem;

namespace XplorerPlugin.ExpressValidation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [XplorerUiElement(PluginWindowUiContainerEnum.LayoutAnchorable, PluginWindowActivation.OnMenu, "Express Validation")]
    public partial class MainWindow : IXbimXplorerPluginWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public string WindowTitle => "Express Validation";


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
            DependencyProperty.Register("SelectedEntity", typeof(IPersistEntity), typeof(MainWindow), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits,
                                                                      OnSelectedEntityChanged));

        // Model
        public IModel Model
        {
            get { return (IModel)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        public static DependencyProperty ModelProperty =
            DependencyProperty.Register("Model", typeof(IModel), typeof(MainWindow), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits,
                                                                      OnSelectedEntityChanged));

        private static void OnSelectedEntityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {


            //// if any UI event should happen it needs to be specified here
            //var ctrl = d as MainWindow;
            //if (ctrl == null)
            //    return;
            //if (e.Property.Name != "SelectedEntity")
            //    return;
            //var newValue = e.NewValue as IPersistEntity;
            //if (newValue == null)
            //    return;
            //var validator = new IfcValidator()
            //{
            //    CreateEntityHierarchy = true,
            //    ValidateLevel = ValidationFlags.All 
            //};
            //var ret = validator.Validate(newValue);
            //var sb = new StringBuilder();
            //int issues = 0;
            //foreach (var validationResult in new ValidationReporter(ret))
            //{
            //    sb.AppendLine(validationResult);
            //    issues++;
            //}
            //if (issues==0)
            //    ctrl.Results.Text = $"0 issues {DateTime.Now.ToLongTimeString()}";
            //else
            //{
            //    ctrl.Results.Text = sb.ToString();
            //}


        }
    }
}
