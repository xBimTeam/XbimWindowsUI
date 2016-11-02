using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XbimXplorer.PluginSystem
{
    internal class PluginConfigurationVm
    {
        private readonly PluginConfiguration _model;

        public PluginConfigurationVm(PluginConfiguration model)
        {
            _model = model;
        }
        
        public  string PluginId => _model.PluginId;

        public string AvailableVersion => _model.AvailableVersion;

        public string InstalledVersion => _model.InstalledVersion;

        public void ExtractPlugin(DirectoryInfo getPluginDirectory)
        {
            _model.ExtractPlugin(getPluginDirectory);
        }
    }
}
