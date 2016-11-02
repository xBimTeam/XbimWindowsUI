using System;
using System.Diagnostics;
using System.Windows.Input;

namespace Xbim.Presentation
{
    public class LayerViewModel
    {
        public LayerViewModel(string layerName)
        {
            NameOnMenu = layerName;
        }

        public string NameOnMenu { get; }

        public ICommand Open => new LayerToggleCommand();
    }

    public class LayerToggleCommand : ICommand
    {
        bool ICommand.CanExecute(object parameter)
        {
            return true;
        }

        event EventHandler ICommand.CanExecuteChanged
        {
            add {  }
            remove {  }
        }

        void ICommand.Execute(object parameter)
        {
            Debug.WriteLine("Doing it");
        }
    }

    class DrawingControl3DLayers
    {
    }
}
