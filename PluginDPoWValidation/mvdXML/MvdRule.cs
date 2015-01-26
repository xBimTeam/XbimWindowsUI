using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.XPath;

namespace Validation.mvdXML
{
    public class MvdRule
    {
        public string Type;
        public List<MvdRule> NestedRules;
        public Dictionary<string, string> Properties;
        public MvdRule Parent;

        public MvdRule(XPathNavigator Navigator, MvdRule parent)
        {
            Parent = parent;
            Type = Navigator.Name;
            var childNav = Navigator.Clone();
            Properties = new Dictionary<string, string>();
            NestedRules = new List<MvdRule>();
            
            // properties
            var ret = childNav.MoveToFirstAttribute();
            while (ret)
            {
                Properties.Add(childNav.Name, childNav.Value);
                ret = childNav.MoveToNextAttribute();
            }

            // rules
            childNav = Navigator.Clone();
            ret = childNav.MoveToFirstChild();
            if (childNav.Name.Contains("Rules"))
            {
                ret = childNav.MoveToFirstChild();
            }
            while (ret)
            {
                NestedRules.Add(new MvdRule(childNav, this));
                ret = childNav.MoveToNext();
            }
        }

        public override string ToString()
        {
            StringBuilder sb =new StringBuilder();
            foreach (var item in Properties.Keys)
	        {
		        if (item.Contains("Name"))
                {
                    sb.Append (Properties[item]);
                }
	        }
            return this.Type + " " + sb.ToString();
        }

        public string StringReport(int nestingLevel)
        {
            StringBuilder sb = new StringBuilder();
            string head = new string('\t', nestingLevel);

            sb.AppendFormat("{0} {1}", head, Type);
            foreach (var item in Properties)
            {
                sb.AppendFormat(" {0}='{1}'", item.Key, item.Value);
            }
            sb.AppendLine();
            foreach (var item in NestedRules)
            {
                sb.Append(item.StringReport(nestingLevel+1));
            }
            return sb.ToString();
        }

        internal void AnalyseProp(Dictionary<string, List<string>> props)
        {
            foreach (var item in this.Properties)
            {
                string k = this.Type + ";" + item.Key;
                if (!props.ContainsKey(k))
                {
                    props.Add(k, new List<string>());
                }
                if (!props[k].Contains(item.Value))
                    props[k].Add(item.Value);
            }
            foreach (var item in this.NestedRules)
            {
                item.AnalyseProp(props);
            }
        }

        internal bool HasParameterValuePair(string p, string parname)
        {
            return Properties.Any(x => x.Key == p && x.Value == parname);
        }

        internal string QueryStack()
        {
            string pqs = "";
            if (Parent != null)
                pqs = Parent.QueryStack();
            if (this.Type == "AttributeRule" && this.Properties.ContainsKey("AttributeName"))
            {
                if (pqs != "")
                    return pqs + " " + this.Properties["AttributeName"];
                else
                    return this.Properties["AttributeName"];
            }
            else
                return pqs;
        }

        internal MvdRule FindParameterValuePair(string p, string parname)
        {
            if (this.HasParameterValuePair(p, parname))
                return this;
            foreach (var item in NestedRules)
            {
                var v = item.FindParameterValuePair(p, parname);
                if (v != null)
                    return v;
            }
            return null;
        }
    }
}
