using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Xbim.BCF.UI
{
    public class BcfInstanceViewModel : DependencyObject
    {
        BcfInstance _instance;

        public BcfInstanceViewModel(BcfInstance instance)
        {
            _instance = instance;
            if (_instance != null)
            {
                TopicTitle = instance.Markup.Topic.Title;
                TopicReferenceLink = instance.Markup.Topic.ReferenceLink;
            }
            GoCam = new GoCamera(this);
            _addCmt = new AddCommentCommand(this);
        }

        #region ViewModelProperties

        
        public BindingList<Comment> Comments
        {
            get
            {
                return _instance != null 
                    ? _instance.Markup.Comment 
                    : null;
            }
        }

        public string TopicTitle
        {
            get { return (string)GetValue(TopicTitleProperty); }
            set { 
                SetValue(TopicTitleProperty, value); 
            }
        }

        // Using a DependencyProperty as the backing store for Title.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TopicTitleProperty =
            DependencyProperty.Register("TopicTitle", typeof(string), typeof(BcfInstanceViewModel), 
                new PropertyMetadata("", new PropertyChangedCallback(OnTextsChanged))
            );

        private static void OnTextsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DependencyObject)
            {
                var D = d as BcfInstanceViewModel;
                if (e.Property.Name == "TopicTitle")
                    D._instance.Markup.Topic.Title = e.NewValue as string;
                if (e.Property.Name == "TopicReferenceLink")
                    D._instance.Markup.Topic.ReferenceLink = e.NewValue as string;
            }
        }

        public string TopicReferenceLink
        {
            get { return (string)GetValue(TopicReferenceLinkProperty); }
            set { SetValue(TopicReferenceLinkProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TopicReferenceLink.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TopicReferenceLinkProperty =
            DependencyProperty.Register("TopicReferenceLink", typeof(string), typeof(BcfInstanceViewModel), new PropertyMetadata("", new PropertyChangedCallback(OnTextsChanged)));

        public BitmapImage Img
        {
            get
            {
                if (_instance == null)
                    return null;
                return _instance.SnapShot;
            }
        }

        internal bool HasCamera()
        {
            if (_instance.VisualizationInfo == null)
                return false;
            if (
                _instance.VisualizationInfo.OrthogonalCamera == null
                &&
                _instance.VisualizationInfo.PerspectiveCamera == null
                )
                return false;
            return true;
        }

        internal void SendViewerToCamera()
        {
            
        }

        public ICommand GoCam { get; set; }
        #endregion

        private AddCommentCommand _addCmt;
        public ICommand AddComment
        {
            get {
                return _addCmt;
            }
        }

        internal void AddCommentFunction()
        {
            var cmt = new Comment()
            {
                Author = "CB",
                Date = DateTime.Now,
                Status = CommentStatus.Unknown,
                VerbalStatus = "Unknown"
            };
            _instance.Markup.Comment.Add(cmt);
            // this.PropertyChanged(this, new PropertyChangedEventArgs("Comments"));
        }



        
    }

    public class AddCommentCommand : ICommand
    {
        BcfInstanceViewModel _vm;
        public AddCommentCommand(BcfInstanceViewModel vm)
        {
            _vm = vm;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        /// <summary>
        /// Occurs when changes occur that affect whether or not the command should execute.
        /// </summary>
        public event EventHandler CanExecuteChanged // Never used, but required by ICommand interface
        {
            add { }
            remove { }
        }


        public void Execute(object parameter)
        {
            _vm.AddCommentFunction();
        }
    }

    public class GoCamera : ICommand
    {
        BcfInstanceViewModel _vm;
        public GoCamera(BcfInstanceViewModel vm)
        {
            _vm = vm;
        }

        public bool CanExecute(object parameter)
        {
            return _vm.HasCamera();
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            _vm.SendViewerToCamera();
        }
    }
}
