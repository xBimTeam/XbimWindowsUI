using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Xbim.Common.Geometry;

namespace Xbim.Presentation.Overlay
{
    public class ImageOverlay
    {
        internal BillboardVisual3D GraphicsItem;
        public enum ModelRelation
        {
            HidesBehindModel,
            AlwaysVisible
        }
        public XbimPoint3D Position { get; private set; }

        public double Width = 40;
        public double Height = 40;

		public ImageOverlay(
            string imagePath,
            XbimPoint3D position,
            double width,
            double height
            )
		{
            Position = position;
            Uri fileUri = new Uri(new Uri("file://"), imagePath);
			GraphicsItem = new BillboardVisual3D()
			{
				Width = width,
				Height = height,
				Material = MaterialHelper.CreateEmissiveImageMaterial(
					fileUri.AbsoluteUri,
					Brushes.Transparent,
					UriKind.Absolute
					)
			};
			//GraphicsItem = new BillboardVisual3D()
			//{
           //             Width = width,
           //             Height = height,
           //             Material = MaterialHelper.CreateImageMaterial(
			//		fileUri.AbsoluteUri,
			//		1,
			//		UriKind.Absolute
			//		)
			//};

		}

        public void UpdatePosition(XbimModelPositioningCollection modelPositions, XbimPoint3D? newPosition = null)
        {
            if (newPosition.HasValue)
                Position = newPosition.Value;
            Point3D computedPoint = modelPositions.GetPoint(Position);
            GraphicsItem.Position = computedPoint;
        }
    }
}
