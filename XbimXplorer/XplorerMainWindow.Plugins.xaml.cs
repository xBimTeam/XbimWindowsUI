using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private readonly bool _preventPluginLoad;

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

        public Visibility PluginMenuVisibility
        {
            get
            {   return PluginMenu.Items.Count == 0
                    ? Visibility.Collapsed
                    : Visibility.Visible;
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
            PluginMenu.Visibility = PluginMenuVisibility;
        }

        string _assemblyLoadFolder = "";

        internal void LoadPlugin(string fullAssemblyName)
        {
            if (!File.Exists(fullAssemblyName))
            {
                Log.ErrorFormat("Plugin loading error: Assembly not found {0}", fullAssemblyName);
                return;
            }
            Log.InfoFormat("Attempting to load plugin: {0}", fullAssemblyName);
            var contPath = Path.GetDirectoryName(fullAssemblyName);
            _assemblyLoadFolder = contPath;

            var assembly = Assembly.LoadFile(fullAssemblyName);
            var loadQueue = new Queue<AssemblyName>(assembly.GetReferencedAssemblies());

            while (loadQueue.Any())
            {
                var refReq = loadQueue.Dequeue();
            
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
                        Log.DebugFormat("Versioning issues:\r\n" +
                                       "Required -> {0}\r\n" +
                                       "Loaded   -> {1}", refReq.FullName, asmName.FullName);
                    }
                }
                if (reqFound) 
                    continue;
                
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
                try
                {
                    var reqAss = Assembly.Load(refReq);
                    Log.DebugFormat("Loaded assembly: {0}", refReq.FullName);
                    foreach (var referenced in reqAss.GetReferencedAssemblies())
                    {
                        loadQueue.Enqueue(referenced);
                    }
                }
                catch (Exception ex)
                {
                    var msg = "Problem loading assembly " + refReq + " for " + fullAssemblyName;
                    Log.ErrorFormat(msg, ex);
                    MessageBox.Show(msg + ", " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
            }
            var types = new List<Type>();
            try
            {
                types.AddRange(assembly.GetTypes()
                        .Where(i => i != null && typeof (UserControl).IsAssignableFrom(i) && i.Assembly == assembly)
                        );
                types.AddRange(assembly.GetTypes()
                        .Where(i => i != null && typeof(Window).IsAssignableFrom(i) && i.Assembly == assembly)
                        );
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
                    catch (BadImageFormatException bfe)
                    {
                        Log.Error("Plugin error, bad format exception.", bfe);
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
            if (!typeof(IXbimXplorerPluginWindow).IsAssignableFrom(type))
            {
                return;
            }
            EvaluateXbimUiMenu(type);

            var act = type.GetUiActivation();
            if (act != PluginWindowActivation.OnLoad) 
                return;
            var instance = Activator.CreateInstance(type);
            var asPWin = instance as IXbimXplorerPluginWindow;
            if (asPWin == null)
                return;
            if (_pluginWindows.Contains(asPWin))
                return;
            ShowPluginWindow(asPWin);
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
            v.Click += OpenPluginWindow;
        }

        private void OpenPluginWindow(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            if (mi == null)
                return;
            OpenOrFocusPluginWindow(mi.Tag as Type);
        }

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
        
        private object ShowPluginWindow(IXbimXplorerPluginWindow pluginWindow, bool setCurrent = false)
        {
            var aswindow = pluginWindow as Window;
            if (aswindow != null)
            {
                
                var cmode = pluginWindow.GetUiContainerMode();
                if (cmode == PluginWindowUiContainerEnum.Dialog)
                {
                    pluginWindow.BindUi(MainWindow);
                    aswindow.ShowDialog();
                    var closeAction = pluginWindow.GetUiAttribute().CloseAction;
                    if (closeAction == PluginWindowCloseAction.Hide)
                        return aswindow;
                }
                else
                {
                    Log.ErrorFormat("Plugin type {0} has unsuitable containermode ({1}).", aswindow.GetType().Name, cmode);
                }
                return null;
            }

            var asControl = pluginWindow as UserControl;
            if (asControl != null)
            {
                if (!_pluginWindows.Contains(pluginWindow))
                    _pluginWindows.Add(pluginWindow);
                // preparing user control
                asControl.HorizontalAlignment = HorizontalAlignment.Stretch;
                asControl.VerticalAlignment = VerticalAlignment.Stretch;
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
                            Content = asControl
                        };

                        GetRightPane().Children.Add(inner);

                        if (setCurrent)
                            inner.IsActive = true;
                        return inner;
                    }
                    case PluginWindowUiContainerEnum.LayoutDoc:
                    {
                        var ld = new LayoutDocument
                        {
                            Title = pluginWindow.WindowTitle,
                            Content = asControl
                        };
                        MainDocPane.Children.Add(ld);
                        ld.Closed += PluginWindowClosed;
                        if (setCurrent)
                            ld.IsActive = true;
                        return ld;
                    }
                    default:
                        Log.ErrorFormat("Plugin type {0} has unsuitable containermode.", asControl.GetType().Name);
                        break;
                }
            }
            Log.ErrorFormat("{0} does not inherit from UserControl as expected", pluginWindow.GetType());
            return null;
        }
        
        LayoutAnchorablePaneGroup _rightPaneGroup;

        private LayoutAnchorablePaneGroup GetRightPaneGroup()
        {
            if (_rightPaneGroup != null)
                return _rightPaneGroup;
            _rightPaneGroup = new LayoutAnchorablePaneGroup
            {
                Orientation = Orientation.Vertical,
                DockMinWidth = 300
            };
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

        private class SinglePluginItem
        {
            public IXbimXplorerPluginWindow PluginInterface;
            public object UiObject;
        }

        // todo: do we need both the following?
        private readonly Dictionary<Type, SinglePluginItem> _retainedControls = new Dictionary<Type, SinglePluginItem>();
        private readonly PluginWindowCollection _pluginWindows = new PluginWindowCollection();

        private class PluginWindowCollection : ObservableCollection<IXbimXplorerPluginWindow>
        {
        }
  
        private void OpenOrFocusPluginWindow(Type tp)
        {
            if (!_retainedControls.ContainsKey(tp))
            {
                IXbimXplorerPluginWindow instance;
                if (_retainedControls.ContainsKey(tp))
                    instance = _retainedControls[tp].PluginInterface;
                else
                {
                    try
                    {
                        instance = (IXbimXplorerPluginWindow) Activator.CreateInstance(tp);
                    }
                    catch (Exception ex)
                    {
                        var msg = string.Format("Error creating instance of type '{0}'", tp);
                        Log.Error(msg, ex);
                        return;
                    }
                }
                var menuWindow = ShowPluginWindow(instance, true);
                if (menuWindow == null)
                    return;
                // if returned the window must be retained.
                var i = new SinglePluginItem()
                {
                    PluginInterface = instance,
                    UiObject = menuWindow
                };
                _retainedControls.Add(tp, i);
                return;
            }
            var v = _retainedControls[tp];
            var anchorable = v.UiObject as LayoutAnchorable;
            if (anchorable != null)
            {
                if (anchorable.IsHidden)
                    anchorable.Show();
                anchorable.IsActive = true;
                return;
            }
            //if (v.UiObject is Window)
            //{
            //    // ShowPluginWindow(v, true);
            //}
            return;
        }

        private void PluginWindowClosed(object sender, EventArgs eventArgs)
        {
            IXbimXplorerPluginWindow vPlug = null;
            if (sender is LayoutAnchorable)
            {
                // nothing to do here, window is only hidden
                return;
            }
            // here we find the associated plugin item
            if (sender is LayoutDocument)
            {
                var cnt = ((LayoutDocument)sender).Content;
                vPlug = cnt as IXbimXplorerPluginWindow;
            }
            else if (sender is Window)
            {
                var cnt = (Window)sender;
                vPlug = cnt as IXbimXplorerPluginWindow;
            }
            if (vPlug == null)
                return;
            var tp = vPlug.GetType();
            var closeAction = vPlug.GetUiAttribute().CloseAction;

            if (closeAction == PluginWindowCloseAction.Close && _retainedControls.ContainsKey(tp) )
            {
                _retainedControls.Remove(tp);
            }
        }
    }
}
