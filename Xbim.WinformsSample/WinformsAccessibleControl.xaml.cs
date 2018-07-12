using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.IO;

namespace Xbim.WinformsSample
{
    /// <summary>
    /// Interaction logic for XplorerUserControl.xaml
    /// </summary>
    public partial class WinformsAccessibleControl
    {
        public static readonly DependencyProperty ModelProviderProperty = DependencyProperty.Register("ModelProvider", typeof(ObjectDataProvider), typeof(WinformsAccessibleControl));

        #region DataContext

        public ObjectDataProvider ModelProvider
        {
            get { return (ObjectDataProvider)GetValue(ModelProviderProperty); }
            private set { SetValue(ModelProviderProperty, value); }
        }

        #endregion

        public IfcStore Model
        {
            get { return ModelProvider.ObjectInstance as IfcStore; }
        }

        public IPersistEntity SelectedElement
        {
            get
            {
                return DrawingControl.SelectedEntity;
            }
            set
            {
                DrawingControl.SelectedEntity = value;
            }
        }

        public delegate void SelectionChangedHandler(object sender, System.Windows.Controls.SelectionChangedEventArgs e);

        public event SelectionChangedEventHandler SelectionChanged;

        public WinformsAccessibleControl()
        {
            InitializeComponent();
            ModelProvider = new ObjectDataProvider { IsInitialLoadEnabled = false };
        }

        private void DrawingControl_SelectedEntityChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            SelectionChanged?.Invoke(this, e);
        }
    }
}
