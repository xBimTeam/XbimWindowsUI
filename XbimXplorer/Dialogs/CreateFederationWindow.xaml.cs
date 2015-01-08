using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace XbimXplorer.Dialogs
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class CreateFederationWindow : Window
    {
        public string ModelPath
        {
            get
            {
                return Location.Text;
            }
        }
        public string ModelFullPath
        {
            get
            {
                string path = System.IO.Path.Combine(Location.Text,FederationName.Text) ;
                return  System.IO.Path.ChangeExtension(path, ".xBIMF");
            }
        }
        public string Author
        {
            get
            {
                return CreatedPersonTextBox.Text; ;
            }
        }
        public string Organisation
        {
            get
            {
                return CreatedOrgTextBox.Text; ;
            }
        }
        public string Project
        {
            get
            {
                return ProjectName.Text; ;
            }
        }
        public CreateFederationWindow()
        {
            InitializeComponent();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ProjectName.Text))
            {
                System.Windows.MessageBox.Show("Please specify a Project Name");
                ProjectName.Focus();
            } 
            else if (string.IsNullOrWhiteSpace(FederationName.Text))
            {
                System.Windows.MessageBox.Show("Please specify a Federation Name");
                FederationName.Focus();
            }
            else if (string.IsNullOrWhiteSpace(Location.Text))
            {
                System.Windows.MessageBox.Show("Please specify a Folder Location");
                Location.Focus();
            }
            else if (!Directory.Exists(Location.Text))
            {
                System.Windows.MessageBox.Show("Invalid folder name, please specify a valid one");
                Location.Focus();
            }
            else if (string.IsNullOrWhiteSpace(CreatedPersonTextBox.Text))
            {
                System.Windows.MessageBox.Show("Please specify an Authors Name");
                CreatedPersonTextBox.Focus();
            }
            else if (string.IsNullOrWhiteSpace(CreatedOrgTextBox.Text))
            {
                System.Windows.MessageBox.Show("Please specify an Organisation Name");
                CreatedOrgTextBox.Focus();
            }
            else
            {
                DialogResult = true; 
                this.Close();
            }
        }

        private void BrowseLocation_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            System.Windows.Forms.DialogResult r = dialog.ShowDialog();

            if (r == System.Windows.Forms.DialogResult.OK)
            {
                Location.Text = dialog.SelectedPath;
            }
        }

        private void FederationName_GotFocus(object sender, RoutedEventArgs e)
        {
            FederationName.SelectAll();
        }

        private void ProjectName_GotFocus(object sender, RoutedEventArgs e)
        {
            ProjectName.SelectAll();
        }

        private void CreatedPersonTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            CreatedPersonTextBox.SelectAll();
        }

        private void CreatedOrgTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            CreatedOrgTextBox.SelectAll();
        }

       
    }
}
