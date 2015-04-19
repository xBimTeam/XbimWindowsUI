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

    [System.Obsolete("The plugin system is in alpha version, it will likely require a substantial redesign.", false)]
    public interface IXbimXplorerPluginWindow 
    {
        string MenuText { get; }
        string WindowTitle { get; }
        void BindUI(IXbimXplorerPluginMasterWindow mainWindow);
        PluginWindowDefaultUIContainerEnum DefaultUIContainer { get; }
        PluginWindowDefaultUIShow DefaultUIActivation { get; }
    }
}
