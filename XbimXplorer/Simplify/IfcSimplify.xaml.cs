using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Bonghi.FileIO;

namespace XbimXplorer.Simplify
{
    /// <summary>
    /// Interaction logic for IfcSimplify.xaml
    /// </summary>
    public partial class IfcSimplify : Window
    {
        public IfcSimplify()
        {
            InitializeComponent();
        }

        Dictionary<int, string> _IfcLines = new Dictionary<int, string>();
        Dictionary<int, string> _IfcContents = new Dictionary<int, string>();
        Dictionary<int, string> _IfcType = new Dictionary<int, string>();
        List<int> _ElementsToExport = new List<int>();

        string _Header;
        string _Footer;

        private void cmdInit_Click(object sender, RoutedEventArgs e)
        {
            _IfcLines = new Dictionary<int, string>();
            _IfcContents = new Dictionary<int, string>();
            _ElementsToExport = new List<int>();
            _IfcType = new Dictionary<int, string>();
            _Header = "";
            _Footer = "";


            FileTextParser fp = new FileTextParser(txtInputFile.Text);
            string ReadLine;
            bool FoundAnyLine = false;

            List<int> RequiredLines = new List<int>();

            //Regex re = new Regex(
            //    "#(\\d+)" + // integer index
            //    " *" + // optional spaces
            //    "=" + // =
            //    " *" + // optional spaces
            //    "([^(]*)" +  // class information type (anything but an open bracket as many times)
            //    "\\(" + // the open bracket (escaped)
            //    "(.*)" + // anything repeated
            //    "\\);" // the closing bracket escaped and the semicolon
            //    );

            Regex re = new Regex(
                "#(\\d+)" + // integer index
                " *" + // optional spaces
                "=" + // =
                " *" + // optional spaces
                "([^ (]*)" +  // class information type (anything but an open bracket as many times)
                " *" + // optional spaces
                "\\(" + // the open bracket (escaped)
                "(.*)" + // anything repeated
                "\\);" // the closing bracket escaped and the semicolon
                );

            while ((ReadLine = fp.NextLine()) != null)
            {
                Match m = re.Match(ReadLine);
                if (m.Success)
                {
                    FoundAnyLine = true;
                    int iId = Convert.ToInt32(m.Groups[1].ToString());
                    string type = m.Groups[2].ToString();

                    _IfcLines.Add(iId, ReadLine);
                    _IfcContents.Add(iId, m.Groups[3].ToString());
                    _IfcType.Add(iId, type);

                    if (
                        type == "IFCPROJECT"
                        )
                    {
                        RequiredLines.Add(iId);
                    }

                }
                else
                {
                    if (FoundAnyLine == false)
                        _Header += ReadLine + "\r\n";
                    else
                        _Footer += ReadLine + "\r\n";
                }
            }
            fp.Close();
            fp.Dispose();
            gCommands.IsEnabled = true;

            if (true)
            {
                foreach (int i in RequiredLines)
                {
                    recursiveAdd(i);
                }
            }
            UpdateStatusCount();
        }

        int SelectedIfcIndex
        {
            get
            {
                int iConv = -1;
                try
                {
                    iConv = Convert.ToInt32(txtEntityLabelAdd.Text);
                }
                catch
                {

                }
                return iConv;
            }
        }

        private void txtEntityLabelAdd_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtEntityLabelAdd.Text == "")
            {
                UpdateStatusCount();
                return;
            }

            int ic = SelectedIfcIndex;
            if (_IfcLines.ContainsKey(ic))
            {
                txtOutput.Text = _IfcLines[ic];
            }
            else
            {
                txtOutput.Text = "Not found";
            }
        }

        private void recursiveAdd(int IfcIndex)
        {
            if (_ElementsToExport.Contains(IfcIndex))
                return; // been exported already;

            _ElementsToExport.Add(IfcIndex);

            Regex re = new Regex(
                "#(\\d+)" + // hash and integer index
                ""
                );
            try
            {
                MatchCollection mc = re.Matches(_IfcContents[IfcIndex]);
                foreach (Match mtch in mc)
                {
                    int ThisIndex = Convert.ToInt32(mtch.Groups[1].ToString());
                    //if (!_ElementsToExport.Contains(ThisIndex))
                    //    _ElementsToExport.Add(ThisIndex);
                    recursiveAdd(ThisIndex);
                }
            }
            catch
            {
            }
        }

        private void CmdAdd_Click(object sender, RoutedEventArgs e)
        {
            recursiveAdd(SelectedIfcIndex);
            UpdateStatusCount();
        }

        private void UpdateStatusCount()
        {
            _ElementsToExport.Sort();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Elements: " + _ElementsToExport.Count.ToString());
            foreach (int i in _ElementsToExport)
            {
                try
                {
                    sb.AppendLine(i.ToString() + ":" + _IfcType[i]);
                }
                catch
                {
                }
            }
            txtOutput.Text = sb.ToString();
        }

        private void cmdSave_Click(object sender, RoutedEventArgs e)
        {
            FileInfo t = new FileInfo(txtInputFile.Text + ".stripped.ifc");
            StreamWriter Tex = t.CreateText();

            Tex.Write(_Header);
            foreach (int i in _ElementsToExport)
            {
                Tex.WriteLine(_IfcLines[i]);
            }
            Tex.Write(_Footer);
            Tex.Close();
            txtOutput.Text = "Done";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = ""; // Default file name
            dlg.DefaultExt = ".ifc"; // Default file extension
            dlg.Filter = "Ifc files (.ifc)|*.ifc"; // Filter files by extension 

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results 
            if (result == true)
            {
                // Open document 
                txtInputFile.Text = dlg.FileName;
            }
        }

    }
}
