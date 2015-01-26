using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.XPath;

namespace Validation.mvdXML
{
    public class MVDNamedIdentifiedItem
    {

        public MVDNamedIdentifiedItem(MvdXMLDocument mvdXMLDocument, XPathNavigator xPathNavigator)
        {
            TopXmlDoc = mvdXMLDocument;
            Navigator = xPathNavigator;

            Name = xPathNavigator.GetAttribute("name", "");
            uuid = xPathNavigator.GetAttribute("uuid", "");
        }

        protected MvdXMLDocument TopXmlDoc;
        protected XPathNavigator Navigator;

        public string Name { get; set; }
        public string uuid { get; set; }
    }
}
