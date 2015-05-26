using System;
using System.Diagnostics;
using System.Windows.Input;

namespace Xbim.Presentation
{
    public class LayerViewModel
    {
        string _layerName;

        public LayerViewModel(string layerName)
        {
            _layerName = layerName;
        }

        public string NameOnMenu
        {
            get 
            {
                return _layerName;
            }
        }

        public ICommand Open
        {
            get
            {
                return new LayerToggleCommand();
            }
        }
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
