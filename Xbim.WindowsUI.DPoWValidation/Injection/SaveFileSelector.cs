using System.Windows.Forms;

namespace Xbim.WindowsUI.DPoWValidation.Injection
{
    public class SaveFileSelector : ISaveFileSelector
    {
        private SaveFileDialog _dialog;

        public SaveFileSelector()
        {
            _dialog = new SaveFileDialog();
        }

        public string Filter
        {
            set { _dialog.Filter = value; }
        }

        public string Title
        {
            set { _dialog.Title = value; }
        }

        public string FileName
        {
            get { return _dialog.FileName; }
        }

        public DialogResult ShowDialog()
        {
            return _dialog.ShowDialog();
        }

        public string InitialDirectory
        {
            set { _dialog.InitialDirectory = value; }
        }
    }
}
