using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Xbim.Common;
using Xbim.Common.Geometry;
using Xbim.Presentation;
using Xbim.Presentation.XplorerPluginSystem;


namespace Xbim.BCF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [XplorerUiElement(PluginWindowUiContainerEnum.LayoutDoc, PluginWindowActivation.OnMenu, "BCF Editor")]
    public partial class MainWindow : IXbimXplorerPluginWindow, IXbimXplorerPluginMessageReceiver
    {
        string _baseFolder = @"..\..\Examples\BuildingSmart\fdb92063-a353-4882-a4a9-b333fe0b2985\";

        public MainWindow()
        {
            InitializeComponent();
            CommandBindings.Add(new CommandBinding(BcfFileCommands.Load, ExecuteLoad, CanExecuteLoad));
            CommandBindings.Add(new CommandBinding(BcfFileCommands.Save, ExecuteSave, CanExecuteSave));

            CommandBindings.Add(new CommandBinding(BcfInstanceCommands.GotoCameraPosition, ExecuteCamera, CanExecuteLoad));
        }

        private void ExecuteSave(object sender, ExecutedRoutedEventArgs e)
        {
            var ofd = new SaveFileDialog();
            ofd.Filter = @"BIM Collaboration Format|*.bcfzip";
            var v = ofd.ShowDialog();
            if (v.HasValue && v.Value)
            {
                SelFile.Save(ofd.FileName);
            }

        }

        private void CanExecuteSave(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = SelFile.CanSave;
        }

        public void ExecuteLoad(object sender, ExecutedRoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = @"BIM Collaboration Format|*.bcfzip";
            var v = ofd.ShowDialog();
            if (v.HasValue && v.Value)
            {
                SelFile.Load(ofd.FileName);
            }
        }

        public void ExecuteCamera(object sender, ExecutedRoutedEventArgs e)
        {
            if (SelFile.T == null)
                return;
            var inst = SelFile.T.SelectedItem as BcfInstance;
            if (inst == null)
                return;
            var v = inst.VisualizationInfo;
            if (v == null)
                return;

            var position = new XbimPoint3D();
            var direction = new XbimPoint3D();
            var upDirection = new XbimPoint3D();

            XbimVector3D directionV;
            XbimVector3D upDirectionV;

            if (v.PerspectiveCamera != null)
            {
                var pc = v.PerspectiveCamera;
                position = new XbimPoint3D(pc.CameraViewPoint.X, pc.CameraViewPoint.Y, pc.CameraViewPoint.Z);
                direction = new XbimPoint3D(pc.CameraDirection.X, pc.CameraDirection.Y, pc.CameraDirection.Z);
                upDirection = new XbimPoint3D(pc.CameraUpVector.X, pc.CameraUpVector.Y, pc.CameraUpVector.Z);

                _xpWindow.DrawingControl.Viewport.Orthographic = false;
                var pCam = _xpWindow.DrawingControl.Viewport.Camera as System.Windows.Media.Media3D.PerspectiveCamera;
                if (pCam != null)
                    pCam.FieldOfView = pc.FieldOfView;
            }
            else if (v.OrthogonalCamera != null)
            {
                var pc = v.OrthogonalCamera;
                _xpWindow.DrawingControl.Viewport.Orthographic = true;
                position = new XbimPoint3D(pc.CameraViewPoint.X, pc.CameraViewPoint.Y, pc.CameraViewPoint.Z);
                direction = new XbimPoint3D(pc.CameraDirection.X, pc.CameraDirection.Y, pc.CameraDirection.Z);
                upDirection = new XbimPoint3D(pc.CameraUpVector.X, pc.CameraUpVector.Y, pc.CameraUpVector.Z);

                var pCam = _xpWindow.DrawingControl.Viewport.Camera as OrthographicCamera;
                if (pCam != null)
                    pCam.Width = pc.ViewToWorldScale;
            }
            directionV = new XbimVector3D(direction.X, direction.Y, direction.Z);
            upDirectionV = new XbimVector3D(upDirection.X, upDirection.Y, upDirection.Z);

            var pos = new Point3D(position.X, position.Y, position.Z);
            var dir = new Vector3D(directionV.X, directionV.Y, directionV.Z);
            var upDir = new Vector3D(upDirectionV.X, upDirectionV.Y, upDirectionV.Z);
            _xpWindow.DrawingControl.Viewport.SetView(pos, dir, upDir, 500);

            if (v.ClippingPlanes.Any())
            {
                var curP = v.ClippingPlanes[0];
                _xpWindow.DrawingControl.SetCutPlane(
                    curP.Location.X, curP.Location.Y, curP.Location.Z,
                    curP.Direction.X, curP.Direction.Y, curP.Direction.Z
                    );
            }
            else
            {
                _xpWindow.DrawingControl.ClearCutPlane();
            }

            // xpWindow.DrawingControl.Viewport.FieldOfViewText
        }

        public void CanExecuteLoad(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        internal string Filename(string s)
        {
            return Path.Combine(_baseFolder, s);
        }


        private void Markup_Load_Click(object sender, RoutedEventArgs e)
        {
            Markup.LoadFromFile(Filename(@"viewpoint.bcfv"));
        }


        private void Markup_Save_Click(object sender, RoutedEventArgs e)
        {
            var m = new Markup();
            m.Topic = new Topic();
            m.Topic.Guid = "fdb92063-a353-4882-a4a9-b333fe0b2985";
            m.Topic.ReferenceLink = "referecne 1";
            m.Topic.Title = "topic1";
            m.SaveToFile(Filename(@"markup.generated.bcf"));
        }

        private void Visinfo_Load_Click(object sender, RoutedEventArgs e)
        {
            VisualizationInfo.LoadFromFile(Filename(@"viewpoint.bcfv"));
        }

        private void Visinfo_Save_Click(object sender, RoutedEventArgs e)
        {
            var m = new VisualizationInfo();
            var c = new Component();
            c.IfcGuid = "1gF16zAF1DSPP1$Ex04e4k";
            c.OriginatingSystem = "DDS-CAD";


            m.Components.Add(c);

            var p = new PerspectiveCamera();
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

            m.SaveToFile(Filename(@"viewpoint.generated.bcfv"));
        }


        // ---------------------------
        // plugin system related stuff
        //

        private IXbimXplorerPluginMasterWindow _xpWindow;

        public void BindUi(IXbimXplorerPluginMasterWindow mainWindow)
        {
            _xpWindow = mainWindow;
            SetBinding(SelectedItemProperty, new Binding("SelectedItem") { Source = mainWindow, Mode = BindingMode.OneWay });
            SetBinding(ModelProperty, new Binding()); // whole datacontext binding, see http://stackoverflow.com/questions/8343928/how-can-i-create-a-binding-in-code-behind-that-doesnt-specify-a-path
        }

        // SelectedEntity
        public IPersistEntity SelectedEntity
        {
            get { return (IPersistEntity)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public static DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedEntity", typeof(IPersistEntity), typeof(MainWindow), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits,
                                                                      OnSelectedEntityChanged));

        // Model
        public IModel Model
        {
            get { return (IModel)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        public static DependencyProperty ModelProperty =
            DependencyProperty.Register("Model", typeof(IModel), typeof(MainWindow), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits,
                                                                      OnSelectedEntityChanged));

        private static void OnSelectedEntityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // if any UI event should happen it needs to be specified here
            var ctrl = d as MainWindow;
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
        
        public string WindowTitle
        {
            get { return "BCF Editor"; }
        }

        public static RenderTargetBitmap Get3DVisual(FrameworkElement element)
        {
            var width = element.ActualWidth;
            var height = element.ActualHeight;
            var bmpCopied = new RenderTargetBitmap((int)Math.Round(width), (int)Math.Round(height), 96, 96, PixelFormats.Default);
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                var vb = new VisualBrush(element);
                dc.DrawRectangle(vb, null, new Rect(new System.Windows.Point(), new Size(width, height)));
            }
            bmpCopied.Render(dv);
            return bmpCopied;
        }

        private void NewComment(object sender, RoutedEventArgs e)
        {
            var vi = new VisualizationInfo(_xpWindow.DrawingControl);
            var bitmapImage = GetSnapshotImage(_xpWindow.DrawingControl);
            SelFile.NewInstance(@"New thread", bitmapImage, vi);
        }

        private BitmapImage GetSnapshotImage(DrawingControl3D control)
        {
            var renderTargetBitmap = Get3DVisual(control);
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

        public void ProcessMessage(object sender, string messageTypeString, object messageData)
        {
            if (messageTypeString == "BcfAddInstance" && messageData is Dictionary<string, object>)
            {
                var data = messageData as Dictionary<string, object>;
                var vi = new VisualizationInfo(_xpWindow.DrawingControl);
                var bitmapImage = GetSnapshotImage(_xpWindow.DrawingControl);
                var instanceTitle = (string)data["InstanceTitle"];
                var destinationEmail = (string)data["DestinationEmail"];
                var commentVerbalStatus = (string)data["CommentVerbalStatus"];
                var commentAuthor = (string)data["CommentAuthor"];
                var commentText = (string)data["CommentText"];
                
                var instance = SelFile.NewInstance(instanceTitle, bitmapImage, vi);

                var cmt = new Comment();

                cmt.Author = commentAuthor;
                cmt.Date = DateTime.Now;
                cmt.VerbalStatus = commentVerbalStatus;
                cmt.Comment1 = commentText;
                instance.Markup.Topic.ReferenceLink = destinationEmail;
                instance.Markup.Comment.Add(cmt);
            }
            
        }
    }
}
