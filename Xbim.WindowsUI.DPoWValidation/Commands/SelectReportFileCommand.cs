using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Input;
using Microsoft.Win32;
using Xbim.WindowsUI.DPoWValidation.Models;
using Xbim.WindowsUI.DPoWValidation.ViewModels;

namespace Xbim.WindowsUI.DPoWValidation.Commands
{
    class SelectReportFileCommand : ICommand
    {
        private readonly SourceFile _currentFile;
        private readonly ValidationViewModel _vm;
        public bool IncludeIfc = false;


        public SelectReportFileCommand(SourceFile tb, ValidationViewModel model)
        {
            _currentFile = tb;
            _vm = model;
        }

        public bool CanExecute(object parameter)
        {
            return _vm.FilesCanChange;
        }

        public event EventHandler CanExecuteChanged;

        public void ChangesHappened()
        {
            if (CanExecuteChanged != null)
            {
                CanExecuteChanged.Invoke(this, new EventArgs());
            }
        }

        public void Execute(object parameter)
        {
            var filters = new List<string>();
            filters.Add("Validation report|*.xlsx");
            filters.Add("Validation report|*.xls");
            filters.Add(@"Automation format|*.json");
            filters.Add(@"Automation format|*.xml");

            var dlg = new SaveFileDialog
            {
                Filter = string.Join("|", filters.ToArray())
            };
            if (_currentFile.Exists)
            {
                dlg.InitialDirectory = Path.GetDirectoryName(_currentFile.File);
            }

            var result = dlg.ShowDialog();
            if (!result.HasValue || result != true) 
                return;

            _currentFile.File = dlg.FileName ;
            _vm.FilesUpdate();
        }
    }
}
