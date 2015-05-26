namespace Xbim.Presentation.XplorerPluginSystem
{
    public enum PluginWindowDefaultUiContainerEnum
    {
        LayoutDoc,
        LayoutAnchorable
    }

    public enum PluginWindowDefaultUiShow
    {
        OnMenu,
        OnLoad
    }

    
    public interface IXbimXplorerPluginWindow 
    {
        string MenuText { get; }
        string WindowTitle { get; }
        void BindUi(IXbimXplorerPluginMasterWindow mainWindow);
        PluginWindowDefaultUiContainerEnum DefaultUiContainer { get; }
        PluginWindowDefaultUiShow DefaultUiActivation { get; }
    }
}
