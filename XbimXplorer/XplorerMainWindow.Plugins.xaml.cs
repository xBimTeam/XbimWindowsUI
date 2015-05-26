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

        /// <summary>
        /// 
        /// </summary>
        public void RefreshPlugins()
        {

            //try
            //{
            //    Xbim3DModelContext context = new Xbim3DModelContext(null);
            //}
            //catch (Exception)
            //{

            //}

            string path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            if (!string.IsNullOrWhiteSpace(path))
                path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (path != null)
            {
                path = Path.Combine(path, "Plugins");

                DirectoryInfo di = new DirectoryInfo(path);
                if (!di.Exists)
                    return;
                var dirs = di.GetDirectories();
                foreach (var dir in dirs)
                {
                    string fullAssemblyName = Path.Combine(dir.FullName, dir.Name + ".exe");
                    LoadPlugin(fullAssemblyName);
                }
            }
        }

        string _assemblyLoadFolder = "";

        internal void LoadPlugin(string fullAssemblyName)
        {
            Debug.WriteLine(string.Format("Attempting to load: {0}", fullAssemblyName));
            if (!File.Exists(fullAssemblyName))
                return;
            var contPath = Path.GetDirectoryName(fullAssemblyName);
            _assemblyLoadFolder = contPath;

            var assembly = Assembly.LoadFile(fullAssemblyName);
            foreach (var refReq in assembly.GetReferencedAssemblies())
            {
                //check if the assembly is loaded
                Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
                bool reqFound = false;
                foreach (Assembly asm in asms)
                {
                    AssemblyName asmName = asm.GetName();
                    if (asmName.FullName.Equals(refReq.FullName))
                    {
                        reqFound = true;
                        break;
                    }
                    if (asmName.Name.Equals(refReq.Name))
                    {
                        Debug.WriteLine("Versioning issues: \r\na -> {0} \r\nb -> {1}", refReq.FullName, asmName.FullName);
                    }
                }
                if (!reqFound)
                {
                    Debug.WriteLine(string.Format("Will need to load: {0}", refReq.FullName));
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
            string fName = Path.Combine(_assemblyLoadFolder, parts[0] + ".exe");
            if (File.Exists(fName))
                return Assembly.LoadFile(fName);
            fName = Path.Combine(_assemblyLoadFolder, parts[0] + ".dll");
            if (File.Exists(fName))
                return Assembly.LoadFile(fName);
            return null;
        }

        private void ShowPluginWindow(IXbimXplorerPluginWindow pluginWindow)
        {
            if (pluginWindow is UserControl)
            {
                // preparing user control
                UserControl uc = pluginWindow as UserControl;
                uc.HorizontalAlignment = HorizontalAlignment.Stretch;
                uc.VerticalAlignment = VerticalAlignment.Stretch;
                //set data binding
                pluginWindow.BindUi(MainWindow);

                // add into UI
                if (pluginWindow.DefaultUiContainer == PluginWindowDefaultUiContainerEnum.LayoutDoc)
                {
                    // layout document mode
                    LayoutDocument ld = new LayoutDocument();
                    ld.Title = pluginWindow.WindowTitle;
                    ld.Content = uc;
                    MainDocPane.Children.Add(ld);
                }
                else if (pluginWindow.DefaultUiContainer == PluginWindowDefaultUiContainerEnum.LayoutAnchorable)
                {
                    LayoutAnchorablePaneGroup pg = GetRightPane();
                    LayoutAnchorablePane lap = new LayoutAnchorablePane();
                    pg.Children.Add(lap);
                    LayoutAnchorable ld = new LayoutAnchorable();
                    ld.Title = pluginWindow.WindowTitle;
                    ld.Content = uc;
                    lap.Children.Add(ld);
                }
            }
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
