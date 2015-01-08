using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Controls;
using XbimXplorer.PluginSystem;
using Xceed.Wpf.AvalonDock.Layout;

namespace XbimXplorer
{
    public partial class XplorerMainWindow
    {
        public void BroadCastMessage(object Sender, string MessageTypeString, object MessageData)
        {
            foreach (var window in PluginWindows)
            {
                IxBimXplorerPluginWindowMessaging msging = window as IxBimXplorerPluginWindowMessaging;
                if (msging != null)
                {
                    msging.ProcessMessage(Sender, MessageTypeString, MessageData);
                }
            }
        }

        public void RefreshPlugins()
        {
            string path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            if (!string.IsNullOrWhiteSpace(path))
                path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
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

        string AssemblyLoadFolder = "";

        internal void LoadPlugin(string fullAssemblyName)
        {
            Debug.WriteLine(string.Format("Attempting to load: {0}", fullAssemblyName));
            if (!File.Exists(fullAssemblyName))
                return;
            var ContPath = Path.GetDirectoryName(fullAssemblyName);
            AssemblyLoadFolder = ContPath;
            try
            {
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
                        else if (asmName.Name.Equals(refReq.Name))
                        {
                            Debug.WriteLine(string.Format("Versioning issues: \r\na -> {0} \r\nb -> {1}", refReq.FullName, asmName.FullName));
                        }
                    };
                    if (!reqFound)
                    {
                        Debug.WriteLine(string.Format("Will need to load: {0}", refReq.FullName));
                        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
                        Assembly.Load(refReq);
                        AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
                    }
                }

                var UserControls = assembly.GetTypes().Where(x => x.BaseType == typeof(UserControl));
                foreach (var tp in UserControls)
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
            catch (Exception ex)
            {

            }
        }

        PluginWindowCollection PluginWindows = new PluginWindowCollection();

        Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var parts = args.Name.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            string fName = Path.Combine(AssemblyLoadFolder, parts[0] + ".exe");
            if (File.Exists(fName))
                return Assembly.LoadFile(fName);
            fName = Path.Combine(AssemblyLoadFolder, parts[0] + ".dll");
            if (File.Exists(fName))
                return Assembly.LoadFile(fName);
            return null;
        }

        private void ShowPluginWindow(xBimXplorerPluginWindow PluginWindow)
        {
            if (PluginWindow is UserControl)
            {
                // preparing user control
                UserControl uc = PluginWindow as UserControl;
                uc.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                uc.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
                //set data binding
                PluginWindow.BindUI(MainWindow);

                // add into UI
                if (PluginWindow.DefaultUIContainer == PluginWindowDefaultUIContainerEnum.LayoutDoc)
                {
                    // layout document mode
                    LayoutDocument ld = new LayoutDocument();
                    ld.Title = PluginWindow.WindowTitle;
                    ld.Content = uc;
                    MainDocPane.Children.Add(ld);
                }
                else if (PluginWindow.DefaultUIContainer == PluginWindowDefaultUIContainerEnum.LayoutAnchorable)
                {
                    LayoutAnchorablePaneGroup pg = GetRightPane();
                    LayoutAnchorablePane lap = new LayoutAnchorablePane();
                    pg.Children.Add(lap);
                    LayoutAnchorable ld = new LayoutAnchorable();
                    ld.Title = PluginWindow.WindowTitle;
                    ld.Content = uc;
                    lap.Children.Add(ld);
                }
            }
        }

        LayoutAnchorablePaneGroup _rightPane = null;
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
