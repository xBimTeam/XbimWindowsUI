using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.XPath;

namespace Validation.mvdXML
{
    public class MvdExchangeRequirement : MVDNamedIdentifiedItem
    {

        public MvdExchangeRequirement(MvdXMLDocument mvdXMLDocument, XPathNavigator xPathNavigator)
            : base (mvdXMLDocument, xPathNavigator)
        {
            Applicability = xPathNavigator.GetAttribute("Applicability", "");
            
            XPathNavigator childNav = xPathNavigator.Clone();
            var ret = childNav.MoveToChild("Definitions", mvdXMLDocument.fileNameSpace);
            ret = childNav.MoveToChild("Definition", mvdXMLDocument.fileNameSpace);
            ret = childNav.MoveToChild("Body", mvdXMLDocument.fileNameSpace);
            Body = childNav.Value;
        }

        public string Applicability { get; set; }
        public string Body { get; set; }

    }
}
