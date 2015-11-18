namespace Xbim.Presentation.XplorerPluginSystem
{
    public enum PluginWindowUiContainerEnum
    {
        LayoutDoc,
        LayoutAnchorable
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
