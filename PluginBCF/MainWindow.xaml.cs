using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Xbim.Common.Geometry;
using Xbim.Presentation;
using Xbim.XbimExtensions.Interfaces;
using XbimXplorer;
using XbimXplorer.PluginSystem;

namespace Xbim.BCF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : UserControl, xBimXplorerPluginWindow, IxBimXplorerPluginWindowMessaging
    {
        string baseFolder = @"..\..\Examples\BuildingSmart\fdb92063-a353-4882-a4a9-b333fe0b2985\";

        public MainWindow()
        {
            InitializeComponent();
            this.CommandBindings.Add(new CommandBinding(BCFFileCommands.Load, ExecuteLoad, CanExecuteLoad));
            this.CommandBindings.Add(new CommandBinding(BCFFileCommands.Save, ExecuteSave, CanExecuteSave));

            this.CommandBindings.Add(new CommandBinding(BCFInstanceCommands.GotoCameraPosition, ExecuteCamera, CanExecuteLoad));
        }

        private void ExecuteSave(object sender, ExecutedRoutedEventArgs e)
        {
            SaveFileDialog ofd = new SaveFileDialog();
            ofd.Filter = @"BIM Collaboration Format|*.bcfzip";
            var v = ofd.ShowDialog();
            if (v.HasValue && v.Value == true)
            {
                selFile.Save(ofd.FileName);
            }

        }

        private void CanExecuteSave(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = selFile.CanSave;
        }

        public void ExecuteLoad(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = @"BIM Collaboration Format|*.bcfzip";
            var v = ofd.ShowDialog();
            if (v.HasValue && v.Value == true)
            {
                selFile.Load(ofd.FileName);
            }
        }

        public void ExecuteCamera(object sender, ExecutedRoutedEventArgs e)
        {
            if (selFile.t == null)
                return;
            BCFInstance inst = selFile.t.SelectedItem as BCFInstance;
            if (inst == null)
                return;
            var v = inst.VisualizationInfo;
            if (v == null)
                return;

            XbimPoint3D position = new XbimPoint3D();
            XbimPoint3D direction = new XbimPoint3D();
            XbimPoint3D upDirection = new XbimPoint3D();

            XbimVector3D directionV = new XbimVector3D();
            XbimVector3D upDirectionV = new XbimVector3D();

            if (v.PerspectiveCamera != null)
            {
                var pc = v.PerspectiveCamera;
                position = new XbimPoint3D(pc.CameraViewPoint.X, pc.CameraViewPoint.Y, pc.CameraViewPoint.Z);
                direction = new XbimPoint3D(pc.CameraDirection.X, pc.CameraDirection.Y, pc.CameraDirection.Z);
                upDirection = new XbimPoint3D(pc.CameraUpVector.X, pc.CameraUpVector.Y, pc.CameraUpVector.Z);

                xpWindow.DrawingControl.Viewport.Orthographic = false;
                var pCam = xpWindow.DrawingControl.Viewport.Camera as System.Windows.Media.Media3D.PerspectiveCamera;
                if (pCam != null)
                    pCam.FieldOfView = pc.FieldOfView;
            }
            else if (v.OrthogonalCamera != null)
            {
                var pc = v.OrthogonalCamera;
                xpWindow.DrawingControl.Viewport.Orthographic = true;
                position = new XbimPoint3D(pc.CameraViewPoint.X, pc.CameraViewPoint.Y, pc.CameraViewPoint.Z);
                direction = new XbimPoint3D(pc.CameraDirection.X, pc.CameraDirection.Y, pc.CameraDirection.Z);
                upDirection = new XbimPoint3D(pc.CameraUpVector.X, pc.CameraUpVector.Y, pc.CameraUpVector.Z);

                var pCam = xpWindow.DrawingControl.Viewport.Camera as System.Windows.Media.Media3D.OrthographicCamera;
                if (pCam != null)
                    pCam.Width = pc.ViewToWorldScale;

            }
            if (false)
            {
                XbimMatrix3D mcp = XbimMatrix3D.Copy(xpWindow.DrawingControl.wcsTransform);
                position = mcp.Transform(position);
                var zero = mcp.Transform(new XbimPoint3D(0, 0, 0));
                directionV = mcp.Transform(direction) - zero;
                directionV.Normalize();
                upDirectionV = mcp.Transform(upDirection) - zero;
                upDirectionV.Normalize();
            }
            else
            {
                directionV = new XbimVector3D(direction.X, direction.Y, direction.Z);
                upDirectionV = new XbimVector3D(upDirection.X, upDirection.Y, upDirection.Z);
            }

            Point3D Pos = new Point3D(position.X, position.Y, position.Z);
            Vector3D Dir = new Vector3D(directionV.X, directionV.Y, directionV.Z);
            Vector3D UpDir = new Vector3D(upDirectionV.X, upDirectionV.Y, upDirectionV.Z);
            xpWindow.DrawingControl.Viewport.SetView(Pos, Dir, UpDir, 500);

            if (v.ClippingPlanes.Any())
            {
                var curP = v.ClippingPlanes[0];
                xpWindow.DrawingControl.SetCutPlane(
                    curP.Location.X, curP.Location.Y, curP.Location.Z,
                    curP.Direction.X, curP.Direction.Y, curP.Direction.Z
                    );
            }
            else
            {
                xpWindow.DrawingControl.ClearCutPlane();
            }

            // xpWindow.DrawingControl.Viewport.FieldOfViewText
        }

        public void CanExecuteLoad(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private string filename(string s)
        {
            return System.IO.Path.Combine(baseFolder, s);
        }


        private void Markup_Load_Click(object sender, RoutedEventArgs e)
        {
            var m = Markup.LoadFromFile(filename(@"viewpoint.bcfv"));
            string a = "";
        }


        private void Markup_Save_Click(object sender, RoutedEventArgs e)
        {
            Markup m = new Markup();
            m.Topic = new Topic();
            m.Topic.Guid = "fdb92063-a353-4882-a4a9-b333fe0b2985";
            m.Topic.ReferenceLink = "referecne 1";
            m.Topic.Title = "topic1";
            m.SaveToFile(filename(@"markup.generated.bcf"));
        }

        private void Visinfo_Load_Click(object sender, RoutedEventArgs e)
        {
            var m = VisualizationInfo.LoadFromFile(filename(@"viewpoint.bcfv"));
            string a = "";
        }

        private void Visinfo_Save_Click(object sender, RoutedEventArgs e)
        {
            var m = new VisualizationInfo();
            Component c = new Component();
            c.IfcGuid = "1gF16zAF1DSPP1$Ex04e4k";
            c.OriginatingSystem = "DDS-CAD";


            m.Components.Add(c);

            PerspectiveCamera p = new PerspectiveCamera();
            p.CameraViewPoint.X = -732.062499964083;
            p.CameraViewPoint.Y = -1152.1249999640554;
            p.CameraViewPoint.Z = 1452.1249999640554;

            p.CameraDirection.X = 0.57735026918962573;
            p.CameraDirection.Y = 0.57735026918962573;
            p.CameraDirection.Z = -0.57735026918962573;

            p.CameraUpVector.X = 0.40824829046386307;
            p.CameraUpVector.Y = 0.40824829046386307;
            p.CameraUpVector.Z = 0.81649658092772615;

            p.FieldOfView = 99.121698557632413;

            m.PerspectiveCamera = p;
            m.OrthogonalCamera = null;

            m.Lines = null;
            m.ClippingPlanes = null;

            m.SaveToFile(filename(@"viewpoint.generated.bcfv"));
        }

        string baseBcfFile = @"..\..\Examples\FromRevit.bcfzip";



        // ---------------------------
        // plugin system related stuff
        //

        XplorerMainWindow xpWindow;

        public void BindUI(XplorerMainWindow MainWindow)
        {
            xpWindow = MainWindow;
            this.SetBinding(SelectedItemProperty, new Binding("SelectedItem") { Source = MainWindow, Mode = BindingMode.OneWay });
            this.SetBinding(ModelProperty, new Binding()); // whole datacontext binding, see http://stackoverflow.com/questions/8343928/how-can-i-create-a-binding-in-code-behind-that-doesnt-specify-a-path
        }

        // SelectedEntity
        public IPersistIfcEntity SelectedEntity
        {
            get { return (IPersistIfcEntity)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public static DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedEntity", typeof(IPersistIfcEntity), typeof(MainWindow), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits,
                                                                      new PropertyChangedCallback(OnSelectedEntityChanged)));

        // Model
        public IModel Model
        {
            get { return (IModel)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        public static DependencyProperty ModelProperty =
            DependencyProperty.Register("Model", typeof(IModel), typeof(MainWindow), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits,
                                                                      new PropertyChangedCallback(OnSelectedEntityChanged)));

        private static void OnSelectedEntityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // if any UI event should happen it needs to be specified here
            MainWindow ctrl = d as MainWindow;
            if (ctrl != null)
            {
                if (e.Property.Name == "Model")
                {
                    // ctrl.txtElementReport.Text = "Model updated";

                }
                else if (e.Property.Name == "SelectedEntity")
                {

                }
            }
        }

        public string MenuText
        {
            get { return "BCF Viewer"; }
        }

        public string WindowTitle
        {
            get { return "BCF Viewer"; }
        }

        public static RenderTargetBitmap get3DVisual(FrameworkElement element)
        {
            double width = element.ActualWidth;
            double height = element.ActualHeight;
            RenderTargetBitmap bmpCopied = new RenderTargetBitmap((int)Math.Round(width), (int)Math.Round(height), 96, 96, PixelFormats.Default);
            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                VisualBrush vb = new VisualBrush(element);
                dc.DrawRectangle(vb, null, new Rect(new System.Windows.Point(), new Size(width, height)));
            }
            bmpCopied.Render(dv);
            return bmpCopied;
        }

        private void newComment(object sender, RoutedEventArgs e)
        {
            VisualizationInfo vi = new VisualizationInfo(xpWindow.DrawingControl);
            var bitmapImage = GetSnapshotImage(xpWindow.DrawingControl);
            selFile.NewInstance(@"New thread", bitmapImage, vi);
        }

        private BitmapImage GetSnapshotImage(DrawingControl3D control)
        {
            var renderTargetBitmap = get3DVisual(control);
            var bitmapImage = new BitmapImage();
            var bitmapEncoder = new PngBitmapEncoder();
            bitmapEncoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

            using (var stream = new MemoryStream())
            {
                bitmapEncoder.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);

                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
            }
            return bitmapImage;
        }


        public PluginWindowDefaultUIShow DefaultUIActivation
        { get { return PluginWindowDefaultUIShow.onLoad; } }

        public PluginWindowDefaultUIContainerEnum DefaultUIContainer
        { get { return PluginWindowDefaultUIContainerEnum.LayoutAnchorable; } }

        public void ProcessMessage(object Sender, string MessageTypeString, object MessageData)
        {
            if (MessageTypeString == "BcfAddInstance" && MessageData is Dictionary<string, object>)
            {
                Dictionary<string, object> data = MessageData as Dictionary<string, object>;
                VisualizationInfo vi = new VisualizationInfo(xpWindow.DrawingControl);
                var bitmapImage = GetSnapshotImage(xpWindow.DrawingControl);
                string InstanceTitle = (string)data["InstanceTitle"];
                string DestinationEmail = (string)data["DestinationEmail"];
                string CommentVerbalStatus = (string)data["CommentVerbalStatus"];
                string CommentAuthor = (string)data["CommentAuthor"];
                string CommentText = (string)data["CommentText"];
                
                var instance = selFile.NewInstance(InstanceTitle, bitmapImage, vi);

                Comment cmt = new Comment();

                cmt.Author = CommentAuthor;
                cmt.Date = DateTime.Now;
                cmt.VerbalStatus = CommentVerbalStatus;
                cmt.Comment1 = CommentText;
                instance.Markup.Topic.ReferenceLink = DestinationEmail;
                instance.Markup.Comment.Add(cmt);
            }
            
        }
    }
}
