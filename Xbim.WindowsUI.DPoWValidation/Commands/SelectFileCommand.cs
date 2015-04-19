using System;
using System.IO;
using System.Windows.Input;
using Microsoft.Win32;
using Xbim.WindowsUI.DPoWValidation.Models;
using Xbim.WindowsUI.DPoWValidation.ViewModels;

namespace Xbim.WindowsUI.DPoWValidation.Commands
{
    public class SelectFileCommand : ICommand
    {
        private readonly SourceFile _currentFile;
        private readonly ValidationViewModel _vm;
        public bool IncludeIfc = false;


        public SelectFileCommand(SourceFile tb, ValidationViewModel model)
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
            const string modelExtensions = @";*.ifc;*.ifcxml;*.xbim;*.ifczip";

            var filter = IncludeIfc
                ? @"All model files|*.xls;*.xlsx;*.json" + modelExtensions + "|" +
                    "COBie files|*.xls;*.xlsx|" +
                    "CobieLite files|*.json;*.xml|" +
                    "IFC Files|*.Ifc;*.ifcxml;*.xbim;*.ifczip"
                : @"All model files|*.xls;*.xlsx;*.json" +  "|" +
                    "COBie files|*.xls;*.xlsx|" +
                    "CobieLite files|*.json;*.xml"
                ;

            filter = @"All files|*.*|" + filter;

            var dlg = new OpenFileDialog
            {
                Filter = filter
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
