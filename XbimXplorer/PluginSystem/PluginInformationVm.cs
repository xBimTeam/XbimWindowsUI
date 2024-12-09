using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NuGet;
using NuGet.Versioning;
using XbimXplorer.Annotations;

namespace XbimXplorer.PluginSystem
{
    public class PluginInformationVm : INotifyPropertyChanged
    {
        private readonly PluginInformation _model;

        public PluginInformationVm(PluginInformation model)
        {
            _model = model;
        }

        public bool CanDownload
        {
            get
            {
                if (AvailableVersion == null)
                    return false;
                return AvailableVersion != InstalledVersion;
            }
        }

        public string DownloadCaption
        {
            get
            {
                // when button is grayed 
                if (!CanDownload)
                    return "Download";

                // if can download and not installed 
                if (InstalledVersion == null)
                    return "Download";

                // if installed
                var remoteVersion = new SemanticVersion(AvailableVersion);
                var localVersion = new SemanticVersion(InstalledVersion);

                return remoteVersion > localVersion 
                    ? "Update" 
                    : "Replace";
            }
        }

        public bool IsInstalled => (InstalledVersion!=null);

        public bool IsInstalledAndNotLoaded => (InstalledVersion!=null) && string.IsNullOrWhiteSpace(LoadedVersion);

        public string PluginId => _model?.PluginId;

        internal void RemoveInstallation()
        {
            if (_model == null)
                return;
            _model.DeleteFromDisk();
            OnPropertyChanged("");
        }

        public bool CanLoad => IsInstalled && string.IsNullOrEmpty(LoadedVersion);

        public Visibility VisibleWhenInstalled => IsInstalled 
            ? Visibility.Visible 
            : Visibility.Hidden;

        public string EnableToggleCaption => _model?.Config?.OnStartup == PluginConfiguration.PluginFlag.Enabled 
            ? "Disable" 
            : "Enable";

        public NuGetVersion AvailableVersion => _model?.AvailableVersion;

        public NuGetVersion InstalledVersion => _model?.InstalledVersion;

        public string LoadedVersion => _model?.LoadedVersion;

        public string Startup => _model?.Config?.OnStartup.ToString();

        public async Task ExtractPlugin(DirectoryInfo pluginsDirectory)
        {
            if (_model == null)
                return;
            
            await _model.ExtractPlugin(pluginsDirectory);
            OnPropertyChanged("");
        }

        public string Action
        {
            get
            {
                if (LoadedVersion != "" && InstalledVersion.ToString() != LoadedVersion)
                    return "Restart required.";
                return "";
            }
        }

        /// <returns>True if plugin is completely loaded. False if not, for any reason.</returns>
        public bool Load()
        {
            if (_model == null)
                return false;

            var l = _model.Load();
            OnPropertyChanged("");
            return l;
        }

        public void ToggleEnabled()
        {
            if (_model == null)
                return;

            _model.ToggleEnabled();
            OnPropertyChanged("");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
