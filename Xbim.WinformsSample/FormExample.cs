using log4net;
using System;
using System.Linq;
using System.Windows.Forms;
using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.ModelGeometry.Scene;

namespace Xbim.WinformsSample
{
    public partial class FormExample : Form
    {
        private WinformsAccessibleControl _wpfControl;

        private static readonly ILog Log = LogManager.GetLogger(typeof(FormExample));

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
            dlg.Filter = "IFC Files|*.ifc;*.ifczip;*.ifcxml|Xbim Files|*.xbim";
            dlg.FileOk += (s, args) =>
            {
                LoadXbimFile(dlg.FileName);
            };
            dlg.ShowDialog(this);
        }

        private void LoadXbimFile(string dlgFileName)
        {
            // TODO: should do the load on a worker thread so as not to lock the UI. 
            // See how we use BackgroundWorker in XbimXplorer

            Clear();

            var model = IfcStore.Open(dlgFileName);
            if (model.GeometryStore.IsEmpty)
            {
                // Create the geometry using the XBIM GeometryEngine
                try
                {
                    var context = new Xbim3DModelContext(model);

                    context.CreateContext();

                    // TODO: SaveAs(xbimFile); // so we don't re-process every time
                }
                catch (Exception geomEx)
                {
                    Log.Error($"Failed to create geometry for ${dlgFileName}", geomEx);
                }
            }
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
