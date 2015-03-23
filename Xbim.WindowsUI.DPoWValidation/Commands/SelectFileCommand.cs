using System;
using System.IO;
using System.Windows.Input;
using Microsoft.Win32;
using Xbim.WindowsUI.DPoWValidation.Models;
using Xbim.WindowsUI.DPoWValidation.ViewModels;

namespace Xbim.WindowsUI.DPoWValidation.Commands
{
    class SelectFileCommand : ICommand
    {
        private SourceFile _textBox;
        private ValidationViewModel _vm;
        public bool IncludeIfc = false;


        public SelectFileCommand(SourceFile tb, ValidationViewModel model)
        {
            _textBox = tb;
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
            const string modelExtensions = @"*.ifc;*.ifcxml;*.xbim;*.ifczip;";

            var filter = IncludeIfc
                ? @"All model files|*.json;" + modelExtensions + "|" +
                    "CobieLite files|*.json;*.xml|" +
                    "IFC Files|*.Ifc"
                : @"CobieLite files|*.json;*.xml"
                ;

            filter = @"All files|*.*|" + filter;

            var dlg = new OpenFileDialog
            {
                Filter = filter
            };
            if (_textBox.Exists)
            {
                dlg.InitialDirectory = Path.GetDirectoryName(_textBox.File);
            }

            var result = dlg.ShowDialog();
            if (!result.HasValue || result != true) 
                return;

            _textBox.File = dlg.FileName ;
            _vm.FilesUpdate();
        }
    }
}
