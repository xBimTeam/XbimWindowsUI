// the code on this page has been cloned from the Helix toolkit to fix a bug in the MeshHelper class.
// The class will be removed (or wrap the helix toolkit one) if they accept changes to their codebase.
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CuttingPlaneGroup.cs" company="Helix 3D Toolkit">
//   http://helixtoolkit.codeplex.com, license: MIT
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Xbim.Presentation
{
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Media3D;
    using HelixToolkit.Wpf;
    using Xbim.ModelGeometry.Scene;

    /// <summary>
    /// A visual element that applies cutting planes to all children.
    /// </summary>
    public class XbimCuttingPlaneGroup : RenderingModelVisual3D
    {
        /// <summary>
        /// Identifies the <see cref="IsEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.Register(
            "IsEnabled", typeof(bool), typeof(XbimCuttingPlaneGroup), new UIPropertyMetadata(false, IsEnabledChanged));

        /// <summary>
        /// The cut geometries.
        /// </summary>
        private Dictionary<Model3D, Geometry3D> CutGeometries = new Dictionary<Model3D, Geometry3D>();

        /// <summary>
        /// The cut geometries being processed.
        /// </summary>
        private Dictionary<Model3D, Geometry3D> TempCutGeometries;

        /// <summary>
        /// The original geometries being processed.
        /// </summary>
        private Dictionary<Model3D, Geometry3D> TempOriginalGeometries;

        /// <summary>
        /// The original geometries.
        /// </summary>
        private Dictionary<Model3D, Geometry3D> OriginalGeometries = new Dictionary<Model3D, Geometry3D>();

        /// <summary>
        /// The force update.
        /// </summary>
        private bool forceUpdate;

        /// <summary>
        /// Initializes a new instance of the <see cref = "CuttingPlaneGroup" /> class.
        /// </summary>
        public XbimCuttingPlaneGroup()
        {
            this.IsEnabled = true;
            this.CuttingPlanes = new List<Plane3D>();
        }

        /// <summary>
        /// Gets or sets the cutting planes.
        /// </summary>
        /// <value>The cutting planes.</value>
        public List<Plane3D> CuttingPlanes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether cutting is enabled.
        /// </summary>
        public bool IsEnabled
        {
            get
            {
                return (bool)this.GetValue(IsEnabledProperty);
            }

            set
            {
                this.SetValue(IsEnabledProperty, value);
            }
        }

        /// <summary>
        /// The is sorting changed.
        /// </summary>
        /// <param name="d">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The event arguments.
        /// </param>
        private static void IsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((XbimCuttingPlaneGroup)d).OnIsEnabledChanged();
        }

        /// <summary>
        /// Applies the cutting planes.
        /// </summary>
        /// <param name="forceUpdate">
        /// if set to <c>true</c> [force update].
        /// </param>
        private void ApplyCuttingPlanes(bool forceUpdate = false)
        {
            lock (this)
            {
                this.TempCutGeometries = new Dictionary<Model3D, Geometry3D>();
                this.TempOriginalGeometries = new Dictionary<Model3D, Geometry3D>();
                this.forceUpdate = forceUpdate;
                Visual3DHelper.Traverse<GeometryModel3D>(this.Children, this.ApplyCuttingPlanesToModel);
                this.CutGeometries = this.TempCutGeometries;
                this.OriginalGeometries = this.TempOriginalGeometries;
            }
        }




        /// <summary>
        /// Applies the cutting planes to the model.
        /// </summary>
        /// <param name="model">
        /// The model to be modified (it is also the key to be searched for in the dictionaries).
        /// </param>
        /// <param name="transform">
        /// The transform.
        /// </param>
        private void ApplyCuttingPlanesToModel(GeometryModel3D model, Transform3D transform)
        {
            

            if (model.Geometry == null)
            {
                return;
            }

            bool updateRequired = this.forceUpdate;

            if (!this.IsEnabled)
            {
                updateRequired = true;
            }

            Geometry3D cutGeometry;
            if (this.CutGeometries.TryGetValue(model, out cutGeometry))
            {
                if (cutGeometry != model.Geometry)
                {
                    updateRequired = true;
                }
            }

            Geometry3D originalGeometry;
            if (!this.OriginalGeometries.TryGetValue(model, out originalGeometry))
            {
                originalGeometry = model.Geometry;
                updateRequired = true;
            }

            this.TempOriginalGeometries.Add(model, originalGeometry);

            if (!updateRequired)
            {
                return;
            }

            var g = originalGeometry as MeshGeometry3D;

            if (this.IsEnabled)
            {
                XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial> layer = model.GetValue(FrameworkElement.TagProperty) as XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial>;
                // todo: bonghi: in the click mechanism the default is to use
                // var frag = layer.Visible.Meshes.Find(hit.VertexIndex1);
                // but there are cases when the new indices created below are not successfully returned from that query
                // data must be added in this stage somehow to add ways to identify the fragment.
                //

                var inverseTransform = transform.Inverse;
                foreach (var cp in this.CuttingPlanes)
                {
                    var p = inverseTransform.Transform(cp.Position);
                    var p2 = inverseTransform.Transform(cp.Position + cp.Normal);
                    var n = p2 - p;

                    // var p = transform.Transform(cp.Position);
                    // var n = transform.Transform(cp.Normal);
                    g = XbimMeshHelper.Cut(g, p, n);
                }
            }
            
            model.Geometry = g;
            this.TempCutGeometries.Add(model, g);
        }

        /// <summary>
        /// The compositiontarget rendering.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The event arguments.
        /// </param>
        protected override void OnCompositionTargetRendering(object sender, RenderingEventArgs e)
        {
            if (this.IsEnabled)
            {
                this.ApplyCuttingPlanes();
            }
        }

        /// <summary>
        /// Called when IsEnabled is changed.
        /// </summary>
        private void OnIsEnabledChanged()
        {
            if (this.IsEnabled)
            {
                this.SubscribeToRenderingEvent();
            }
            else
            {
                this.UnsubscribeRenderingEvent();
            }

            this.ApplyCuttingPlanes(true);
        }
    }
}