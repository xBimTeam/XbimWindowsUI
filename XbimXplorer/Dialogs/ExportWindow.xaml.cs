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
        /// <summary>
        /// 
        /// </summary>
        public ExportWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="callingWindow"></param>
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
            int totExports =
                (ChkWexbim.IsChecked.HasValue && ChkWexbim.IsChecked.Value ? 1 : 0) +
                (ChkCobileLiteXml.IsChecked.HasValue && ChkCobileLiteXml.IsChecked.Value ? 1 : 0) +
                (ChkCobileLiteJson.IsChecked.HasValue && ChkCobileLiteJson.IsChecked.Value ? 1 : 0);
            if (totExports == 0)
                return;

            Cursor = Cursors.Wait;

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
                    using (var wexBimFile = new FileStream(wexbimFileName, FileMode.Create))
                    {
                        using (var binaryWriter = new BinaryWriter(wexBimFile))
                        {
                            try
                            {
                                var geomContext = new Xbim3DModelContext(mainWindow.Model);
                                geomContext.Write(binaryWriter);
                            }
                            finally
                            {
                                binaryWriter.Flush();
                                wexBimFile.Close();
                            }
                        }
                    }
                }
                catch (Exception ce)
                {
                    if (CancelAfterNotification("Error exporting Wexbim file.", ce, totExports))
                    {
                        Cursor = Cursors.Arrow;
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
                            Cursor = Cursors.Arrow;
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
                            Cursor = Cursors.Arrow;
                            return;
                        }
                    }
                    totExports--;
                }
            }
            Cursor = Cursors.Arrow;
            Close();
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
