using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Xbim.Common.Metadata;
using Xbim.Ifc;
using Xbim.Presentation;

namespace XbimXplorer.Dialogs.ExcludedTypes
{
    /// <summary>
    /// Interaction logic for SurfaceLayerStylerConfiguration.xaml
    /// </summary>
    public partial class SurfaceLayerStylerConfiguration : Window
    {
        public IfcStore Model { get; set; }

        private SurfaceLayerStylerConfiguration()
        {
            InitializeComponent();
            MustUpdate = false;
        }

        public SurfaceLayerStylerConfiguration(IfcStore model) : this()
        {
            Model = model;
            PopulateTree();
        }

        public bool MustUpdate { get; private set; }
        
        private void PopulateTree()
        {
            TypesTree.Items.Clear();

			foreach (var item in Infrastructure.SchemaMetadatas)
			{
				var schemaVersion = item.Key;
				var meta = item.Value;

				// this is done through the metadata in order to ensure that class relationships are loaded
				var product4 = meta.ExpressType("IFCPRODUCT");
				TypesTree.Items.Add(new ObjectViewModel() { Header = $"{schemaVersion}.IfcProduct", Tag = new ExpressTypeExpander(product4, Model), IsChecked = true });
			}
        }

        public List<Type> ExcludedTypes
        {
            get
            {
                var ret = new List<Type>();
                foreach (var item in TypesTree.Items.OfType<ObjectViewModel>())
                {
                    EvaluateExclusion(item, ret);
                }
                return ret;
            }
        }

        private static void EvaluateExclusion(ObjectViewModel item, List<Type> ret)
        {
            if (item.IsChecked.HasValue && item.IsChecked.Value == false)
            {
                ret.Add(((ExpressTypeExpander) item.Tag).ExpressType.Type);
            }
            else
            {
                foreach (var child in item.Children)
                {
                    EvaluateExclusion(child, ret);    
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MustUpdate = true;
            Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public void InitialiseSettings(List<Type> excludedTypes)
        {
            foreach (var item in TypesTree.Items.OfType<ObjectViewModel>())
            {
                item.InitialiseSettings(excludedTypes);
            }            
        }

        private void SetDefaults(object sender, RoutedEventArgs e)
        {
            PopulateTree();
            InitialiseSettings(DrawingControl3D.DefaultExcludedTypes);
        }
    }
}
