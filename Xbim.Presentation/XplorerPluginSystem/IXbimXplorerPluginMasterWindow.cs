using Xbim.IO;
using Xbim.XbimExtensions.Interfaces;

namespace Xbim.Presentation.XplorerPluginSystem
{
    public interface IXbimXplorerPluginMasterWindow
    {

        DrawingControl3D DrawingControl { get; }

        IPersistIfcEntity SelectedItem { get; set; }

        XbimModel Model { get; }

        void BroadCastMessage(object sender, string messageTypeString, object messageData);

        void RefreshPlugins();

        bool Activate();
        bool Focus();

        string GetOpenedModelFileName();
    }
}