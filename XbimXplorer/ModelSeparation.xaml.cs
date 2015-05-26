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

namespace XbimXplorer
{
    /// <summary>
    /// Interaction logic for ModelSeparation.xaml
    /// </summary>
    public partial class ModelSeparation : Window
    {
        /// <summary>
        /// 
        /// </summary>
        public ModelSeparation()
        {
            InitializeComponent();

        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Window.ContentRendered"/> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
        }
    }
}
