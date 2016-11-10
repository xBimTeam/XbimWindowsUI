using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xbim.BCF
{
    public partial class Topic
    {
        public Topic()
        {
            Guid = System.Guid.NewGuid().ToString();
        }
    }
}
