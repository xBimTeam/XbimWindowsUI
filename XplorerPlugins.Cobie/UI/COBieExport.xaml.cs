using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using log4net;
using Xbim.COBie;
using Xbim.COBie.Serialisers;
using Xbim.IO;
using Xbim.Presentation.XplorerPluginSystem;
using Xbim.Ifc;
using Xbim.Ifc2x3.IO;

namespace XplorerPlugins.Cobie.UI
{
    /// <summary>
    /// Interaction logic for COBieClassFilter.xaml
    /// </summary>
    [XplorerUiElement(PluginWindowUiContainerEnum.Dialog, PluginWindowActivation.OnMenu, "File/Export/COBie")]
    public partial class COBieExport: IXbimXplorerPluginWindow
    {
        const string UkTemplate = "COBie-UK-2012-template.xls";
        const string UsTemplate = "COBie-US-2_4-template.xls";

        private static readonly ILog Log = LogManager.GetLogger("Xbim.WinUI");

        public ObservableCollection<String> Templates { get; set; }
        
        public IfcStore Model
        {
            get { return (IfcStore)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public static DependencyProperty ModelProperty =
            DependencyProperty.Register("Model", typeof(IfcStore), typeof(COBieExport),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, OnSelectedEntityChanged));

        private static void OnSelectedEntityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // if any UI event should happen it needs to be specified here
            var ctrl = d as COBieExport;
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

        private void ConfigureFolder()
        {
            var dir = new DirectoryInfo(".");
            if (_parentWindow != null)
            {
                var openedModel = _parentWindow.GetOpenedModelFileName();
                if (!string.IsNullOrEmpty(openedModel))
                {
                    dir = new DirectoryInfo(
                        Path.Combine(
                            new FileInfo(_parentWindow.GetOpenedModelFileName()).DirectoryName,
                            "Export"
                            ));
                }
            }
            // main folder config
            TxtFolderName.Text = dir.FullName;
        }

        public string SelectedTemplate { get; set; }


        public FilterValues UserFilters { get; set; }    //hold the user required class types, as required by the user

        public ObservableCollection<CheckedListItem<Type>> ClassFilterComponent { get; set; }
        
        public ObservableCollection<CheckedListItem<Type>> ClassFilterType { get; set; }
        
        public ObservableCollection<CheckedListItem<Type>> ClassFilterAssembly { get; set; }

        public COBieExport()
        {
            InitializeComponent();

            ConfigureFolder();

            // prepare templates list
            Templates = new ObservableCollection<string>() {UkTemplate, UsTemplate};
            SelectedTemplate = UkTemplate;

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

        /// <summary>
        /// OK button event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetExcludes(ClassFilterComponent, UserFilters.ObjectType.Component);
                SetExcludes(ClassFilterType, UserFilters.ObjectType.Types);
                SetExcludes(ClassFilterAssembly, UserFilters.ObjectType.Assembly);
                if (!ExportCoBie()) 
                    return;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                var msg = string.Format("Error exporting cobie from plugin.");
                Log.Error(msg, ex);
            }
        }

        // todo: rename to cobietemplatename
        private string CoBieTemplate
        {
            get { return UkTemplate; }
        }

        /// <summary>
        /// Performs the export
        /// </summary>
        /// <returns>True on success, false on error.</returns>
        private bool ExportCoBie()
        {

            if (!Directory.Exists(TxtFolderName.Text))
            {
                try
                {
                    Directory.CreateDirectory(TxtFolderName.Text);
                }
                catch (Exception)
                {
                    MessageBox.Show("Error creating directory. Select a different location.");
                    return false;
                }
            }

            var f = new FileInfo(Path.ChangeExtension(Model.FileName, ".xls"));
            var outputFile = Path.Combine(TxtFolderName.Text, f.Name);


            var context = new COBieContext
            {
                TemplateFileName = CoBieTemplate,
                Model = Model,
                Exclude = UserFilters
            };

            // setting culture for thread; the current one will be restored later.
            CultureInfo exisitingCultureInfo = null;
            try
            {
                var ci = new CultureInfo("en-GB");
                exisitingCultureInfo = Thread.CurrentThread.CurrentUICulture;
                Thread.CurrentThread.CurrentUICulture = ci;
            }
            catch (Exception ex)
            {
                Log.Error("CurrentUICulture could not be set to en-GB.", ex);
            }

            // actual export code
            var builder = new COBieBuilder(context);
            var serialiser = new COBieXLSSerialiser(outputFile, context.TemplateFileName) { Excludes = UserFilters };
            builder.Export(serialiser);


            // restoring culture for thread;
            try
            {
                if (exisitingCultureInfo != null)
                    Thread.CurrentThread.CurrentUICulture = exisitingCultureInfo;
                
            }
            catch (Exception ex)
            {
                Log.Error("CurrentUICulture could not restored.", ex);
            }

            if (ChkOpenExcel.IsChecked.HasValue && ChkOpenExcel.IsChecked.Value)
                Process.Start(outputFile);
            return true;
        }

        /// <summary>
        /// set the UserFilters to the required class types to exclude 
        /// </summary>
        /// <param name="obsColl">ObservableCollection</param>
        /// <param name="userExcludeTypes">List of Type, holding user's  list of class types</param>
        private static void SetExcludes(IEnumerable<CheckedListItem<Type>> obsColl, ICollection<Type> userExcludeTypes)
        {
            foreach (var item in obsColl)
            {
                if (item.IsChecked)
                {
                    if (!userExcludeTypes.Contains(item.Item))
                        userExcludeTypes.Add(item.Item);
                }
                else
                {
                    userExcludeTypes.Remove(item.Item);
                }
            }
        }

        private void SetDefaultFilters(object sender, RoutedEventArgs e)
        {
            SetDefaultFilters();
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
    }
}
