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

using Squirrel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
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

        private static async void Update()
        {
            if (!IsSquirrelInstall)
                return;
            try
            {
                var updateSource = "http://www.overarching.it/dload/XbimXplorer";
                var ext = ".php";
                Debug.WriteLine(ext);

                //// used to test local installations
                ////
                //var di = new DirectoryInfo(@"C:\Data\dev\XbimTeam\Squirrel.Windows\XplorerReleases");
                //if (di.Exists)
                //{
                //    updateSource = di.FullName;
                //    ext = "";
                //}


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