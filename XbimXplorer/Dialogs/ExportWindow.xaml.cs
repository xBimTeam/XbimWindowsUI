using System;
using System.Collections.Generic;
using System.Globalization;
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
using Xbim.COBieLite;
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

            TxtFolderName.Text =
                Path.Combine(
                    new FileInfo(mainWindow.GetOpenedModelFileName()).DirectoryName,
                    "Export"
                    );

        }

        private XplorerMainWindow mainWindow;

        private void DoExport(object sender, RoutedEventArgs e)
        {


            if (ChkWexbim.IsChecked.HasValue && ChkWexbim.IsChecked.Value &&
                !(mainWindow.GetOpenedModelFileName().EndsWith(".ifc", true, CultureInfo.InvariantCulture)))
            {
                MessageBox.Show("Wexbim only supported for IFC files at the moment.", "Warning", MessageBoxButton.OK,
                    MessageBoxImage.Information);
                ChkWexbim.IsChecked = false;
            }

            int totExports =
                (ChkWexbim.IsChecked.HasValue && ChkWexbim.IsChecked.Value ? 1 : 0) +
                (ChkCobileLiteXml.IsChecked.HasValue && ChkCobileLiteXml.IsChecked.Value ? 1 : 0) +
                (ChkCobileLiteJson.IsChecked.HasValue && ChkCobileLiteJson.IsChecked.Value ? 1 : 0);
            if (totExports == 0)
                return;

            this.Cursor = Cursors.Wait;

            if (!Directory.Exists(TxtFolderName.Text))
            {
                try
                {
                    Directory.CreateDirectory(TxtFolderName.Text);
                }
                catch (Exception)
                {
                    MessageBox.Show("Error creating directory. Select a different location.");
                }
            }

            if (ChkWexbim.IsChecked.HasValue && ChkWexbim.IsChecked.Value)
            {
                // file preparation
                //
                var wexbimFileName = GetExportName("wexbim");

                try
                {
                    DoSpecial(mainWindow.GetOpenedModelFileName(), wexbimFileName);
                    //using (var wexBimFile = new FileStream(wexbimFileName, FileMode.Create))
                    //{
                    //    using (var binaryWriter = new BinaryWriter(wexBimFile))
                    //    {
                    //        try
                    //        {
                    // NOTE: Here we need to make sure that the version is PolyhedronBinary only, if the model has been meshed with normal poly it launches an exception.
                    //            var geomContext = new Xbim3DModelContext(mainWindow.Model);
                    //            // var geomContext = new Xbim3DModelContext(mainWindow.Model);
                    //            // geomContext.CreateContext(XbimGeometryType.PolyhedronBinary);
                    //            geomContext.Write(binaryWriter);
                    //        }
                    //        finally
                    //        {
                    //            binaryWriter.Flush();
                    //            wexBimFile.Close();
                    //        }
                    //    }
                    //}
                }
                catch (Exception ce)
                {
                    if (CancelAfterNotification("Error exporting Wexbim file.", ce, totExports))
                    {
                        this.Cursor = Cursors.Arrow;
                        return;
                    }
                }
                totExports--;
            }
            if (
                (ChkCobileLiteXml.IsChecked.HasValue && ChkCobileLiteXml.IsChecked.Value) ||
                (ChkCobileLiteJson.IsChecked.HasValue && ChkCobileLiteJson.IsChecked.Value)
                )
            {

                var helper = new CoBieLiteHelper(mainWindow.Model, "UniClass");
                var facilities = helper.GetFacilities();

                if (ChkCobileLiteXml.IsChecked.HasValue && ChkCobileLiteXml.IsChecked.Value)
                {
                    try
                    {
                        var i = 0;
                        foreach (var facilityType in facilities)
                        {
                            var xportname = GetExportName(".CobieLite.XML", i);
                            using (TextWriter writer = File.CreateText(xportname))
                            {
                                CoBieLiteHelper.WriteXml(writer, facilityType);
                            }
                        }
                    }
                    catch (Exception ce)
                    {
                        if (CancelAfterNotification("Error exporting CobieLite.XML file.", ce, totExports))
                        {
                            this.Cursor = Cursors.Arrow;
                            return;
                        }
                    }
                    totExports--;
                }
                if (ChkCobileLiteJson.IsChecked.HasValue && ChkCobileLiteJson.IsChecked.Value)
                {
                    try
                    {
                        var i = 0;
                        foreach (var facilityType in facilities)
                        {
                            var xportname = GetExportName(".CobieLite.json", i);
                            using (TextWriter writer = File.CreateText(xportname))
                            {
                                CoBieLiteHelper.WriteJson(writer, facilityType);
                            }
                        }
                    }
                    catch (Exception ce)
                    {
                        if (CancelAfterNotification("Error exporting CobieLite.json file.", ce, totExports))
                        {
                            this.Cursor = Cursors.Arrow;
                            return;
                        }
                    }
                    totExports--;
                }
            }
            this.Cursor = Cursors.Arrow;
            this.Close();
        }

        private void DoSpecial(string ifcFileFullName, string wexBimFileName)
        {
            var fileName = Path.GetFileName(ifcFileFullName);
            var xbimFile = Path.GetTempFileName();
            try
            {

                using (var wexBimFile = new FileStream(wexBimFileName, FileMode.Create))
                {
                    using (var binaryWriter = new BinaryWriter(wexBimFile))
                    {

                        using (var model = new XbimModel())
                        {
                            try
                            {
                                model.CreateFrom(ifcFileFullName, xbimFile, null, true);
                                var geomContext = new Xbim3DModelContext(model);
                                geomContext.CreateContext(XbimGeometryType.PolyhedronBinary);
                                geomContext.Write(binaryWriter);
                            }
                            finally
                            {
                                model.Close();
                                binaryWriter.Flush();
                                wexBimFile.Close();
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                
            }
        }

        private string GetExportName(string extension, int progressive = 0)
        {
            var basefile = new FileInfo(mainWindow.GetOpenedModelFileName());
            var wexbimFileName = Path.Combine(TxtFolderName.Text, basefile.Name);
            if (progressive != 0)
                extension = progressive + "." + extension;
            wexbimFileName = Path.ChangeExtension(wexbimFileName, extension);
            return wexbimFileName;
        }

        private bool CancelAfterNotification(string errorZoneMessage, Exception ce, int totExports)
        {
            var tasksLeft = totExports - 1;
            var message = errorZoneMessage + "\r\n" + ce.Message + "\r\n";

            if (tasksLeft > 0)
            {
                message += "\r\n" +
                           string.Format(
                               "Do you wish to continue exporting other formats?", tasksLeft
                               );
                var ret = MessageBox.Show(message, "Error", MessageBoxButton.YesNoCancel, MessageBoxImage.Error);
                return ret != MessageBoxResult.Yes;
            }
            else
            {
                var ret = MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return ret != MessageBoxResult.Yes;
            }
        }
    }
}
