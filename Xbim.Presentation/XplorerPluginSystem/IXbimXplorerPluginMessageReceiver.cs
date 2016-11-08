namespace Xbim.Presentation.XplorerPluginSystem
{
    public interface IXbimXplorerPluginMessageReceiver
    {
        void ProcessMessage(object sender, string messageTypeString, object messageData);
    }
}