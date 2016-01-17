using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using XbimXplorer.Properties;

namespace XbimXplorer.Dialogs
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow
    {
        /// <summary>
        /// 
        /// </summary>
        public class SettingWindowVm: INotifyPropertyChanged
        {
            /// <summary>
            /// 
            /// </summary>
            public SettingWindowVm()
            {               
                NumberRecentFiles = Settings.Default.MRUFilesCount.ToString();
                PluginStartupLoad = Settings.Default.PluginStartupLoad;
                DeveloperMode = Settings.Default.DeveloperMode;
            }

            
            /// <summary>
            /// 
            /// </summary>
            public string NumberRecentFiles { get; set; }

            internal void SaveSettings()
            {              
                int iNumber;
                if (!int.TryParse(NumberRecentFiles, out iNumber))
                    iNumber = 4;
                Settings.Default.MRUFilesCount = iNumber;
                Settings.Default.PluginStartupLoad = PluginStartupLoad;
                Settings.Default.DeveloperMode = DeveloperMode;

                Settings.Default.Save();
            }

            /// <summary>
            /// Defines whether to enable plugins at startup.
            /// </summary>
            public bool PluginStartupLoad { get; set; }

            /// <summary>
            /// Defines whether to enable extra UI elements aimed at developers.
            /// </summary>
            public bool DeveloperMode { get; set; }

            /// <summary>
            /// </summary>
            public event PropertyChangedEventHandler PropertyChanged;
        }

        readonly SettingWindowVm _vm;

        /// <summary>
        /// 
        /// </summary>
        public SettingsWindow()
        {
            InitializeComponent();
            _vm = new SettingWindowVm();
            DataContext = _vm;
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            _vm.SaveSettings();
            _settingsChanged = true;
            Close();            
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();            
        }

        private bool _settingsChanged;
        /// <summary>
        /// 
        /// </summary>
        public bool SettingsChanged
        {
            get
            {
                return _settingsChanged;
            }
        }

        private void ButtonReset_Click(object sender, RoutedEventArgs e)
        {
            var retVal = MessageBox.Show("Are you sure you wish to reset all settings to defalut valuse?", "Reset settings", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
            if (retVal != MessageBoxResult.Yes) 
                return;
            Settings.Default.Reset();
            _settingsChanged = true;
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

        private void ManualPluginLoad(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow is XplorerMainWindow)
            {
                ((XplorerMainWindow)Application.Current.MainWindow).RefreshPlugins();
            }
        }
    }
}
