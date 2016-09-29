using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Xbim.CobieLiteUk.FilterHelper;


namespace XplorerPlugins.Cobie.UI
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public FilterViewModel Filters { get; set; }

        public Window1()
        {
            InitializeComponent();
            var f = OutPutFilters.GetDefaults(RoleFilter.Unknown);
            Filters = new FilterViewModel(f);
        }
    }
}
