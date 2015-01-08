using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xbim.Presentation
{
    using System.ComponentModel;

    public class Observable : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string property)
        {
            //    Debug.WriteLine(property + " was changed.");
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(property));
            }
        }
    }
}
