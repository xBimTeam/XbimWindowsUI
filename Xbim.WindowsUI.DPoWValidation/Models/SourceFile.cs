using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xbim.WindowsUI.DPoWValidation.Models
{
    public class SourceFile
    {
        public SourceFile()
        { }

        public string File { get; set; }

        public bool Exists
        {
            get { return System.IO.File.Exists(File); }
        }

        public FileInfo FileInfo
        {
            get
            {
                return  new FileInfo(File);
            }
        }


    }
}
