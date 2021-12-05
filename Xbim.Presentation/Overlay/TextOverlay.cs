using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;
using Xbim.Common.Geometry;

namespace Xbim.Presentation.Overlay
{
	public class TextOverlay
	{
		internal BillboardTextItem GraphicsItem;

		TextOverlayStyle style;

		internal TextOverlayStyle Style { get => style; }

		public TextOverlay(string text, XbimPoint3D position, TextOverlayStyle style)
		{
			this.style = style;
			Position = position;
			GraphicsItem = new BillboardTextItem()
			{
				Text = text,
				DepthOffset = 0.1,
				WorldDepthOffset = 0.0,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};
		}

		public void UpdateText(string text)
		{
			var tlist = style.GraphicsItem.Items.ToList();
			GraphicsItem.Text = text;
			style.GraphicsItem.Items = tlist; // does this trigger an update?
		}

		/// <summary>
		/// Updates the underlying graphics position in the model
		/// </summary>
		/// <param name="modelPositions">required wcs reference</param>
		/// <param name="newPosition">If omitted refreshes the underlying graphics retaining its absolute value</param>
		public void UpdatePosition(XbimModelPositioningCollection modelPositions, XbimPoint3D? newPosition = null)
		{
			if (newPosition.HasValue)
				Position = newPosition.Value;
			var pnt = modelPositions.GetPoint(Position);
			Point3D computedPoint = new Point3D(pnt.X, pnt.Y, pnt.Z);
			GraphicsItem.Position = computedPoint;
		}

		public string Text { get; private set; }
		public XbimPoint3D Position { get; private set; }
		public HorizontalAlignment HorizontalAlignment
		{
			get => GraphicsItem.HorizontalAlignment;
			set => GraphicsItem.HorizontalAlignment = value;
		}

		public VerticalAlignment VerticalAlignment
		{
			get => GraphicsItem.VerticalAlignment;
			set => GraphicsItem.VerticalAlignment = value;
		} 

	}
}
