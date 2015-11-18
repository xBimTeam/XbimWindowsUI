using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Xbim.Ifc2x3.GeometricModelResource;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.UtilityResource;
using Xbim.IO;

namespace XbimXplorer.Dialogs
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {

        private Assembly assembly;

        public AboutWindow()
        {
            InitializeComponent();
            DataContext = this;
            Logo.Source = new BitmapImage(new Uri(@"pack://application:,,/xBIM.ico", UriKind.RelativeOrAbsolute));
            assembly = Assembly.GetEntryAssembly();
        }

        private void AboutWindow_OnDeactivated(object sender, EventArgs e)
        {
            Close();
        }

        public string AppVersion
        {
            get { return string.Format("Assembly Version: {0}", assembly.GetName().Version); }
        }
        public string FileVersion
        {
            get
            {
                var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                return string.Format("File Version: {0}", fvi.FileVersion);
            }
        }


        public string ModelInfo
        {
            get
            {
                var sb = new StringBuilder();
                if (Model != null)
                {
                    sb.AppendLine("File information:");
                    sb.AppendFormat("- {0} (GeometrySupport: {1})\r\n", Model.DatabaseName, Model.GeometrySupportLevel);
                    foreach (var subModel in Model.ReferencedModels)
                    {
                        sb.AppendFormat("- {0} (GeometrySupport: {1})\r\n", subModel.Model.DatabaseName, subModel.Model.GeometrySupportLevel);
                    }
                    sb.AppendLine();
                    sb.AppendLine("Model information:");
                    var ohs = Model.Instances.OfType<IfcOwnerHistory>().FirstOrDefault();
                    sb.AppendFormat("Entities count: {0}\r\n", Model.Instances.Count);
                    sb.AppendFormat("Schema: {0}\r\n", Model.Header.FileSchema.Schemas.FirstOrDefault());
                    sb.AppendFormat("Description: {0}\r\n", Model.Header.FileDescription.Description.FirstOrDefault());
                    sb.AppendFormat("ImplementationLevel: {0}\r\n", Model.Header.FileDescription.ImplementationLevel);
                    sb.AppendFormat("GeometryEntries count: {0}\r\n", Model.GeometriesCount);
                    sb.AppendFormat("IfcProduct count: {0}\r\n", Model.Instances.CountOf<IfcProduct>());
                    sb.AppendFormat("IfcSolidModel count: {0}\r\n", Model.Instances.CountOf<IfcSolidModel>());
                    sb.AppendFormat("IfcMappedItem count: {0}\r\n", Model.Instances.CountOf<IfcMappedItem>());
                    sb.AppendFormat("IfcBooleanResult count: {0}\r\n", Model.Instances.CountOf<IfcBooleanResult>());
                    sb.AppendFormat("BReps count: {0}\r\n", 
                        Model.Instances.CountOf<IfcFaceBasedSurfaceModel>() + 
                        Model.Instances.CountOf<IfcShellBasedSurfaceModel>() + 
                        Model.Instances.CountOf<IfcManifoldSolidBrep>() 
                        );
                    sb.AppendFormat("Application: {0}\r\n",
                            ohs != null 
                                ? ohs.OwningApplication.ToString()
                                : "<Null>" 
                        );
                }
                else
                {
                    sb.AppendLine("No model opened.");
                }
                return sb.ToString();
            }
        }

        public XbimModel Model { get; set; }

        private void UserControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs eventArgs)
        {
            DragMove();
        }

        private void Link_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
