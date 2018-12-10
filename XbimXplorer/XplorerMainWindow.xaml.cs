#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Project:     XbimXplorer
// Published:   01, 2012

#endregion

#region Directives

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Xbim.Common;
using Xbim.Common.Metadata;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.IO.Esent;
using Xbim.ModelGeometry.Scene;
using Xbim.Presentation;
using Xbim.Presentation.FederatedModel;
using Xbim.Presentation.LayerStyling;
using Xbim.Presentation.ModelGeomInfo;
using Xbim.Presentation.XplorerPluginSystem;
using XbimXplorer.Dialogs;
using XbimXplorer.Dialogs.ExcludedTypes;
using XbimXplorer.LogViewer;
using XbimXplorer.Properties;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xbim.IO;

#endregion

namespace XbimXplorer
{
    /// <summary>
    /// Interaction logic for XplorerMainWindow
    /// </summary>
    public partial class XplorerMainWindow : IXbimXplorerPluginMasterWindow, INotifyPropertyChanged
    {
        private BackgroundWorker _loadFileBackgroundWorker;
        /// <summary>
        /// Used for the creation of a new federation file
        /// </summary>
        public static RoutedCommand CreateFederationCmd = new RoutedCommand();
        /// <summary>
        /// Edit the current federation environment
        /// </summary>
        public static RoutedCommand EditFederationCmd = new RoutedCommand();
        /// <summary>
        /// Currently supoorts the export function for Wexbim
        /// </summary>
        public static RoutedCommand OpenExportWindowCmd = new RoutedCommand();

        private string _temporaryXbimFileName;

        private string _openedModelFileName;

        protected Microsoft.Extensions.Logging.ILogger Logger { get; private set; }

        public static ILoggerFactory LoggerFactory { get; private set; }


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
            // try to update the window title through a delegate for multithreading
            Dispatcher.BeginInvoke(new Action(delegate
            {
                Title = string.IsNullOrEmpty(ifcFilename)
                    ? "Xbim Xplorer" :
                    "Xbim Xplorer - [" + ifcFilename + "]";
            }));
        }

        public const string LogOutputTemplate =
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} ({ThreadId}){NewLine}{Exception}";

        private LogEventLevel LoggingLevel { get => Settings.Default.LoggingLevel; }
        private LoggingLevelSwitch loggingLevelSwitch = new LoggingLevelSwitch(LogEventLevel.Debug);


        public XplorerMainWindow(bool preventPluginLoad = false)
        {
            // So we can use *.xbim files.
            IfcStore.ModelProviderFactory.UseHeuristicModelProvider();

            LogSink = new InMemoryLogSink { Tag = "MainWindow" };
            LogSink.Logged += LogEvent_Added;
            LogSink.EventsLimit = 1000; // log event's minute

            // Use the standard ME.LoggerFactory, but add Serilog as a provider. 
            // This provides a richer configuration and allows us to create a custom Sink for the disply of 'in app' logs
            LoggerFactory = new LoggerFactory();
            
            //loggingLevelSwitch.
            LoggerFactory.AddSerilog();
            Serilog.Log.Logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(loggingLevelSwitch)
                .WriteTo.File("XbimXplorer.txt", rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true, 
                    outputTemplate: LogOutputTemplate)
                .WriteTo.Sink(LogSink)
                .Enrich.WithThreadId()
                .Enrich.FromLogContext()
                .CreateLogger();
            // Set XBIM Essentials/Geometries's LoggerFactory - so Serilog drives everything.
            XbimLogging.LoggerFactory = LoggerFactory;

            Logger = LoggerFactory.CreateLogger<XplorerMainWindow>();

            Logger.LogInformation("Xplorer started...");

            InitializeComponent();

            PreventPluginLoad = preventPluginLoad;

            // initialise the internal elements of the UI that behave like plugins
            EvaluateXbimUiType(typeof(IfcValidation.ValidationWindow), true);
            EvaluateXbimUiType(typeof(LogViewer.LogViewer), true);
            EvaluateXbimUiType(typeof(Commands.wdwCommands), true);
            
            
            // attach window managment functions
            Closed += XplorerMainWindow_Closed;
            Loaded += XplorerMainWindow_Loaded;
            Closing += XplorerMainWindow_Closing;

            // notify the user of changes in the measures taken in the 3d viewer.
            DrawingControl.UserModeledDimensionChangedEvent += DrawingControl_MeasureChangedEvent;

            // Get the settings
            InitFromSettings();
            RefreshRecentFiles();

            // initialise the logging repository
            LoggedEvents = new ObservableCollection<EventViewModel>();
            // any logging event required should happen after XplorerMainWindow_Loaded
        }


        public Visibility DeveloperVisible => Settings.Default.DeveloperMode 
            ? Visibility.Visible 
            : Visibility.Collapsed;

        private void InitFromSettings()
        {
            FileAccessMode = Settings.Default.FileAccessMode;
            OnPropertyChanged("DeveloperVisible");
            OnPropertyChanged(nameof(LoggingLevel));
            loggingLevelSwitch.MinimumLevel = Settings.Default.LoggingLevel;
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
            if (e == null) 
                return;
            EntityLabel.Text = e.ToString();
            Debug.WriteLine("Points:");
            foreach (var pt in e.VisualPoints)
            {
                Debug.WriteLine("X:{0} Y:{1} Z:{2}", pt.X, pt.Y, pt.Z);
            }
        }
        
        #region "Model File Operations"

        void XplorerMainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (_loadFileBackgroundWorker != null && _loadFileBackgroundWorker.IsBusy)
            {
                Logger.LogWarning("Closing cancelled because of active background task.");
                e.Cancel = true; //do nothing if a thread is alive
            }
            else
                e.Cancel = false;
        }

        void XplorerMainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var model = IfcStore.Create(null, XbimSchemaVersion.Ifc2X3, XbimStoreType.InMemoryModel);
            ModelProvider.ObjectInstance = model;
            ModelProvider.Refresh();

            

            TestCRedist();
        }

        private void TestCRedist()
        {
            if (Xbim.ModelGeometry.XbimEnvironment.RedistInstalled())
                return;
            Logger.LogError("Requisite C++ environment missing, download and install from {VCPath}", 
                Xbim.ModelGeometry.XbimEnvironment.RedistDownloadPath());
        }

        private void XplorerMainWindow_Closed(object sender, EventArgs e)
        {
            CloseAndDeleteTemporaryFiles();
        }
        
        public XbimDBAccess FileAccessMode { get; set; } = XbimDBAccess.Read;
        
        private void OpenAcceptableExtension(object s, DoWorkEventArgs args)
        {
            var worker = s as BackgroundWorker;
            var selectedFilename = args.Argument as string;

            try
            {
                if (worker == null)
                    throw new Exception("Background thread could not be accessed");
                _temporaryXbimFileName = Path.GetTempFileName();
                SetOpenedModelFileName(selectedFilename);
                var model = IfcStore.Open(selectedFilename, null, null, worker.ReportProgress, FileAccessMode);
                if (_meshModel)
                {
                    // mesh direct model
                    if (model.GeometryStore.IsEmpty)
                    {
                        try
                        {
                            var context = new Xbim3DModelContext(model);
                            
                            if (!_multiThreading)
                                context.MaxThreads = 1;
#if FastExtrusion
                            context.UseSimplifiedFastExtruder = _simpleFastExtrusion;
#endif
                            SetDeflection(model);
                            //upgrade to new geometry representation, uses the default 3D model
                            context.CreateContext(worker.ReportProgress, App.ContextWcsAdjustment);
                        }
                        catch (Exception geomEx)
                        {
                            var sb = new StringBuilder();
                            sb.AppendLine($"Error creating geometry context of '{selectedFilename}' {geomEx.StackTrace}.");
                            var newexception = new Exception(sb.ToString(), geomEx);
                            Logger.LogError(0, newexception, "Error creating geometry context of {filename}", selectedFilename);
                        }
                    }

                    // mesh references
                    foreach (var modelReference in model.ReferencedModels)
                    {
                        // creates federation geometry contexts if needed
                        Debug.WriteLine(modelReference.Name);
                        if (modelReference.Model == null)
                            continue;
                        if (!modelReference.Model.GeometryStore.IsEmpty)
                            continue;
                        var context = new Xbim3DModelContext(modelReference.Model);
                        if (!_multiThreading)
                            context.MaxThreads = 1;
#if FastExtrusion
                        context.UseSimplifiedFastExtruder = _simpleFastExtrusion;
#endif
                        SetDeflection(modelReference.Model);                        
                        //upgrade to new geometry representation, uses the default 3D model
                        context.CreateContext(worker.ReportProgress, App.ContextWcsAdjustment);
                    }
                    if (worker.CancellationPending)
                    //if a cancellation has been requested then don't open the resulting file
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
                            Logger.LogError(0, ex, "Failed to cancel open of model {filename}", selectedFilename);
                        }
                        return;
                    }
                }
                else
                {
                    Logger.LogWarning("Settings prevent mesh creation.");
                }
                args.Result = model;
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Error opening '{selectedFilename}' {ex.StackTrace}.");
                var newexception = new Exception(sb.ToString(), ex);
                Logger.LogError(0, ex, "Error opening {filename}", selectedFilename);
                args.Result = newexception;
            }
        }

        private void SetDeflection(IModel model)
        {
            var mf = model.ModelFactors;
            if (mf == null)
                return;
            if (!double.IsNaN(_angularDeflectionOverride))
                mf.DeflectionAngle = _angularDeflectionOverride;
            if (!double.IsNaN(_deflectionOverride))
                mf.DeflectionTolerance = mf.OneMilliMetre * _deflectionOverride;
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
            SetWorkerForFileLoad();

            var ext = fInfo.Extension.ToLower();
            switch (ext)
            {
                case ".ifc": //it is an Ifc File
                case ".ifcxml": //it is an IfcXml File
                case ".ifczip": //it is a zip file containing xbim or ifc File
                case ".zip": //it is a zip file containing xbim or ifc File
                case ".xbimf":
                case ".xbim":
                    _loadFileBackgroundWorker.RunWorkerAsync(modelFileName);
                    break;              
                default:
                    Logger.LogWarning("Extension '{extension}' has not been recognised.", ext);
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

        private void SetWorkerForFileLoad()
        {
            _loadFileBackgroundWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            _loadFileBackgroundWorker.ProgressChanged += OnProgressChanged;
            _loadFileBackgroundWorker.DoWork += OpenAcceptableExtension;
            _loadFileBackgroundWorker.RunWorkerCompleted += FileLoadCompleted;
        }

        private void FileLoadCompleted(object s, RunWorkerCompletedEventArgs args)
        {
            if (args.Result is IfcStore) //all ok
            {
                //this Triggers the event to load the model into the views 
                ModelProvider.ObjectInstance = args.Result; 
                ModelProvider.Refresh();
                ProgressBar.Value = 0;
                StatusMsg.Text = "Ready";
                AddRecentFile();
            }
            else //we have a problem
            {
                var errMsg = args.Result as string;
                if (!string.IsNullOrEmpty(errMsg))
                    MessageBox.Show(this, errMsg, "Error Opening File", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.None);
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
                    MessageBox.Show(this, sb.ToString(), "Error Opening Ifc File", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.None);
                }
                ProgressBar.Value = 0;
                StatusMsg.Text = "Error/Ready";
                SetOpenedModelFileName("");
            }
            FireLoadingComplete(s, args);
        }

        private void OnProgressChanged(object s, ProgressChangedEventArgs args)
        {
            if (args.ProgressPercentage < 0 || args.ProgressPercentage > 100)
                return;

            Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Send,
                new Action(() =>
                {
                    ProgressBar.Value = args.ProgressPercentage;
                    StatusMsg.Text = (string) args.UserState;
                }));

        }

        private void dlg_FileSaveAs(object sender, CancelEventArgs e)
        {
            var dlg = sender as SaveFileDialog;
            if (dlg == null) 
                return;
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

        private void CommandBinding_Refresh(object sender, ExecutedRoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_openedModelFileName))
                return;
            if (!File.Exists(_openedModelFileName))
                return;
            LoadAnyModel(_openedModelFileName);
        }
        
        private void CommandBinding_SaveAs(object sender, ExecutedRoutedEventArgs e)
        {
            var dlg = new SaveFileDialog();
            var ext = "";
            if (GetOpenedModelFileName() != null)
            {
                var f = new FileInfo(GetOpenedModelFileName());
                dlg.DefaultExt = f.Extension;
                ext = f.Extension.ToLower();
                dlg.InitialDirectory = f.DirectoryName;
                dlg.FileName = f.Name;
            }

            Dictionary<string, string> options = new Dictionary<string, string>();
            options.Add(".ifc", "Ifc File (*.ifc)|*.ifc");
            options.Add(".xbim", "xBIM File (*.xBIM)|*.xBIM");
            options.Add(".ifcxml", "IfcXml File (*.IfcXml)|*.ifcxml");
            options.Add(".ifczip", "IfcZip File (*.IfcZip)|*.ifczip");

            var filters = new List<string>();
            if (options.ContainsKey(ext))
            {
                filters.Add(options[ext]);
                options.Remove(ext);
            }
            filters.AddRange(options.Values);

            // now set dialog
            dlg.Filter = string.Join("|", filters.ToArray());
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
            var corefilters = new[] {
                "Xbim Files|*.xbim;*.xbimf;*.ifc;*.ifcxml;*.ifczip",
                "Ifc File (*.ifc)|*.ifc",
                "xBIM File (*.xBIM)|*.xBIM",
                "IfcXml File (*.IfcXml)|*.ifcxml",
                "IfcZip File (*.IfcZip)|*.ifczip",
                "Zipped File (*.zip)|*.zip"
            };

            // Filter files by extension 
            var dlg = new OpenFileDialog
            {
                Filter = string.Join("|", corefilters)
            };
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
                if (_loadFileBackgroundWorker != null && _loadFileBackgroundWorker.IsBusy)
                    _loadFileBackgroundWorker.CancelAsync(); //tell it to stop
                
                SetOpenedModelFileName(null);
                if (Model != null)
                {
                    Model.Dispose();
                    ModelProvider.ObjectInstance = null;
                    ModelProvider.Refresh();
                }
                if (!(DrawingControl.DefaultLayerStyler is SurfaceLayerStyler))
                    SetDefaultModeStyler(null, null);
            }
            finally
            {
                if (!(_loadFileBackgroundWorker != null && _loadFileBackgroundWorker.IsBusy && _loadFileBackgroundWorker.CancellationPending)) //it is still busy but has been cancelled 
                {
                    if (!string.IsNullOrWhiteSpace(_temporaryXbimFileName) && File.Exists(_temporaryXbimFileName))
                        File.Delete(_temporaryXbimFileName);
                    _temporaryXbimFileName = null;
                } //else do nothing it will be cleared up in the worker thread
            }
        }

        private void CanExecuteIfFileOpen(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (Model != null) && (!string.IsNullOrEmpty(GetOpenedModelFileName()));
        }

        private void CanExecuteIfModelNotNull(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (Model != null);
        }

        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (_loadFileBackgroundWorker != null && _loadFileBackgroundWorker.IsBusy)
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

#region "Federation Model operations"
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
                        fedModel = IfcStore.Open(dlg.FileNames[0]);
                    }
                    break;
                case ".ifc":
                case ".ifczip":
                case ".ifcxml":
                    // create temp file as a placeholder for the temporory xbim file                   
                    fedModel = IfcStore.Create(null, XbimSchemaVersion.Ifc2X3, XbimStoreType.InMemoryModel);
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

        /// <summary>
        /// this variable is used to determine when the user is trying again to double click on the selected item
        /// from this we detect that he's probably not happy with the view, therefore we add a cutting plane to make the 
        /// element visible.
        /// </summary>
        private bool _camChanged;

        /// <summary>
        /// determines if models need to be meshed on opening
        /// </summary>
        private bool _meshModel = true;

        
        private double _deflectionOverride = double.NaN;
        private double _angularDeflectionOverride = double.NaN;
        
        /// <summary>
        /// determines if the geometry engine will run on parallel threads.
        /// </summary>
        private bool _multiThreading = true;

        /// <summary>
        /// determines if the geometry engine will run on parallel threads.
        /// </summary>
        private bool _simpleFastExtrusion = false;

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
            var w = new AboutWindow
            {
                Model = Model,
                Assemblies = _pluginAssemblies,
                MainWindow = this
            };
            w.Show();
        }
        
        private void DisplaySettingsPage(object sender, RoutedEventArgs e)
        {
            var sett = new SettingsWindow();
            // geom engine
            sett.ComputeGeometry.IsChecked = _meshModel;
            sett.MultiThreading.IsChecked = _multiThreading;
            sett.SimpleFastExtrusion.IsChecked = _simpleFastExtrusion;
            if (!double.IsNaN(_angularDeflectionOverride))
                sett.AngularDeflection.Text = _angularDeflectionOverride.ToString();
            if (!double.IsNaN(_deflectionOverride))
                sett.Deflection.Text = _deflectionOverride.ToString();
            
            // visuals
            sett.SimplifiedRendering.IsChecked = DrawingControl.HighSpeed;
            sett.ShowFps.IsChecked = DrawingControl.ShowFps;
            
            // show dialog
            sett.ShowDialog();
            
            
            // dialog closed
            if (!sett.SettingsChanged)
                return;
            InitFromSettings();

            // all settings that are not saved
            //

            // geom engine
            if (sett.ComputeGeometry.IsChecked != null)
                _meshModel = sett.ComputeGeometry.IsChecked.Value;
            if (sett.MultiThreading.IsChecked != null)
                _multiThreading = sett.MultiThreading.IsChecked.Value;
            if (sett.SimpleFastExtrusion.IsChecked != null)
                _simpleFastExtrusion = sett.SimpleFastExtrusion.IsChecked.Value;

            _deflectionOverride = double.NaN;
            _angularDeflectionOverride = double.NaN;
            if (!string.IsNullOrWhiteSpace(sett.AngularDeflection.Text))
                double.TryParse(sett.AngularDeflection.Text, out _angularDeflectionOverride);
            
            if (!string.IsNullOrWhiteSpace(sett.Deflection.Text))
                double.TryParse(sett.Deflection.Text, out _deflectionOverride);

            if (!string.IsNullOrWhiteSpace(sett.BooleanTimeout.Text))
                ConfigurationManager.AppSettings["BooleanTimeOut"] = sett.BooleanTimeout.Text;

            // visuals
            if (sett.SimplifiedRendering.IsChecked != null)
                DrawingControl.HighSpeed = sett.SimplifiedRendering.IsChecked.Value;
            if (sett.ShowFps.IsChecked != null)
                DrawingControl.ShowFps = sett.ShowFps.IsChecked.Value;

        }

        private void RecentFileClick(object sender, RoutedEventArgs e)
        {
            var obMenuItem = e.OriginalSource as MenuItem;
            if (obMenuItem == null) 
                return;
            var fileName = obMenuItem.Header.ToString();
            if (!File.Exists(fileName))
            {
                return;
            }
            LoadAnyModel(fileName);
        }

        private void SetDefaultModeStyler(object sender, RoutedEventArgs e)
        {           
            DrawingControl.DefaultLayerStyler = new SurfaceLayerStyler(this.Logger);
            ConnectStylerFeedBack();
            DrawingControl.ReloadModel();
        }

        private void ConnectStylerFeedBack()
        {
            if (DrawingControl.DefaultLayerStyler is IProgressiveLayerStyler)
            {
                ((IProgressiveLayerStyler)DrawingControl.DefaultLayerStyler).ProgressChanged += OnProgressChanged;
            }
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

        private void ShowErrors(object sender, MouseButtonEventArgs e)
        {
            OpenOrFocusPluginWindow(typeof (LogViewer.LogViewer));
        }

        private void Exit(object sender, RoutedEventArgs e)
        {
            // todo: should we persist UI appearence across sessions?
#if PERSIST_UI
            // experiment
            using (var fs = new StringWriter())
            {
                var xmlLayout = new XmlLayoutSerializer(DockingManager);
                xmlLayout.Serialize(fs);
                var xmlLayoutString = fs.ToString();
                Clipboard.SetText(xmlLayoutString);
            }
#endif
            Close();
        }

        /// <summary>
        /// this event is run after the window is fully rendered.
        /// </summary>
        private void RenderedEvents(object sender, EventArgs e)
        {
            // command line arg can prevent plugin loading
            if (Settings.Default.PluginStartupLoad && !PreventPluginLoad)
                RefreshPlugins();
            ConnectStylerFeedBack();

        }
        
        private void EntityLabel_KeyDown()
        {
            var input = EntityLabel.Text;
            var re = new Regex(@"#[ \t]*(\d+)");
            var m = re.Match(input);
            IPersistEntity entity = null;
            if (m.Success)
            {
                int isLabel;
                if (!int.TryParse(m.Groups[1].Value, out isLabel))
                    return;
                entity = Model.Instances[isLabel];
            }
            else
            {
                entity = Model.Instances.OfType<IIfcRoot>().FirstOrDefault(x => x.GlobalId == input);
            }

            if (entity != null)
                SelectedItem = entity;

        }

        private void ConfigureStyler(object sender, RoutedEventArgs e)
        {
            var c = new SurfaceLayerStylerConfiguration(Model);
            if (DrawingControl.ExcludedTypes != null)
                c.InitialiseSettings(DrawingControl.ExcludedTypes);
            c.ShowDialog();
            if (!c.MustUpdate) 
                return;
            DrawingControl.ExcludedTypes = c.ExcludedTypes;
            DrawingControl.ReloadModel(DrawingControl3D.ModelRefreshOptions.ViewPreserveCameraPosition);
        }

        private void SelectionMode(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            if (mi == null)
            {  
                return;
            }
            WholeMesh.IsChecked = false;
            Normals.IsChecked = false;
            WireFrame.IsChecked = false;
            mi.IsChecked = true;
            switch (mi.Name)
            {
                case "WholeMesh":
                    DrawingControl.SelectionHighlightMode = DrawingControl3D.SelectionHighlightModes.WholeMesh;
                    break;
                case "Normals":
                    DrawingControl.SelectionHighlightMode = DrawingControl3D.SelectionHighlightModes.Normals;
                    break;
                case "WireFrame":
                    DrawingControl.SelectionHighlightMode = DrawingControl3D.SelectionHighlightModes.WireFrame;
                    break;
            }
        }

        private void OpenStrippingWindow(object sender, RoutedEventArgs e)
        {
            Simplify.IfcSimplify s = new Simplify.IfcSimplify();
            s.Show();
        }

        private void MenuItem_ZoomSelected(object sender, RoutedEventArgs e)
        {
            DrawingControl.ZoomSelected();
        }

        private void StylerIfcSpacesOnly(object sender, RoutedEventArgs e)
        {
            var module2X3 = (typeof(Xbim.Ifc2x3.Kernel.IfcProduct)).Module;
            var meta2X3 = ExpressMetaData.GetMetadata(module2X3);
            var product2X3 = meta2X3.ExpressType("IFCPRODUCT");

            var module4 = (typeof(Xbim.Ifc4.Kernel.IfcProduct)).Module;
            var meta4 = ExpressMetaData.GetMetadata(module4);
            var product4 = meta4.ExpressType("IFCPRODUCT");
            


            var tpcoll = product2X3.NonAbstractSubTypes.Select(x => x.Type).ToList();
            tpcoll.AddRange(product4.NonAbstractSubTypes.Select(x => x.Type).ToList());
            tpcoll.RemoveAll(x => x.Name == "IfcSpace");

            DrawingControl.ExcludedTypes = tpcoll;
            DrawingControl.ReloadModel(DrawingControl3D.ModelRefreshOptions.ViewPreserveCameraPosition);
        }

        private void SetStylerBoundCorners(object sender, RoutedEventArgs e)
        {
            DrawingControl.DefaultLayerStyler = new BoundingBoxStyler(this.Logger);
            ConnectStylerFeedBack();
            DrawingControl.ReloadModel();
        }

        private void CommandBoxLost(object sender, RoutedEventArgs e)
        {
            CommandBox.Visibility = Visibility.Collapsed;
        }

        private void CommandBoxEval(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CommandBox.Visibility = Visibility.Collapsed;

                var cmd = CommandPrompt.Text;
                if (string.IsNullOrWhiteSpace(cmd))
                    return;
                Type t = typeof(Commands.wdwCommands);
                var opened = OpenOrFocusPluginWindow(t) as Commands.wdwCommands;
                opened.Execute(cmd);
            }
        }

        private void ShowCommandBox(object sender, RoutedEventArgs e)
        {
            CommandBox.Visibility = Visibility.Visible;
            CommandPrompt.Focus();
        }
    }
}
