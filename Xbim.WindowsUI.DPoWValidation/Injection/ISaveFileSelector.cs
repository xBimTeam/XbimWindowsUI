using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Xbim.WindowsUI.DPoWValidation.Injection
{
    public interface ISaveFileSelector
    {
        
        string Filter { set; }
        string Title { set; }
        string FileName { get; }

        DialogResult ShowDialog();

        string InitialDirectory { set; }
    }
}
