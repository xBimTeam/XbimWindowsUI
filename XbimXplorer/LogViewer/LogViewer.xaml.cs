﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using log4net;
using NuGet;
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
            if (Verbose.IsChecked != null && Verbose.IsChecked.Value)
            {
                sb.AppendFormat("==== {0}\t{1}\t{2}\r\n{3}\r\n{4}\r\n\r\n",
                    eventViewModel.TimeStamp,
                    eventViewModel.Level,
                    eventViewModel.Logger,
                    eventViewModel.Message,
                    eventViewModel.ErrorMessage
                    );
            }
            else
            {
                sb.AppendFormat("{0}\t{1}\t{2}\r\n",
                    eventViewModel.Level,
                    eventViewModel.Logger,
                    eventViewModel.Message
                    );
            }
        }

        private void ClearDebug(object sender, RoutedEventArgs e)
        {
            LoggedEvents.RemoveAll(x => x.Level == "DEBUG");
            if (_mw == null)
                return;
            _mw.UpdateLoggerCounts();
        }

        private void ClearWarning(object sender, RoutedEventArgs e)
        {
            LoggedEvents.RemoveAll(x =>
                x.Level == "DEBUG"
                || x.Level == "INFO"
                || x.Level == "WARN"
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
                x.Level == "DEBUG"
                || x.Level == "INFO"
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
            if (string.IsNullOrEmpty(msg))
                return;
            var reEntityLabel = new Regex(@"#(\d+)");
            var mEntityLabel = reEntityLabel.Match(msg);
            if (mEntityLabel.Success)
            {
                int eLabel;
                if (!int.TryParse(mEntityLabel.Groups[1].Value, out eLabel))
                    return;

                var ipers = _mw.Model.Instances[eLabel];
                if (ipers == null)
                    return;
                _mw.SelectedItem = ipers;
            }

            var reUrl = new Regex(@"(http([^ ]+))", RegexOptions.IgnoreCase);
            var mUrl = reUrl.Match(msg);
            
            // ReSharper disable once InvertIf
            if (mUrl.Success)
            {
                var text = mUrl.Groups[1].Value;
                System.Diagnostics.Process.Start(text);
            }
        }
    }
}
