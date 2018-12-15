using System;
using System.Collections.ObjectModel;
using System.Windows;
using XbimXplorer.LogViewer;

namespace XbimXplorer
{
    public partial class XplorerMainWindow
    {
        public InMemoryLogSink LogSink { get; private set; }

        public Visibility AnyErrors
        {
            get
            {
                return NumErrors > 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        public int NumErrors { get; private set; }

        public Visibility AnyWarnings
        {
            get
            {
                return NumWarnings > 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        public int NumWarnings { get; private set; }

        public ObservableCollection<EventViewModel> LoggedEvents { get; private set; }

        internal void LogEvent_Added(object sender, LogEventArgs e)
        {
            foreach (var loggingEvent in e.LoggingEvents)
            {
                var m = new EventViewModel(loggingEvent);
                Application.Current.Dispatcher.BeginInvoke((Action)delegate
                {
                    LoggedEvents.Add(m);
                });
                Application.Current.Dispatcher.BeginInvoke((Action)UpdateLoggerCounts);
            }
        }

        internal void UpdateLoggerCounts()
        {
            NumErrors = 0;
            NumWarnings = 0;
            foreach (var loggedEvent in LoggedEvents)
            {
                switch (loggedEvent.Level)
                {
                    case "ERROR":
                        NumErrors++;
                        break;
                    case "WARN":
                        NumWarnings++;
                        break;
                }
            }
            OnPropertyChanged("AnyErrors");
            OnPropertyChanged("NumErrors");
            OnPropertyChanged("AnyWarnings");
            OnPropertyChanged("NumWarnings");
        }
    }
}
