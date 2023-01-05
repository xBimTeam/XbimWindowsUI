using Microsoft.Extensions.Logging;
using Microsoft.Isam.Esent.Interop;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using Xbim.Presentation.XplorerPluginSystem;

namespace XbimXplorer.LogViewer
{
    /// <summary>
    /// Interaction logic for LogViewer.xaml
    /// </summary>
    [XplorerUiElement(PluginWindowUiContainerEnum.LayoutAnchorable , PluginWindowActivation.OnMenu, 
        "View/Developer/Information Log", "LogViewer/LogViewer.png")]
    public partial class LogViewer : IXbimXplorerPluginWindow
    {
        private const string VerboseString = "Verbose";
        private const string DebugString = "Debug";
        private const string InfoString = "Information";
        private const string WarningString = "Warning";

        private XplorerMainWindow _mw;

        protected ILogger Logger { get; private set; }

        public ObservableCollection<EventViewModel> LoggedEvents { get; set; }
        
        public LogViewer()
        {
            InitializeComponent();
            Logger = XplorerMainWindow.LoggerFactory.CreateLogger<LogViewer>();
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
            Logger.LogDebug("Test");
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
            if (Verbose.IsChecked != null && Verbose.IsChecked.Value)
            {
                sb.AppendFormat("==== {0}\t{1}\t{2}\t{3}\t{4}\t{5}\r\n{6}\r\n\r\n",
                    eventViewModel.TimeStamp,
                    eventViewModel.ThreadId,
                    eventViewModel.Level,
                    eventViewModel.Logger,
                    eventViewModel.EntityLabel,
                    eventViewModel.Message,
                    eventViewModel.ErrorMessage
                    );
            }
            else
            {
                sb.AppendFormat("{0}\t{1}\t{2}\t{3}\r\n",
                    eventViewModel.Level,
                    eventViewModel.Logger,
                    eventViewModel.EntityLabel,
                    eventViewModel.Message
                    );
            }
        }

        private void ClearVerbose(object sender, RoutedEventArgs e)
        {
            LoggedEvents.RemoveAll(x => x.Level == VerboseString);
            if (_mw == null)
                return;
            _mw.UpdateLoggerCounts();
        }

        private void ClearDebug(object sender, RoutedEventArgs e)
        {
            LoggedEvents.RemoveAll(x => x.Level == DebugString || x.Level == VerboseString);
            if (_mw == null)
                return;
            _mw.UpdateLoggerCounts();
        }

        private void ClearWarning(object sender, RoutedEventArgs e)
        {
            LoggedEvents.RemoveAll(x =>
                x.Level == VerboseString
                || x.Level == DebugString
                || x.Level == InfoString
                || x.Level == WarningString
                );
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
                x.Level == VerboseString
                || x.Level == DebugString
                || x.Level == InfoString
                );
            if (_mw == null)
                return;
            _mw.UpdateLoggerCounts();
        }

        private void WindowKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Copy();
                e.Handled = true;
            }
        }

        private void AttemptOpenSelection(object sender, MouseButtonEventArgs e)
        {
            var first = View.SelectedItems.OfType<EventViewModel>().FirstOrDefault();
            
            var msg = first?.Message;
            var ent = first?.EntityLabel;
            if (string.IsNullOrEmpty(msg))
                return;

            if(ent.HasValue)
            {
                SelectItem(ent.Value);
            }
            else
            {
                // Legacy method: extract from message
                var reEntityLabel = new Regex(@"#(\d+)");
                var mEntityLabel = reEntityLabel.Match(msg);
                if (mEntityLabel.Success)
                {
                    int eLabel;
                    if (!int.TryParse(mEntityLabel.Groups[1].Value, out eLabel))
                        return;

                    SelectItem(eLabel);
                }
            }
            

            var reUrl = new Regex(@"(http([^ ]+))", RegexOptions.IgnoreCase);
            var mUrl = reUrl.Match(msg);
            
            if (mUrl.Success)
            {
                var text = mUrl.Groups[1].Value;
                System.Diagnostics.Process.Start(text);
            }
        }

        private void SelectItem(int entityLabel)
        {
            var ipers = _mw.Model.Instances[entityLabel];
            if (ipers == null)
                return;
            _mw.SelectedItem = ipers;
        }
    }
}
