using PropertyTools.Wpf;
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
using Xbim.Ifc2x3.ActorResource;
using System.Diagnostics;

namespace XbimXplorer.Dialogs
{
    /// <summary>
    /// Interaction logic for AddFederatedModel.xaml
    /// </summary>
    public partial class AddFederatedModel : Window
    {
        public AddFederatedModel()
        {
            InitializeComponent();
        }

        public AddFederatedModel(Xbim.IO.XbimReferencedModel rItem) : this()
        {
            FileSelector.FilePath = rItem.DocumentInformation.Name;

            
            if (rItem.DocumentInformation.DocumentOwner is IfcOrganization)
            {
                var own = rItem.DocumentInformation.DocumentOwner as IfcOrganization;
                RoleSelector.Text = own.RolesString;
                OrganisationTextBox.Text = own.Name;
            }
            
           
            
            RoleSelector.IsReadOnly = true;
            RoleSelector.IsEnabled = false;

            OrganisationTextBox.IsReadOnly = true;
        }

        public string FileName
        {
            get
            {
                return FileSelector.FilePath;
            }
        }
        
        public IfcRole Role
        {
            get
            {
                try
                {
                    return (IfcRole)Enum.Parse(typeof(IfcRole), RoleSelector.Text, true);
                }
                catch (Exception)
                {
                    return IfcRole.UserDefined;
                } 
            }
        }

        public string RoleName
        {
            get
            {
                return RoleSelector.Text;
            }
        }

        public string OrganisationName
        {
            get
            {
                return OrganisationTextBox.Text;
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FileName))
            {
                System.Windows.MessageBox.Show("Please specify a Model File Name");
                FileSelector.Focus();
            }
            else
            {
                DialogResult = true;
                this.Close();
            }
        }
    }
}
