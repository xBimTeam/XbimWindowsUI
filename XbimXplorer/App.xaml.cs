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
using Xbim.XbimExtensions;

#endregion

namespace XbimXplorer
{
    /// <summary>
    ///   Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        #region JumpList
/*
        private void JumpList_JumpItemsRejected(object sender, JumpItemsRejectedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0} Jump Items Rejected:\n", e.RejectionReasons.Count);
            for (int i = 0; i < e.RejectionReasons.Count; ++i)
            {
                if (e.RejectedItems[i].GetType() == typeof(JumpPath))
                    sb.AppendFormat("Reason: {0}\tItem: {1}\n", e.RejectionReasons[i], ((JumpPath)e.RejectedItems[i]).Path);
                else
                    sb.AppendFormat("Reason: {0}\tItem: {1}\n", e.RejectionReasons[i], ((JumpTask)e.RejectedItems[i]).ApplicationPath);
            }

            MessageBox.Show(sb.ToString());
        }
*/

/*
        private void JumpList_JumpItemsRemovedByUser(object sender, JumpItemsRemovedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0} Jump Items Removed by the user:\n", e.RemovedItems.Count);
            for (int i = 0; i < e.RemovedItems.Count; ++i)
            {
                sb.AppendFormat("{0}\n", e.RemovedItems[i]);
            }

            MessageBox.Show(sb.ToString());
        }
*/
        #endregion

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Application.Startup"/> event.
        /// </summary>
        /// <param name="e">A <see cref="T:System.Windows.StartupEventArgs"/> that contains the event data.</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            var mainView = new XplorerMainWindow();
            mainView.Show();
            bool bOneModelLoaded = false;
            for (int i = 0; i< e.Args.Length; i++)
            {
                string thisArg = e.Args[i];
                if (String.Compare("/AccessMode", thisArg, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    string stringMode = e.Args[++i];
                    XbimDBAccess mode;
                    if (Enum.TryParse(stringMode, out mode))
                    {
                        mainView.FileAccessMode = mode;
                    }
                }
                else if (String.Compare("/plugin", thisArg, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    var pluginName = e.Args[++i];
                    if (!File.Exists(pluginName))
                        MessageBox.Show(pluginName + " not found.");
                    Debug.Write("Xplorer trying to load plugin from CommandLine");
                    mainView.LoadPlugin(pluginName);
                }
                else if (string.Compare("/select", thisArg, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    string selLabel = e.Args[++i];
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