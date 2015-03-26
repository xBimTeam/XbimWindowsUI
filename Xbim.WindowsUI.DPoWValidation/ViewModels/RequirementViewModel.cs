using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Xbim.WindowsUI.DPoWValidation.ViewModels
{
    public class RequirementViewModel
    {
        private COBieLiteUK.Attribute _attribute;

        public RequirementViewModel(COBieLiteUK.Attribute attribute)
        {     
            this._attribute = attribute;
        }
       
        public string Name
        {
            get { return _attribute.Name; }
        }

        public Brush CircleBrush
        {
            get
            {
                if (_attribute.Categories == null)
                    return Brushes.Transparent;
                var cat = _attribute.Categories.FirstOrDefault(x => x.Classification == @"DPoW");
                if (cat == null)
                    return Brushes.Transparent;

                switch (cat.Code)
                {
                    case @"Passed":
                        return Brushes.Green;
                    case @"Failed":
                        return Brushes.Red;
                    default:
                        return Brushes.Blue;
                }
            }
        }
    }
}
