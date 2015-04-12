using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using Xbim.CobieLiteUK.Validation;
using Xbim.CobieLiteUK.Validation.Reporting;
using Xbim.COBieLiteUK;
using Xbim.WindowsUI.DPoWValidation.Extensions;
using Path = System.IO.Path;

namespace Xbim.WindowsUI.DPoWValidation
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();
        }

        private void SetStatus(string status, int progress)
        {
            ReportText.Text = status;
            ReportText.Refresh();

            ProgressBar.Value = progress;
            ProgressBar.Refresh();
        }

        private Facility validated = null;


        private void Validate_Click(object sender, RoutedEventArgs e)
        {
            SetStatus("Loading requirement.", 10);
            var req = Facility.ReadJson(RequirementFileName.Text);

            SetStatus("Loading submission.", 30);
            var mod = Facility.ReadJson(SubmissionFileName.Text);

            SetStatus("Validating model.", 60);
            
            var vd = new FacilityValidator();
            validated = vd.Validate(req, mod);

            SetStatus("Validation completed.", 100);
            
            Thread.Sleep(400);
            SetStatus("Ready to save report.", 0);
            SetButtonsToSaveReport();
            SaveReportButton.IsEnabled = true;
            OutFileName = ReportFileName.Text;

        }

        private void SetButtonsToSaveReport()
        {
            SaveReportButton.IsEnabled = true;
            OpenReportButton.Visibility = Visibility.Collapsed;
            SaveReportButton.Visibility = Visibility.Visible;
        }

        private string OutFileName;
        private string Result;
        // private string OutFileName;

        public void WorkThreadFunction()
        {
            try
            {
                var outFilInfo = new FileInfo(OutFileName);
                //SetStatus("Exporting report.", 80);
                Result = validated.ExportFacility(outFilInfo);
            }
            catch (Exception ex)
            {
                Result = @"Error.";
            }
        }

        private void ReportSave_Click(object sender, RoutedEventArgs e)
        {
            this.Cursor = Cursors.Wait;


            var thread = new Thread(new ThreadStart(WorkThreadFunction));
            thread.Start();
            thread.Join();
            
            SetStatus(Result, 0);

            var f = new FileInfo(ReportFileName.Text);
            if (f.Exists && f.Length > 0)
            {
                SetButtonsToOpen();
            }
            this.Cursor = Cursors.Arrow;
        }

        private void SetButtonsToOpen()
        {
            OpenReportButton.Visibility = Visibility.Visible;
            SaveReportButton.Visibility = Visibility.Collapsed;
        }

        private void RequirementSelect_Click(object sender, RoutedEventArgs e)
        {
            RequirementFileName.Text = LoadSubmission(false, RequirementFileName.Text);
        }
        private void SubmissionSelect_Click(object sender, RoutedEventArgs e)
        {
            SubmissionFileName.Text = LoadSubmission(false, SubmissionFileName.Text);
        }

        private static string LoadSubmission(bool includeIfc, string currentFile)
        {
            const string modelExtensions = @";*.ifc;*.ifcxml;*.xbim;*.ifczip";

            var filter = includeIfc 
                ? @"All model files|*.xls;*.xlsx;*.json" + modelExtensions + "|" +
                    "COBie files|*.xls;*.xlsx|" +
                    "CobieLite files|*.json;*.xml|" +
                    "IFC Files|*.Ifc;*.ifcxml;*.xbim;*.ifczip"
                : @"All model files|*.xls;*.xlsx;*.json" + "|" +
                    "COBie files|*.xls;*.xlsx|" +
                    "CobieLite files|*.json;*.xml"
                ;

            filter = @"All files|*.*|" + filter;

            var dlg = new OpenFileDialog
            {
                Filter = filter
            };
            if (File.Exists(currentFile))
            {
                dlg.InitialDirectory = Path.GetDirectoryName(currentFile);
            }

            var result = dlg.ShowDialog();
            if (!result.HasValue || result != true)
                return dlg.FileName;
            return null;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var filters = new List<string>();
            filters.Add("Validation report|*.xlsx");
            filters.Add("Validation report|*.xls");
            filters.Add(@"Automation format|*.json");
            filters.Add(@"Automation format|*.xml");

            var dlg = new SaveFileDialog
            {
                Filter = string.Join("|", filters.ToArray())
            };
            if (File.Exists(ReportFileName.Text))
            {
                dlg.InitialDirectory = Path.GetDirectoryName(ReportFileName.Text);
            }

            var result = dlg.ShowDialog();
            if (!result.HasValue || result != true)
                return;

            ReportFileName.Text = dlg.FileName;
        }

        private void OpenReport_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(OutFileName);
        }
    }
}
