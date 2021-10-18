using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Xbim.Common.Geometry;

namespace Xbim.Presentation.Overlay
{
	public class TextOverlayStyle
	{
		internal BillboardTextGroupVisual3D GraphicsItem;

		public TextOverlayStyle()
		{
			GraphicsItem = new BillboardTextGroupVisual3D()
			{
				Background = Brushes.White,
				BorderBrush = Brushes.Black,
				Foreground = Brushes.Black,
				BorderThickness = new Thickness(1),
				FontSize = 12,
				Padding = new Thickness(2),
				Offset = new Vector(+20, -20), // 2D offset from the reference point fo the billboard item
				PinBrush = Brushes.Gray,
				IsEnabled = true
			};
		}

		public Brush Background { get => GraphicsItem.Background; set => GraphicsItem.Background = value; }
		public Brush Border { get => GraphicsItem.BorderBrush; set => GraphicsItem.BorderBrush = value; }
		public Brush Foreground { get => GraphicsItem.Foreground; set => GraphicsItem.Foreground = value; }
		public Brush PinBrush { get => GraphicsItem.PinBrush; set => GraphicsItem.PinBrush = value; }
		public Thickness BorderThickness { get => GraphicsItem.BorderThickness; set => GraphicsItem.BorderThickness = value; }
		public double FontSize { get => GraphicsItem.FontSize; set => GraphicsItem.FontSize = value; }
		public Thickness Padding { get => GraphicsItem.Padding; set => GraphicsItem.Padding = value; }
		public Vector Offset { get => GraphicsItem.Offset; set => GraphicsItem.Offset = value; }

		public TextOverlay CreateText(string content, XbimPoint3D xbimPoint3D)
		{
			TextOverlay ret = new TextOverlay(content, xbimPoint3D, this);
			return ret;
		}
	}
}
