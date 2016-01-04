using Xbim.Common;
using Xbim.Ifc2x3.IO;


namespace Xbim.Presentation.XplorerPluginSystem
{
    public interface IXbimXplorerPluginMasterWindow
    {

        DrawingControl3D DrawingControl { get; }

        IPersistEntity SelectedItem { get; set; }

        XbimModel Model { get; }

        void BroadCastMessage(object sender, string messageTypeString, object messageData);

        void RefreshPlugins();

        bool Activate();
        bool Focus();

        string GetOpenedModelFileName();
    }
}