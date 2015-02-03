using System;
using System.Collections.Generic;
using System.IO;
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

using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using XbimGeometry.Interfaces;

namespace XbimXplorer.Dialogs
{
    /// <summary>
    /// Interaction logic for ExportWindow.xaml
    /// </summary>
    public partial class ExportWindow : Window
    {
        public ExportWindow()
        {
            InitializeComponent();
        }

        public ExportWindow(XplorerMainWindow callingWindow) : this()
        {
            mainWindow = callingWindow;
            TxtFolderName.Text = new FileInfo(mainWindow.GetOpenedModelFileName()).DirectoryName;

        }

        private XplorerMainWindow mainWindow;

        private void DoExport(object sender, RoutedEventArgs e)
        {
            // file preparation
            //
            string wexbimFileName = Path.Combine(TxtFolderName.Text, new FileInfo(mainWindow.GetOpenedModelFileName()).Name + ".wexbim");
            
            var m3D = new Xbim3DModelContext(mainWindow.Model);
            try
            {
                m3D.CreateContext(geomStorageType: XbimGeometryType.PolyhedronBinary);
                var bw = new BinaryWriter(new FileStream(wexbimFileName, FileMode.Create));
                m3D.Write(bw);
                bw.Close();
            }
            catch (Exception ce)
            {
                Console.WriteLine("Error compiling geometry\n" + ce.Message);
            }
        }
    }
}
