using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using Xbim.IO;

namespace Xbim.Presentation
{
    public class FederatedModelItem : Observable
    {
        XbimReferencedModel refModel;
        public FederatedModelItem(XbimReferencedModel refModel)
        {
            this.refModel = refModel;
        }

        public string Id
        {
            get
            {
                return refModel.DocumentInformation.DocumentId;
            }
        }
        private Color colour;
        public Color Colour
        {
            get
            {
                return colour;
            }
            set { colour = value; RaisePropertyChanged("Colour"); }
        }
        public string Owner
        {
            get
            {
                return refModel.DocumentInformation.DocumentOwner.ToString();
            }
        }
        public string Name
        {
            get
            {
                return refModel.DocumentInformation.Name;
            }
        }
    }
}
