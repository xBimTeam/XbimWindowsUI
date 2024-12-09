using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using NuGet;
using Xbim.Presentation;
using Xbim.Presentation.XplorerPluginSystem;
using XbimXplorer.PluginSystem;

using Image = System.Windows.Controls.Image;
using Microsoft.Extensions.Logging;
using AvalonDock.Layout;
using NuGet.Packaging;

namespace XbimXplorer
{
	public partial class XplorerMainWindow
	{
		internal readonly bool PreventPluginLoad = false;

		public void BroadCastMessage(object sender, string messageTypeString, object messageData)
		{
			foreach (var window in _pluginWindows)
			{
				var msging = window as IXbimXplorerPluginMessageReceiver;
				msging?.ProcessMessage(sender, messageTypeString, messageData);
			}
		}

		public Visibility PluginMenuVisibility =>
			PluginMenu.Items.Count == 0
				? Visibility.Collapsed
				: Visibility.Visible;

		public void RefreshPlugins()
		{
			var dirs = PluginManagement.GetPluginDirectories();
			foreach (var dir in dirs)
			{
				// evaluate the loading of the plugin from a folder
				LoadPlugin(dir, false);
			}
			PluginMenu.Visibility = PluginMenuVisibility;
		}

		private string _assemblyLoadFolder = "";

		/// <summary>
		/// key is ManifestMetadata.Id, data is ManifestMetadata
		/// </summary>
		private readonly Dictionary<string, ManifestMetadata> _loadedPlugins =
			new Dictionary<string, ManifestMetadata>();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dir"></param>
		/// <param name="forceLoad"></param>
		/// <param name="fileName"></param>
		/// <returns>True if plugin is completely loaded. False if not, for any reason.</returns>
		internal bool LoadPlugin(DirectoryInfo dir, bool forceLoad, string fileName = null)
		{
			var mfst = PluginManagement.GetManifestMetadata(dir);
			if (_loadedPlugins.ContainsKey(mfst.Id))
			{
				Logger.LogWarning("Re-load of previously loaded plugin {pluginId} aborted.", mfst.Id);
				return false;
			}
			var conf = PluginManagement.GetConfiguration(dir);
			if (!forceLoad) // if don't have to load forcedly
			{
				// check startup setting
				//
				if (conf?.OnStartup != PluginConfiguration.PluginFlag.Enabled)
					return false;
			}
			var logLoadingDiagnostics = conf?.LoadingDiagnostics == PluginConfiguration.PluginFlag.Enabled;
			var pluginAssemblyFullFileName = PluginManagement.GetEntryFile(dir, fileName);
			if (!File.Exists(pluginAssemblyFullFileName))
			{
				Logger.LogError("Plugin loading error: Assembly file not found {pluginName}", pluginAssemblyFullFileName);
				return false;
			}
			Logger.LogInformation("Attempting to load plugin: {pluginName}", pluginAssemblyFullFileName);
			_assemblyLoadFolder = dir.FullName;

			var pluginAssembly = LoadAssembly(pluginAssemblyFullFileName);
			if (pluginAssembly == null)
				return false;
			_loadedPlugins.Add(mfst.Id, mfst);
			_pluginAssemblies.Add(pluginAssembly);

			if (logLoadingDiagnostics)
			{
				var requiringAssemblies = new Dictionary<string, List<Assembly>>(); // this helps debug invalid dependencies
				var init = pluginAssembly.GetReferencedAssemblies().ToList();
				foreach (var item in init)
				{
					var pluginList = new List<Assembly>(new[] { pluginAssembly });
					requiringAssemblies.Add(item.FullName, pluginList);
				}
				var loadQueue = new Queue<AssemblyName>(init);
				while (loadQueue.Any())
				{
					var referencedRequirement = loadQueue.Dequeue();

					//check if the assembly is loaded

					var currentDomainAssemblies = AppDomain.CurrentDomain.GetAssemblies();
					var reqFound = false;
					foreach (var currentDomainAssemblyName in currentDomainAssemblies.Select(asm => asm.GetName()))
					{
						if (currentDomainAssemblyName.FullName.Equals(referencedRequirement.FullName))
						{
							reqFound = true;
							break;
						}
						if (currentDomainAssemblyName.Name.Equals(referencedRequirement.Name) && currentDomainAssemblyName.Name != "netstandard")
						{
							Logger.LogWarning($"Incompatible plugin components identified:" +
											"Plugin requires -> {required}. " +
											"But currently loaded -> {loaded}", referencedRequirement.FullName, currentDomainAssemblyName.FullName);
						}
					}
					if (reqFound)
						continue;

					try
					{
						AppDomain.CurrentDomain.AssemblyResolve += PluginAssemblyResolvingFunction;
						var loadedReqAss = Assembly.Load(referencedRequirement);
						if (!_pluginAssemblies.Contains(loadedReqAss))
							_pluginAssemblies.Add(loadedReqAss);
						Logger.LogDebug("Loaded assembly: {assembly}", referencedRequirement.FullName);
						foreach (var referenced in loadedReqAss.GetReferencedAssemblies())
						{
							if (requiringAssemblies.ContainsKey(referenced.FullName))
							{
								requiringAssemblies[referenced.FullName].Add(loadedReqAss);
							}
							else
							{
								requiringAssemblies.Add(
									referenced.FullName,
									new List<Assembly>(new[] { loadedReqAss })
									);
								loadQueue.Enqueue(referenced);
							}

						}
					}
					catch (FileNotFoundException ex)
					{
						Logger.LogWarning(0, ex, "Failed to load {file} required by {assembly}", ex.FileName, referencedRequirement);
					}
					catch (Exception ex)
					{
						var referencingAssemblies = string.Join(", ", requiringAssemblies[referencedRequirement.FullName].Select(x => x.FullName).ToArray());
						Logger.LogError(0, ex, "Exception loading assembly {required} required by {assembly}", referencedRequirement, referencingAssemblies);
						var msg = "Problem loading assembly " + referencedRequirement + " required by " + referencingAssemblies;
						MessageBox.Show(msg + "\r\n\r\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

					}
					finally
					{

						AppDomain.CurrentDomain.AssemblyResolve -= PluginAssemblyResolvingFunction;
					}
				}
			}
			var types = new List<Type>();
			try
			{
				types.AddRange(pluginAssembly.GetTypes()
						.Where(i => i != null && typeof(UserControl).IsAssignableFrom(i) && i.Assembly == pluginAssembly)
				);
				types.AddRange(pluginAssembly.GetTypes()
						.Where(i => i != null && typeof(Window).IsAssignableFrom(i) && i.Assembly == pluginAssembly)
				);
			}
			catch (ReflectionTypeLoadException ex)
			{
				foreach (var theType in ex.Types)
				{
					try
					{
						if (theType != null && typeof(UserControl).IsAssignableFrom(theType) &&
							theType.Assembly == pluginAssembly)
							types.Add(theType);
					}
					// This exception list is not exhaustive, modify to suit any reasons
					// you find for failure to parse a single assembly
					catch (BadImageFormatException bfe)
					{
						Logger.LogError(0, bfe, "Plugin error, bad format exception.");
					}
				}
			}
			// set UI visibility
			try
			{
				foreach (var tp in types)
				{
					EvaluateXbimUiType(tp, false);
				}
			}
			catch (Exception ex)
			{
				Logger.LogError(0, ex, "Error activating plugin {plugin}; startup mode set to 'Disabled'.", mfst.Id);
				PluginManagement.SetStartup(dir, PluginConfiguration.PluginFlag.Disabled);
				PluginMenu.Visibility = PluginMenuVisibility;
				return false;
			}
			PluginMenu.Visibility = PluginMenuVisibility;
			return true;
		}

		/// <summary>
		/// key is assembly full name, value is path to file
		/// </summary>
		private readonly Dictionary<string, string> _assemblyLocations = new Dictionary<string, string>();

		/// <summary>
		/// loads an assembly from a file.
		/// Loading happens through a memory stream to allow file deletion, while retaining the information of its location for the plugin API.
		/// </summary>
		/// <param name="fullAssemblyPath">Full path of the assembly to load.</param>
		/// <returns>the assembly or null in case of failure</returns>
		private Assembly LoadAssembly(string fullAssemblyPath)
		{
			// the use of Assembly.Load(File.ReadAllBytes(assemblypath)) is introduced to allow plugin files to be deleted.
			// this is required for the plugin update feature 
			//
			var loaded = Assembly.Load(File.ReadAllBytes(fullAssemblyPath));
			if (!_assemblyLocations.ContainsKey(loaded.FullName))
			{
				_assemblyLocations.Add(loaded.FullName, fullAssemblyPath);
			}
			return loaded;
		}

		private void EvaluateXbimUiType(Type type, bool InsertAtTopOfMenu)
		{
			if (!typeof(IXbimXplorerPluginWindow).IsAssignableFrom(type))
			{
				return;
			}
			EvaluateXbimUiMenu(type, InsertAtTopOfMenu);

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

		private void EvaluateXbimUiMenu(Type type, bool InsertAtTopOfMenu)
		{
			var att = type.GetUiAttribute();
			if (string.IsNullOrEmpty(att?.MenuText))
				return;
			Logger.LogDebug($"Adding Menu: {att.MenuText}");
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
			if (att.IconPath != "")
			{
				try
				{
					var aname = type.Assembly.GetName().Name;
					var str = $"pack://application:,,,/{aname};component/{att.IconPath}";
					var bi = new BitmapImage(new Uri(str, UriKind.Absolute));
					var i = new Image() { Source = bi };
					v.Icon = i;
				}
				catch (Exception ex)
				{
					Logger.LogError(0, ex, "Path {iconPath} not found when loading icon.", att.IconPath);
				}
			}
			if (InsertAtTopOfMenu)
			{
				destMenu.Items.Insert(0, v);
			}
			else
			{
				destMenu.Items.Add(v);
			}
			v.Click += OpenPluginWindow;

		}

		private void OpenPluginWindow(object sender, RoutedEventArgs e)
		{
			var mi = sender as MenuItem;
			if (mi == null)
				return;
			OpenOrFocusPluginWindow(mi.Tag as Type);
		}

		private Assembly PluginAssemblyResolvingFunction(object sender, ResolveEventArgs args)
		{
			var parts = args.Name.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
			var fName = Path.Combine(_assemblyLoadFolder, parts[0] + ".exe");
			if (File.Exists(fName))
			{
				return LoadAssembly(fName);
			}
			fName = Path.Combine(_assemblyLoadFolder, parts[0] + ".dll");
			return File.Exists(fName)
				? LoadAssembly(fName)
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
					Logger.LogError("Plugin type {pluginType} has unsuitable containermode {containerMode}.",
						aswindow.GetType().Name, cmode);
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
							var pane = GetRightPane();
							pane.Children.Add(inner);
							inner.Closed += PluginWindowClosed;
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
						Logger.LogError("Plugin type {pluginType} has unsuitable containermode.", asControl.GetType().Name);
						break;
				}
			}
			Logger.LogError("{pluginWindow} does not inherit from UserControl as expected", pluginWindow.GetType());
			return null;
		}

		LayoutAnchorablePaneGroup _rightPaneGroup;

		private LayoutAnchorablePaneGroup GetRightPaneGroup()
		{
			if (_rightPaneGroup != null && _rightPaneGroup.IsVisible)
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
			if (_rightPane != null && _rightPane.IsVisible)
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
		private readonly List<Assembly> _pluginAssemblies = new List<Assembly>();

		private class PluginWindowCollection : ObservableCollection<IXbimXplorerPluginWindow>
		{
		}

		private object OpenOrFocusPluginWindow(Type tp)
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
						instance = (IXbimXplorerPluginWindow)Activator.CreateInstance(tp);
					}
					catch (Exception ex)
					{
						var msg = $"Error creating instance of type '{tp}'";
						Logger.LogError(0, ex, "Error creating instance of type '{type}'", tp);
						return null;
					}
				}
				var menuWindow = ShowPluginWindow(instance, true);
				if (menuWindow == null)
					return null;
				// if returned the window must be retained.
				var i = new SinglePluginItem()
				{
					PluginInterface = instance,
					UiObject = menuWindow
				};
				_retainedControls.Add(tp, i);
				return instance;
			}
			var v = _retainedControls[tp];
			var anchorable = v.UiObject as LayoutAnchorable;
			if (anchorable == null)
				return null;
			if (anchorable.IsHidden)
				anchorable.Show();
			if (!anchorable.IsVisible)
			{
				GetRightPane().Children.Add(anchorable);
			}
			anchorable.IsActive = true;

			return anchorable.Content;
		}

		private void PluginWindowClosed(object sender, EventArgs eventArgs)
		{
			IXbimXplorerPluginWindow vPlug = null;
			if (sender is LayoutAnchorable)
			{
				// if it get here it is because the anchorable has been moved to a dockedDocument and then closed
				//
				var cnt = ((LayoutAnchorable)sender).Content;
				vPlug = cnt as IXbimXplorerPluginWindow;
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
			if (closeAction == PluginWindowCloseAction.Close && _retainedControls.ContainsKey(tp))
			{
				_retainedControls.Remove(tp);
			}
		}

		public string GetAssemblyLocation(Assembly requestingAssembly)
		{
			return _assemblyLocations.ContainsKey(requestingAssembly.FullName)
				? _assemblyLocations[requestingAssembly.FullName]
				: null;
		}

		public string GetLoadedVersion(string pluginId)
		{
			return _loadedPlugins.ContainsKey(pluginId)
				? _loadedPlugins[pluginId].Version.ToString()
				: null;
		}
	}
}
