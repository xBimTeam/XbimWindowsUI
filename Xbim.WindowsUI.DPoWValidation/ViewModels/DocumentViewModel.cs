using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.CobieLiteUk;

namespace Xbim.WindowsUI.DPoWValidation.ViewModels
{
    class DocumentViewModel
    {
        private Document _document;

        public DocumentViewModel(Document document)
        {
            _document = document;
            DocumentName = _document.Name;
            DocumentDescription = _document.Description;

            if (_document.Attributes == null) 
                return;
            var attCode = _document.Attributes.FirstOrDefault(att => att.Name == "DocumentCode");
            if (attCode != null)
            {
                var asString = attCode.Value as StringAttributeValue;
                if (asString != null)
                    DocumentCode = asString.Value;
            }
        }

        public string DocumentCode { get; private set; }
        public string DocumentName { get; private set; }
        public string DocumentDescription { get; private set; }


    }
}
