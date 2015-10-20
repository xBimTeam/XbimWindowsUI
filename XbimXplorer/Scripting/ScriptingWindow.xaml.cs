using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using Xbim.Presentation;
using Xbim.Presentation.XplorerPluginSystem;
using Xbim.Script;
using Xbim.XbimExtensions.Interfaces;
using XbimGeometry.Interfaces;
using XbimXplorer.Querying;

namespace XbimXplorer.Scripting
{
    /// <summary>
    /// Interaction logic for ScriptingWindow.xaml
    /// </summary>
    public partial class ScriptingWindow : IXbimXplorerPluginWindow, INotifyPropertyChanged
    {

        /// <summary>
        /// 
        /// </summary>
        public ScriptingWindow()
        {
            InitializeComponent();
            MenuText = "Scripting Window";
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

        //public event ScriptParsedHandler OnScriptParsed;
        //private void ScriptParsed()
        //{
        //    if (OnScriptParsed != null)
        //        OnScriptParsed(this, new ScriptParsedEventArgs());
        //}
        public string MenuText { get; private set; }
        public string WindowTitle { get; private set; }
        public void BindUi(IXbimXplorerPluginMasterWindow mainWindow)
        {
            // win.Owner = this;
            // _parentWindow = mainWindow;
            SetBinding(ModelProperty, new Binding());
            // SetBinding(ScriptingControl.ModelProperty, new Binding());

            //win.ScriptingControl.OnModelChangedByScript += delegate(object o, ModelChangedEventArgs arg)
            //{
            //    ModelProvider.ObjectInstance = null;
            //    var m3D = new Xbim3DModelContext(arg.NewModel);
            //    m3D.CreateContext(geomStorageType: XbimGeometryType.PolyhedronBinary, progDelegate: null, adjustWCS: false);
            //    ModelProvider.ObjectInstance = arg.NewModel;
            //    ModelProvider.Refresh();
            //};

            //win.ScriptingControl.OnScriptParsed += delegate
            //{
            //    GroupControl.Regenerate();
            //    //SpatialControl.Regenerate();
            //};
        }
        public PluginWindowDefaultUiContainerEnum DefaultUiContainer
        {
            get { return PluginWindowDefaultUiContainerEnum.LayoutDoc; }
        }


        public PluginWindowDefaultUiShow DefaultUiActivation
        {
            get { return PluginWindowDefaultUiShow.OnMenu; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
