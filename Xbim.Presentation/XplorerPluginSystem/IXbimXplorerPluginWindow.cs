namespace Xbim.Presentation.XplorerPluginSystem
{
    public enum PluginWindowDefaultUIContainerEnum
    {
        LayoutDoc,
        LayoutAnchorable
    }

    public enum PluginWindowDefaultUIShow
    {
        onMenu,
        onLoad
    }

    
    public interface IXbimXplorerPluginWindow 
    {
        string MenuText { get; }
        string WindowTitle { get; }
        void BindUI(IXbimXplorerPluginMasterWindow mainWindow);
        PluginWindowDefaultUIContainerEnum DefaultUIContainer { get; }
        PluginWindowDefaultUIShow DefaultUIActivation { get; }
    }
}
