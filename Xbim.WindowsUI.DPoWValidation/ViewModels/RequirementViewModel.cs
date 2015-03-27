using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Xbim.WindowsUI.DPoWValidation.ViewModels
{
    public class RequirementViewModel
    {
        private COBieLiteUK.Attribute _attribute;

        public RequirementViewModel(COBieLiteUK.Attribute attribute)
        {
            this._attribute = attribute;
            EvaluateType();
        }

        private void EvaluateType()
        {
            if (_attribute.Categories == null)
            {
                CircleBrush = Brushes.Transparent;
                Type = @"";
                return;
            }
            var cat = _attribute.Categories.FirstOrDefault(x => x.Classification == @"DPoW");
            if (cat != null)
            {
                Type = cat.Code;
                switch (cat.Code)
                {
                    case @"Passed":
                        CircleBrush = Brushes.Green;
                        return;
                    case @"Failed":
                        CircleBrush = Brushes.Red;
                        return;
                    case @"required":
                        CircleBrush = Brushes.Blue;
                        return;
                    default:
                        CircleBrush = Brushes.Transparent;
                        return;
                }
            }
            cat = _attribute.Categories.FirstOrDefault(x => x.Classification == @"DPoW Status");
            if (cat != null)
            {
                Type = cat.Code;
                switch (cat.Code)
                {
                    case @"Submitted":
                        CircleBrush = Brushes.Orange;
                        return;
                    case @"Approved":
                        CircleBrush = Brushes.Green;
                        return;
                    case @"As built":
                        CircleBrush = Brushes.DarkGreen;
                        return;
                    default:
                        CircleBrush = Brushes.Transparent;
                        return;
                }
            }
            CircleBrush = Brushes.Transparent;
        }

        public string Type { get; private set; }

        public string Name
        {
            get { return _attribute.Name; }
        }

        public Visibility CircleVisibility
        {
            get
            {
                return CircleBrush.Equals(Brushes.Transparent)
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            }
        }

        public Brush CircleBrush { get; private set; }
    }
}
