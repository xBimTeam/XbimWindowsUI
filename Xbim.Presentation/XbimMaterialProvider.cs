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
        private bool _isTransparent;

        public bool IsTransparent
        {
            get { return _isTransparent; }
            set { _isTransparent = value; }
        }
        Binding _faceMaterialBinding;
        Binding _backgroundMaterialBinding;


        public Binding FaceMaterialBinding
        {
            get { return _faceMaterialBinding; }
           
        }
        

        public Binding BackgroundMaterialBinding
        {
            get { return _backgroundMaterialBinding; }
            
        }

        /// <summary>
        ///   Sets face and background Material to material
        /// </summary>
        /// <param name = "material"></param>
        /// <param name="transparent"></param>
        public XbimMaterialProvider(Material material, bool transparent = false)
        {
            FaceMaterial = material;
            BackgroundMaterial = material;
            _isTransparent = transparent;
        }

        public XbimMaterialProvider(Material faceMaterial, Material backgroundMaterial, bool transparent = false)
        {
            FaceMaterial = faceMaterial;
            BackgroundMaterial = backgroundMaterial;
            _isTransparent = transparent;
        }

       

        public Material FaceMaterial
        {
            get { return _faceMaterial; }
            set
            {
                _faceMaterial = value;
                if (_faceMaterialBinding == null)
                {
                    _faceMaterialBinding = new Binding("FaceMaterial");
                    _faceMaterialBinding.Source = this;
                }
                
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null)
                {
                    handler(this, new PropertyChangedEventArgs("FaceMaterial"));
                }
            }
        }


        public Material BackgroundMaterial
        {
            get { return _backgroundMaterial; }
            set
            {
                if (_backgroundMaterialBinding == null)
                {
                    _backgroundMaterialBinding = new Binding("BackgroundMaterial");
                    _backgroundMaterialBinding.Source = this;
                }
                _backgroundMaterial = value;
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null)
                {
                    handler(this, new PropertyChangedEventArgs("BackgroundMaterial"));
                }
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