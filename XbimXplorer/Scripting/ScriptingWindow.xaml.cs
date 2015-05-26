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
using Xbim.IO;
using Xbim.Script;

namespace XbimXplorer.Scripting
{
    /// <summary>
    /// Interaction logic for ScriptingWindow.xaml
    /// </summary>
    public partial class ScriptingWindow : Window
    {

        /// <summary>
        /// 
        /// </summary>
        public ScriptingWindow()
        {
            InitializeComponent();
        }

        //public event ScriptParsedHandler OnScriptParsed;
        //private void ScriptParsed()
        //{
        //    if (OnScriptParsed != null)
        //        OnScriptParsed(this, new ScriptParsedEventArgs());
        //}
    }
}
