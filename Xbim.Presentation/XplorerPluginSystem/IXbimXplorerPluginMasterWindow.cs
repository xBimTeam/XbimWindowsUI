using System.Reflection;
using Xbim.Common;
using Xbim.Ifc;


namespace Xbim.Presentation.XplorerPluginSystem
{
    public interface IXbimXplorerPluginMasterWindow
    {
        DrawingControl3D DrawingControl { get; }
        IPersistEntity SelectedItem { get; set; }
        IfcStore Model { get; }
        void BroadCastMessage(object sender, string messageTypeString, object messageData);
        void RefreshPlugins();
        bool Activate();
        bool Focus();
        string GetOpenedModelFileName();
        string GetAssemblyLocation(Assembly requestingAssembly);

    }
}