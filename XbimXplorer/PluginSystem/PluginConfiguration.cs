using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace XbimXplorer.PluginSystem
{
	public class PluginConfiguration
    {
        public PluginFlag OnStartup = PluginFlag.Disabled;
		public PluginFlag LoadingDiagnostics = PluginFlag.Disabled;

		public enum PluginFlag
        {
            Enabled,
            Disabled            
        }

        private static XmlSerializer GetXmlSerializer()
        {
            return new XmlSerializer(typeof(PluginConfiguration));
        }

        public void WriteXml(string path, bool indented = true)
        {
            using (var stream = File.Create(path))
            {
                var serializer = GetXmlSerializer();
                using (var w = new StreamWriter(stream))
                using (var xmlWriter = new XmlTextWriter(w)
                {
                    Formatting = indented ? Formatting.Indented : Formatting.None
                })
                {
                    serializer.Serialize(xmlWriter, this);
                }
                stream.Close();
            }
        }

        public static PluginConfiguration ReadXml(string path)
        {
            if (!File.Exists(path))
                return null;
            using (var stream = File.OpenRead(path))
            {
                var serializer = GetXmlSerializer();
                var deserialised = (PluginConfiguration)serializer.Deserialize(stream);
                stream.Close();
                return deserialised;
            }
        }

        public void ToggleEnabled()
        {
            OnStartup = (OnStartup == PluginFlag.Enabled)
                ? PluginFlag.Disabled 
                : PluginFlag.Enabled;
        }
    }
}
