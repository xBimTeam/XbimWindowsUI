using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using log4net;
using Xbim.COBie;
using Xbim.FilterHelper;
using Xbim.Presentation.XplorerPluginSystem;
using Xbim.Ifc;
using XbimExchanger.IfcToCOBieLiteUK.Conversion;
using System.Text;
using Xbim.COBieLiteUK;
using XbimExchanger.IfcToCOBieLiteUK;


namespace XplorerPlugins.Cobie.UI
{
    /// <summary>
    /// Interaction logic for COBieClassFilter.xaml
    /// </summary>
    [XplorerUiElement(PluginWindowUiContainerEnum.Dialog, PluginWindowActivation.OnMenu, "File/Export/COBieLiteUk")]
    public partial class CobieLiteUkExport: IXbimXplorerPluginWindow
    {
        private static readonly ILog Log = LogManager.GetLogger("Xbim.WinUI");

        public ObservableCollection<string> AvailableTemplates { get; set; }

        public ObservableCollection<String> ExportTypes { get; set; }

        public ObservableCollection<SystemModeItem> AvailableSystemModes { get; set; }

        public string SelectedExportType { get; set; }
        
        public IfcStore Model
        {
            get { return (IfcStore)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public static DependencyProperty ModelProperty =
            DependencyProperty.Register("Model", typeof(IfcStore), typeof(CobieLiteUkExport),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, OnSelectedEntityChanged));

        private static void OnSelectedEntityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // if any UI event should happen it needs to be specified here
            var ctrl = d as CobieLiteUkExport;
            if (ctrl == null)
                return;
            switch (e.Property.Name)
            {
                case "Model":
                    Debug.WriteLine("Model Updated");
                    ctrl.OnPropertyChanged("Model");
                    break;
                case "SelectedEntity":
                    break;
            }
        }

        public FileInfo ConfigFile { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private IXbimXplorerPluginMasterWindow _parentWindow;

        public string WindowTitle { get; private set; }

        public void BindUi(IXbimXplorerPluginMasterWindow mainWindow)
        {
            _parentWindow = mainWindow;
            SetBinding(ModelProperty, new Binding("Model") { Source = mainWindow.DrawingControl, Mode = BindingMode.OneWay });

            ConfigureFolder();
        }

        public void CreateDefaultAppConfig(FileInfo configFile)
        {
            var asss = System.Reflection.Assembly.GetAssembly(typeof(IfcToCOBieLiteUkExchanger));

            using (var input = asss.GetManifestResourceStream("XbimExchanger.IfcToCOBieLiteUK.COBieAttributes.config"))
            {
                if (input != null)
                    using (var output = configFile.Create())
                    {
                        input.CopyTo(output);
                    }
            }
            configFile.Refresh();
        }

        private void ConfigureFolder()
        {

            // defaults to current directory
            var dir = new DirectoryInfo(".");
            if (_parentWindow != null)
            {
                var openedModel = _parentWindow.GetOpenedModelFileName();
                // if we have a model then use its direcotry
                if (!string.IsNullOrEmpty(openedModel))
                {
                    var directoryName = new FileInfo(openedModel).DirectoryName;
                    if (directoryName != null)
                        dir = new DirectoryInfo(directoryName);
                }
            }
            // main folder config
            TxtFolderName.Text = dir.FullName;

            // configure settings for exporter
            var asss = System.Reflection.Assembly.GetAssembly(GetType());
            var pluginDir = new FileInfo(asss.Location).Directory;
            if (pluginDir == null)
            {
                Log.ErrorFormat("Failed to get plugin folder.");
                return;
            }

            ConfigFile = new FileInfo(Path.Combine(pluginDir.FullName, "COBieAttributesCustom.config"));
            if (ConfigFile.Exists) 
                return;
            AppendLog("Creating Config File");
            CreateDefaultAppConfig(ConfigFile);
            ConfigFile.Refresh();
        }

        public string SelectedTemplate { get; set; }


        public FilterValues UserFilters { get; set; }    //hold the user required class types, as required by the user

        public ObservableCollection<CheckedListItem<Type>> ClassFilterComponent { get; set; }
        
        public ObservableCollection<CheckedListItem<Type>> ClassFilterType { get; set; }
        
        public ObservableCollection<CheckedListItem<Type>> ClassFilterAssembly { get; set; }

        public class SystemModeItem
        {
            public SystemModeItem(SystemExtractionMode mode)
            {
                Item = mode;
                IsSelected = true;
            }

            public string Name
            {
                get { return Item.ToString(); }
            }

            public SystemExtractionMode Item { get; set; }
        
            public bool IsSelected { get; set; }
        }

        public CobieLiteUkExport()
        {
            InitializeComponent();

            ConfigureFolder();

            // prepare templates list
            AvailableTemplates = new ObservableCollection<string>();
            foreach (var avail in Templates.GetAvalilableTemplateTypes())
            {
                AvailableTemplates.Add(avail);
            }
            SelectedTemplate = AvailableTemplates.FirstOrDefault();

            // prepare export
            ExportTypes = new ObservableCollection<string>() {"XLS", "XLSX", "JSON", "XML", "IFC"};
            SelectedExportType = "XLSX";

            // prepare system modes
            AvailableSystemModes = new ObservableCollection<SystemModeItem>();
            foreach (var valid in Enum.GetValues(typeof(SystemExtractionMode)).OfType<SystemExtractionMode>().Where(r => r != SystemExtractionMode.System))
            {
                AvailableSystemModes.Add(new SystemModeItem(valid));
            }
            
            // define filters and set defaults
            UserFilters = new FilterValues();
            SetDefaultFilters();

            DataContext = this;
        }

        /// <summary>
        /// Initialize the ObservableCollection's 
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="defaultExcludeTypes"></param>
        /// <param name="userExcludeTypes">List of Type, holding user's  list of class types</param>
        private void InitExcludes(ObservableCollection<CheckedListItem<Type>> destination, IEnumerable<Type> defaultExcludeTypes, List<Type> userExcludeTypes)
        {
            destination.Clear();
            foreach (var typeobj in defaultExcludeTypes)
            {
                destination.Add(new CheckedListItem<Type>(typeobj, userExcludeTypes.Contains(typeobj))); //see if in user list, if so check it
            }
        }

        #region Worker Methods

        /// <summary>
        /// Worker Complete method
        /// </summary>
        /// <param name="s"></param>
        /// <param name="args"></param>
        public void WorkerCompleted(object s, RunWorkerCompletedEventArgs args)
        {
            try
            {
                var ex = args.Result as Exception;
                if (ex != null)
                {
                    var sb = new StringBuilder();

                    var indent = "";
                    while (ex != null)
                    {
                        sb.AppendFormat("{0}{1}\n", indent, ex.Message);
                        ex = ex.InnerException;
                        indent += "\t";
                    }
                    AppendLog(sb.ToString());
                }
                else
                {
                    var errMsg = args.Result as string;
                    if (!string.IsNullOrEmpty(errMsg))
                        AppendLog(errMsg);

                }
            }
            catch (Exception ex)
            {
                Log.Error(string.Format("Error on work completion: {0}", ex.Message), ex);

                var sb = new StringBuilder();
                var indent = "";

                while (ex != null)
                {
                    sb.AppendFormat("{0}{1}\n", indent, ex.Message);
                    ex = ex.InnerException;
                    indent += "\t";
                }
                AppendLog(sb.ToString());
            }
            finally
            {
                btnGenerate.IsEnabled = true;
            }
            //open file if ticked to open excel
            if (ChkOpenExcel.IsChecked != null && ChkOpenExcel.IsChecked.Value && args.Result is IEnumerable<string>)
            {
                var ien = (IEnumerable<string>) args.Result;
                foreach (var file in ien)
                {
                    Process.Start(file);    
                }
            }
            StatusGrid.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Worker Progress Changed
        /// </summary>
        /// <param name="s"></param>
        /// <param name="args"></param>
        public void WorkerProgressChanged(object s, ProgressChangedEventArgs args)
        {
            //Show message in Text List Box
            if (args.ProgressPercentage == 0)
            {
                StatusMsg.Text = string.Empty;
                StatusGrid.Visibility = Visibility.Collapsed;
                AppendLog(args.UserState.ToString());
            }
            else //show message on status bar and update progress bar
            {
                if (StatusGrid.Visibility == Visibility.Collapsed)
                {
                    StatusGrid.Visibility = Visibility.Visible;
                }
                StatusMsg.Text = args.UserState.ToString();
                ProgressBar.Value = args.ProgressPercentage;
            }
        }

        private const int MaxCapacity = 300;

        private readonly Queue<string> _messageQueue = new Queue<string>(MaxCapacity);

        /// <summary>
        /// Add string to Text Output List Box 
        /// </summary>
        /// <param name="text"></param>
        private void AppendLog(string text)
        {
            _messageQueue.Enqueue(text);
            while (_messageQueue.Count >= MaxCapacity)
            {
                _messageQueue.Dequeue();
            }
            LogBlock.Text = string.Join("\r\n", _messageQueue.ToArray());
        }

        #endregion

        /// <summary>
        /// Get Excel Type From Combo
        /// </summary>
        /// <returns>ExcelTypeEnum</returns>
        private ExportFormatEnum GetExcelType()
        {
            return (ExportFormatEnum)Enum.Parse(typeof(ExportFormatEnum), SelectedExportType);
        }

        private ICobieLiteConverter _cobieWorker;

        private void DoExport(object sender, RoutedEventArgs e)
        {
            // todo: does it make sense to restore the flip option?
            //if (chkBoxFlipFilter.Checked)
            //{
            //    // ReSharper disable LocalizableElement
            //    var result = MessageBox.Show(
            //        "Flip Filter is ticked, this will show only excluded items, Do you want to continue",
            //        "Warning", MessageBoxButtons.YesNo);
            //    if (result == DialogResult.No)
            //    {
            //        return;
            //    }
            //}
            btnGenerate.IsEnabled = false;

            if (_cobieWorker == null)
            {
                _cobieWorker = new CobieLiteConverter();
                _cobieWorker.Worker.ProgressChanged += WorkerProgressChanged;
                _cobieWorker.Worker.RunWorkerCompleted += WorkerCompleted;
            }
            //get Excel File Type
            var excelType = GetExcelType();
            //set filters
            // todo: restore filters
            //var filterRoles = SetRoles();
            //if (!chkBoxNoFilter.Checked)
            //{
            //    _assetfilters.ApplyRoleFilters(filterRoles);
            //    _assetfilters.FlipResult = chkBoxFlipFilter.Checked;
            //}
            var v = new FileInfo(Model.FileName);
            var exportFileName = Path.ChangeExtension(Path.Combine(TxtFolderName.Text, v.Name), "tempExtension");

            //set parameters
            var conversionSettings = new CobieConversionParams
            {
                Source = Model,
                OutputFileName = exportFileName,
                TemplateFile = SelectedTemplate,
                ExportFormat = excelType,
                ExtId = (UseExternalIds.IsChecked != null && UseExternalIds.IsChecked.Value) ? EntityIdentifierMode.IfcEntityLabels : EntityIdentifierMode.GloballyUniqueIds,
                SysMode = SetSystemMode(),
                Filter = new OutPutFilters(), // todo: restore filters // chkBoxNoFilter.Checked ? new OutPutFilters() : _assetfilters,
                ConfigFile = ConfigFile.FullName,
                Log = true
            };
            //run worker
            _cobieWorker.Run(conversionSettings);    
        }

        /// <summary>
        /// Set the System extraction Methods
        /// </summary>
        /// <returns></returns>
        private SystemExtractionMode SetSystemMode()
        {
            var sysMode = SystemExtractionMode.System;
            
            //add the checked system modes
            foreach (var item in AvailableSystemModes)
            {
                try
                {
                    if (!item.IsSelected) 
                        continue;
                    var mode = (SystemExtractionMode) Enum.Parse(typeof(SystemExtractionMode), item.Name);
                    sysMode |= mode;
                }
                catch (Exception)
                {
                    AppendLog("Error: Failed to get requested system extraction mode");
                }
            }

            return sysMode;
        }


        private void SetDefaultFilters()
        {
            var defaultFilters = new FilterValues(); //gives us the initial list of types

            //initialize the collection classes for the list box's
            if (ClassFilterComponent == null)
                ClassFilterComponent  = new ObservableCollection<CheckedListItem<Type>>();
            if (ClassFilterType == null)
                ClassFilterType = new ObservableCollection<CheckedListItem<Type>>();
            if (ClassFilterAssembly == null)
                ClassFilterAssembly = new ObservableCollection<CheckedListItem<Type>>();

            //fill in the collections to display the check box's in the list box's
            InitExcludes(ClassFilterComponent, defaultFilters.ObjectType.Component, UserFilters.ObjectType.Component);
            InitExcludes(ClassFilterType, defaultFilters.ObjectType.Types, UserFilters.ObjectType.Types);
            InitExcludes(ClassFilterAssembly, defaultFilters.ObjectType.Assembly, UserFilters.ObjectType.Assembly);
        }

        private void ClearLog(object sender, RoutedEventArgs e)
        {
            LogBlock.Text = "";
        }
    }
}
