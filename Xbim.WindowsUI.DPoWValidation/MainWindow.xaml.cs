using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using Microsoft.Practices.Unity;
using Xbim.COBieLiteUK;
using Xbim.WindowsUI.DPoWValidation.Injection;
using Xbim.WindowsUI.DPoWValidation.Properties;
using Xbim.WindowsUI.DPoWValidation.ViewModels;
using System.Diagnostics;

namespace Xbim.WindowsUI.DPoWValidation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, ICanShow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public MainWindow([Dependency]ValidationViewModel ViewModel) 
            : this ()
        {
            LoadSettings(ViewModel);
            ValidationGrid.DataContext = ViewModel;
            CreateCobieGrid.DataContext = ViewModel;
        }


        private static void LoadSettings(ValidationViewModel vm)
        {
            if (File.Exists(Settings.Default.LastOpenedRequirement))
                vm.RequirementFileSource = Settings.Default.LastOpenedRequirement;
            if (File.Exists(Settings.Default.LastOpenedSubmission))
                vm.SubmissionFileSource = Settings.Default.LastOpenedSubmission;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
        }

        private void SaveSettings()
        {
            if (!(DataContext is ValidationViewModel)) 
                return;
            var vm = DataContext as ValidationViewModel;
            Settings.Default.LastOpenedRequirement = vm.RequirementFileSource;
            Settings.Default.LastOpenedSubmission = vm.SubmissionFileSource;
            Settings.Default.Save();
        }

        private Facility f;

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(CobieFile.Text))
                return;


            var filters = new List<string>();
            filters.Add("text file|*.txt");
            //filters.Add(@"Automation format|*.json");
            //filters.Add(@"Automation format|*.xml");

            var file = GetSaveFileName("Select destination file", filters);
            if (file == "")
                return;
            
            string read;
            f = Facility.ReadCobie(CobieFile.Text, out read);
            var flogger = new FileInfo(file);
            using (var logger = flogger.CreateText())
            {
                f.ValidateUK2012(logger, true);
            }
            if (flogger.Exists)
            {
                Process.Start(flogger.FullName);
                ImproveCObie.IsEnabled = true;
            }
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            if (f == null)
                return;
            string log;

            var filters = new List<string>();
            filters.Add("COBie excel|*.xlsx");
            filters.Add("COBie binary excel|*.xls");
            //filters.Add(@"Automation format|*.json");
            //filters.Add(@"Automation format|*.xml");

            var file = GetSaveFileName("Select destination file", filters);
            if (file == "")
                return;

            f.WriteCobie(file, out log);
            if (File.Exists(file))
                Process.Start(file);
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            const string modelExtensions = @";*.ifc;*.ifcxml;*.xbim;*.ifczip";

            const string filter = @"IFC Files|*.Ifc;*.ifcxml;*.xbim;*.ifczip|" +
                                  // "COBie files|*.xls;*.xlsx|" +
                                  "CobieLite files|*.json;*.xml|" +
                                  @"All model files|*.json" + modelExtensions + "|" +
                                  "";
                

            // filter = @"All files|*.*|" + filter;

            var dlg = new OpenFileDialog
            {
                Filter = filter
            };
            
            if (File.Exists(IfcToConvert.Text ))
            {
                dlg.InitialDirectory = System.IO.Path.GetDirectoryName(IfcToConvert.Text);
            }

            var result = dlg.ShowDialog();

            if (result != System.Windows.Forms.DialogResult.OK)
                return;

            IfcToConvert.Text  = dlg.FileName;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(IfcToConvert.Text))
                return;

            var filters = new List<string>();
            filters.Add("COBie excel|*.xlsx");
            filters.Add("COBie binary excel|*.xls");
            //filters.Add(@"Automation format|*.json");
            //filters.Add(@"Automation format|*.xml");

            var file = GetSaveFileName("Select destination file", filters);
            if (file == "")
                return;


            // _currentFile.File = dlg.FileName;
            // _vm.FilesUpdate();

        }

        ISaveFileSelector FileSelector { get; set; }

        private string GetSaveFileName(string repName, List<string> filters)
        {
            FileSelector.Filter = string.Join("|", filters.ToArray());
            FileSelector.Title = repName;
            
            var file = "";
            var result = FileSelector.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
                file = FileSelector.FileName;
            return file;
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            const string filter = "COBie files|*.xls;*.xlsx";
            var dlg = new OpenFileDialog
            {
                Filter = filter
            };

            if (File.Exists(CobieFile.Text))
            {
                dlg.InitialDirectory = System.IO.Path.GetDirectoryName(IfcToConvert.Text);
            }

            var result = dlg.ShowDialog();

            if (result != System.Windows.Forms.DialogResult.OK)
                return;

            CobieFile.Text = dlg.FileName;
        }
    }
}
