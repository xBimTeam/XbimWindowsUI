using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Xbim.Presentation.XplorerPluginSystem;
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
        }

        string _assemblyLoadFolder = "";

        internal void LoadPlugin(string fullAssemblyName)
        {
            if (!File.Exists(fullAssemblyName))
            {
                Debug.WriteLine(string.Format("Assembly not found {0}", fullAssemblyName));
                return;
            }
            Debug.WriteLine(string.Format("Attempting to load: {0}", fullAssemblyName));

            var contPath = Path.GetDirectoryName(fullAssemblyName);
            _assemblyLoadFolder = contPath;

            var assembly = Assembly.LoadFile(fullAssemblyName);
            foreach (var refReq in assembly.GetReferencedAssemblies())
            {
                //check if the assembly is loaded
                var asms = AppDomain.CurrentDomain.GetAssemblies();
                var reqFound = false;
                foreach (var asm in asms)
                {
                    
                    var asmName = asm.GetName();
                    
                    if (asmName.FullName.Equals(refReq.FullName))
                    {
                        reqFound = true;
                        break;
                    }
                    if (asmName.Name.Equals(refReq.Name))
                    {
                        Debug.WriteLine("Versioning issues:\r\n" +
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
                    MessageBox.Show("Problem loading assembly " + refReq + " for " + fullAssemblyName + ", " + ex.Message);
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
                var instance = Activator.CreateInstance(tp);
                var asPWin = instance as IXbimXplorerPluginWindow;
                if (asPWin == null)
                    continue;
                if (_pluginWindows.Contains(asPWin))
                    continue;
                ShowPluginWindow(asPWin);
                _pluginWindows.Add(asPWin);
            }
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
                return null;
            // preparing user control
            var uc = pluginWindow as UserControl;
            uc.HorizontalAlignment = HorizontalAlignment.Stretch;
            uc.VerticalAlignment = VerticalAlignment.Stretch;
            //set data binding
            pluginWindow.BindUi(MainWindow);

            // add into UI
            switch (pluginWindow.DefaultUiContainer)
            {
                case PluginWindowDefaultUiContainerEnum.LayoutDoc:
                {
                    // layout document mode
                    var ld = new LayoutDocument
                    {
                        Title = pluginWindow.WindowTitle,
                        Content = uc
                    };
                    MainDocPane.Children.Add(ld);
                    if (setCurrent)
                        ld.IsActive = true;
                    return ld;
                    
                }
                case PluginWindowDefaultUiContainerEnum.LayoutAnchorable:
                {
                    var pg = GetRightPane();
                    var lap = new LayoutAnchorablePane();
                    pg.Children.Add(lap);
                    var ld = new LayoutAnchorable
                    {
                        Title = pluginWindow.WindowTitle,
                        Content = uc
                    };
                    lap.Children.Add(ld);
                    if (setCurrent)
                        ld.IsActive = true;
                    return ld;
                }
            }
            return null;
        }

        LayoutAnchorablePaneGroup _rightPane;
        private LayoutAnchorablePaneGroup GetRightPane()
        {
            if (_rightPane != null)
                return _rightPane;
            _rightPane = new LayoutAnchorablePaneGroup();
            _rightPane.Orientation = Orientation.Vertical;
            _rightPane.DockMinWidth = 300;
            MainPanel.Children.Add(_rightPane);
            return _rightPane;
        }

    }
}
