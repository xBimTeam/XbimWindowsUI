using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Windows.Input;
using Microsoft.Practices.Unity;
using Xbim.WindowsUI.DPoWValidation.Injection;
using Xbim.WindowsUI.DPoWValidation.Models;
using Xbim.WindowsUI.DPoWValidation.ViewModels;

namespace Xbim.WindowsUI.DPoWValidation.Commands
{
    public class SelectReportFileCommand : ICommand
    {
        private readonly SourceFile _currentFile;
        private readonly ValidationViewModel _vm;
        public bool IncludeIfc = false;


        public SelectReportFileCommand(SourceFile tb, ValidationViewModel model)
        {
            FileSelector = ContainerBootstrapper.Instance.Container.Resolve<ISaveFileSelector>();
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

        ISaveFileSelector FileSelector { get; set; }

        public void Execute(object parameter)
        {
            var filters = new List<string>();
            filters.Add("Validation report|*.xlsx");
            filters.Add("Validation report|*.xls");
            filters.Add(@"Automation format|*.json");
            filters.Add(@"Automation format|*.xml");

            FileSelector.Filter = string.Join("|", filters.ToArray());

            
            if (_currentFile.Exists)
            {
                FileSelector.InitialDirectory = Path.GetDirectoryName(_currentFile.File);
            }

            var result = FileSelector.ShowDialog();
            if (result != DialogResult.OK) 
                return;

            _currentFile.File = FileSelector.FileName;
            _vm.FilesUpdate();
        }
    }
}
