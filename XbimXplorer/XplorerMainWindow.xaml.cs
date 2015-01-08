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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Xml;
using Microsoft.Win32;
using Xbim.IO;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Presentation;
using Xbim.Presentation.LayerStyling;
using Xbim.XbimExtensions;
using Xbim.ModelGeometry;
using Xbim.ModelGeometry.Scene;
using Xbim.XbimExtensions.Interfaces;
using Xbim.Ifc2x3.Extensions;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;
using Xbim.Common.Exceptions;
using System.Diagnostics;
using Xbim.Ifc2x3.ActorResource;
using Xbim.Common.Geometry;
using Xbim.COBie.Serialisers;
using Xbim.COBie;
using Xbim.COBie.Contracts;
using Xbim.ModelGeometry.Converter;
using XbimXplorer.Dialogs;
using System.Windows.Media.Imaging;
using Xbim.Presentation.FederatedModel;
using Xceed.Wpf.AvalonDock.Layout;
using XbimXplorer.PluginSystem;
using System.Reflection;
using System.Windows.Shell;
using XbimXplorer.Properties;
#endregion

namespace XbimXplorer
{
    /// <summary>
    ///   Interaction logic for Window1.xaml
    /// </summary>
    public partial class XplorerMainWindow : Window
    {
        private BackgroundWorker _worker;
        public static RoutedCommand CreateFederationCmd = new RoutedCommand();
        public static RoutedCommand EditFederationCmd = new RoutedCommand();
        public static RoutedCommand OpenFederationCmd = new RoutedCommand();
        public static RoutedCommand ExportCOBieCmd = new RoutedCommand();
        public static RoutedCommand COBieClassFilter = new RoutedCommand();
        private string _openedModelFileName;
        private string _temporaryXbimFileName;
        // private string _defaultFileName;
        const string _UKTemplate = "COBie-UK-2012-template.xls";
        const string _USTemplate = "COBie-US-2_4-template.xls";

        private FilterValues UserFilters { get; set; }
        public string COBieTemplate { get; set; }

        public XplorerMainWindow()
        {
            InitializeComponent();
            this.Closed += new EventHandler(XplorerMainWindow_Closed);
            this.Loaded += XplorerMainWindow_Loaded;
            this.Closing += new CancelEventHandler(XplorerMainWindow_Closing);
            this.DrawingControl.UserModeledDimensionChangedEvent += DrawingControl_MeasureChangedEvent;
            InitFromSettings();

            RefreshRecentFiles();

            UserFilters = new FilterValues();//COBie Class filters, set to initial defaults
            COBieTemplate = _UKTemplate;

            
            if (Settings.Default.PluginStartupLoad)
                RefreshPlugins();
        }

        private void InitFromSettings()
        {
            _FileAccessMode = Settings.Default.FileAccessMode;
        }

        private ObservableMruList<string> _MRUFiles = new ObservableMruList<string>();
        private void RefreshRecentFiles()
        {
            List<String> s = new List<string>();
            if (Settings.Default.MRUFiles != null)
                foreach (var item in Settings.Default.MRUFiles)
                    s.Add(item);

            _MRUFiles = new ObservableMruList<string>(s, 4, StringComparer.InvariantCultureIgnoreCase);
            mnuRecent.ItemsSource = _MRUFiles;
        }

        private void AddRecentFile(string fileName)
        {
            _MRUFiles.Add(_openedModelFileName);
            Settings.Default.MRUFiles = new System.Collections.Specialized.StringCollection();
            foreach (var item in _MRUFiles)
            {
                Settings.Default.MRUFiles.Add(item);
            }
            Settings.Default.Save();
        }

        private void DrawingControl_MeasureChangedEvent(DrawingControl3D m, Xbim.Presentation.ModelGeomInfo.PolylineGeomInfo e)
        {
            if (e != null)
            {
                this.EntityLabel.Text = e.ToString();
                Debug.WriteLine("Points:");
                foreach (var pt in e.VisualPoints)
                {
                    Debug.WriteLine(string.Format("X:{1} Y:{1} Z:{2}", pt.X, pt.Y, pt.Z));
                }
            }
        }

        void OpenQuery(object sender, RoutedEventArgs e)
        {
            XbimXplorer.Querying.wdwQuery qw = new Querying.wdwQuery();
            ShowPluginWindow(qw);
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
            XbimModel model = XbimModel.CreateTemporaryModel();
            model.Initialise();
            ModelProvider.ObjectInstance = model;
            ModelProvider.Refresh();
        }

        void XplorerMainWindow_Closed(object sender, EventArgs e)
        {
            CloseAndDeleteTemporaryFiles();
        }


        private void OpenIfcFile(object s, DoWorkEventArgs args)
        {
            BackgroundWorker worker = s as BackgroundWorker;
            string ifcFilename = args.Argument as string;

            XbimModel model = new XbimModel();
            try
            {
                _temporaryXbimFileName = Path.GetTempFileName();
                _openedModelFileName = ifcFilename;

                model.CreateFrom(ifcFilename, _temporaryXbimFileName, worker.ReportProgress, true, false);
                Xbim3DModelContext context = new Xbim3DModelContext(model);//upgrade to new geometry represenation, uses the default 3D model
                context.CreateContext(progDelegate: worker.ReportProgress);
            
                if (worker.CancellationPending == true) //if a cancellation has been requested then don't open the resulting file
                {
                    try
                    {
                        model.Close();
                        if (File.Exists(_temporaryXbimFileName))
                            File.Delete(_temporaryXbimFileName); //tidy up;
                        _temporaryXbimFileName = null;
                        _openedModelFileName = null;
                    }
                    catch (Exception)
                    { }
                    return;
                }
                args.Result = model;
            }
            catch (Exception ex)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Error reading " + ifcFilename);
                string indent = "\t";
                while (ex != null)
                {
                    sb.AppendLine(indent + ex.Message);
                    ex = ex.InnerException;
                    indent += "\t";
                }

                args.Result = new Exception(sb.ToString());
            }
        }

        XbimDBAccess _FileAccessMode = XbimDBAccess.Read;
        public XbimDBAccess FileAccessMode
        {
            get { return _FileAccessMode; }
            set { _FileAccessMode = value; }
        }

        /// <summary>
        ///   This is called when we explcitly want to open an xBIM file
        /// </summary>
        /// <param name = "s"></param>
        /// <param name = "args"></param>
        private void OpenXbimFile(object s, DoWorkEventArgs args)
        {
            BackgroundWorker worker = s as BackgroundWorker;
            string fileName = args.Argument as string;
            XbimModel model = new XbimModel();
            try
            {
                var dbAccessMode = _FileAccessMode;
                model.Open(fileName, dbAccessMode, worker.ReportProgress); //load entities into the model

                if (model.IsFederation)
                {
                    // needs to open the federation in rw mode
                    if (dbAccessMode != XbimDBAccess.ReadWrite)
                    {
                        model.Close();
                        model.Open(fileName, XbimDBAccess.ReadWrite, worker.ReportProgress); // federations need to be opened in read/write for the editor to work
                    }

                    // sets a convenient integer to all children for model identification
                    // this is used by the federated model selection mechanisms.
                    int i = 0;
                    foreach (var item in model.AllModels)
                    {
                        item.Tag = i++;
                    }
                }

                args.Result = model;
            }
            catch (Exception ex)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Error reading " + fileName);
                string indent = "\t";
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
            OpenFileDialog dlg = sender as OpenFileDialog;
            if (dlg != null)
            {
                LoadAnyModel(dlg.FileName);
                return;
            }
        }

        public void LoadAnyModel(string ModelFileName)
        {
            FileInfo fInfo = new FileInfo(ModelFileName);
            if (!fInfo.Exists) // file does not exist; do nothing
                return;
            if (fInfo.FullName.ToLower() == _openedModelFileName) //same file do nothing
                return;

            // there's no going back; if it fails after this point the current file should be closed anyway
            CloseAndDeleteTemporaryFiles();

            _openedModelFileName = ModelFileName.ToLower();
            StatusBar.Visibility = Visibility.Visible;
            CreateWorker();

            string ext = fInfo.Extension.ToLower();
            switch (ext)
            {
                case ".ifc": //it is an Ifc File
                case ".ifcxml": //it is an IfcXml File
                case ".ifczip": //it is a xip file containing xbim or ifc File
                case ".zip": //it is a xip file containing xbim or ifc File
                    _worker.DoWork += OpenIfcFile;
                    _worker.RunWorkerAsync(ModelFileName);
                    break;
                case ".xbimf":
                case ".xbim": //it is an xbim File, just open it in the main thread
                    _worker.DoWork += OpenXbimFile;
                    _worker.RunWorkerAsync(ModelFileName);
                    break;
                default:
                    break;
            }
            return;
        }

        public delegate void LoadingCompleteEventHandler(object s, RunWorkerCompletedEventArgs args);
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
            _worker = new BackgroundWorker();
            _worker.WorkerReportsProgress = true;
            _worker.WorkerSupportsCancellation = true;
            _worker.ProgressChanged += delegate(object s, ProgressChangedEventArgs args)
            {
                ProgressBar.Value = args.ProgressPercentage;
                StatusMsg.Text = (string)args.UserState;
            };

            _worker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs args)
            {
                if (args.Result is XbimModel) //all ok
                {
                    ModelProvider.ObjectInstance = (XbimModel)args.Result; //this Triggers the event to load the model into the views 
                    // PropertiesControl.Model = (XbimModel)args.Result; // // done thtough binding in xaml
                    ModelProvider.Refresh();
                    ProgressBar.Value = 0;
                    StatusMsg.Text = "Ready";

                    AddRecentFile(_openedModelFileName);



                    // todo: file extensions need to be registered to allow an easy use of the jumplist
                    // this is best done in the installer.


                    //// METHOD 1 - for a task
                    //// Configure a new JumpTask.
                    //// 
                    //FileInfo Fi = new FileInfo(_openedModelFileName);
                    //JumpTask jumpTask1 = new JumpTask();
                    //// Get the path to Calculator and set the JumpTask properties.
                    //jumpTask1.ApplicationPath = Assembly.GetExecutingAssembly().Location;
                    //jumpTask1.Arguments = _openedModelFileName;
                    //jumpTask1.IconResourcePath = Assembly.GetExecutingAssembly().Location;
                    //jumpTask1.Title = Fi.Name;
                    //jumpTask1.Description = Fi.FullName;
                    //var v = JumpList.GetJumpList(Application.Current);
                    //v.JumpItems.Add(jumpTask1);
                    //v.Apply();

                    // METHOD 2: recent files.
                    //// Get the JumpList from the application and update it.
                    //var jumpList1 = JumpList.GetJumpList(App.Current);
                    //jumpList1.JumpItems.Add()
                    //JumpList.AddToRecentCategory(_openedModelFileName);
                    //jumpList1.Apply();


                }
                else //we have a problem
                {
                    string errMsg = args.Result as String;
                    if (!string.IsNullOrEmpty(errMsg))
                        MessageBox.Show(this, errMsg, "Error Opening File",
                                        MessageBoxButton.OK, MessageBoxImage.Error,
                                        MessageBoxResult.None, MessageBoxOptions.None);
                    if (args.Result is Exception)
                    {
                        StringBuilder sb = new StringBuilder();
                        Exception ex = args.Result as Exception;
                        String indent = "";
                        while (ex != null)
                        {
                            sb.AppendFormat("{0}{1}\n", indent, ex.Message);
                            ex = ex.InnerException;
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

        private void ProgressChanged(object sender, ProgressChangedEventArgs args)
        {
            ProgressBar.Value = args.ProgressPercentage;
            string msg = args.UserState as string;
            if (msg != null) StatusMsg.Text = msg;
        }

        void DrawingControl_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

        }

        private void dlg_FileSaveAs(object sender, CancelEventArgs e)
        {
            SaveFileDialog dlg = sender as SaveFileDialog;
            if (dlg != null)
            {
                FileInfo fInfo = new FileInfo(dlg.FileName);
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
                        string extension = Path.GetExtension(dlg.FileName).ToLowerInvariant();
                        if (extension == "xbim" && !string.IsNullOrWhiteSpace(_temporaryXbimFileName))  //we have a temp file open, it is now redundant as we have upgraded to another xbim file
                        {
                            File.Delete(_temporaryXbimFileName);
                            _temporaryXbimFileName = null;
                        }
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
            SaveFileDialog dlg = new SaveFileDialog();
            if (_openedModelFileName != null)
            {
                FileInfo f = new FileInfo(_openedModelFileName);
                dlg.DefaultExt = f.Extension;
                dlg.InitialDirectory = f.DirectoryName;
                dlg.FileName = f.Name;
            }
            // filter starts with basic xbim formats
            var Corefilters = new string[] {
                    "xBIM File (*.xBIM)|*.xBIM", 
                    "IfcXml File (*.IfcXml)|*.ifcxml", 
                    "IfcZip File (*.IfcZip)|*.ifczip"
                };
            var FedFilter = new string[] { "xBIM Federation file (*.xBIMF)|*.xbimf" };
            var IfcFilter = new string[] { "Ifc File (*.ifc)|*.ifc" };
            string filter = "";

            if (Model.IsFederation)
            {
                dlg.DefaultExt = "xBIMF";
                var filterA = FedFilter.Concat(IfcFilter).Concat(Corefilters).ToArray();
                filter = String.Join("|", filterA);
            }
            else
            {
                var filterA = IfcFilter.Concat(Corefilters).Concat(FedFilter).ToArray();
                filter = String.Join("|", filterA);
            }

            dlg.Filter = filter;// Filter files by extension 
            dlg.Title = "Save As";
            dlg.AddExtension = true;

            // Show open file dialog box 
            dlg.FileOk += new CancelEventHandler(dlg_FileSaveAs);
            dlg.ShowDialog(this);
        }

        private void CommandBinding_Close(object sender, ExecutedRoutedEventArgs e)
        {
            CloseAndDeleteTemporaryFiles();
        }

        private void CommandBinding_Open(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Xbim Files|*.xbim;*.xbimf;*.ifc;*.ifcxml;*.ifczip"; // Filter files by extension 
            dlg.FileOk += new CancelEventHandler(dlg_OpenAnyFile);
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
                XbimModel model = ModelProvider.ObjectInstance as XbimModel;
                _openedModelFileName = null;
                if (model != null)
                {
                    model.Dispose();
                    ModelProvider.ObjectInstance = null;
                    ModelProvider.Refresh();
                }
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
                    XbimModel model = ModelProvider.ObjectInstance as XbimModel;
                    e.CanExecute = (model != null);
                }
                else
                    e.CanExecute = true; //for everything else
            }
        }


        #endregion

        # region "Federation Model operations"
        private void EditFederationCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            FederatedModelDialog fdlg = new FederatedModelDialog();
            fdlg.DataContext = Model;
            bool? done = fdlg.ShowDialog();
            if (done.HasValue && done.Value == true)
            {

            }
        }
        private void EditFederationCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Model != null && Model.IsFederation;
        }

        private void OpenFederationCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Xbim Federation Files|*.xbimf|Xbim Model Files|*.ifc"; // Filter files by extension 
            dlg.CheckFileExists = true;
            dlg.Multiselect = true;
            bool? done = dlg.ShowDialog(this);
            if (done.HasValue && done.Value == true)
            {
                if (dlg.FileNames.Any()) // collection is not empty
                {
                    //use the first filename it's extension to decide which action should happen
                    var firstExtension = Path.GetExtension(dlg.FileNames[0]).ToLower();

                    XbimModel fedModel = null;
                    if (firstExtension == ".xbimf")
                    {
                        if (dlg.FileNames.Length > 1)
                        {
                            var res = MessageBox.Show("Multiple files selected, open " + dlg.FileNames[0] + "?", "Cannot open multiple Xbim files",
                                MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                            if (res == MessageBoxResult.Cancel)
                                return;
                        }
                        fedModel = new XbimModel();
                        fedModel.Open(dlg.FileNames[0], XbimDBAccess.ReadWrite);
                    }
                    else if (firstExtension == ".ifc")
                    {
                        //create temp file as a placeholder for the temperory xbim file
                        var filePath = Path.GetTempFileName();
                        filePath = Path.ChangeExtension(filePath, "xbimf");
                        fedModel = XbimModel.CreateModel(filePath);
                        fedModel.Initialise("Default Author", "Default Organization");
                        using (var txn = fedModel.BeginTransaction())
                        {
                            fedModel.IfcProject.Name = "Default Project Name";
                            txn.Commit();
                        }


                        bool informUser = true;
                        for (int i = 0; i < dlg.FileNames.Length; i++)
                        {
                            var fileName = dlg.FileNames[i];
                            var builder = new XbimReferencedModelViewModel();
                            builder.Name = fileName;
                            builder.OrganisationName = "OrganisationName " + i;
                            builder.OrganisationRole = "Undefined";

                            bool buildRes = false;
                            Exception exception = null;
                            try
                            {
                                buildRes = builder.TryBuild(fedModel);
                            }
                            catch (Exception ex)
                            {
                                //usually an EsentDatabaseSharingViolationException, user needs to close db first
                                exception = ex;
                            }

                            if (!buildRes && informUser)
                            {
                                string msg = exception == null ? "" : "\r\nMessage: " + exception.Message;
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
                        }
                    }
                    if (fedModel != null)
                    {
                        CloseAndDeleteTemporaryFiles();
                        ModelProvider.ObjectInstance = fedModel;
                        ModelProvider.Refresh();

                    }
                }
            }
        }

        private void OpenFederationCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        #endregion

        public IPersistIfcEntity SelectedItem
        {
            get { return (IPersistIfcEntity)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedItem.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(IPersistIfcEntity), typeof(XplorerMainWindow),
                                        new UIPropertyMetadata(null, new PropertyChangedCallback(OnSelectedItemChanged)));


        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            XplorerMainWindow mw = d as XplorerMainWindow;
            if (mw != null && e.NewValue is IPersistIfcEntity)
            {
                IPersistIfcEntity label = (IPersistIfcEntity)e.NewValue;
                mw.EntityLabel.Text = label != null ? "#" + label.EntityLabel.ToString() : "";
            }
            else
                mw.EntityLabel.Text = "";
        }


        private ObjectDataProvider ModelProvider
        {
            get
            {
                return MainFrame.DataContext as ObjectDataProvider;
            }
        }

        public XbimModel Model
        {
            get
            {
                ObjectDataProvider op = MainFrame.DataContext as ObjectDataProvider;
                return op == null ? null : op.ObjectInstance as XbimModel;
            }
        }

        // this variable is used to determine when the user is trying again to double click on the selected item
        // from this we detect that he's probably not happy with the view, therefore we add a cutting plane to make the 
        // element visible.
        //
        private bool _camChanged = false;
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

        private void ExportCOBieCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            string outputFile = Path.ChangeExtension(Model.DatabaseName, ".xls");

            // Build context
            COBieContext context = new COBieContext();
            context.TemplateFileName = COBieTemplate;
            context.Model = Model;
            //set filter option
            context.Exclude = UserFilters;

            //set the UI language to get correct resource file for template
            //if (Path.GetFileName(parameters.TemplateFile).Contains("-UK-"))
            //{
            try
            {
                System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo("en-GB");
                System.Threading.Thread.CurrentThread.CurrentUICulture = ci;
            }
            catch (Exception)
            {
                //to nothing Default culture will still be used
            }

            COBieBuilder builder = new COBieBuilder(context);
            COBieXLSSerialiser serialiser = new COBieXLSSerialiser(outputFile, context.TemplateFileName);
            serialiser.Excludes = UserFilters;
            builder.Export(serialiser);
            Process.Start(outputFile);
        }

        // CanExecuteRoutedEventHandler for the custom color command.
        private void ExportCOBieCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            XbimModel model = ModelProvider.ObjectInstance as XbimModel;
            bool canEdit = (model != null && model.CanEdit && model.Instances.OfType<IfcBuilding>().FirstOrDefault() != null);
            e.CanExecute = canEdit && !(_worker != null && _worker.IsBusy);
        }

        private void COBieClassFilterCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            COBieClassFilter classFilterDlg = new COBieClassFilter(UserFilters);
            bool? done = classFilterDlg.ShowDialog();
            if (done.HasValue && done.Value == true)
            {
                UserFilters = classFilterDlg.UserFilters; //not needed, but makes intent clear
            }
        }

        private void COBieClassFilterCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void SeparateMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ModelSeparation separate = new ModelSeparation();

            //set data binding
            Binding b = new Binding("DataContext");
            b.Source = this.MainFrame;
            b.Mode = BindingMode.TwoWay;
            separate.SetBinding(ModelSeparation.DataContextProperty, b);

            separate.Show();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            Gat.Controls.About about = new Gat.Controls.About();
            //
            about.Title = "xBIM Xplorer";
            about.Hyperlink = new Uri("http://xbim.codeplex.com", UriKind.Absolute);
            about.HyperlinkText = "http://xbim.codeplex.com";
            about.Publisher = "xBIM Team - Steve Lockley";
            about.Description = "This application is designed to demonstrate potential usages of the xBIM toolkit";
            about.ApplicationLogo = new BitmapImage(new Uri(@"pack://application:,,/xBIM.ico", UriKind.RelativeOrAbsolute));
            about.Copyright = "Prof. Steve Lockley";
            // about.PublisherLogo = about.ApplicationLogo;
            about.AdditionalNotes = "The xBIM toolkit is an Open Source software initiative to help software developers and researchers to support the next generation of BIM tools; unlike other open source application xBIM license is compatible with commercial environments (http://xbim.codeplex.com/license)";
            about.Show();
        }

        private void UKTemplate_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = (MenuItem)sender;

            if (mi.IsChecked)
            {
                COBieTemplate = _UKTemplate;
                if (US.IsChecked)
                {
                    US.IsChecked = false;
                }
            }
            else
            {
                US.IsChecked = true;
                COBieTemplate = _USTemplate;
            }
        }

        private void USTemplate_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = (MenuItem)sender;

            if (mi.IsChecked)
            {
                COBieTemplate = _USTemplate;
                if (UK.IsChecked)
                {
                    UK.IsChecked = false;
                }
            }
            else
            {
                UK.IsChecked = true;
                COBieTemplate = _UKTemplate;
            }
        }

        private void OpenScriptingWindow(object sender, RoutedEventArgs e)
        {
            var win = new Scripting.ScriptingWindow();
            win.Owner = this;

            win.ScriptingConcrol.DataContext = ModelProvider;
            var binding = new Binding();
            win.ScriptingConcrol.SetBinding(ScriptingControl.ModelProperty, binding);

            win.ScriptingConcrol.OnModelChangedByScript += delegate(object o, Xbim.Script.ModelChangedEventArgs arg)
            {
                ModelProvider.ObjectInstance = null;
                var m3D = new Xbim3DModelContext(arg.NewModel);
                m3D.CreateContext();
                ModelProvider.ObjectInstance = arg.NewModel;
                ModelProvider.Refresh();
            };

            win.ScriptingConcrol.OnScriptParsed += delegate(object o, Xbim.Script.ScriptParsedEventArgs arg)
            {
                GroupControl.Regenerate();
                //SpatialControl.Regenerate();
            };


            ScriptResults.Visibility = Visibility.Visible;
            win.Closing += new CancelEventHandler(delegate(object s, CancelEventArgs arg)
            {
                ScriptResults.Visibility = Visibility.Collapsed;
            });

            win.Show();
        }

        private void DisplaySettingsPage(object sender, RoutedEventArgs e)
        {
            Dialogs.SettingsWindow sett = new SettingsWindow();
            sett.ShowDialog();
            if (sett.SettingsChanged)
                InitFromSettings();
        }

        private void RecentFileClick(object sender, RoutedEventArgs e)
        {
            MenuItem obMenuItem = e.OriginalSource as MenuItem;
            string fileName = obMenuItem.Header.ToString();
            if (!File.Exists(fileName))
            {
                return;
            }
            LoadAnyModel(fileName);
        }

        private void SetDefaultModeStyler(object sender, RoutedEventArgs e)
        {
            this.DrawingControl.LayerStyler = new LayerStylerTypeAndIFCStyle();
            DrawingControl.ReloadModel();
        }

        private void SetFederationStylerRole(object sender, RoutedEventArgs e)
        {
            this.DrawingControl.FederationLayerStyler = new LayerStylerSingleColour();
            DrawingControl.ReloadModel();
        }

        private void SetFederationStylerType(object sender, RoutedEventArgs e)
        {
            this.DrawingControl.FederationLayerStyler = new LayerStylerTypeAndIFCStyle();
            DrawingControl.ReloadModel();
        }
    }
}
