using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Xbim.Presentation.Overlay
{
	public class TextOveralyStyle
	{
		Brush Background { get; set; } = Brushes.White;
		Brush Border { get; set; } = Brushes.Black;
		Brush Foreground { get; set; } = Brushes.Black;

		Thickness BorderThickness { get; set; } = new Thickness(1);
		double FontSize { get; set; } = 12;

		Thickness Padding = new Thickness(2);

		Vector Offset { get; set; } = new Vector(0, -20);


	}
}
