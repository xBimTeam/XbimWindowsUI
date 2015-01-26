using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Validation.mvdXML
{
    public class MvdConceptERReference
    {
        public MvdConceptERReference(MvdXMLDocument mvdXMLDocument, System.Xml.XPath.XPathNavigator childNav, string conceptUUID)
        {
            Concept = conceptUUID;
            applicability = childNav.GetAttribute("applicability", "");
            requirement = childNav.GetAttribute("requirement", "");
            exchangeRequirement = childNav.GetAttribute("exchangeRequirement", "");
            mvdXMLDocument.Refs.Add(this);
        }

        public string applicability { get; set; }
        public string requirement { get; set; }
        public string Concept { get; set; }
        public string exchangeRequirement { get; set; }
    }
}
