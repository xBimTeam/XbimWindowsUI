using System.ComponentModel;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using Xbim.Presentation.Extensions.Utility;

namespace Xbim.Presentation.Extensions
{
    public class ObservableMeshVisual3D : MeshVisual3D, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public new Mesh3D Mesh
        {
            get
            {
                return base.Mesh;
            }
            set
            {
                base.Mesh = value;
                OnPropertyChanged(this.GetPropertyName(o => o.Mesh));
            }
        }

        public new Model3D Content
        {
            get
            {
                return base.Content;
            }
            set
            {
                base.Content = value;
                OnPropertyChanged(this.GetPropertyName(o => o.Content));
            }
        }

        public new Material FaceMaterial
        {
            get
            {
                return base.FaceMaterial;
            }
            set
            {
                base.FaceMaterial = value;
                OnPropertyChanged(this.GetPropertyName(o => o.FaceMaterial));
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
