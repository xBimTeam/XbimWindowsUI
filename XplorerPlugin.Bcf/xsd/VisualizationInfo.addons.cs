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
            Location = new Point(plane.Position);
            Direction = new Direction(plane.Normal);
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
            if (control == null)
                return;
            if (control.Viewport.Orthographic)
            {
                orthogonalCameraField = new OrthogonalCamera((System.Windows.Media.Media3D.OrthographicCamera)control.Viewport.Camera);
            }
            else
            {
                perspectiveCameraField = new PerspectiveCamera((System.Windows.Media.Media3D.PerspectiveCamera)control.Viewport.Camera);
            }
            var cg = control.GetCutPlane();
            if (cg != null)
            {
                ClippingPlanes.Add(new ClippingPlane(cg));
            }
        }
    }

    public partial class OrthogonalCamera
    {
        public OrthogonalCamera(System.Windows.Media.Media3D.OrthographicCamera cam)
        {
            CameraViewPoint = new Point(cam.Position);
            CameraDirection = new Direction(cam.LookDirection);
            CameraUpVector = new Direction(cam.UpDirection);
            ViewToWorldScale = cam.Width;
        }
    }

    public partial class PerspectiveCamera
    {
        public PerspectiveCamera(System.Windows.Media.Media3D.PerspectiveCamera cam )
        {
            CameraViewPoint = new Point(cam.Position);
            CameraDirection = new Direction(cam.LookDirection);
            CameraUpVector = new Direction(cam.UpDirection);
            fieldOfViewField = cam.FieldOfView;
        }
    }

    public partial class Point
    {
        public Point() { }
        public Point(Point3D pnt)
        {
            xField = pnt.X;
            yField = pnt.Y;
            zField = pnt.Z;
        }
    }

    public partial class Direction
    {
        public Direction() { }

        public Direction(Vector3D dir)
        {
            xField = dir.X;
            yField = dir.Y;
            zField = dir.Z;
        }
    }
}
