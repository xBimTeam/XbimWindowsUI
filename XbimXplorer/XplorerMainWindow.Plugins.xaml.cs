using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Xbim.Presentation.XplorerPluginSystem;
using XbimXplorer.PluginSystem;
using Xceed.Wpf.AvalonDock.Layout;

namespace XbimXplorer
{
    public partial class XplorerMainWindow
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="messageTypeString"></param>
        /// <param name="messageData"></param>
        public void BroadCastMessage(object sender, string messageTypeString, object messageData)
        {
            foreach (var window in _pluginWindows)
            {
                var msging = window as IXbimXplorerPluginMessageReceiver;
                if (msging != null)
                {
                    msging.ProcessMessage(sender, messageTypeString, messageData);
                }
            }
        }

        public void RefreshPlugins()
        {
            var path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            if (!string.IsNullOrWhiteSpace(path))
                path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (path == null) 
                return;
            path = Path.Combine(path, "Plugins");

            var di = new DirectoryInfo(path);
            if (!di.Exists)
                return;
            var dirs = di.GetDirectories();
            foreach (var dir in dirs)
            {
                var fullAssemblyName = Path.Combine(dir.FullName, dir.Name + ".exe");
                LoadPlugin(fullAssemblyName);
            }
            
            PluginMenu.Visibility = PluginMenu.Items.Count == 0 
                ? Visibility.Collapsed 
                : Visibility.Visible;
        }

        string _assemblyLoadFolder = "";

        internal void LoadPlugin(string fullAssemblyName)
        {
            if (!File.Exists(fullAssemblyName))
            {
                Log.ErrorFormat("Assembly not found {0}", fullAssemblyName);
                return;
            }
            Log.InfoFormat("Attempting to load: {0}", fullAssemblyName);

            var contPath = Path.GetDirectoryName(fullAssemblyName);
            _assemblyLoadFolder = contPath;

            var assembly = Assembly.LoadFile(fullAssemblyName);
            foreach (var refReq in assembly.GetReferencedAssemblies())
            {
                //check if the assembly is loaded
                var asms = AppDomain.CurrentDomain.GetAssemblies();
                var reqFound = false;
                foreach (var asmName in asms.Select(asm => asm.GetName()))
                {
                    if (asmName.FullName.Equals(refReq.FullName))
                    {
                        reqFound = true;
                        break;
                    }
                    if (asmName.Name.Equals(refReq.Name))
                    {
                        Log.WarnFormat("Versioning issues:\r\n" +
                                       "Required -> {0}\r\n" +
                                       "Loaded   -> {1}", refReq.FullName, asmName.FullName);
                    }
                }
                if (reqFound) 
                    continue;
                Log.DebugFormat("Will need to load: {0}", refReq.FullName);
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
                try
                {
                    Assembly.Load(refReq);
                }
                catch (Exception ex)
                {
                    var msg = "Problem loading assembly " + refReq + " for " + fullAssemblyName;
                    Log.ErrorFormat(msg, ex);
                    MessageBox.Show(msg + ", " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
            }
            ICollection<Type> types = new List<Type>();
            try
            {
                types = assembly.GetTypes().Where(i => i != null && typeof(UserControl).IsAssignableFrom(i)  && i.Assembly == assembly).ToList();
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach (var theType in ex.Types)
                {
                    try
                    {
                        if (theType != null && typeof(UserControl).IsAssignableFrom(theType) && theType.Assembly == assembly)
                            types.Add(theType);
                    }
                    // This exception list is not exhaustive, modify to suit any reasons
                    // you find for failure to parse a single assembly
                    catch (BadImageFormatException)
                    {
                        // Type not in this assembly - reference to elsewhere ignored
                    }
                }
            }

            foreach (var tp in types)
            {
                EvaluateXbimUiType(tp);
            }
        }

        private void EvaluateXbimUiType(Type type)
        {
            EvaluateXbimUiMenu(type);

            var act = type.GetUiActivation();
            if (act == PluginWindowActivation.OnLoad)
            {
                var instance = Activator.CreateInstance(type);
                var asPWin = instance as IXbimXplorerPluginWindow;
                if (asPWin == null)
                    return;
                if (_pluginWindows.Contains(asPWin))
                    return;
                ShowPluginWindow(asPWin);
            }           
        }

        private void EvaluateXbimUiMenu(Type type)
        {
            var att = type.GetUiAttribute();
            if (att == null) 
                return;
            if (string.IsNullOrEmpty(att.MenuText)) 
                return;
            var destMenu = PluginMenu;
            var menuHeader = type.Name;
            if (!string.IsNullOrEmpty(att.MenuText))
            {
                menuHeader = att.MenuText;    
            }
            if (att.MenuText.StartsWith(@"View/Developer/"))
            {
                menuHeader = menuHeader.Substring(15);
                destMenu = DeveloperMenu;
            }
            if (att.MenuText.StartsWith(@"File/Export/"))
            {
                menuHeader = menuHeader.Substring(12);
                destMenu = ExportMenu;
            }
            var v = new MenuItem { Header = menuHeader, Tag = type };
            destMenu.Items.Add(v);
            v.Click += OpenWindow;
        }

        private class PluginWindowCollection : ObservableCollection<IXbimXplorerPluginWindow>
        {
        }

        readonly PluginWindowCollection _pluginWindows = new PluginWindowCollection();

        Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var parts = args.Name.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            var fName = Path.Combine(_assemblyLoadFolder, parts[0] + ".exe");
            if (File.Exists(fName))
                return Assembly.LoadFile(fName);
            fName = Path.Combine(_assemblyLoadFolder, parts[0] + ".dll");
            return File.Exists(fName) 
                ? Assembly.LoadFile(fName) 
                : null;
        }
        
        private LayoutContent ShowPluginWindow(IXbimXplorerPluginWindow pluginWindow, bool setCurrent = false)
        {
            if (!(pluginWindow is UserControl))
            {
                Log.ErrorFormat("{0} does not inherit from UserControl as expected", pluginWindow.GetType());
                return null;
            }

            
            if (!_pluginWindows.Contains(pluginWindow))
                _pluginWindows.Add(pluginWindow);
            // preparing user control
            var uc = pluginWindow as UserControl;
            uc.HorizontalAlignment = HorizontalAlignment.Stretch;
            uc.VerticalAlignment = VerticalAlignment.Stretch;
            //set data binding
            pluginWindow.BindUi(MainWindow);

            switch (pluginWindow.GetUiContainerMode())
            {
                case PluginWindowUiContainerEnum.LayoutAnchorable:
                    {
                        // inner 
                        var inner = new LayoutAnchorable()
                        {
                            Title = pluginWindow.WindowTitle,
                            Content = uc
                        };
                        
                       GetRightPane().Children.Add(inner);
                        
                        if (setCurrent)
                            inner.IsActive = true;
                        return inner;
                    }
                case PluginWindowUiContainerEnum.LayoutDoc:
                default:
                    {
                        var ld = new LayoutDocument
                        {
                            Title = pluginWindow.WindowTitle,
                            Content = uc
                        };
                        MainDocPane.Children.Add(ld);
                        ld.Closed += PluginWindowClosed;
                        if (setCurrent)
                            ld.IsActive = true;
                        return ld;
                    }                  
            }
        }
        
        LayoutAnchorablePaneGroup _rightPaneGroup;

        private LayoutAnchorablePaneGroup GetRightPaneGroup()
        {
            if (_rightPaneGroup != null)
                return _rightPaneGroup;
            _rightPaneGroup = new LayoutAnchorablePaneGroup();
            _rightPaneGroup.Orientation = Orientation.Vertical;
            _rightPaneGroup.DockMinWidth = 300;
            MainPanel.Children.Add(_rightPaneGroup);
            return _rightPaneGroup;
        }

        private LayoutAnchorablePane _rightPane;

        private LayoutAnchorablePane GetRightPane()
        {
            if (_rightPane != null)
                return _rightPane;
            var rigthPanel = GetRightPaneGroup();
            _rightPane = new LayoutAnchorablePane();
            rigthPanel.Children.Add(_rightPane);
            return _rightPane;
        }


        private readonly Dictionary<Type, LayoutContent> _menuWindows = new Dictionary<Type, LayoutContent>();
        private readonly Dictionary<Type, IXbimXplorerPluginWindow> _retainedControls = new Dictionary<Type, IXbimXplorerPluginWindow>();
        
        private bool OpenOrFocusPluginWindow(Type tp)
        {
            if (!_menuWindows.ContainsKey(tp))
            {
                IXbimXplorerPluginWindow instance;
                if (_retainedControls.ContainsKey(tp))
                    instance = _retainedControls[tp];
                else
                    instance = (IXbimXplorerPluginWindow)Activator.CreateInstance(tp);

                var menuWindow = ShowPluginWindow(instance, true);
                _menuWindows.Add(tp, menuWindow);
                return true;
            }
            var v = _menuWindows[tp];
            if (v is LayoutAnchorable)
            {
                var anch = v as LayoutAnchorable;
                
                if (anch.IsHidden)
                    anch.Show();
                v.IsActive = true;
                return true;
            }

            return false;
            
        }

        private void PluginWindowClosed(object sender, EventArgs eventArgs)
        {
            IXbimXplorerPluginWindow vPlug = null;
            if (sender is LayoutDocument)
            {
                var cnt = ((LayoutDocument)sender).Content;
                vPlug = cnt as IXbimXplorerPluginWindow;
            }
            else if (sender is LayoutAnchorable)
            {

            }
            if (vPlug == null)
                return;
            var tp = vPlug.GetType();
            if (vPlug.GetUiAttribute().CloseAction == PluginWindowCloseAction.Hide && !_retainedControls.ContainsKey(tp))
                _retainedControls.Add(tp, vPlug);
            _menuWindows.Remove(tp);
        }

    }
}
