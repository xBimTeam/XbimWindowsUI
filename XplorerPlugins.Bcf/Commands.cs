using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Xbim.BCF
{
    public static class BcfFileCommands
    {
        public static RoutedCommand Load = new RoutedCommand(); 
    
        public static RoutedCommand Save = new RoutedCommand();
    }
    
    public static class BcfInstanceCommands
    {
        public static RoutedCommand GotoCameraPosition = new RoutedCommand();
    }
}
