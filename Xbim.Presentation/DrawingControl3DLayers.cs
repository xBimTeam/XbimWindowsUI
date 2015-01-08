using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Xbim.Presentation
{
    public class LayerViewModel
    {
        string _LayerName;
        DrawingControl3D _d3d;
        public LayerViewModel(string LayerName, DrawingControl3D control3D)
        {
            _LayerName = LayerName;
            _d3d = control3D;
        }

        public string NameOnMenu
        {
            get 
            {
                return _LayerName;
            }
        }

        public ICommand Open
        {
            get
            {
                return new LayerToggleCommand(this);
            }
        }
    }

    public class LayerToggleCommand : ICommand
    {
        private LayerViewModel layerViewModel;

        public LayerToggleCommand(LayerViewModel layerViewModel)
        {
            // TODO: Complete member initialization
            this.layerViewModel = layerViewModel;
        }
        

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
