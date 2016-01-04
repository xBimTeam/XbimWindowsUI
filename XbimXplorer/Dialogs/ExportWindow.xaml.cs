using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Xbim.ModelGeometry.Scene;

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
            _mainWindow = callingWindow;
            TxtFolderName.Text = Path.Combine(
                new FileInfo(_mainWindow.GetOpenedModelFileName()).DirectoryName, 
                "Export" 
                );

        }

        private XplorerMainWindow _mainWindow;

        private void DoExport(object sender, RoutedEventArgs e)
        {
            var totExports = (ChkWexbim.IsChecked.HasValue && ChkWexbim.IsChecked.Value ? 1 : 0);
            if (totExports == 0)
                return;

            if (!Directory.Exists(TxtFolderName.Text))
            {
                try
                {
                    Directory.CreateDirectory(TxtFolderName.Text);
                }
                catch (Exception)
                {
                    MessageBox.Show("Error creating directory. Select a different location.");
                    return;
                }
            }

            Cursor = Cursors.Wait;
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
                                // todo: restore wexbim writer

                                //var geomContext = new Xbim3DModelContext(_mainWindow.Model);
                                //geomContext. Write(binaryWriter);

                                MessageBox.Show("wexbim writer temporary disabled.");
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
            Cursor = Cursors.Arrow;
            Close();
        }
                

        private string GetExportName(string extension, int progressive = 0)
        {
            var basefile = new FileInfo(_mainWindow.GetOpenedModelFileName());
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
