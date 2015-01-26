using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;

namespace Validation.mvdXML
{
    public class MvdXMLDocument
    {
        // XPathNavigator _nav;
        internal string fileNameSpace;
        private XmlNamespaceManager _nsmgr;
        XPathDocument _docNav;

        public Dictionary<string, MvdConceptRoot> ConceptRoots = new Dictionary<string, MvdConceptRoot>();
        public Dictionary<string, MvdConcept> Concepts = new Dictionary<string, MvdConcept>();
        public Dictionary<string, MvdExchangeRequirement> ExchangeRequirement = new Dictionary<string, MvdExchangeRequirement>();
        public List<MvdConceptERReference> Refs = new List<MvdConceptERReference>();

        public MvdXMLDocument(string fileName)
        {
            _docNav = new XPathDocument(fileName);
            XPathNavigator _nav = _docNav.CreateNavigator();
            
            _nsmgr = new XmlNamespaceManager(_nav.NameTable);
            _nav.MoveToFirstChild();
            fileNameSpace = _nav.NamespaceURI;
            _nsmgr.AddNamespace("mvd", fileNameSpace);

            foreach (var item in GetConceptRoots())
                ConceptRoots.Add(item.uuid, item);

            foreach (var item in GetConceptExchangeRequirements())
                ExchangeRequirement.Add(item.uuid, item);
        }

        internal XPathNodeIterator GetElements(string strExpression)
        {
            XPathNavigator _nav = _docNav.CreateNavigator();
            var NodeIter = _nav.Select(strExpression, _nsmgr);
            return NodeIter;
        }

        internal List<MvdConceptRoot> GetConceptRoots(string ApplicableClass = "")
        {
            List<MvdConceptRoot> retval = new List<MvdConceptRoot>();

            string strExpression = string.Format(
                "/mvd:mvdXML/mvd:Views/mvd:ModelView/mvd:Roots/mvd:ConceptRoot[@applicableRootEntity='{0}']",
                ApplicableClass
                );
            if (ApplicableClass == "")
                strExpression = "/mvd:mvdXML/mvd:Views/mvd:ModelView/mvd:Roots/mvd:ConceptRoot";
            
            var NodeIter = GetElements(strExpression);
            while (NodeIter.MoveNext())
            {
                MvdConceptRoot cr = new MvdConceptRoot(this, NodeIter.Current);
                retval.Add(cr);
            };
            return retval;
        }

        internal List<MvdExchangeRequirement> GetConceptExchangeRequirements(string uuid = "")
        {
            List<MvdExchangeRequirement> retval = new List<MvdExchangeRequirement>();

            string strExpression = string.Format(
                "/mvd:mvdXML/mvd:Views/mvd:ModelView/mvd:ExchangeRequirements/mvd:ExchangeRequirement[@uuid='{0}']",
                uuid
                );
            if (uuid == "")
                strExpression = "/mvd:mvdXML/mvd:Views/mvd:ModelView/mvd:ExchangeRequirements/mvd:ExchangeRequirement";

            var NodeIter = GetElements(strExpression);
            while (NodeIter.MoveNext())
            {
                var cr = new MvdExchangeRequirement(this, NodeIter.Current);
                retval.Add(cr);
            };
            return retval;
        }

        public Dictionary<string, MvdConceptTemplate> ConceptTemplates = new Dictionary<string, MvdConceptTemplate>();
        
        
        internal bool LoadConceptTemplate(string Template)
        {
            if (ConceptTemplates.ContainsKey(Template))
                return true;

            string strExpression = string.Format(
                "//mvd:ConceptTemplate[@uuid='{0}']",
                Template
                );

            bool ret = false;
            var NodeIter = GetElements(strExpression);
            while (NodeIter.MoveNext())
            {
                MvdConceptTemplate ct = new MvdConceptTemplate(this, NodeIter.Current);
                ConceptTemplates.Add(Template, ct);
                ret = true;
            };
            return ret;
        }

        internal string ReportProps()
        {
            Dictionary<string, List<string>> props = new Dictionary<string, List<string>>();
            StringBuilder sb = new StringBuilder();
            foreach (var tplt in ConceptTemplates)
            {
                foreach (var item in tplt.Value.Rules)
                {
                    item.AnalyseProp(props);
                }
            }
            foreach (var item in props)
            {
                sb.AppendLine("Prop:" + item.Key);
                foreach (var value in props[item.Key])
                {
                    sb.AppendLine(" - " + value);
                }
            }
            return sb.ToString();
        }

        internal void AddConcept(MvdConcept mvdConcept)
        {
            if (this.Concepts.ContainsKey(mvdConcept.uuid))
                return;
            Concepts.Add(mvdConcept.uuid, mvdConcept);
        }
    }
}
