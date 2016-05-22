#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     XbimXplorer
// Filename:    XplorerMainWindow.xaml.cs
// Published:   01, 2012
// Last Edited: 9:05 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using log4net;
using log4net.Repository.Hierarchy;
using Microsoft.Win32;
using Xbim.Common;
using Xbim.Common.Geometry;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.ModelGeometry.Scene;
using Xbim.Presentation;
using Xbim.Presentation.FederatedModel;
using Xbim.Presentation.LayerStyling;
using Xbim.Presentation.ModelGeomInfo;
using Xbim.Presentation.XplorerPluginSystem;
using XbimXplorer.Dialogs;
using XbimXplorer.LogViewer;
using XbimXplorer.Properties;
using Xceed.Wpf.AvalonDock.Layout.Serialization;

#endregion

namespace XbimXplorer
{
    /// <summary>
    ///   Interaction logic for Window1.xaml
    /// </summary>
    public partial class XplorerMainWindow : IXbimXplorerPluginMasterWindow, INotifyPropertyChanged
    {
        private static readonly ILog Log = LogManager.GetLogger("Xbim.WinUI");

        public Visibility AnyErrors
        {
            get
            {
                return NumErrors > 0 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            }
        }

        public int NumErrors { get; private set; }

        public Visibility AnyWarnings
        {
            get
            {
                return NumWarnings > 0 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            }
        }

        public int NumWarnings { get; private set; }
        
        private BackgroundWorker _worker;
        /// <summary>
        /// 
        /// </summary>
        public static RoutedCommand CreateFederationCmd = new RoutedCommand();
        /// <summary>
        /// 
        /// </summary>
        public static RoutedCommand EditFederationCmd = new RoutedCommand();
        
        /// <summary>
        /// 
        /// </summary>
        public static RoutedCommand OpenExportWindowCmd = new RoutedCommand();
        /// <summary>
        /// 
        /// </summary>
        public static RoutedCommand ExportCoBieCmd = new RoutedCommand();
        /// <summary>
        /// 
        /// </summary>
        public static RoutedCommand CoBieClassFilter = new RoutedCommand();
        
        private string _temporaryXbimFileName;

        private string _openedModelFileName;

        /// <summary>
        /// Deals with the user-defined model file name.
        /// The underlying XbimModel might be pointing to a temporary file elsewhere.
        /// </summary>
        /// <returns>String pointing to the file or null if the file is not defined (e.g. not saved federation).</returns>
        public string GetOpenedModelFileName()
        {
            return _openedModelFileName;
        }

        private void SetOpenedModelFileName(string ifcFilename)
        {
            _openedModelFileName = ifcFilename;

            Dispatcher.BeginInvoke(new Action(delegate
            {
                // Do your work
                Title = string.IsNullOrEmpty(ifcFilename)
                    ? "Xbim Xplorer" :
                    "Xbim Xplorer - [" + ifcFilename + "]";
            }));
        }
        
        private EventAppender _appender;

        private bool _blockPlugin;

        public XplorerMainWindow(bool blockPlugin = false)
        {
            InitializeComponent();
            _blockPlugin = blockPlugin;

            // initialise the internal elements of the UI that behave like plugins
            EvaluateXbimUiType(typeof(LogViewer.LogViewer));
            EvaluateXbimUiType(typeof(Commands.wdwCommands));


            Closed += XplorerMainWindow_Closed;
            Loaded += XplorerMainWindow_Loaded;
            Closing += XplorerMainWindow_Closing;
            DrawingControl.UserModeledDimensionChangedEvent += DrawingControl_MeasureChangedEvent;
            InitFromSettings();
            RefreshRecentFiles();

            // logging 
            LoggedEvents = new ObservableCollection<EventViewModel>();
        }

        public ObservableCollection<EventViewModel> LoggedEvents { get; private set; }

        internal void appender_Logged(object sender, LogEventArgs e)
        {
            foreach (var loggingEvent in e.LoggingEvents)
            {
                var m = new EventViewModel(loggingEvent);
                Application.Current.Dispatcher.BeginInvoke((Action) delegate {
                    LoggedEvents.Add(m);
                });
                Application.Current.Dispatcher.BeginInvoke((Action) UpdateLoggerCounts);
            }
        }

        internal void UpdateLoggerCounts()
        {
            NumErrors = 0;
            NumWarnings = 0;
            foreach (var loggedEvent in LoggedEvents)
            {
                switch (loggedEvent.Level)
                {
                    case "ERROR":
                        NumErrors++;
                        break;
                    case "WARN":
                        NumWarnings++;
                        break;
                }
            }
            OnPropertyChanged("AnyErrors");
            OnPropertyChanged("NumErrors");
            OnPropertyChanged("AnyWarnings");
            OnPropertyChanged("NumWarnings");
        }

        public Visibility DeveloperVisible
        {
            get {
                return Settings.Default.DeveloperMode 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            }
        }
            

        private void InitFromSettings()
        {          
            OnPropertyChanged("DeveloperVisible");           
        }

        private ObservableMruList<string> _mruFiles = new ObservableMruList<string>();
        private void RefreshRecentFiles()
        {
            var s = new List<string>();
            if (Settings.Default.MRUFiles != null)
                s.AddRange(Settings.Default.MRUFiles.Cast<string>());

            _mruFiles = new ObservableMruList<string>(s, 4, StringComparer.InvariantCultureIgnoreCase);
            MnuRecent.ItemsSource = _mruFiles;
        }

        private void AddRecentFile()
        {
            _mruFiles.Add(_openedModelFileName);
            Settings.Default.MRUFiles = new StringCollection();
            foreach (var item in _mruFiles)
            {
                Settings.Default.MRUFiles.Add(item);
            }
            Settings.Default.Save();
        }

        private void DrawingControl_MeasureChangedEvent(DrawingControl3D m, PolylineGeomInfo e)
        {
            if (e != null)
            {
                EntityLabel.Text = e.ToString();
                Debug.WriteLine("Points:");
                foreach (var pt in e.VisualPoints)
                {
                    Debug.WriteLine("X:{0} Y:{1} Z:{2}", pt.X, pt.Y, pt.Z);
                }
            }
        }
        
        #region "Model File Operations"

        void XplorerMainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (_worker != null && _worker.IsBusy)
                e.Cancel = true; //do nothing if a thread is alive
            else
                e.Cancel = false;

        }

        void XplorerMainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // todo: inspect unexpected behaviour in Logging system
            // Claudio Benghi's Note:
            // For reasons I am failing to understand if I remove the following line whole Logging system does not work.
            Xbim.Ifc2x3.IO.XbimModel.CreateTemporaryModel();
            // somehow the line avove is needed for the Logging system to funcion

            var model = IfcStore.Create(null,IfcSchemaVersion.Ifc2X3, XbimStoreType.InMemoryModel);
            ModelProvider.ObjectInstance = model;
            ModelProvider.Refresh();

            // logging information warnings
            _appender = new EventAppender {Tag = "MainWindow"};
            _appender.Logged += appender_Logged;

            var hier = LogManager.GetRepository() as Hierarchy;
            if (hier != null)
                hier.Root.AddAppender(_appender);

        }

        void XplorerMainWindow_Closed(object sender, EventArgs e)
        {
            CloseAndDeleteTemporaryFiles();
        }


        private void OpenIfcFile(object s, DoWorkEventArgs args)
        {
            var worker = s as BackgroundWorker;
            var ifcFilename = args.Argument as string;


            try
            {
                if (worker == null) throw new Exception("Background thread could not be accessed");
                _temporaryXbimFileName = Path.GetTempFileName();
                SetOpenedModelFileName(ifcFilename);

                var model = IfcStore.Open(ifcFilename, null, null, worker.ReportProgress);
                if (model.GeometryStore.IsEmpty)
                {
                    var context = new Xbim3DModelContext(model);
                        //upgrade to new geometry representation, uses the default 3D model
                    context.CreateContext(progDelegate: worker.ReportProgress);
                }
                foreach (var modelReference in model.ReferencedModels)
                {
                    Debug.WriteLine(modelReference.Name);
                    if (modelReference.Model != null)
                    {
                        if (modelReference.Model.GeometryStore.IsEmpty)
                        {
                            var context = new Xbim3DModelContext(modelReference.Model);
                            //upgrade to new geometry representation, uses the default 3D model
                            context.CreateContext(progDelegate: worker.ReportProgress);
                        }
                    }
                }
                if (worker.CancellationPending) //if a cancellation has been requested then don't open the resulting file
                {
                    try
                    {
                        model.Close();
                        if (File.Exists(_temporaryXbimFileName))
                            File.Delete(_temporaryXbimFileName); //tidy up;
                        _temporaryXbimFileName = null;
                        SetOpenedModelFileName(null);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Message, ex);
                    }
                    return;
                }
                args.Result = model;
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Error reading " + ifcFilename);
                var indent = "\t";
                while (ex != null)
                {
                    sb.AppendLine(indent + ex.Message);
                    ex = ex.InnerException;
                    indent += "\t";
                }

                args.Result = new Exception(sb.ToString());
            }
        }

      
      

        private void dlg_OpenAnyFile(object sender, CancelEventArgs e)
        {
            var dlg = sender as OpenFileDialog;
            if (dlg != null) LoadAnyModel(dlg.FileName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="modelFileName"></param>
        public void LoadAnyModel(string modelFileName)
        {
            var fInfo = new FileInfo(modelFileName);
            if (!fInfo.Exists) // file does not exist; do nothing
                return;
            if (fInfo.FullName.ToLower() == GetOpenedModelFileName()) //same file do nothing
                return;

            // there's no going back; if it fails after this point the current file should be closed anyway
            CloseAndDeleteTemporaryFiles();

            SetOpenedModelFileName(modelFileName.ToLower());

            ProgressStatusBar.Visibility = Visibility.Visible;
            CreateWorker();

            var ext = fInfo.Extension.ToLower();
            switch (ext)
            {
                case ".ifc": //it is an Ifc File
                case ".ifcxml": //it is an IfcXml File
                case ".ifczip": //it is a xip file containing xbim or ifc File
                case ".zip": //it is a xip file containing xbim or ifc File
                case ".xbimf":
                case ".xbim":
                    _worker.DoWork += OpenIfcFile;
                    _worker.RunWorkerAsync(modelFileName);
                    break;              
            }           
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="args"></param>
        public delegate void LoadingCompleteEventHandler(object s, RunWorkerCompletedEventArgs args);
        /// <summary>
        /// 
        /// </summary>
        public event LoadingCompleteEventHandler LoadingComplete;

        private void FireLoadingComplete(object s, RunWorkerCompletedEventArgs args)
        {
            if (LoadingComplete != null)
            {
                LoadingComplete(s, args);
            }
        }

        private void CreateWorker()
        {
            _worker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            _worker.ProgressChanged += delegate(object s, ProgressChangedEventArgs args)
            {
                if (args.ProgressPercentage < 0 || args.ProgressPercentage > 100) 
                    return;
                ProgressBar.Value = args.ProgressPercentage;
                StatusMsg.Text = (string) args.UserState;
            };

            _worker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs args)
            {
                if (args.Result is IfcStore) //all ok
                {
                    ModelProvider.ObjectInstance = args.Result; //this Triggers the event to load the model into the views 
                    // PropertiesControl.Model = (XbimModel)args.Result; // // done thtough binding in xaml
                    ModelProvider.Refresh();
                    ProgressBar.Value = 0;
                    StatusMsg.Text = "Ready";
                    AddRecentFile();
                }
                else //we have a problem
                {
                    var errMsg = args.Result as string;
                    if (!string.IsNullOrEmpty(errMsg))
                        MessageBox.Show(this, errMsg, "Error Opening File",
                                        MessageBoxButton.OK, MessageBoxImage.Error,
                                        MessageBoxResult.None, MessageBoxOptions.None);
                    var exception = args.Result as Exception;
                    if (exception != null)
                    {
                        var sb = new StringBuilder();
                        
                        var indent = "";
                        while (exception != null)
                        {
                            sb.AppendFormat("{0}{1}\n", indent, exception.Message);
                            exception = exception.InnerException;
                            indent += "\t";
                        }
                        MessageBox.Show(this, sb.ToString(), "Error Opening Ifc File",
                                        MessageBoxButton.OK, MessageBoxImage.Error,
                                        MessageBoxResult.None, MessageBoxOptions.None);
                    }
                    ProgressBar.Value = 0;
                    StatusMsg.Text = "Error/Ready";
                }
                FireLoadingComplete(s, args);
            };
        }

        private void dlg_FileSaveAs(object sender, CancelEventArgs e)
        {
            var dlg = sender as SaveFileDialog;
            if (dlg != null)
            {
                var fInfo = new FileInfo(dlg.FileName);
                try
                {
                    if (fInfo.Exists)
                    {
                        // the user has been asked to confirm deletion previously
                        fInfo.Delete();
                    }
                    if (Model != null)
                    {
                        Model.SaveAs(dlg.FileName);
                        SetOpenedModelFileName(dlg.FileName);
                        var s = Path.GetExtension(dlg.FileName);
                        if (string.IsNullOrWhiteSpace(s)) 
                            return;
                        var extension = s.ToLowerInvariant();
                        if (extension != "xbim" || string.IsNullOrWhiteSpace(_temporaryXbimFileName)) 
                            return;
                        File.Delete(_temporaryXbimFileName);
                        _temporaryXbimFileName = null;
                    }
                    else throw new Exception("Invalid Model Server");
                }
                catch (Exception except)
                {
                    MessageBox.Show(except.Message, "Error Saving as", MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                }
            }
        }



        private void CommandBinding_SaveAs(object sender, ExecutedRoutedEventArgs e)
        {
            var dlg = new SaveFileDialog();
            if (GetOpenedModelFileName() != null)
            {
                var f = new FileInfo(GetOpenedModelFileName());
                dlg.DefaultExt = f.Extension;
                dlg.InitialDirectory = f.DirectoryName;
                dlg.FileName = f.Name;
            }

            // filter starts with basic xbim formats
            var corefilters = new[] {
                    "Ifc File (*.ifc)|*.ifc",
                    "xBIM File (*.xBIM)|*.xBIM", 
                    "IfcXml File (*.IfcXml)|*.ifcxml", 
                    "IfcZip File (*.IfcZip)|*.ifczip"
                };

            dlg.Filter = string.Join("|", corefilters);
            dlg.Title = "Save As";
            dlg.AddExtension = true;

            // Show open file dialog box 
            dlg.FileOk += dlg_FileSaveAs;
            dlg.ShowDialog(this);
        }

        private void CommandBinding_Close(object sender, ExecutedRoutedEventArgs e)
        {
            CloseAndDeleteTemporaryFiles();
        }

        private void CommandBinding_Open(object sender, ExecutedRoutedEventArgs e)
        {
            // Filter files by extension 
            var dlg = new OpenFileDialog {Filter = "Xbim Files|*.xbim;*.xbimf;*.ifc;*.ifcxml;*.ifczip"};
            dlg.FileOk += dlg_OpenAnyFile;
            dlg.ShowDialog(this);
        }

        /// <summary>
        /// Tidies up any open files and closes any open models
        /// </summary>
        private void CloseAndDeleteTemporaryFiles()
        {
            try
            {
                if (_worker != null && _worker.IsBusy)
                    _worker.CancelAsync(); //tell it to stop
                
                SetOpenedModelFileName(null);
                if (Model != null)
                {
                    Model.Dispose();
                    ModelProvider.ObjectInstance = null;
                    ModelProvider.Refresh();
                }
                SetDefaultModeStyler(null, null);
            }
            finally
            {
                if (!(_worker != null && _worker.IsBusy && _worker.CancellationPending)) //it is still busy but has been cancelled 
                {
                    if (!string.IsNullOrWhiteSpace(_temporaryXbimFileName) && File.Exists(_temporaryXbimFileName))
                        File.Delete(_temporaryXbimFileName);
                    _temporaryXbimFileName = null;
                } //else do nothing it will be cleared up in the worker thread
            }
        }

        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (_worker != null && _worker.IsBusy)
                e.CanExecute = false;
            else
            {
                if (e.Command == ApplicationCommands.Close || e.Command == ApplicationCommands.SaveAs)
                {
                    
                    e.CanExecute = (Model != null);
                }
                else if (e.Command == OpenExportWindowCmd)
                {
                    
                    e.CanExecute = (Model != null) && (!string.IsNullOrEmpty(GetOpenedModelFileName()));
                }
                else
                    e.CanExecute = true; //for everything else
            }
        }


        #endregion

        # region "Federation Model operations"
        private void EditFederationCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var fdlg = new FederatedModelDialog {DataContext = Model};
            var done = fdlg.ShowDialog();
            if (done.HasValue && done.Value)
            {
                // todo: is there something that needs to happen here?
            }
            DrawingControl.ReloadModel();
        }
        private void EditFederationCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Model != null && Model.IsFederation;
        }
       
        private void CreateFederationCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Select model files to federate.",
                Filter = "Model Files|*.ifc;*.ifcxml;*.ifczip", // Filter files by extension 
                CheckFileExists = true,
                Multiselect = true
            };
            

            var done = dlg.ShowDialog(this);

            if (!done.Value)
                return;

            FederationFromDialogbox(dlg);
        }


        private void FederationFromDialogbox(OpenFileDialog dlg)
        {
            if (!dlg.FileNames.Any())
                return;
            //use the first filename it's extension to decide which action should happen
            var s = Path.GetExtension(dlg.FileNames[0]);
            if (s == null)
                return;
            var firstExtension = s.ToLower();

            IfcStore fedModel = null;
            switch (firstExtension)
            {
                case ".xbimf":
                    if (dlg.FileNames.Length > 1)
                    {
                        var res = MessageBox.Show("Multiple files selected, open " + dlg.FileNames[0] + "?",
                            "Cannot open multiple Xbim files",
                            MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                        if (res == MessageBoxResult.Cancel)
                            return;
                        fedModel = IfcStore.Open(dlg.FileNames[0], null, true);
                    }
                    break;
                case ".ifc":
                case ".ifczip":
                case ".ifcxml":
                    // create temp file as a placeholder for the temporory xbim file                   
                    fedModel = IfcStore.Create(null,IfcSchemaVersion.Ifc2X3, XbimStoreType.InMemoryModel);                    
                    using (var txn = fedModel.BeginTransaction())
                    {
                        var project = fedModel.Instances.New<Xbim.Ifc2x3.Kernel.IfcProject>();
                        project.Name = "Default Project Name";
                        project.Initialize(ProjectUnits.SIUnitsUK);
                        txn.Commit();
                    }

                    var informUser = true;
                    for (var i = 0; i < dlg.FileNames.Length; i++)
                    {
                        var fileName = dlg.FileNames[i];
                        var temporaryReference = new XbimReferencedModelViewModel
                        {
                            Name = fileName,
                            OrganisationName = "OrganisationName " + i,
                            OrganisationRole = "Undefined"
                        };

                        var buildRes = false;
                        Exception exception = null;
                        try
                        {
                            buildRes = temporaryReference.TryBuildAndAddTo(fedModel);
                        }
                        catch (Exception ex)
                        {
                            //usually an EsentDatabaseSharingViolationException, user needs to close db first
                            exception = ex;
                        }

                        if (buildRes || !informUser)
                            continue;
                        var msg = exception == null ? "" : "\r\nMessage: " + exception.Message;
                        var res = MessageBox.Show(fileName + " couldn't be opened." + msg + "\r\nShow this message again?",
                            "Failed to open a file", MessageBoxButton.YesNoCancel, MessageBoxImage.Error);
                        if (res == MessageBoxResult.No)
                            informUser = false;
                        else if (res == MessageBoxResult.Cancel)
                        {
                            fedModel = null;
                            break;
                        }
                    }
                    break;
            }
            if (fedModel == null)
                return;
            CloseAndDeleteTemporaryFiles();
            ModelProvider.ObjectInstance = fedModel;
            ModelProvider.Refresh();
        }
        
        #endregion

        /// <summary>
        /// 
        /// </summary>
        public IPersistEntity SelectedItem
        {
            get { return (IPersistEntity)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedItem.  This enables animation, styling, binding, etc...
        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(IPersistEntity), typeof(XplorerMainWindow),
                                        new UIPropertyMetadata(null, OnSelectedItemChanged));


        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var mw = d as XplorerMainWindow;
            if (mw != null && e.NewValue is IPersistEntity)
            {
                var label = (IPersistEntity)e.NewValue;
                mw.EntityLabel.Text = label != null ? "#" + label.EntityLabel : "";
            }
            else if (mw != null) mw.EntityLabel.Text = "";
        }


        private ObjectDataProvider ModelProvider
        {
            get
            {
                return MainFrame.DataContext as ObjectDataProvider;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IfcStore Model
        {
            get
            {
                var op = MainFrame.DataContext as ObjectDataProvider;
                return op == null ? null : op.ObjectInstance as IfcStore;
            }
        }

        // this variable is used to determine when the user is trying again to double click on the selected item
        // from this we detect that he's probably not happy with the view, therefore we add a cutting plane to make the 
        // element visible.
        //
        private bool _camChanged;
        
        private void SpatialControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            _camChanged = false;
            DrawingControl.Viewport.Camera.Changed += Camera_Changed;
            DrawingControl.ZoomSelected();
            DrawingControl.Viewport.Camera.Changed -= Camera_Changed;
            if (!_camChanged)
                DrawingControl.ClipBaseSelected(0.15);
        }

        void Camera_Changed(object sender, EventArgs e)
        {
            _camChanged = true;
        }


        private void MenuItem_ZoomExtents(object sender, RoutedEventArgs e)
        {
            DrawingControl.ViewHome();
        }
        
        private void OpenExportWindow(object sender, ExecutedRoutedEventArgs executedRoutedEventArgs)
        {
            var wndw = new ExportWindow(this);
            wndw.ShowDialog();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            var w = new AboutWindow {Model = Model};
            w.Show();
        }
        
        private void DisplaySettingsPage(object sender, RoutedEventArgs e)
        {
            var sett = new SettingsWindow();
            sett.ShowDialog();
            if (sett.SettingsChanged)
                InitFromSettings();
        }

        private void RecentFileClick(object sender, RoutedEventArgs e)
        {
            var obMenuItem = e.OriginalSource as MenuItem;
            if (obMenuItem != null)
            {
                var fileName = obMenuItem.Header.ToString();
                if (!File.Exists(fileName))
                {
                    return;
                }
                LoadAnyModel(fileName);
            }
        }

        private void SetDefaultModeStyler(object sender, RoutedEventArgs e)
        {           
            DrawingControl.DefaultLayerStyler = new SurfaceLayerStyler();
            DrawingControl.ReloadModel();
        }

        [Obsolete]
        private void SetStylerVersion1(object sender, RoutedEventArgs e)
        {
            DrawingControl.ReloadModel();
        }

        [Obsolete]
        private void SetFederationStylerRole(object sender, RoutedEventArgs e)
        {
            
            DrawingControl.ReloadModel();
        }

        [Obsolete]
        private void SetFederationStylerType(object sender, RoutedEventArgs e)
        {
            DrawingControl.ReloadModel();
        }

        DrawingControl3D IXbimXplorerPluginMasterWindow.DrawingControl
        {
            get { return DrawingControl; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OpenWindow(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            if (mi == null)
                return;
            OpenOrFocusPluginWindow(mi.Tag as Type);
        }

        private void ShowErrors(object sender, MouseButtonEventArgs e)
        {
            OpenOrFocusPluginWindow(typeof (LogViewer.LogViewer));
        }

        private void Exit(object sender, RoutedEventArgs e)
        {
            using (var fs = new StringWriter())
            {
                var xmlLayout = new XmlLayoutSerializer(DockingManager);
                xmlLayout.Serialize(fs);
                var xmlLayoutString = fs.ToString();
                Clipboard.SetText(xmlLayoutString);
            }
            Close();
        }

        private void RenderedEvents(object sender, EventArgs e)
        {
            // command line arg can prevent plugin loading
            if (Settings.Default.PluginStartupLoad && !_blockPlugin)
                RefreshPlugins();
        }
        
        private void EntityLabel_KeyDown()
        {
            var str = EntityLabel.Text.Trim(new[] { ' ', '#' });
            int isLabel;
            if (Int32.TryParse(str, out isLabel))
            {
                var entity = Model.Instances[isLabel];
                if (entity != null)
                    SelectedItem = entity;
            }
        }
    }
}
