using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Media3D;
using System.Windows.Shell;
using System.Windows.Threading;
// using Xbim.COBie;
using Xbim.IO;
using Xbim.Presentation;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

namespace Xbim.Presentation.XplorerPluginSystem
{
    [System.Obsolete("The plugin system is in alpha version, it will likely require a substantial redesign.", false)]
    public interface IXbimXplorerPluginMasterWindow
    {

        DrawingControl3D DrawingControl { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        // string GetOpenedModelFileName();

        ///// <summary>
        ///// 
        ///// </summary>
        //FilterValues UserFilters { get; set; }


        /// <summary>
        /// 
        /// </summary>
        // XbimDBAccess FileAccessMode { get; set; }

        /// <summary>
        /// 
        /// </summary>
        IPersistIfcEntity SelectedItem { get; set; }

        /// <summary>
        /// 
        /// </summary>
        XbimModel Model { get; }

        
        //Thickness Padding { get; set; }
        //ControlTemplate Template { get; set; }
        //Style Style { get; set; }
        //bool OverridesDefaultStyle { get; set; }
        //bool UseLayoutRounding { get; set; }
        //TriggerCollection Triggers { get; }
        //DependencyObject TemplatedParent { get; }
        //ResourceDictionary Resources { get; set; }
        //object DataContext { get; set; }
        //BindingGroup BindingGroup { get; set; }
        //XmlLanguage Language { get; set; }
        //string Name { get; set; }
        //object Tag { get; set; }
        //InputScope InputScope { get; set; }
        //double ActualWidth { get; }
        //double ActualHeight { get; }
        //Transform LayoutTransform { get; set; }
        //double Width { get; set; }
        //double MinWidth { get; set; }
        //double MaxWidth { get; set; }
        //double Height { get; set; }
        //double MinHeight { get; set; }
        //double MaxHeight { get; set; }
        //FlowDirection FlowDirection { get; set; }
        //Thickness Margin { get; set; }
        //HorizontalAlignment HorizontalAlignment { get; set; }
        //VerticalAlignment VerticalAlignment { get; set; }
        //Style FocusVisualStyle { get; set; }
        //Cursor Cursor { get; set; }
        //bool ForceCursor { get; set; }
        //bool IsInitialized { get; }
        //bool IsLoaded { get; }
        //object ToolTip { get; set; }
        //ContextMenu ContextMenu { get; set; }
        //DependencyObject Parent { get; }
        //bool HasAnimatedProperties { get; }
        //InputBindingCollection InputBindings { get; }
        //CommandBindingCollection CommandBindings { get; }
        //bool AllowDrop { get; set; }
        //Size DesiredSize { get; }
        //bool IsMeasureValid { get; }
        //bool IsArrangeValid { get; }
        //Size RenderSize { get; set; }
        //Transform RenderTransform { get; set; }
        //Point RenderTransformOrigin { get; set; }
        //bool IsMouseDirectlyOver { get; }
        //bool IsMouseOver { get; }
        //bool IsStylusOver { get; }
        //bool IsKeyboardFocusWithin { get; }
        //bool IsMouseCaptured { get; }
        //bool IsMouseCaptureWithin { get; }
        //bool IsStylusDirectlyOver { get; }
        //bool IsStylusCaptured { get; }
        //bool IsStylusCaptureWithin { get; }
        //bool IsKeyboardFocused { get; }
        //bool IsInputMethodEnabled { get; }
        //double Opacity { get; set; }
        //Brush OpacityMask { get; set; }
        //BitmapEffect BitmapEffect { get; set; }
        //Effect Effect { get; set; }
        //BitmapEffectInput BitmapEffectInput { get; set; }
        //CacheMode CacheMode { get; set; }
        //string Uid { get; set; }
        //Visibility Visibility { get; set; }
        //bool ClipToBounds { get; set; }
        //Geometry Clip { get; set; }
        //bool SnapsToDevicePixels { get; set; }
        //bool IsFocused { get; }
        //bool IsEnabled { get; set; }
        //bool IsHitTestVisible { get; set; }
        //bool IsVisible { get; }
        //bool Focusable { get; set; }
        //int PersistId { get; }
        //bool IsManipulationEnabled { get; set; }
        //bool AreAnyTouchesOver { get; }
        //bool AreAnyTouchesDirectlyOver { get; }
        //bool AreAnyTouchesCapturedWithin { get; }
        //bool AreAnyTouchesCaptured { get; }
        //IEnumerable<TouchDevice> TouchesCaptured { get; }
        //IEnumerable<TouchDevice> TouchesCapturedWithin { get; }
        //IEnumerable<TouchDevice> TouchesOver { get; }
        //IEnumerable<TouchDevice> TouchesDirectlyOver { get; }
        //DependencyObjectType DependencyObjectType { get; }
        //bool IsSealed { get; }
        //Dispatcher Dispatcher { get; }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="modelFileName"></param>
        //void LoadAnyModel(string modelFileName);

        ///// <summary>
        ///// 
        ///// </summary>
        //event XplorerMainWindow.LoadingCompleteEventHandler LoadingComplete;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="messageTypeString"></param>
        /// <param name="messageData"></param>
        void BroadCastMessage(object sender, string messageTypeString, object messageData);

        /// <summary>
        /// 
        /// </summary>
        void RefreshPlugins();

        //void Show();
        //void Hide();
        //void Close();
        //void DragMove();
        //bool? ShowDialog();
        bool Activate();
        //event EventHandler SourceInitialized;
        //event EventHandler Activated;
        //event EventHandler Deactivated;
        //event EventHandler StateChanged;
        //event EventHandler LocationChanged;
        //event CancelEventHandler Closing;
        //event EventHandler Closed;
        //event EventHandler ContentRendered;
        //bool ShouldSerializeContent();
        //string ToString();
        //event MouseButtonEventHandler PreviewMouseDoubleClick;
        //event MouseButtonEventHandler MouseDoubleClick;
        //bool ShouldSerializeStyle();
        //bool ApplyTemplate();
        //void OnApplyTemplate();
        //void BeginStoryboard(Storyboard storyboard);
        //void BeginStoryboard(Storyboard storyboard, HandoffBehavior handoffBehavior);
        //void BeginStoryboard(Storyboard storyboard, HandoffBehavior handoffBehavior, bool isControllable);
        //bool ShouldSerializeTriggers();
        //bool ShouldSerializeResources();
        //object FindResource(object resourceKey);
        //object TryFindResource(object resourceKey);
        //void SetResourceReference(DependencyProperty dp, object name);
        //BindingExpression GetBindingExpression(DependencyProperty dp);
        //BindingExpressionBase SetBinding(DependencyProperty dp, BindingBase binding);
        //BindingExpression SetBinding(DependencyProperty dp, string path);
        //void BringIntoView();
        //void BringIntoView(Rect targetRectangle);
        //bool MoveFocus(TraversalRequest request);
        //DependencyObject PredictFocus(FocusNavigationDirection direction);
        //void BeginInit();
        //void EndInit();
        //void RegisterName(string name, object scopedElement);
        //void UnregisterName(string name);
        //object FindName(string name);
        //void UpdateDefaultStyle();
        //event EventHandler<DataTransferEventArgs> TargetUpdated;
        //event EventHandler<DataTransferEventArgs> SourceUpdated;
        //event DependencyPropertyChangedEventHandler DataContextChanged;
        //event RequestBringIntoViewEventHandler RequestBringIntoView;
        //event SizeChangedEventHandler SizeChanged;
        //event EventHandler Initialized;
        //event RoutedEventHandler Loaded;
        //event RoutedEventHandler Unloaded;
        //event ToolTipEventHandler ToolTipOpening;
        //event ToolTipEventHandler ToolTipClosing;
        //event ContextMenuEventHandler ContextMenuOpening;
        //event ContextMenuEventHandler ContextMenuClosing;
        //void ApplyAnimationClock(DependencyProperty dp, AnimationClock clock);
        //void ApplyAnimationClock(DependencyProperty dp, AnimationClock clock, HandoffBehavior handoffBehavior);
        //void BeginAnimation(DependencyProperty dp, AnimationTimeline animation);
        //void BeginAnimation(DependencyProperty dp, AnimationTimeline animation, HandoffBehavior handoffBehavior);
        //object GetAnimationBaseValue(DependencyProperty dp);
        //bool ShouldSerializeInputBindings();
        //bool ShouldSerializeCommandBindings();
        //void RaiseEvent(RoutedEventArgs e);
        //void AddHandler(RoutedEvent routedEvent, Delegate handler);
        //void AddHandler(RoutedEvent routedEvent, Delegate handler, bool handledEventsToo);
        //void RemoveHandler(RoutedEvent routedEvent, Delegate handler);
        //void AddToEventRoute(EventRoute route, RoutedEventArgs e);
        //void InvalidateMeasure();
        //void InvalidateArrange();
        //void InvalidateVisual();
        //void Measure(Size availableSize);
        //void Arrange(Rect finalRect);
        //void UpdateLayout();
        //Point TranslatePoint(Point point, UIElement relativeTo);
        //IInputElement InputHitTest(Point point);
        //bool CaptureMouse();
        //void ReleaseMouseCapture();
        //bool CaptureStylus();
        //void ReleaseStylusCapture();
        bool Focus();
        //bool CaptureTouch(TouchDevice touchDevice);
        //bool ReleaseTouchCapture(TouchDevice touchDevice);
        //void ReleaseAllTouchCaptures();
        //event MouseButtonEventHandler PreviewMouseDown;
        //event MouseButtonEventHandler MouseDown;
        //event MouseButtonEventHandler PreviewMouseUp;
        //event MouseButtonEventHandler MouseUp;
        //event MouseButtonEventHandler PreviewMouseLeftButtonDown;
        //event MouseButtonEventHandler MouseLeftButtonDown;
        //event MouseButtonEventHandler PreviewMouseLeftButtonUp;
        //event MouseButtonEventHandler MouseLeftButtonUp;
        //event MouseButtonEventHandler PreviewMouseRightButtonDown;
        //event MouseButtonEventHandler MouseRightButtonDown;
        //event MouseButtonEventHandler PreviewMouseRightButtonUp;
        //event MouseButtonEventHandler MouseRightButtonUp;
        //event MouseEventHandler PreviewMouseMove;
        //event MouseEventHandler MouseMove;
        //event MouseWheelEventHandler PreviewMouseWheel;
        //event MouseWheelEventHandler MouseWheel;
        //event MouseEventHandler MouseEnter;
        //event MouseEventHandler MouseLeave;
        //event MouseEventHandler GotMouseCapture;
        //event MouseEventHandler LostMouseCapture;
        //event QueryCursorEventHandler QueryCursor;
        //event StylusDownEventHandler PreviewStylusDown;
        //event StylusDownEventHandler StylusDown;
        //event StylusEventHandler PreviewStylusUp;
        //event StylusEventHandler StylusUp;
        //event StylusEventHandler PreviewStylusMove;
        //event StylusEventHandler StylusMove;
        //event StylusEventHandler PreviewStylusInAirMove;
        //event StylusEventHandler StylusInAirMove;
        //event StylusEventHandler StylusEnter;
        //event StylusEventHandler StylusLeave;
        //event StylusEventHandler PreviewStylusInRange;
        //event StylusEventHandler StylusInRange;
        //event StylusEventHandler PreviewStylusOutOfRange;
        //event StylusEventHandler StylusOutOfRange;
        //event StylusSystemGestureEventHandler PreviewStylusSystemGesture;
        //event StylusSystemGestureEventHandler StylusSystemGesture;
        //event StylusEventHandler GotStylusCapture;
        //event StylusEventHandler LostStylusCapture;
        //event StylusButtonEventHandler StylusButtonDown;
        //event StylusButtonEventHandler StylusButtonUp;
        //event StylusButtonEventHandler PreviewStylusButtonDown;
        //event StylusButtonEventHandler PreviewStylusButtonUp;
        //event KeyEventHandler PreviewKeyDown;
        //event KeyEventHandler KeyDown;
        //event KeyEventHandler PreviewKeyUp;
        //event KeyEventHandler KeyUp;
        //event KeyboardFocusChangedEventHandler PreviewGotKeyboardFocus;
        //event KeyboardFocusChangedEventHandler GotKeyboardFocus;
        //event KeyboardFocusChangedEventHandler PreviewLostKeyboardFocus;
        //event KeyboardFocusChangedEventHandler LostKeyboardFocus;
        //event TextCompositionEventHandler PreviewTextInput;
        //event TextCompositionEventHandler TextInput;
        //event QueryContinueDragEventHandler PreviewQueryContinueDrag;
        //event QueryContinueDragEventHandler QueryContinueDrag;
        //event GiveFeedbackEventHandler PreviewGiveFeedback;
        //event GiveFeedbackEventHandler GiveFeedback;
        //event DragEventHandler PreviewDragEnter;
        //event DragEventHandler DragEnter;
        //event DragEventHandler PreviewDragOver;
        //event DragEventHandler DragOver;
        //event DragEventHandler PreviewDragLeave;
        //event DragEventHandler DragLeave;
        //event DragEventHandler PreviewDrop;
        //event DragEventHandler Drop;
        //event EventHandler<TouchEventArgs> PreviewTouchDown;
        //event EventHandler<TouchEventArgs> TouchDown;
        //event EventHandler<TouchEventArgs> PreviewTouchMove;
        //event EventHandler<TouchEventArgs> TouchMove;
        //event EventHandler<TouchEventArgs> PreviewTouchUp;
        //event EventHandler<TouchEventArgs> TouchUp;
        //event EventHandler<TouchEventArgs> GotTouchCapture;
        //event EventHandler<TouchEventArgs> LostTouchCapture;
        //event EventHandler<TouchEventArgs> TouchEnter;
        //event EventHandler<TouchEventArgs> TouchLeave;
        //event DependencyPropertyChangedEventHandler IsMouseDirectlyOverChanged;
        //event DependencyPropertyChangedEventHandler IsKeyboardFocusWithinChanged;
        //event DependencyPropertyChangedEventHandler IsMouseCapturedChanged;
        //event DependencyPropertyChangedEventHandler IsMouseCaptureWithinChanged;
        //event DependencyPropertyChangedEventHandler IsStylusDirectlyOverChanged;
        //event DependencyPropertyChangedEventHandler IsStylusCapturedChanged;
        //event DependencyPropertyChangedEventHandler IsStylusCaptureWithinChanged;
        //event DependencyPropertyChangedEventHandler IsKeyboardFocusedChanged;
        //event EventHandler LayoutUpdated;
        //event RoutedEventHandler GotFocus;
        //event RoutedEventHandler LostFocus;
        //event DependencyPropertyChangedEventHandler IsEnabledChanged;
        //event DependencyPropertyChangedEventHandler IsHitTestVisibleChanged;
        //event DependencyPropertyChangedEventHandler IsVisibleChanged;
        //event DependencyPropertyChangedEventHandler FocusableChanged;
        //event EventHandler<ManipulationStartingEventArgs> ManipulationStarting;
        //event EventHandler<ManipulationStartedEventArgs> ManipulationStarted;
        //event EventHandler<ManipulationDeltaEventArgs> ManipulationDelta;
        //event EventHandler<ManipulationInertiaStartingEventArgs> ManipulationInertiaStarting;
        //event EventHandler<ManipulationBoundaryFeedbackEventArgs> ManipulationBoundaryFeedback;
        //event EventHandler<ManipulationCompletedEventArgs> ManipulationCompleted;
        //bool IsAncestorOf(DependencyObject descendant);
        //bool IsDescendantOf(DependencyObject ancestor);
        //DependencyObject FindCommonVisualAncestor(DependencyObject otherVisual);
        //GeneralTransform TransformToAncestor(Visual ancestor);
        //GeneralTransform2DTo3D TransformToAncestor(Visual3D ancestor);
        //GeneralTransform TransformToDescendant(Visual descendant);
        //GeneralTransform TransformToVisual(Visual visual);
        //Point PointToScreen(Point point);
        //Point PointFromScreen(Point point);
        //bool Equals(object obj);
        //int GetHashCode();
        //object GetValue(DependencyProperty dp);
        //void SetValue(DependencyProperty dp, object value);
        //void SetCurrentValue(DependencyProperty dp, object value);
        //void SetValue(DependencyPropertyKey key, object value);
        //void ClearValue(DependencyProperty dp);
        //void ClearValue(DependencyPropertyKey key);
        //void CoerceValue(DependencyProperty dp);
        //void InvalidateProperty(DependencyProperty dp);
        //object ReadLocalValue(DependencyProperty dp);
        //LocalValueEnumerator GetLocalValueEnumerator();
        //bool CheckAccess();
        //void VerifyAccess();
    }
}