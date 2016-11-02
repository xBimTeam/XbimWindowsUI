#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Presentation
// Filename:    XbimMaterialProvider.cs
// Published:   01, 2012
// Last Edited: 9:05 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Media.Media3D;

#endregion

namespace Xbim.Presentation
{
    public class XbimMaterialProvider : INotifyPropertyChanged
    {
        private Material _faceMaterial;
        private Material _backgroundMaterial;

        public bool IsTransparent { get; set; }


        public Binding FaceMaterialBinding { get; private set; }


        public Binding BackgroundMaterialBinding { get; private set; }

        /// <summary>
        ///   Sets face and background Material to material
        /// </summary>
        /// <param name = "material"></param>
        /// <param name="transparent"></param>
        public XbimMaterialProvider(Material material, bool transparent = false)
        {
            FaceMaterial = material;
            BackgroundMaterial = material;
            IsTransparent = transparent;
        }

        public XbimMaterialProvider(Material faceMaterial, Material backgroundMaterial, bool transparent = false)
        {
            FaceMaterial = faceMaterial;
            BackgroundMaterial = backgroundMaterial;
            IsTransparent = transparent;
        }

       

        public Material FaceMaterial
        {
            get { return _faceMaterial; }
            set
            {
                _faceMaterial = value;
                if (FaceMaterialBinding == null)
                {
                    FaceMaterialBinding = new Binding("FaceMaterial");
                    FaceMaterialBinding.Source = this;
                }
                
                var handler = PropertyChanged;
                handler?.Invoke(this, new PropertyChangedEventArgs("FaceMaterial"));
            }
        }


        public Material BackgroundMaterial
        {
            get { return _backgroundMaterial; }
            set
            {
                if (BackgroundMaterialBinding == null)
                {
                    BackgroundMaterialBinding = new Binding("BackgroundMaterial");
                    BackgroundMaterialBinding.Source = this;
                }
                _backgroundMaterial = value;
                var handler = PropertyChanged;
                handler?.Invoke(this, new PropertyChangedEventArgs("BackgroundMaterial"));
            }
        }

        #region INotifyPropertyChanged Members

        [field: NonSerialized] //don't serialize events
            private event PropertyChangedEventHandler PropertyChanged;

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { PropertyChanged += value; }
            remove { PropertyChanged -= value; }
        }

        #endregion
    }
}