using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Input;
using Microsoft.Win32;
using Xbim.WindowsUI.DPoWValidation.Extensions;
using Xbim.WindowsUI.DPoWValidation.ViewModels;

namespace Xbim.WindowsUI.DPoWValidation.Commands
{
    public class FacilitySaveCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        private ValidationViewModel _vm;

        public bool CanExecute(object parameter)
        {
            return _vm.ValidationFacility != null;
        }

        public void Execute(object parameter)
        {
            _vm.ExportValidatedFacility();
        }

        public void ChangesHappened()
        {
            if (CanExecuteChanged != null)
            {
                CanExecuteChanged.Invoke(this, new EventArgs());
            }
        }

        public FacilitySaveCommand(ValidationViewModel viewModel)
        {
            _vm = viewModel;
        }
    }
}
