using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using log4net;
using log4net.Core;
using log4net.Repository.Hierarchy;
using Xbim.Presentation.XplorerPluginSystem;

namespace XbimXplorer.LogViewer
{

    

    /// <summary>
    /// Interaction logic for LogViewer.xaml
    /// </summary>
    [XplorerUiElement(PluginWindowUiContainerEnum.LayoutAnchorable , PluginWindowActivation.OnMenu, "View/Developer/Information Log")]
    public partial class LogViewer : IXbimXplorerPluginWindow
    {
        private XplorerMainWindow _mw;

        private static readonly ILog Log = LogManager.GetLogger("Xbim.WinUI");

        public ObservableCollection<EventViewModel> LoggedEvents { get; set; }
        
        public LogViewer()
        {
            InitializeComponent();
            WindowTitle = "Information Log";
           
            DataContext = this;

        }

        public string WindowTitle { get; private set; }
        public void BindUi(IXbimXplorerPluginMasterWindow mainWindow)
        {
            var mWindow = mainWindow as XplorerMainWindow;
            if (mWindow == null)
                return;
            _mw = mWindow;
            LoggedEvents = mWindow.LoggedEvents;
        }

        private void Test(object sender, System.Windows.RoutedEventArgs e)
        {
            Log.Debug("Test");
        }

        private void Clear(object sender, System.Windows.RoutedEventArgs e)
        {
            LoggedEvents.Clear();
            if (_mw == null)
                return;
            _mw.UpdateLoggerCounts();
        }
    }
}
