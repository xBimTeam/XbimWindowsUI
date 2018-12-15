#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     XbimXplorer
// Filename:    App.xaml.cs
// Published:   01, 2012
// Last Edited: 9:05 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Xbim.IO;
using Xbim.IO.Esent;
using XbimXplorer.Properties;

#endregion

namespace XbimXplorer
{
    /// <summary>
    ///   Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        // todo: the whole concept of ContextWcsAdjustment need to be reviewed in the geometry engine.

        /// <summary>
        /// Todo, this feature has to do with the transformation of the model to 0,0,0 point of coordinate system
        /// Its use has to be consistent across the call to the XbimPlacementTree class
        /// </summary>
        public static bool ContextWcsAdjustment = true;

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Application.Startup"/> event.
        /// </summary>
        /// <param name="e">A <see cref="T:System.Windows.StartupEventArgs"/> that contains the event data.</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            // evaluate special parameters before loading MainWindow
            var blockPlugin = false;
            foreach (var thisArg in e.Args)
            {
                if (string.Compare("/noplugins", thisArg, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    blockPlugin = true;
                }
            }

            // see if an update of settings is required from a previous version of the app.
            // this will allow to retain the configuration across versions, it is useful for the squirrel installer
            //
            if (Settings.Default.SettingsUpdateRequired)
            {
                Settings.Default.Upgrade();
                Settings.Default.SettingsUpdateRequired = false;
                Settings.Default.Save();
            }

            var mainView = new XplorerMainWindow(blockPlugin);
            mainView.Show();
            mainView.DrawingControl.ViewHome();
            var bOneModelLoaded = false;
            for (var i = 0; i< e.Args.Length; i++)
            {
                var thisArg = e.Args[i];
                if (string.Compare("/AccessMode", thisArg, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    var stringMode = e.Args[++i];
                    XbimDBAccess acce;
                    if (Enum.TryParse(stringMode, out acce))
                    {
                        mainView.FileAccessMode = acce;
                    }
                }
                else if (string.Compare("/plugin", thisArg, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    var pluginName = e.Args[++i];
                    if (File.Exists(pluginName))
                    {
                        var fi = new FileInfo(pluginName);
                        var di = fi.Directory;
                        mainView.LoadPlugin(di, true, fi.Name);
                        continue;
                    }
                    if (Directory.Exists(pluginName) )
                    {
                        var di = new DirectoryInfo(pluginName);
                        mainView.LoadPlugin(di, true);
                        continue;
                    }
                    Clipboard.SetText(pluginName);
                    MessageBox.Show(pluginName + " not found. The full file name has been copied to clipboard.", "Plugin not found", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (string.Compare("/select", thisArg, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    var selLabel = e.Args[++i];
                    Debug.Write("Select " + selLabel + "... ");
                    mainView.LoadingComplete += delegate
                    {
                        int iSel;
                        if (!int.TryParse(selLabel, out iSel))
                            return;
                        if (mainView.Model == null)
                            return;
                        if (mainView.Model.Instances[iSel] == null)
                            return;
                        mainView.SelectedItem = mainView.Model.Instances[iSel];    
                    };
                }
                else if (File.Exists(thisArg) && bOneModelLoaded == false)
                {
                    // does not support the load of two models
                    bOneModelLoaded = true;
                    mainView.LoadAnyModel(thisArg);
                }
            }
        }
    }
}