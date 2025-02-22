using System;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using Microsoft.Win32;
using Xbim.Ifc;
using Xbim.Common;
using Xbim.Presentation;
using Xbim.Common.Geometry;
using Xbim.ModelGeometry.Scene;
using Xbim.Presentation.LayerStyling;
using Xbim.IO;

namespace ITI
{
	public partial class MainWindow : Window
	{
		private ObjectDataProvider _modelProvider;

		public MainWindow()
		{
			InitializeComponent();

			// Set up the ObjectDataProvider for model binding
			_modelProvider = new ObjectDataProvider();
			this.DataContext = _modelProvider;

			// Set the default layer styler
			DrawingControl.DefaultLayerStyler = new SurfaceLayerStyler();
		}

		private void LoadIfc_Click(object sender, RoutedEventArgs e)
		{
			// Ask the user if they want to load the test file
			MessageBoxResult result = MessageBox.Show(
				"Do you want to load the test file? (Click 'Yes' for test file, 'No' to choose a custom file)",
				"Load IFC File",
				MessageBoxButton.YesNoCancel,
				MessageBoxImage.Question
			);

			string ifcFilePath;

			if (result == MessageBoxResult.Yes)
			{
				// Load the test file
				ifcFilePath = @"C:\AEC\17-Open Bim\ITIXBIM\ITI\ITI_Ahmed_Ahmed_STR.ifc";
			}
			else if (result == MessageBoxResult.No)
			{
				// Let the user choose a custom file
				OpenFileDialog openFileDialog = new OpenFileDialog
				{
					Filter = "IFC Files (*.ifc)|*.ifc|All Files (*.*)|*.*",
					Title = "Select an IFC File",
					InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
				};

				bool? dialogResult = openFileDialog.ShowDialog();

				if (dialogResult == true)
				{
					ifcFilePath = openFileDialog.FileName;
				}
				else
				{
					MessageBox.Show("No file selected.");
					return;
				}
			}
			else
			{
				// User clicked Cancel
				MessageBox.Show("Operation canceled.");
				return;
			}

			// Load the IFC file
			try
			{
				// Open the IFC model with read-write access
				using (var model = IfcStore.Open(ifcFilePath, null, null, null, XbimDBAccess.ReadWrite))
				{
					// Verify entities exist
					if (model.Instances.Count == 0)
					{
						MessageBox.Show("The IFC file is empty or contains no renderable data.");
						return;
					}

					// Check and generate geometry if needed
					if (model.GeometryStore.IsEmpty)
					{
						var context = new Xbim3DModelContext(model);
						context.CreateContext();
						MessageBox.Show("Geometry generated for the model.");
					}
					else
					{
						MessageBox.Show("Geometry store found.");
					}

					// Set the model to the ObjectDataProvider
					_modelProvider.ObjectInstance = model;
					_modelProvider.Refresh();

					// Verify binding
					MessageBox.Show($"Model bound: {DrawingControl.Model != null}");
					DrawingControl.ReloadModel(); // Trigger rendering

					// Adjust the camera
					DrawingControl.Viewport.ZoomExtents();
					DrawingControl.InvalidateVisual();

					// Confirm load
					MessageBox.Show($"Loaded IFC file: {System.IO.Path.GetFileName(ifcFilePath)} with {model.Instances.Count} entities.");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error loading IFC file: {ex.Message}");
			}
		}

		private void DrawingControl_Loaded(object sender, RoutedEventArgs e)
		{
			MessageBox.Show("DrawingControl3D is loaded and ready.");
		}
	}
}