using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Xbim.BCF
{
    public static class BCFFileCommands
    {
        public static RoutedCommand Load = new RoutedCommand(); 
    
        public static RoutedCommand Save = new RoutedCommand();
    }
    
    public static class BCFInstanceCommands
    {
        public static RoutedCommand GotoCameraPosition = new RoutedCommand();
    }
}
