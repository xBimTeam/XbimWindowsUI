using Serilog.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Xbim.IO;
using Xbim.IO.Esent;
using XbimXplorer.Properties;

namespace XbimXplorer.Dialogs
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow
    {
        public class SettingWindowVm: INotifyPropertyChanged
        {
            public SettingWindowVm()
            {
                SelFileAccessMode = Settings.Default.FileAccessMode;

                NumberRecentFiles = Settings.Default.MRUFilesCount.ToString();
                PluginStartupLoad = Settings.Default.PluginStartupLoad;
                DeveloperMode = Settings.Default.DeveloperMode;
                LoggingLevel = Settings.Default.LoggingLevel;
                PluginMessage = "Enabled";

                var mainApp = Application.Current.MainWindow as XplorerMainWindow;
                if (mainApp == null)
                    return;
                if (mainApp.PreventPluginLoad)
                {
                    PluginMessage = "Enabled by default (currently disabled via command line)";
                }
            }

            public XbimDBAccess SelFileAccessMode { get; set; }

            List<XbimDBAccess> _fileAccessModes;

            public IEnumerable<XbimDBAccess> FileAccessModes
            {
                get
                {
                    if (_fileAccessModes != null)
                        return _fileAccessModes;

                    _fileAccessModes = new List<XbimDBAccess>();
                    var values = Enum.GetValues(typeof(XbimDBAccess));
                    foreach (var item in values)
                    {
                        _fileAccessModes.Add((XbimDBAccess)item);
                    }
                    return _fileAccessModes;
                }
            }

            public string NumberRecentFiles { get; set; }

            internal void SaveSettings()
            {
                Settings.Default.FileAccessMode = SelFileAccessMode;
                int iNumber;
                if (!int.TryParse(NumberRecentFiles, out iNumber))
                    iNumber = 4;
                Settings.Default.MRUFilesCount = iNumber;
                Settings.Default.PluginStartupLoad = PluginStartupLoad;
                Settings.Default.DeveloperMode = DeveloperMode;
                Settings.Default.LoggingLevel = LoggingLevel;
                Settings.Default.Save();
            }

            /// <summary>
            /// Defines whether to enable plugins at startup.
            /// </summary>
            public bool PluginStartupLoad { get; set; }

            public string PluginMessage { get; set; }

            /// <summary>
            /// Defines whether to enable extra UI elements aimed at developers.
            /// </summary>
            public bool DeveloperMode { get; set; }

            public LogEventLevel LoggingLevel { get; set; }

            private Lazy<IEnumerable<LogEventLevel>> loggingLevels = new Lazy<IEnumerable<LogEventLevel>>(
                () => Enum.GetValues(typeof(LogEventLevel)).Cast<LogEventLevel>().OrderBy(e => (int)e) );

            public IEnumerable<LogEventLevel> LoggingLevels
            {
                get => loggingLevels.Value;
                
            }

            public event PropertyChangedEventHandler PropertyChanged // Not currently used
            {
                add { }
                remove { }
            }
        }

        private readonly SettingWindowVm _vm;

        public SettingsWindow()
        {
            InitializeComponent();
            _vm = new SettingWindowVm();
            DataContext = _vm;
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            _vm.SaveSettings();
            SettingsChanged = true;
            Close();            
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();            
        }

        public bool SettingsChanged { get; private set; }

        private void ButtonReset_Click(object sender, RoutedEventArgs e)
        {
            var retVal = MessageBox.Show("Are you sure you wish to reset all settings to defalut valuse?", "Reset settings", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
            if (retVal != MessageBoxResult.Yes) 
                return;
            Settings.Default.Reset();
            SettingsChanged = true;
            Close();
        }

        private static bool IsTextAllowed(string text)
        {
            int v;
            return int.TryParse(text, out v);
        }

        private void IntOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        //private void ManualPluginLoad(object sender, RoutedEventArgs e)
        //{
        //    var window = Application.Current.MainWindow as XplorerMainWindow;
        //    window?.RefreshPlugins();
        //}
    }
}
