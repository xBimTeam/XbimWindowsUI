using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using Xbim.IO;
using Xbim.Presentation.XplorerPluginSystem;

namespace XplorerPlugins.Cobie
{
    /// <summary>
    /// Interaction logic for ScriptingWindow.xaml
    /// </summary>
    [XplorerUiElement(PluginWindowUiContainerEnum.LayoutDoc, PluginWindowActivation.OnMenu, "File/Export/COBie")]
    public partial class CobieExport : IXbimXplorerPluginWindow, INotifyPropertyChanged
    {
        /// <summary>
        /// 
        /// </summary>
        public CobieExport()
        {
            InitializeComponent();
            WindowTitle = "Cobie Export";

            foreach (var var in Enum.GetValues(typeof(ExportTypeEnum)))
            {
                ExportFormat.Items.Add(var.ToString());
            }
            ExportFormat.SelectedIndex = 0;
        }

        // Model
        /// <summary>
        /// 
        /// </summary>
        public XbimModel Model
        {
            get { return (XbimModel)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public static DependencyProperty ModelProperty =
            DependencyProperty.Register("Model", typeof(XbimModel), typeof(CobieExport),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, OnSelectedEntityChanged));

        private static void OnSelectedEntityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // if any UI event should happen it needs to be specified here
            var ctrl = d as CobieExport;
            if (ctrl == null)
                return;
            switch (e.Property.Name)
            {
                case "Model":
                    Debug.WriteLine("Model Updated");
                    ctrl.OnPropertyChanged("Model");
                    // ModelProperty =
                    break;
                case "SelectedEntity":
                    break;
            }
        }

        private IXbimXplorerPluginMasterWindow _parentWindow;
        
        public string WindowTitle { get; private set; }
        public void BindUi(IXbimXplorerPluginMasterWindow mainWindow)
        {
            _parentWindow = mainWindow;
            SetBinding(ModelProperty, new Binding());
        }
    
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private CobieLiteWorker _cobieWorker;

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
                ProgressBar.Visibility = Visibility.Collapsed;
                // AppendLog(args.UserState.ToString());
            }
            else //show message on status bar and update progress bar
            {
                if (ProgressBar.Visibility == Visibility.Collapsed)
                {
                    ProgressBar.Visibility = Visibility.Visible;
                }
                // StatusMsg.Text = args.UserState.ToString();
                ProgressBar.Value = args.ProgressPercentage;
            }
        }

        private void AppendLog(string text)
        {
            txtOutput.AppendText(text + Environment.NewLine);
            txtOutput.ScrollToEnd();
        } 

        /// <summary>
        /// Worker Complete method
        /// </summary>
        /// <param name="s"></param>
        /// <param name="args"></param>
        public void WorkerCompleted(object s, RunWorkerCompletedEventArgs args)
        {
            try
            {
                if (args.Result is Exception)
                {
                    StringBuilder sb = new StringBuilder();
                    Exception ex = args.Result as Exception;
                    string indent = "";
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
                    string errMsg = args.Result as string;
                    if (!string.IsNullOrEmpty(errMsg))
                        AppendLog(errMsg);

                }
            }
            catch (Exception ex)
            {
                StringBuilder sb = new StringBuilder();
                string indent = "";

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
                BtnGenerate.IsEnabled = true;
            }
            //open file if ticked to open excel
            //if (chkBoxOpenFile.Checked && args.Result != null && !string.IsNullOrEmpty(args.Result.ToString()))
            //{
            //    Process.Start(args.Result.ToString());
            //}
            ProgressBar.Visibility = Visibility.Collapsed;
        }


        private void DoExport(object sender, RoutedEventArgs e)
        {
            BtnGenerate.IsEnabled = false;
            
            if (_cobieWorker == null)
            {
                _cobieWorker = new CobieLiteWorker();
                _cobieWorker.Worker.ProgressChanged += WorkerProgressChanged;
                _cobieWorker.Worker.RunWorkerCompleted += WorkerCompleted;
            }
            //get Excel File Type
            var excelType = ExportFormat.SelectedItem.ToString().GetExcelType();
            //set filters

            // todo: restore commented lines in params.

            //RoleFilter filterRoles = SetRoles();
            //if (!chkBoxNoFilter.Checked)
            //{
            //    _assetfilters.ApplyRoleFilters(filterRoles);
            //    _assetfilters.FlipResult = chkBoxFlipFilter.Checked;
            //}

            

            //set parameters
            var args = new Params
            {
                // ModelFile = txtPath.Text,
                // TemplateFile = txtTemplate.Text,
                // Roles = filterRoles,
                ExportType = excelType,
                FlipFilter = false,
                OpenExcel = false,
                FilterOff = false,
                // ExtId = chkBoxIds.Checked ? EntityIdentifierMode.IfcEntityLabels : EntityIdentifierMode.GloballyUniqueIds,
                // SysMode = SetSystemMode(),
                // Filter = chkBoxNoFilter.Checked ? new OutPutFilters() : _assetfilters,
                // ConfigFile = ConfigFile.FullName,
                // Log = chkBoxLog.Checked,
            };
            //run worker
            _cobieWorker.Run(args);
        }
    }
}
