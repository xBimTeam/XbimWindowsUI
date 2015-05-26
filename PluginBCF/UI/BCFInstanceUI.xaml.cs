using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Xbim.BCF.UI
{
    /// <summary>
    /// Interaction logic for BCFInstanceUI.xaml
    /// </summary>
    public partial class BcfInstanceUi : UserControl
    {
        public BcfInstanceUi()
        {
            InitializeComponent();
        }

        private void cmts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Cmts.SelectedItem == null || !(Cmts.SelectedItem is BcfInstance))
            {
                SelComment.DataContext = Cmts.SelectedItem;
            }
        }
    }
}
