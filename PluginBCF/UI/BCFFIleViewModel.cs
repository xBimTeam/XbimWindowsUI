using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Xbim.BCF.UI
{
    public class BcffIleViewModel : DependencyObject
    {
        public BcfFile File = new BcfFile();
        public ObservableCollection<BcfInstance> Instances
        {
            get
            {
                return File.Instances;
            }
        }

        public string Cnt
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
