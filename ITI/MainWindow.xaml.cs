using HelixToolkit.Wpf;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Xbim.Ifc;
using Xbim.Ifc2x3;
using Xbim.ModelGeometry.Scene;

namespace ITI
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		private void LoadIfc_Click(object sender, RoutedEventArgs e)
		{
			string filePath = "path_to_your_ifc_file.ifc"; // Replace with your IFC file path

			using (var model = IfcStore.Open(filePath))
			{
				var context = new Xbim3DModelContext(model);
				context.CreateContext();  // Generate geometry

				Model3DGroup modelGroup = new Model3DGroup();

				foreach (var shapeInstance in context.ShapeInstances())
				{
					var geometry = shapeInstance.ShapeGeometry;
					if (geometry == null) continue;

					MeshGeometry3D mesh = new MeshGeometry3D
					{
						Positions = new Point3DCollection(geometry.Vertices.Select(v => new Point3D(v.X, v.Y, v.Z))),
						TriangleIndices = new Int32Collection(geometry.TriangleIndices)
					};

					var material = new DiffuseMaterial(new SolidColorBrush(Colors.LightGray));
					modelGroup.Children.Add(new GeometryModel3D(mesh, material));
				}

				viewport.Children.Clear();
				viewport.Children.Add(new ModelVisual3D { Content = modelGroup });
			}
		}
	}
}
