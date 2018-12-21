using System;
using System.IO;
using System.Windows;
using Xbim.Ifc;

namespace XbimXplorer.Dialogs
{
    /// <summary>
    /// Interaction logic for ExportWindow.xaml
    /// </summary>
    public partial class ExportWindow
    {
        public ExportWindow()
        {
            InitializeComponent();
        }
        
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
            
            if (ChkWexbim.IsChecked.HasValue && ChkWexbim.IsChecked.Value)
            {
                // file preparation
                //
                var wexbimFileName = GetExportName("wexbim");
                try
                {
                    using (var c = new Xbim.Presentation.WaitCursor())
                    using (var fs = new FileStream(wexbimFileName, FileMode.Create))
                    using (var bw = new BinaryWriter(fs))
                    {
                        try
                        {
                            _mainWindow.Model.SaveAsWexBim(bw);
                        }
                        finally
                        {
                            bw.Flush();
                            bw.Close();
                            fs.Close();
                        }
                    }
                }
                catch (Exception ce)
                {
                    if (CancelAfterNotification("Error exporting Wexbim file.", ce, totExports))
                    {
                        return;
                    }
                }

                // this makes sense to keep if there will be more export formats again in the future
                totExports--;
            }
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
                           $"Do you wish to continue exporting other {tasksLeft} formats?";
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
