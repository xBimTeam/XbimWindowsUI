﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace XbimXplorer.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "17.4.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Read")]
        public global::Xbim.IO.XbimDBAccess FileAccessMode {
            get {
                return ((global::Xbim.IO.XbimDBAccess)(this["FileAccessMode"]));
            }
            set {
                this["FileAccessMode"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public global::System.Collections.Specialized.StringCollection MRUFiles {
            get {
                return ((global::System.Collections.Specialized.StringCollection)(this["MRUFiles"]));
            }
            set {
                this["MRUFiles"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("4")]
        public int MRUFilesCount {
            get {
                return ((int)(this["MRUFilesCount"]));
            }
            set {
                this["MRUFilesCount"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool PluginStartupLoad {
            get {
                return ((bool)(this["PluginStartupLoad"]));
            }
            set {
                this["PluginStartupLoad"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool DeveloperMode {
            get {
                return ((bool)(this["DeveloperMode"]));
            }
            set {
                this["DeveloperMode"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public global::System.Collections.Specialized.StringCollection PluginSettings {
            get {
                return ((global::System.Collections.Specialized.StringCollection)(this["PluginSettings"]));
            }
            set {
                this["PluginSettings"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool SettingsUpdateRequired {
            get {
                return ((bool)(this["SettingsUpdateRequired"]));
            }
            set {
                this["SettingsUpdateRequired"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Debug")]
        public global::Serilog.Events.LogEventLevel LoggingLevel {
            get {
                return ((global::Serilog.Events.LogEventLevel)(this["LoggingLevel"]));
            }
            set {
                this["LoggingLevel"] = value;
            }
        }
    }
}
