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
    [XplorerUiElement(PluginWindowUiContainerEnum.LayoutDoc, PluginWindowActivation.OnMenu, "View/Developer/Information Log")]
    public partial class LogViewer : IXbimXplorerPluginWindow
    {
        private static readonly ILog Log = LogManager.GetLogger("Xbim.WinUI");

        private EventAppender appender;

        public ObservableCollection<EventViewModel> LoggedEvents { get; private set; }
        

        public LogViewer()
        {
            InitializeComponent();
            WindowTitle = "Information Log";

            LoggedEvents = new ObservableCollection<EventViewModel>();
            DataContext = this;

            appender = new EventAppender();
            appender.Logged += appender_Logged;
            
            var hier = LogManager.GetRepository() as Hierarchy;
            if (hier != null)
                hier.Root.AddAppender(appender);
        }

        void appender_Logged(object sender, LogEventArgs e)
        {
            foreach (var loggingEvent in e.LoggingEvents)
            {
                var m = new EventViewModel(loggingEvent);
                // LoggedEvents.Add(m);

                App.Current.Dispatcher.BeginInvoke((Action)delegate()
                {
                    LoggedEvents.Add(m);
                });

                //if (Report.Dispatcher.CheckAccess())
                //{
                //    Report.Text += loggingEvent.RenderedMessage;
                //    Report.Text += loggingEvent.ExceptionObject.Message;
                //}
                //else
                //{
                //    Dispatcher.Invoke((Action)delegate()
                //    {
                //        Report.Text += loggingEvent.RenderedMessage;
                //    });
                //}

            }
        }

        public string WindowTitle { get; private set; }
        public void BindUi(IXbimXplorerPluginMasterWindow mainWindow)
        {
            // nothing needed here
        }

        private void Test(object sender, System.Windows.RoutedEventArgs e)
        {
            Log.Debug("Test");
        }

        private void Clear(object sender, System.Windows.RoutedEventArgs e)
        {
            LoggedEvents.Clear();
        }
    }
}
