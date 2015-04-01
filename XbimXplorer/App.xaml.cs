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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shell;
using Xbim.XbimExtensions;

#endregion

namespace XbimXplorer
{
    /// <summary>
    ///   Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region JumpList
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
        #endregion

        protected override void OnStartup(StartupEventArgs e)
        {
            var mainView = new XplorerMainWindow();
            mainView.Show();
            bool bOneModelLoaded = false;
            for (int i = 0; i< e.Args.Length; i++)
            {
                string thisArg = e.Args[i];
                if (string.Compare("/AccessMode", thisArg, true) == 0)
                {
                    string StringMode = e.Args[++i];
                    XbimDBAccess mode = (XbimDBAccess)Enum.Parse(typeof(XbimDBAccess), StringMode);
                    if (Enum.TryParse(StringMode, out mode))
                    {
                        mainView.FileAccessMode = mode;
                    }
                }
                else if (string.Compare("/plugin", thisArg, true) == 0)
                {
                    string PluginName = e.Args[++i];
                    mainView.LoadPlugin(PluginName);
                }
                else if (string.Compare("/select", thisArg, true) == 0)
                {
                    string SelLabel = e.Args[++i];
                    Debug.Write("Select " + SelLabel + "... ");
                    mainView.LoadingComplete += delegate(object s, RunWorkerCompletedEventArgs args)
                    {
                        int iSel;
                        if (!int.TryParse(SelLabel, out iSel))
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