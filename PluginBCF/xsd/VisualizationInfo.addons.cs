using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Xbim.Presentation;

namespace Xbim.BCF
{
    public partial class ClippingPlane
    {
        public ClippingPlane(Plane3D plane)
        {
            this.Location = new Point(plane.Position);
            this.Direction = new Direction(plane.Normal);
        }
    }

    public partial class VisualizationInfo
    {
        /// <summary>
        /// Initialises the properties of the instance from the control.
        /// </summary>
        /// <param name="control">Initialisation object; reference not retained</param>
        public VisualizationInfo(DrawingControl3D control) : this()
        {
            if (control != null)
            {
                if (control.Viewport.Orthographic)
                {
                    this.orthogonalCameraField = new OrthogonalCamera((System.Windows.Media.Media3D.OrthographicCamera)control.Viewport.Camera);
                }
                else
                {
                    this.perspectiveCameraField = new PerspectiveCamera((System.Windows.Media.Media3D.PerspectiveCamera)control.Viewport.Camera);
                }
                var cg = control.GetCutPlane();
                if (cg != null)
                {
                    this.ClippingPlanes.Add(new ClippingPlane(cg));
                }
            }
        }
    }

    public partial class OrthogonalCamera
    {
        public OrthogonalCamera(System.Windows.Media.Media3D.OrthographicCamera cam)
        {
            this.CameraViewPoint = new Point(cam.Position);
            this.CameraDirection = new Direction(cam.LookDirection);
            this.CameraUpVector  = new Direction(cam.UpDirection);
            this.ViewToWorldScale = cam.Width;
        }
    }

    public partial class PerspectiveCamera
    {
        public PerspectiveCamera(System.Windows.Media.Media3D.PerspectiveCamera cam )
        {
            this.CameraViewPoint = new Point(cam.Position);
            this.CameraDirection = new Direction(cam.LookDirection);
            this.CameraUpVector = new Direction(cam.UpDirection);
            this.fieldOfViewField = cam.FieldOfView;
        }
    }

    public partial class Point
    {
        public Point() { }
        public Point(Point3D Pnt)
        {
            this.xField = Pnt.X;
            this.yField = Pnt.Y;
            this.zField = Pnt.Z;
        }
    }

    public partial class Direction
    {
        public Direction() { }

        public Direction(Vector3D Dir)
        {
            this.xField = Dir.X;
            this.yField = Dir.Y;
            this.zField = Dir.Z;
        }
    }
}
