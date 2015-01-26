using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.XPath;

namespace Validation.mvdXML
{
    public class MvdConceptRoot : MVDNamedIdentifiedItem
    {
        public List<MvdConcept> Concepts;

        public MvdConceptRoot(MvdXMLDocument mvdXMLDocument, XPathNavigator xPathNavigator)
            : base (mvdXMLDocument, xPathNavigator)
        {
            applicableRootEntity = xPathNavigator.GetAttribute("applicableRootEntity", "");
            XPathNavigator childNav = xPathNavigator.Clone();
            childNav.MoveToChild("Concepts", mvdXMLDocument.fileNameSpace);
            var ret = childNav.MoveToChild("Concept", mvdXMLDocument.fileNameSpace);

            Concepts = new List<MvdConcept>();
            while (ret)
            {
                var c = new MvdConcept(mvdXMLDocument, childNav);
                c.ConceptRoot = this;
                Concepts.Add(c);
                ret = childNav.MoveToNext("Concept", mvdXMLDocument.fileNameSpace);
            }
        }

        public string applicableRootEntity { get; set; }


        internal string StringReport()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("ConceptRoot: {0}\r\n", this.Name);
            foreach (var item in Concepts)
            {
                sb.Append(item.StringReport());
            }
            return sb.ToString();
        }
    }
}
