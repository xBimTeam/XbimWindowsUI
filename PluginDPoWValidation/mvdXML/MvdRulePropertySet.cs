using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Validation.mvdXML
{
    public class MvdRulePropertySet
    {
        private string _s = "";
        public MvdRulePropertySet(string initString)
        {
            _s = initString;
            var ou = initString.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in ou)
            {
                var one = item.Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
                if (one.Length == 2)
                {
                    Properties.Add(one[0], one[1]);
                }
            }
        }

        private Dictionary<string, string> Properties = new Dictionary<string, string>();

        internal string StringReport()
        {
            return "\t" + _s + "\r\n"; 
        }

        internal IEnumerable<string> ParameterNames()
        {
            List<MvdPropertyRuleValue> vals = MvdPropertyRuleValue.GetValues(_s);
            return vals.Select(x => x.Name);
        }

        internal IEnumerable<MvdPropertyRuleValue> ParValues()
        {
            List<MvdPropertyRuleValue> vals = MvdPropertyRuleValue.GetValues(_s);
            return vals;
        }
    }
}
