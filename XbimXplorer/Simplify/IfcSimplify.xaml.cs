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
using Microsoft.Win32;

namespace XbimXplorer.Simplify
{
    /// <summary>
    /// Interaction logic for IfcSimplify.xaml
    /// </summary>
    public partial class IfcSimplify : Window
    {
        /// <summary>
        /// 
        /// </summary>
        public IfcSimplify()
        {
            InitializeComponent();
        }

        Dictionary<int, string> _ifcLines = new Dictionary<int, string>();
        Dictionary<int, string> _ifcContents = new Dictionary<int, string>();
        Dictionary<int, string> _ifcType = new Dictionary<int, string>();
        List<int> _elementsToExport = new List<int>();

        string _header;
        string _footer;

        private void cmdInit_Click(object sender, RoutedEventArgs e)
        {
            _ifcLines = new Dictionary<int, string>();
            _ifcContents = new Dictionary<int, string>();
            _elementsToExport = new List<int>();
            _ifcType = new Dictionary<int, string>();
            _header = "";
            _footer = "";


            FileTextParser fp = new FileTextParser(TxtInputFile.Text);
            string readLine;
            bool foundAnyLine = false;

            List<int> requiredLines = new List<int>();

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

            while ((readLine = fp.NextLine()) != null)
            {
                Match m = re.Match(readLine);
                if (m.Success)
                {
                    foundAnyLine = true;
                    int iId = Convert.ToInt32(m.Groups[1].ToString());
                    string type = m.Groups[2].ToString();

                    _ifcLines.Add(iId, readLine);
                    _ifcContents.Add(iId, m.Groups[3].ToString());
                    _ifcType.Add(iId, type);

                    if (
                        type == "IFCPROJECT"
                        )
                    {
                        requiredLines.Add(iId);
                    }

                }
                else
                {
                    if (foundAnyLine == false)
                        _header += readLine + "\r\n";
                    else
                        _footer += readLine + "\r\n";
                }
            }
            fp.Close();
            fp.Dispose();
            GCommands.IsEnabled = true;

            if (true)
            {
                foreach (int i in requiredLines)
                {
                    RecursiveAdd(i);
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
                    iConv = Convert.ToInt32(TxtEntityLabelAdd.Text);
                }
                catch
                {

                }
                return iConv;
            }
        }

        private void txtEntityLabelAdd_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TxtEntityLabelAdd.Text == "")
            {
                UpdateStatusCount();
                return;
            }

            int ic = SelectedIfcIndex;
            if (_ifcLines.ContainsKey(ic))
            {
                TxtOutput.Text = _ifcLines[ic];
            }
            else
            {
                TxtOutput.Text = "Not found";
            }
        }

        private void RecursiveAdd(int ifcIndex)
        {
            if (_elementsToExport.Contains(ifcIndex))
                return; // been exported already;

            _elementsToExport.Add(ifcIndex);

            Regex re = new Regex(
                "#(\\d+)" + // hash and integer index
                ""
                );
            try
            {
                MatchCollection mc = re.Matches(_ifcContents[ifcIndex]);
                foreach (Match mtch in mc)
                {
                    int thisIndex = Convert.ToInt32(mtch.Groups[1].ToString());
                    //if (!_ElementsToExport.Contains(ThisIndex))
                    //    _ElementsToExport.Add(ThisIndex);
                    RecursiveAdd(thisIndex);
                }
            }
            catch
            {
            }
        }

        private void CmdAdd_Click(object sender, RoutedEventArgs e)
        {
            RecursiveAdd(SelectedIfcIndex);
            UpdateStatusCount();
        }

        private void UpdateStatusCount()
        {
            _elementsToExport.Sort();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Elements: " + _elementsToExport.Count.ToString());
            foreach (int i in _elementsToExport)
            {
                try
                {
                    sb.AppendLine(i.ToString() + ":" + _ifcType[i]);
                }
                catch
                {
                }
            }
            TxtOutput.Text = sb.ToString();
        }

        private void cmdSave_Click(object sender, RoutedEventArgs e)
        {
            FileInfo t = new FileInfo(TxtInputFile.Text + ".stripped.ifc");
            StreamWriter tex = t.CreateText();

            tex.Write(_header);
            foreach (int i in _elementsToExport)
            {
                tex.WriteLine(_ifcLines[i]);
            }
            tex.Write(_footer);
            tex.Close();
            TxtOutput.Text = "Done";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.FileName = ""; // Default file name
            dlg.DefaultExt = ".ifc"; // Default file extension
            dlg.Filter = "Ifc files (.ifc)|*.ifc"; // Filter files by extension 

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results 
            if (result == true)
            {
                // Open document 
                TxtInputFile.Text = dlg.FileName;
            }
        }

    }
}
