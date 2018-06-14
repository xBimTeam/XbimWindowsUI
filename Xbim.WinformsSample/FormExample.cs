using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Linq;
using Xbim.Ifc;
using Xbim.Common;
using Xbim.Ifc4.Interfaces;

namespace Xbim.WinformsSample
{
    public partial class FormExample : Form
    {
        private WinformsAccessibleControl _wpfControl;

        int starting = -1;

        public FormExample()
        {
            InitializeComponent();
            _wpfControl = new WinformsAccessibleControl();
            _wpfControl.SelectionChanged += _wpfControl_SelectionChanged;
            
            controlHost.Child = _wpfControl;
        }

        private void _wpfControl_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var ent = e.AddedItems[0] as IPersistEntity;
            if (ent == null)
                txtEntityLabel.Text = "";
            else
                txtEntityLabel.Text = ent.EntityLabel.ToString();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "Xbim Files|*.xbim";
            dlg.FileOk += (s, args) =>
            {
                LoadXbimFile(dlg.FileName);
            };
            dlg.ShowDialog(this);
        }

        private void LoadXbimFile(string dlgFileName)
        {
            Clear();

            var model = IfcStore.Open(dlgFileName);
            _wpfControl.ModelProvider.ObjectInstance = model;
        }

        public void Clear()
        {
            var currentIfcStore = _wpfControl.ModelProvider.ObjectInstance as IfcStore;
            currentIfcStore?.Dispose();
            _wpfControl.ModelProvider.ObjectInstance = null;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var mod = _wpfControl.ModelProvider.ObjectInstance as IfcStore;
            if (mod == null)
                return;
            var found = mod.Instances.OfType<IIfcProduct>().FirstOrDefault(x => x.EntityLabel > starting);
            _wpfControl.SelectedElement = found;
            if (found != null)
                starting = found.EntityLabel;
            else
                starting = -1;
        }
    }
}
