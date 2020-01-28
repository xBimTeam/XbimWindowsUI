using System;
using System.Windows;
using System.Windows.Input;

namespace XbimXplorer
{
    public partial class XplorerMainWindow
    {
        private bool _updateAvailable = false;

        public static RoutedCommand AppUpdate = new RoutedCommand();

        private void UpdateHappened(object sender, ExecutedRoutedEventArgs e)
        {
            _updateAvailable = true;
            StatusBarUpdateNotification.Visibility = Visibility.Visible;
        }

        private void ShowUpdate(object sender, MouseButtonEventArgs e)
        {
            ShowAboutDialog();
        }

        private void CommandBinding_CanUpdate(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
    }
}