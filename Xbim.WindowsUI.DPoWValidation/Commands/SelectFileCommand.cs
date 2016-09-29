using System;
using System.IO;
using System.Windows.Forms;
using System.Windows.Input;
using Xbim.WindowsUI.DPoWValidation.Models;
using Xbim.WindowsUI.DPoWValidation.ViewModels;

namespace Xbim.WindowsUI.DPoWValidation.Commands
{
    public class SelectFileCommand : ICommand
    {
        private readonly SourceFile _currentFile;
        private readonly ValidationViewModel _vm;
        public bool IncludeIfc = false;

        public bool IncludeZip = false;


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

            var cobieLiteFiles = IncludeZip
                ? "CobieLite files|*.json;*.xml;*.zip"
                : "CobieLite files|*.json;*.xml";
            var cobieLiteExtensions = IncludeZip
                ? ";*.json;*.xml;*.zip"
                : ";*.json;*.xml";

            var filter = IncludeIfc
                ? @"All model files|*.xls;*.xlsx;" + modelExtensions + cobieLiteExtensions + "|" +
                    "COBie files|*.xls;*.xlsx|" +
                    cobieLiteFiles + "|" +
                    "IFC Files|*.Ifc;*.ifcxml;*.xbim;*.ifczip"
                : @"All model files|*.xls;*.xlsx;" + cobieLiteExtensions + "|" +
                    "COBie files|*.xls;*.xlsx|" +
                    cobieLiteFiles;
                

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
            if (result != DialogResult.OK)
                return;

            _currentFile.File = dlg.FileName ;
            _vm.FilesUpdate();
        }
    }
}
