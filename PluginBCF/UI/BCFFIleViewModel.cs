using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Xbim.BCF.UI
{
    public class BCFFIleViewModel : DependencyObject
    {
        public BCFFile File = new BCFFile();
        public ObservableCollection<BCFInstance> Instances
        {
            get
            {
                return File.Instances;
            }
        }

        public string cnt
        {
            get
            {
                return File.Instances.Count().ToString();
            }
        }

        internal void LoadFrom(string fileName)
        {
            File.Instances.Clear();
            File.LoadFile(fileName);
        }

        internal void SaveTo(string filename)
        {
            File.SaveFile(filename);
        }
    }
}
