using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
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
            foreach (var window in PluginWindows)
            {
                var msging = window as IxBimXplorerPluginWindowMessaging;
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
                    Assembly.Load(refReq);
                    AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
                }
            }

            var userControls = assembly.GetTypes().Where(x => x.BaseType == typeof(UserControl));
            foreach (var tp in userControls)
            {
                Debug.WriteLine("Looping " + tp.Name);
                object instance = Activator.CreateInstance(tp);
                xBimXplorerPluginWindow asPWin = instance as xBimXplorerPluginWindow;
                if (asPWin != null)
                {
                    if (!PluginWindows.Contains(asPWin))
                    {
                        ShowPluginWindow(asPWin);
                        PluginWindows.Add(asPWin);
                    }
                }
            }

        }

        PluginWindowCollection PluginWindows = new PluginWindowCollection();

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

        private void ShowPluginWindow(xBimXplorerPluginWindow pluginWindow)
        {
            if (pluginWindow is UserControl)
            {
                // preparing user control
                UserControl uc = pluginWindow as UserControl;
                uc.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                uc.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
                //set data binding
                pluginWindow.BindUI(MainWindow);

                // add into UI
                if (pluginWindow.DefaultUIContainer == PluginWindowDefaultUIContainerEnum.LayoutDoc)
                {
                    // layout document mode
                    LayoutDocument ld = new LayoutDocument();
                    ld.Title = pluginWindow.WindowTitle;
                    ld.Content = uc;
                    MainDocPane.Children.Add(ld);
                }
                else if (pluginWindow.DefaultUIContainer == PluginWindowDefaultUIContainerEnum.LayoutAnchorable)
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
