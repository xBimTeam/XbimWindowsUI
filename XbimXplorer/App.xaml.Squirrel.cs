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

using NuGet;
using Squirrel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using Xbim.IO;
using XbimXplorer.Properties;

#endregion

namespace XbimXplorer
{
    /// <summary>
    /// Extension logic for App for squirrel installer.
    /// </summary>
    public partial class App
    {

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

        internal static bool IsSquirrelInstall
        {
            get
            {
                var sFold = SquirrelFolder();
                if (string.IsNullOrEmpty(sFold))
                    return false;
                if (sFold.IndexOf("\\Dev\\", StringComparison.OrdinalIgnoreCase) >= 0)
                    return false;
                var updateDotExe = Path.Combine(sFold, "Update.exe");
                return File.Exists(updateDotExe);
            }
        }

        internal static void BackupSettings()
        {
            if (!IsSquirrelInstall)
                return;
            var settingsFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;
            if (!File.Exists(settingsFile))
                return;
            var destination = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\..\\last.config";
            File.Copy(settingsFile, destination, true);
        }


        internal static void RestoreSettings()
        {
            //Restore settings after application update            
            var destFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;
            var sourceFile = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\..\\last.config";
            // Check if we have settings that we need to restore
            if (!File.Exists(sourceFile))
            {
                // Nothing we need to do
                return;
            }
            // Create directory as needed
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destFile));
            }
            catch (Exception)
            {

            }

            // Copy our backup file in place 
            try
            {
                File.Copy(sourceFile, destFile, true);
            }
            catch (Exception) { }

            // Delete backup file
            try
            {
                File.Delete(sourceFile);
            }
            catch (Exception) { }

        }

        /// <returns>true if it's a first run of the new app.</returns>
        internal static bool PortPlugins()
        {
            if (!IsSquirrelInstall)
            {
                //// todo: restore squirrel log 
                //Log.Info("Application is not under Squirrel installer. Nothing done.");
                return false;
            }
            var sf = SquirrelFolder();
            if (string.IsNullOrEmpty(sf))
            {
                //// todo: restore squirrel log 
                //Log.Info("Squirrel folder not found. Nothing done.");
                return false;
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
                    //// todo: restore squirrel log 
                    //Log.Info($"Version found: {version}.");
                }
                catch (Exception ex)
                {
                    //// todo: restore squirrel log 
                    //Log.Error($"Error converting semver: {version}.", ex);
                    throw new Exception("Error in squirrel update", ex);
                }
            }

            vrs.Sort();
            if (vrs.Count < 2)
            {
                //// todo: restore squirrel log 
                // Log.Info($"Low version count; nothing to do.");
                return false;
            }
            var latest = PluginFolder(vrs[vrs.Count - 1]);

            //// todo: restore squirrel log 
            // Log.Info($"Latest to test: {latest.FullName}");
            var prev = PluginFolder(vrs[vrs.Count - 2]);

            //// todo: restore squirrel log 
            // Log.Info($"Previous to test: {prev.FullName}");
            if (latest.Exists)
                // if it's already been created we ignore the case, 
                // if plugins get deleted we don't want them back
                return false;

            // make the directory so the action is not repeted.
            //
            Directory.CreateDirectory(latest.FullName);

            // check if there's nothing to copy anyway
            if (!prev.Exists)
                return true;

            //// todo: restore squirrel log 
            //Log.Info("Attempting copy.");

            // perform the copy
            DirectoryCopy(prev, latest.FullName, true);
            //// todo: restore squirrel log 
            // Log.Info("Completed copy.");
            return true;
        }

        private static DirectoryInfo PluginFolder(SemanticVersion version)
        {
            var versionAppFolder = "app-" + version;
            var sf = SquirrelFolder();
            var cmb = Path.Combine(sf, versionAppFolder);
            cmb = Path.Combine(cmb, "Plugins");
            return new DirectoryInfo(cmb);
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

        private static async void Update()
        {
            if (!IsSquirrelInstall)
                return;
            try
            {
                var updateSource = "http://www.overarching.it/dload/XbimXplorer5";
                var ext = ".php";
                Debug.WriteLine(ext);
                using (var mgr = new UpdateManager(updateSource))
                {
                    // todo: Squirrel
                    // var t = await mgr.UpdateApp(releasesExtension: ext);
                    var t = await mgr.UpdateApp();
                    mgr.Dispose();
                    if (EqualityComparer<ReleaseEntry>.Default.Equals(t, default(ReleaseEntry)))
                        return;

                    // an update happened, backup the settings
                    BackupSettings();
                    // this will notify the XplorerMainWindow instance
                    XplorerMainWindow.AppUpdate.Execute(null, null);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                // todo: Squirrel 
                // we need to log the error
            }
        }
    }
}