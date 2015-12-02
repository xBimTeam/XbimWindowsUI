namespace Xbim.Presentation.XplorerPluginSystem
{
    /// <summary>
    /// Defines the UI appearence of the element.
    /// </summary>
    public enum PluginWindowUiContainerEnum
    {
        /// <summary>
        /// The window will open in the central pane as a document
        /// The object implementing this will have to be a UserControl.
        /// </summary>
        LayoutDoc,
        /// <summary>
        /// Use this for contextual toolboxes.
        /// The object implementing this will have to be a UserControl.
        /// </summary>
        LayoutAnchorable,
        /// <summary>
        /// Dialog is the mode for windows that keep focus throughout their lifespan
        /// The object implementing this will have to be a Window.
        /// </summary>
        Dialog
    }

    public enum PluginWindowActivation
    {
        OnMenu,
        OnLoad
    }

    public enum PluginWindowCloseAction
    {
        Hide,
        Close
    }

    public interface IXbimXplorerPluginWindow 
    {
        string WindowTitle { get; }
        void BindUi(IXbimXplorerPluginMasterWindow mainWindow);        
    }
}
