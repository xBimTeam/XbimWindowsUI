using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using XbimXplorer.Annotations;

namespace XbimXplorer.PluginSystem
{
    internal class PluginInformationVm : INotifyPropertyChanged
    {
        private readonly PluginInformation _model;

        public PluginInformationVm(PluginInformation model)
        {
            _model = model;
        }
        
        public  string PluginId => _model.PluginId;

        public string AvailableVersion => _model.AvailableVersion;

        public string InstalledVersion => _model.InstalledVersion;

        public string LoadedVersion => _model.LoadedVersion;

        public string Startup => _model?.Startup?.OnStartup.ToString();

        public void ExtractPlugin(DirectoryInfo pluginsDirectory)
        {
            _model.ExtractPlugin(pluginsDirectory);
        }

        public bool Load()
        {
            return _model.Load();
        }

        public void ToggleEnabled()
        {
            _model.ToggleEnabled();
            OnPropertyChanged(nameof(Startup));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
