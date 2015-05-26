using System.Windows;
using System.Windows.Media.Media3D;

namespace Xbim.Presentation
{
    public class XbimMaterialModelVisual : ModelVisual3D
    {


        public XbimMaterialProvider MaterialProvider
        {
            get { return (XbimMaterialProvider)GetValue(MaterialProviderProperty); }
            set { SetValue(MaterialProviderProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MaterialProvider.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaterialProviderProperty =
            DependencyProperty.Register("MaterialProvider", typeof(XbimMaterialProvider), typeof(XbimMaterialModelVisual), new PropertyMetadata(null));

        
    }
}
