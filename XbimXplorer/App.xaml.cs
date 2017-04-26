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
using System.Linq;
using System.Reflection;
using System.Windows;
using log4net;
using NuGet;
using Squirrel;
using Xbim.IO.Esent;
using System.Collections.Generic;

#endregion

namespace XbimXplorer
{
    /// <summary>
    ///   Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private static readonly ILog Log = LogManager.GetLogger("XbimXplorer.App");

        internal static bool IsSquirrelInstall
        {
            get
            {
                var sFold = SquirrelFolder();
                if (string.IsNullOrEmpty(sFold))
                    return false;
                var updateDotExe = Path.Combine(sFold, "Update.exe");
                return File.Exists(updateDotExe);
            }
        }

        private static async void Update()
        {
            if (!IsSquirrelInstall)
                return;
            try
            {
                using (var mgr = new UpdateManager("http://www.overarching.it/dload/XbimXplorer"))
                {
                    await mgr.UpdateApp();
                    mgr.Dispose();
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error in UpdateManager.", ex);
            }
        }

        private static string SquirrelFolder()
        {
            //var t = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            //t = Path.Combine(t, "Xbim");
            //return t;

            var assembly = Assembly.GetEntryAssembly();
            if (assembly?.Location == null)
                return "";
            var loc = assembly.Location;
            var dirName = Path.GetDirectoryName(loc);
            return string.IsNullOrEmpty(dirName) 
                ? "" 
                : Path.Combine(dirName, "..");
        }

        internal static void PortPlugins()
        {
            if (!IsSquirrelInstall)
            {
                Log.Info("Application is not under Squirrel installer. Nothing done.");
                return;
            }
            var sf = SquirrelFolder();
            if (string.IsNullOrEmpty(sf))
            {
                Log.Info("Squirrel folder not found. Nothing done.");
                return;
            }
            var dsf = new DirectoryInfo(sf);
            var stringVersions = dsf.GetDirectories("app-*").Select(x => x.Name.Substring(4)).ToArray();

            var vrs = new List<SemanticVersion>();
            foreach (var version in stringVersions)
            {
                try
                {
                    var t = new SemanticVersion(version);
                    vrs.Add(t);
                    Log.Info($"Version found: {version}.");
                }
                catch (Exception ex)
                {
                    Log.Error($"Error converting semver: {version}.", ex);
                }
            }

            vrs.Sort();
            if (vrs.Count < 2)
            {
                Log.Info($"Low version count; nothing to do.");
                return;
            }
            var latest = PluginFolder(vrs[vrs.Count - 1]);
            Log.Info($"Latest to test: {latest.FullName}");
            var prev = PluginFolder(vrs[vrs.Count - 2]);
            Log.Info($"Previous to test: {prev.FullName}");
            if (latest.Exists)
                // if it's already been created we ignore the case, 
                // if plugins get deleted we don't want them back
                return;

            // make the directory so the action is not repeted.
            //
            Directory.CreateDirectory(latest.FullName);

            // check if there's nothing to copy anyway
            if (!prev.Exists)
                return;

            Log.Info("Attempting copy.");
            // perform the copy
            DirectoryCopy(prev, latest.FullName, true);
            Log.Info("Completed copy.");
        }

        // taken from https://msdn.microsoft.com/en-us/library/bb762914(v=vs.110).aspx
        // then changed signature for local implementation
        private static void DirectoryCopy(DirectoryInfo dir, string destDirName, bool copySubDirs)
        {
            if (!dir.Exists)
            {
                return;
            }
            var dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            var files = dir.GetFiles();
            foreach (var file in files)
            {
                var temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (!copySubDirs)
                return;
            foreach (var subdir in dirs)
            {
                var temppath = Path.Combine(destDirName, subdir.Name);
                DirectoryCopy(subdir, temppath, true);
            }            
        }

        private static DirectoryInfo PluginFolder(SemanticVersion version)
        {
            var versionAppFolder = "app-" + version;
            var sf = SquirrelFolder();
            var cmb = Path.Combine(sf, versionAppFolder);
            cmb = Path.Combine(cmb, "Plugins");
            return new DirectoryInfo(cmb);
        }
        
        /// <summary>
        /// Raises the <see cref="E:System.Windows.Application.Startup"/> event.
        /// </summary>
        /// <param name="e">A <see cref="T:System.Windows.StartupEventArgs"/> that contains the event data.</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            // evaluate special parameters before loading MainWindow
            var blockUpdate = false;
            foreach (var thisArg in e.Args)
            {
                if (string.Compare("/noupdate", thisArg, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    blockUpdate = true;
                }
            }
            if (!blockUpdate)
                Update();

            PortPlugins();
            

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
            if (XbimXplorer.Properties.Settings.Default.SettingsUpdateRequired)
            {
                XbimXplorer.Properties.Settings.Default.Upgrade();
                XbimXplorer.Properties.Settings.Default.SettingsUpdateRequired = false;
                XbimXplorer.Properties.Settings.Default.Save();
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