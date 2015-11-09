using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using Xbim.Presentation.XplorerPluginSystem;
using Xbim.Script;
using XbimGeometry.Interfaces;

namespace XplorerPlugins.Scripting
{
    /// <summary>
    /// Interaction logic for ScriptingWindow.xaml
    /// </summary>
    [XplorerUiElement(PluginWindowUiContainerEnum.LayoutDoc, PluginWindowActivation.OnMenu, "View/Developer/Scripting")]
    public partial class ScriptingWindow : IXbimXplorerPluginWindow, INotifyPropertyChanged
    {
        /// <summary>
        /// 
        /// </summary>
        public ScriptingWindow()
        {
            InitializeComponent();
            WindowTitle = "Scripting Window";
        }

        // Model
        /// <summary>
        /// 
        /// </summary>
        public XbimModel Model
        {
            get { return (XbimModel)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public static DependencyProperty ModelProperty =
            DependencyProperty.Register("Model", typeof(XbimModel), typeof(ScriptingWindow),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, OnSelectedEntityChanged));

        private static void OnSelectedEntityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // if any UI event should happen it needs to be specified here
            var ctrl = d as ScriptingWindow;
            if (ctrl == null)
                return;
            switch (e.Property.Name)
            {
                case "Model":
                    Debug.WriteLine("Model Updated");
                    ctrl.OnPropertyChanged("Model");
                    // ModelProperty =
                    break;
                case "SelectedEntity":
                    break;
            }
        }

        private IXbimXplorerPluginMasterWindow _parentWindow;
        
        public string WindowTitle { get; private set; }
        public void BindUi(IXbimXplorerPluginMasterWindow mainWindow)
        {
            _parentWindow = mainWindow;
            SetBinding(ModelProperty, new Binding());

            ScriptingControl.OnModelChangedByScript += delegate(object o, ModelChangedEventArgs arg)
            {
                ModelProperty = null;
                var m3D = new Xbim3DModelContext(arg.NewModel);
                m3D.CreateContext(geomStorageType: XbimGeometryType.PolyhedronBinary, progDelegate: null, adjustWCS: false);
                Model = arg.NewModel;
                // todo: Fire Update request of model property (is it needed?)
                // ModelProvider.Refresh();
            };

            ScriptingControl.OnScriptParsed += delegate
            {
                // todo: Fire Update request to parent window
                // GroupControl.Regenerate();
                // SpatialControl.Regenerate();
            };
        }
    
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
