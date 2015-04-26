using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Input;
using Microsoft.Win32;
using Xbim.WindowsUI.DPoWValidation.Extensions;
using Xbim.WindowsUI.DPoWValidation.ViewModels;

namespace Xbim.WindowsUI.DPoWValidation.Commands
{
    public class SubmittedFacilitySaveCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        private ValidationViewModel _vm;

        public bool CanExecute(object parameter)
        {
            return _vm.SubmissionFileInfo.Exists;
        }

        public void Execute(object parameter)
        {
            _vm.OpenOnExported = true;
            _vm.ExecuteSaveCobie();
        }

        public void ChangesHappened()
        {
            if (CanExecuteChanged != null)
            {
                CanExecuteChanged.Invoke(this, new EventArgs());
            }
        }

        public SubmittedFacilitySaveCommand(ValidationViewModel viewModel)
        {
            _vm = viewModel;
        }
    }
}
