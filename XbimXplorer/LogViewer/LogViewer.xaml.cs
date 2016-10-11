﻿using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using log4net;
using NuGet;
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

        private void Test(object sender, RoutedEventArgs e)
        {
            Log.Debug("Test");
        }

        private void Clear(object sender, RoutedEventArgs e)
        {
            LoggedEvents.Clear();
            if (_mw == null)
                return;
            _mw.UpdateLoggerCounts();
        }

        private void Copy()
        {
            var sb = new StringBuilder();
            if (View.SelectedItems.Count > 0)
            {
                foreach (var eventViewModel in View.SelectedItems.OfType<EventViewModel>())
                {
                    DumpEvent(eventViewModel, sb);
                }
            }
            else
            {
                foreach (var eventViewModel in View.Items.OfType<EventViewModel>())
                {
                    DumpEvent(eventViewModel, sb);
                }
            }
            Clipboard.SetText(sb.ToString());
        }

        private void DumpEvent(EventViewModel eventViewModel, StringBuilder sb)
        {
            sb.AppendFormat("==== {0}\t{1}\t{2}\r\n{3}\r\n{4}\r\n\r\n",
                eventViewModel.TimeStamp,
                eventViewModel.Level,
                eventViewModel.Logger,
                eventViewModel.Message,
                eventViewModel.ErrorMessage
                );
        }

        private void ClearDebug(object sender, RoutedEventArgs e)
        {
            LoggedEvents.RemoveAll(x => x.Level == "DEBUG");
            if (_mw == null)
                return;
            _mw.UpdateLoggerCounts();
        }

        private void DoCopy(object sender, RoutedEventArgs e)
        {
            Copy();
        }

        private void ClearInformation(object sender, RoutedEventArgs e)
        {
            LoggedEvents.RemoveAll(x => 
                x.Level == "DEBUG"
                || x.Level == "INFO"
                );
            if (_mw == null)
                return;
            _mw.UpdateLoggerCounts();
        }
    }
}
