using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using Xbim.Common;
using Xbim.Common.Enumerations;
using Xbim.Common.Geometry;
using Xbim.Common.Metadata;
using Xbim.Geometry.Engine.Interop;
using Xbim.Presentation;
using Xbim.Presentation.XplorerPluginSystem;
using XbimXplorer.Simplify;
using Xbim.Ifc;
using Xbim.Ifc.Validation;
using Xbim.Ifc4.Interfaces;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using Xbim.Presentation.LayerStyling;
using Binding = System.Windows.Data.Binding;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Microsoft.CSharp;
using System.CodeDom;
using XbimXplorer.PluginSystem;
using Microsoft.Extensions.Logging;
using Xbim.Common.ExpressValidation;

// todo: see if gemini is a good candidate for a network based ui experience in xbim.
// https://github.com/tgjones/gemini
//

namespace XbimXplorer.Commands
{
    /// <summary>
    /// Interaction logic for wdwQuery.xaml
    /// </summary>
    [XplorerUiElement(PluginWindowUiContainerEnum.LayoutAnchorable, PluginWindowActivation.OnMenu, 
         "View/Developer/Commands", "Commands/console.bmp")]
    public partial class wdwCommands : IXbimXplorerPluginWindow
    {
        protected ILogger Logger { get; private set; }

        /// <summary>
        /// WindowsUI
        /// </summary>
        public wdwCommands()
        {
            InitializeComponent();
            Logger = XplorerMainWindow.LoggerFactory.CreateLogger<wdwCommands>();
            DisplayHelp();
#if DEBUG
            // loads the last commands stored
            var fname = Path.Combine(Path.GetTempPath(), "xbimquerying.txt");
            if (!File.Exists(fname))
                return;
            using (var reader = File.OpenText(fname))
            {
                var read = reader.ReadToEnd();
                TxtCommand.Text = read;
            }
#endif
        }

        private IXbimXplorerPluginMasterWindow _parentWindow;

        private bool _bDoClear = true;

        /// <summary>
        /// Returns all types in the current AppDomain implementing the interface or inheriting the type. 
        /// </summary>
        public static IEnumerable<Type> TypesImplementingInterface(Type desiredType)
        {
            return AppDomain
                .CurrentDomain
                .GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(desiredType.IsAssignableFrom);
        }

        private static bool IsRealClass(Type testType)
        {
            return testType.IsAbstract == false
                   && testType.IsGenericTypeDefinition == false
                   && testType.IsInterface == false;
        }

        private void txtCommand_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl)))
                return;
            Execute();
            e.Handled = true;
        }

        private void Execute()
        {
#if DEBUG
            // stores the commands being launched
            var fname = Path.Combine(Path.GetTempPath(), "xbimquerying.txt");
            using (var writer = File.CreateText(fname))
            {
                writer.Write(TxtCommand.Text);
                writer.Flush();
                writer.Close();
            }
#endif
            if (_bDoClear)
                TxtOut.Document = new FlowDocument();

            var commandArray = TxtCommand.Text.Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries);
            if (TxtCommand.SelectedText != string.Empty)
                commandArray = TxtCommand.SelectedText.Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries);

            foreach (var cmdF in commandArray)
            {
                ReportAdd("> " + cmdF, Brushes.ForestGreen);
                var cmd = cmdF;
                var i = cmd.IndexOf("//", StringComparison.Ordinal);
                if (i > 0)
                {
                    cmd = cmd.Substring(0, i);
                }
                if (cmd.TrimStart().StartsWith("//"))
                    continue;

                // put here all commands that don't require a database open
                var mdbclosed = Regex.Match(cmd, @"^help$", RegexOptions.IgnoreCase);
                if (mdbclosed.Success)
                {
                    DisplayHelp();
                    continue;
                }

                mdbclosed = Regex.Match(cmd, @"^Log *(?<count>[\d]+)? *(?<message>.+)?$", RegexOptions.IgnoreCase);
                if (mdbclosed.Success)
                {
                    int iCnt;
                    if (!Int32.TryParse(mdbclosed.Groups["count"].Value, out iCnt))
                        iCnt = 1;
                    var msg = (string.IsNullOrEmpty(mdbclosed.Groups["message"].Value)) 
                        ? "Message"
                        : mdbclosed.Groups["message"].Value;

                    for (int iLoop = 0; iLoop < iCnt; iLoop++)
                    {
                        Logger.LogInformation(iLoop+1 + " " + msg);
                    }
                    continue;
                }
                
                mdbclosed = Regex.Match(cmd, @"^IfcZip (?<source>[^/]+) *(?<subFolders>/s)?$", RegexOptions.IgnoreCase);
                if (mdbclosed.Success)
                {
                    var source = mdbclosed.Groups["source"].Value.Trim();
                    var subfolders = !string.IsNullOrEmpty(mdbclosed.Groups["subFolders"].Value);

                    if (File.Exists(source))
                    {
                        IfcZipAndDelete(source);
                    }
                    if (Directory.Exists(source))
                    {
                        IfcZipAndDelete(source, subfolders);
                    }
                    continue;
                }
                
                mdbclosed = Regex.Match(cmd, @"^xplorer$", RegexOptions.IgnoreCase);
                if (mdbclosed.Success)
                {
                    if (_parentWindow != null)
                        _parentWindow.Focus();
                    else
                    {
                        // todo: bonghi: open the model in xplorer if needed.
                        var xp = new XplorerMainWindow();
                        _parentWindow = xp;
                        xp.Show();
                    }
                    continue;
                }

                mdbclosed = Regex.Match(cmd, @"^versions$", RegexOptions.IgnoreCase);
                if (mdbclosed.Success)
                {
                    var asms = AppDomain.CurrentDomain.GetAssemblies();
                    ReportAdd("List of loaded assemblies:", Brushes.Black);
                    foreach (var asm in asms)
                    {
                        var asmName = asm.GetName();
                        ReportAdd($" - {asmName.FullName}", Brushes.Black);
                    }
                    continue;
                }

                mdbclosed = Regex.Match(cmd, @"clear *\b(?<mode>(on|off))*", RegexOptions.IgnoreCase);
                if (mdbclosed.Success)
                {
                    try
                    {
                        var option = mdbclosed.Groups["mode"].Value;

                        if (option == "")
                        {
                            TxtOut.Document = new FlowDocument();
                            continue;
                        }
                        if (option == "on")
                            _bDoClear = true;
                        else if (option == "off")
                            _bDoClear = false;
                        else
                        {
                            ReportAdd($"Autoclear not changed ({option} is not a valid option).");
                            continue;
                        }
                        ReportAdd($"Autoclear set to {option.ToLower()}");
                        continue;
                    }
                    catch
                    {
                    }
                    TxtOut.Document = new FlowDocument();
                    continue;
                }



                mdbclosed = Regex.Match(cmd, @"^SimplifyGUI$", RegexOptions.IgnoreCase);
                if (mdbclosed.Success)
                {
                    var s = new IfcSimplify();
                    s.Show();
                    continue;
                }

                mdbclosed = Regex.Match(cmd, @"^(IfcSchema|is) (?<mode>(list|count|short|full) )*(?<type>\w+)[ ]*",
                    RegexOptions.IgnoreCase);
                if (mdbclosed.Success)
                {
                    var typeString = mdbclosed.Groups["type"].Value;
                    var mode = mdbclosed.Groups["mode"].Value;

                    if (typeString == "/")
                    {
                        // this is a magic case handled by the matchingType
                    }
                    else if (typeString == PrepareRegex(typeString))
                    // there's not a regex expression, we will prepare one assuming the search for a bare name.
                    {
                        typeString = @".*\." + typeString + "$";
                        // any character repeated then a dot then the name and the end of line
                    }
                    else
                        typeString = PrepareRegex(typeString);

                    var typeList = MatchingTypes(typeString);


                    if (mode.ToLower() == "list ")
                    {
                        foreach (var item in typeList)
                            ReportAdd(item);
                    }
                    else if (mode.ToLower() == "count ")
                    {
                        ReportAdd("count: " + typeList.Count());
                    }
                    else
                    {
                        // report
                        var beVerbose = 1;
                        if (mode.ToLower() == "short ")
                            beVerbose = 0;
                        if (mode.ToLower() == "full ")
                            beVerbose = 2;
                        foreach (var item in typeList)
                        {
                            ReportAdd(ReportType(item, beVerbose));
                        }
                    }
                    continue;
                }

                mdbclosed = Regex.Match(cmd, @"^(plugin|plugins) ((?<command>install|refresh|load|list|folder|update) *)*(?<pluginName>[^ ]+)*[ ]*", RegexOptions.IgnoreCase);
                if (mdbclosed.Success)
                {
                    var commandString = mdbclosed.Groups["command"].Value;
                    var pluginName = mdbclosed.Groups["pluginName"].Value;
                    if (commandString.ToLower() == "refresh")
                    {
                        _parentWindow?.RefreshPlugins();
                        continue;
                    }
                    else if (commandString.ToLower() == "folder")
                    {
                        // open folder
                        var dir = PluginManagement.GetPluginsDirectory();
                        ReportAdd($"Plugins folder is: {dir.FullName}");
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                        {
                            FileName = dir.FullName,
                            UseShellExecute = true,
                            Verb = "open"
                        });
                        continue;
                    }
                    else if (commandString.ToLower() == "install" || commandString.ToLower() == "update")
                    {
                        var pm = new PluginManagement();
                        var plugin = pm.GetPlugins(PluginChannelOption.Development).FirstOrDefault(x => x.PluginId == pluginName);
                        if (plugin == null)
                        {
                            ReportAdd("Plugin not found.", Brushes.Red);
                            continue;
                        }
                        
                        // test for the right command string
                        if (plugin.InstalledVersion != "" 
                            && commandString.ToLower() == "install")
                        {
                            ReportAdd($"The plugin is already installed, use the 'plugin update {pluginName}' command instead.", Brushes.Red);
                            continue;
                        }

                        // try installing
                        ReportAdd("Plugin found; installing...", Brushes.Blue);
                        var extracted = plugin.ExtractPlugin(PluginManagement.GetPluginsDirectory());
                        if (!extracted)
                        {
                            ReportAdd("Plugin extraction failed.", Brushes.Red);
                        }
                        if (plugin.Startup.OnStartup == PluginConfiguration.StartupBehaviour.Disabled)
                        {
                            plugin.ToggleEnabled();
                        }

                        // try loading
                        var loaded = plugin.Load();
                        if (loaded)
                            ReportAdd("Installed and loaded.", Brushes.Blue);
                        else
                            ReportAdd("Plugin installed, but a restart is required.", Brushes.Red);
                        continue;
                    }
                    else if (commandString.ToLower() == "load")
                    {
                        if (Directory.Exists(pluginName))
                        {
                            var pluginDir = new DirectoryInfo(pluginName);
                            (_parentWindow as XplorerMainWindow)?.LoadPlugin(pluginDir, true);
                        }
                        else
                        {
                            ReportAdd("Plugin not found.", Brushes.Red);
                        }
                        continue;
                    }
                    else if (commandString.ToLower() == "list")
                    {
                        PluginManagement pm = new PluginManagement();
                        var plugins = pm.GetPlugins(PluginChannelOption.Development).ToList();
                        if (plugins.Any())
                        {
                            ReportAdd("Beta versions in the development channel:");
                            foreach (var plugin in plugins)
                            {
                                ReportAdd($" - {plugin.PluginId} Available: {plugin.AvailableVersion} Installed: {plugin.InstalledVersion} Loaded: {plugin.LoadedVersion}");
                            }
                        }
                        plugins = pm.GetPlugins(PluginChannelOption.Stable).ToList();
                        if (plugins.Any())
                        {
                            ReportAdd("Versions in the stable channel:");
                            foreach (var plugin in plugins)
                            {
                                ReportAdd($" - {plugin.PluginId} Available: {plugin.AvailableVersion} Installed: {plugin.InstalledVersion} Loaded: {plugin.LoadedVersion}");
                            }
                        }
                        continue;
                    }
                }

                // above here functions that do not need an opened model
                // #####################################################
                


                // all commands here
                //
                var m = Regex.Match(cmd, @"^(entitylabel|el) (?<el>\d+)(?<recursion> -*\d+)*",
                    RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    if (ModelIsUnavailable) continue;

                    var recursion = 0;
                    var v = Convert.ToInt32(m.Groups["el"].Value);
                    try
                    {
                        recursion = Convert.ToInt32(m.Groups["recursion"].Value);
                    }
                    catch
                    {
                        // ignored
                    }

                    ReportAdd(ReportEntity(v, recursion));
                    continue;
                }

                m = Regex.Match(cmd, @"^(TypeReport|tr)$", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    if (ModelIsUnavailable) continue;

                    ReportAdd("========== TypeReport for " + Model.FileName, Brushes.Blue);
                    ReportAdd("");
                    // very low efficiency, just to have it quick and dirty.
                    var td = new Dictionary<ExpressType, int>();

                    foreach (var modelInstance in Model.Instances)
                    {
                        var t = modelInstance.ExpressType;
                        if (td.ContainsKey(t))
                        {
                            td[t] += 1;
                        }
                        else
                        {
                            td.Add(t, 1);
                        }
                    }

                    var keys = td.Keys.ToList();
                    keys.Sort( // sort inverted
                            (x1, x2) => td[x2].CompareTo(td[x1])
                        );

                    foreach (var key in keys)
                    {
                        var b = Brushes.Black;
                        if (typeof(IIfcElement).IsAssignableFrom(key.Type))
                            b = Brushes.Blue;
                        else if (typeof(IIfcProduct).IsAssignableFrom(key.Type))
                            b = Brushes.BlueViolet;
                        else if (typeof(IIfcRepresentationItem).IsAssignableFrom(key.Type))
                            b = Brushes.ForestGreen;
                        else if (typeof(IIfcRelationship).IsAssignableFrom(key.Type))
                            b = Brushes.DarkOrange;
                        
                        ReportAdd($"{td[key]}\t\t{key.Name}", b);
                    }
                    continue;
                }

                m = Regex.Match(cmd, @"^(Header|he)$", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    if (ModelIsUnavailable) continue;

                    if (Model.Header == null)
                    {
                        ReportAdd("Model header is not defined.", Brushes.Red);
                    }
                    else
                    {
                        ReportAdd("FileDescription:");
                        foreach (var item in Model.Header.FileDescription.Description)
                            ReportAdd($"- Description: {item}");
                        ReportAdd($"- ImplementationLevel: {Model.Header.FileDescription.ImplementationLevel}");
                        ReportAdd($"- EntityCount: {Model.Header.FileDescription.EntityCount}");

                        ReportAdd("FileName:");
                        ReportAdd($"- Name: {Model.Header.FileName.Name}");
                        ReportAdd($"- TimeStamp: {Model.Header.FileName.TimeStamp}");
                        foreach (var item in Model.Header.FileName.Organization)
                            ReportAdd($"- Organization: {item}");
                        ReportAdd($"- OriginatingSystem: {Model.Header.FileName.OriginatingSystem}");
                        ReportAdd($"- PreprocessorVersion: {Model.Header.FileName.PreprocessorVersion}");
                        foreach (var item in Model.Header.FileName.AuthorName)
                            ReportAdd($"- AuthorName: {item}");
                        
                        ReportAdd($"- AuthorizationName: {Model.Header.FileName.AuthorizationName}");
                        foreach (var item in Model.Header.FileName.AuthorizationMailingAddress)
                            ReportAdd($"- AuthorizationMailingAddress: {item}");

                        ReportAdd("FileSchema:");
                        foreach (var item in Model.Header.FileSchema.Schemas)
                            ReportAdd($"- Schema: {item}");
                    }

                    ReportAdd($"Modelfactors:");
                    ReportAdd($"- OneMeter: {Model.ModelFactors.OneMetre}");
                    
                    continue;
                }

                // SelectionHighlighting [WholeMesh|Normals]
                m = Regex.Match(cmd, @"^(SelectionHighlighting|sh) (?<mode>(wholemesh|normals|wireframe))+",
                    RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    if (ModelIsUnavailable) continue;

                    var mode = m.Groups["mode"].Value.ToLowerInvariant();
                    if (mode == "normals")
                    {
                        ReportAdd("Selection visual style set to 'Normals'");
                        _parentWindow.DrawingControl.SelectionHighlightMode =
                            DrawingControl3D.SelectionHighlightModes.Normals;
                    }
                    else if (mode == "wholemesh")
                    {
                        ReportAdd("Selection visual style set to 'WholeMesh'");
                        _parentWindow.DrawingControl.SelectionHighlightMode =
                            DrawingControl3D.SelectionHighlightModes.WholeMesh;
                    }
                    else if (mode == "wireframe")
                    {
                        ReportAdd("Selection visual style set to 'WireFrame'");
                        _parentWindow.DrawingControl.SelectionHighlightMode =
                            DrawingControl3D.SelectionHighlightModes.WireFrame;
                    }
                    continue;
                }

                // we intend to offer estraction of breps here.
                m = Regex.Match(cmd, @"^(brep|br\b) *(?<entities>([\d,]+|[^ ]+))", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    FileInfo fi = new FileInfo(Model.FileName);
                    var dirName = fi.DirectoryName;
                    XbimPlacementTree pt = new XbimPlacementTree(Model, App.ContextWcsAdjustment);
                    // add "DBRep_DrawableShape" as first line
                    var start = m.Groups["entities"].Value;
                    IEnumerable<int> labels = ToIntarray(start, ',');
                    if (labels.Any())
                    {
                        foreach (int label in labels)
                        {
                            bool firstWrite = true;
                            string prevSol = "";
                            var entity = Model.Instances[label];
                            if (entity == null)
                                continue;

                            var entities = new List<IPersistEntity>() { entity };
                            // todo: what to do with subtractionElements?
                            XbimMatrix3D trsf = XbimMatrix3D.Identity;
                            if (entity is IIfcProduct)
                            {
                                var prod = (IIfcProduct)entity;
                                trsf = XbimPlacementTree.GetTransform(prod, pt, new XbimGeometryEngine());
                                entities.Clear();
                                entities.AddRange(prod.Representation?.Representations.SelectMany(x=>x.Items));
                            }
                            else if (entity is IIfcRelVoidsElement)
                            {
                                var prod = ((IIfcRelVoidsElement)entity).RelatedOpeningElement;
                                trsf = XbimPlacementTree.GetTransform(prod, pt, new XbimGeometryEngine());
                                entities.Clear();
                                entities.AddRange(prod.Representation?.Representations.SelectMany(x => x.Items));
                            }
                            foreach (var solEntity in entities)
                            {
                                var sols = GetSolids(solEntity);
                                foreach (var item in sols)
                                {
                                    int iCnt = 0;
                                    foreach (var solid in item.Item2)
                                    {
                                        if (solid != null && solid.IsValid)
                                        {
                                            var trsfSolid = (IXbimSolid)solid.Transform(trsf);
                                            var thisSol = trsfSolid.ToBRep;
                                            if (thisSol == prevSol)
                                                continue;
                                            var fileName = $"{label}.{item.Item1}.{iCnt++}.brep";
                                            if (firstWrite)
                                            {
                                                fileName = $"{label}.brep";
                                                firstWrite = false;
                                            }
                                            FileInfo fBrep = new FileInfo(Path.Combine(dirName, fileName));
                                            using (var tw = fBrep.CreateText())
                                            {
                                                tw.WriteLine("DBRep_DrawableShape");
                                                tw.WriteLine(thisSol);
                                            }
                                            ReportAdd($"=== {fBrep.FullName} written", Brushes.Blue);
                                            prevSol = thisSol;
                                        }
                                    }
                                }
                            }                           
                        }
                    }
                    continue;
                }
                m = Regex.Match(cmd, @"^(opacity|op) *(?<opac>[\d\.]+)", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    if (ModelIsUnavailable) continue;
                    var op = 0.0;
                    if (double.TryParse(m.Groups["opac"].Value, out op))
                    {
                        ReportAdd($"=== opacity set to {op}", Brushes.Blue);
                        _parentWindow.DrawingControl.SetOpacity(op);
                    }
                    else
                    {
                        ReportAdd($"=== invalid opacity level ({op})", Brushes.Blue);
                    }
                    continue;
                }
                m = Regex.Match(cmd, @"^(reload|re\b) *(?<entities>([\d,]+|[^ ]+))", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    // todo: restore function
                    //var start = m.Groups["entities"].Value;
                    //IEnumerable<int> labels = ToIntarray(start, ',');
                    //if (labels.Any())
                    //{
                    //    _parentWindow.DrawingControl.LoadGeometry(Model, labels);
                    //}
                    //else
                    //{
                    //    _parentWindow.DrawingControl.LoadGeometry(Model);
                    //}
                    continue;
                }

                m = Regex.Match(cmd,
                    @"^(GeometryEngine|ge) " +
                    @"(top (?<top>\d+) )*" +
                    // @"(?<mode>(count|list|typelist|short|full) )*" +
                    @"(?<tt>(transverse|tt) )*" +
                    @"(?<ri>(representationitems|ri|surfacesolid|ss|wire|wi) )*" +
                    @"(?<start>([\d,-]+|[^ ]+)) *" +
                    @"(?<props>.*)" +
                    "",
                    RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    if (ModelIsUnavailable) continue;
                    var labels = GetSelection(m).ToArray();
                    if (labels.Any())
                    {
                        foreach (var label in labels)
                        {
                            var entity = Model.Instances[label];
                            if (entity == null)
                            {
                                ReportAdd($"=== Entity {label} not found in model.", Brushes.Red);
                                continue;
                            }
                            ReportAdd($"== Geometry report for {entity.GetType().Name} #{label}", Brushes.Blue);
                            ReportAdd($"=== Geometry functions", Brushes.Blue);

                            var sols = GetSolids(entity);
                            foreach (var item in sols)
                            {
                                ReportAdd($"- {item.Item1}");
                                foreach (var solid in item.Item2)
                                {
                                    if (solid != null)
                                    {
                                        if (solid.IsValid)
                                        {
                                            ReportAdd($"  Ok, returned {solid.GetType().Name} - Volume: {solid.Volume}", Brushes.Green);
                                        }
                                        else
                                            ReportAdd($"  Err, returned {solid.GetType().Name} (not valid)", Brushes.Red);
                                    }
                                    else
                                    {
                                        // probably an error
                                    }
                                }
                            }

                            ReportAdd($"=== Autocad views", Brushes.Blue);
                            var ra = GeometryView.ReportAcadScript(entity);
                            ReportAdd(ra);
                        }
                    }
                    continue;
                }

                m = Regex.Match(cmd,
                    @"^(?<command>select|se|validate|va) " +
                    @"(top (?<top>\d+) )*" +
                    @"(?<mode>(count|list|typelist|short|full) )*" +
                    @"(?<tt>(transverse|tt) )*" +
                    @"(?<ri>(representationitems|ri|surfacesolid|ss|wire|wi) )*" +
                    @"(?<hi>(highlight|hi) )*" +
                    @"(?<svt>(showvaluetype|svt) )*" +
                    @"(?<start>([\d,-]+|[^ ]+)) *" +
                    @"(?<props>.*)" +
                    "",
                    RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    if (ModelIsUnavailable) continue;
                    var command = m.Groups["command"].Value.ToLowerInvariant();
                    var highlight = false;
                    var highlightT = m.Groups["hi"].Value;
                    if (highlightT != "")
                        highlight = true;

                    var mode = m.Groups["mode"].Value;
                    var svt = m.Groups["svt"].Value;

                    var ret = GetSelection(m).ToArray();

                    if (command == "va" || command == "validate")
                    {
                        // validation report

                        var validator = new Validator()
                        {
                            CreateEntityHierarchy = true,
                            ValidateLevel = ValidationFlags.All
                        };
                        var insts = ret.Select(el => Model.Instances[el]);
                        var validInstances = insts.Where(x => x != null).ToList();

                        ReportAdd($"Validating {validInstances.Count()} model instances.");
                        var valresults = validator.Validate(validInstances);

                        var issues = 0;
                        foreach (var validationResult in new IfcValidationReporter(valresults))
                        {
                            ReportAdd(validationResult);
                            issues++;
                        }
                        if (issues == 0)
                            ReportAdd($"No issues found.\r\n{DateTime.Now.ToLongTimeString()}.");
                    }
                    else
                    {
                        // property repor
                        switch (mode.ToLower())
                        {
                            case "count ":
                                ReportAdd($"Count: {ret.Count()}");
                                break;
                            case "list ":
                                foreach (var item in ret)
                                {
                                    ReportAdd(item.ToString(CultureInfo.InvariantCulture));
                                }
                                break;
                            case "typelist ":
                                foreach (var item in ret)
                                {
                                    ReportAdd(item + "\t" + Model.Instances[item].ExpressType.Name);
                                }
                                break;
                            default:
                                var beVerbose = false;
                                if (mode.ToLower() == "short ")
                                    beVerbose = false;
                                if (mode.ToLower() == "full ")
                                    beVerbose = true;
                                var svtB = (svt != "");
                                foreach (var item in ret)
                                {
                                    ReportAdd(ReportEntity(item, 0, verbose: beVerbose, showValueType: svtB));
                                }
                                break;
                        }
                    }
                    if (highlight) // set selection in Xplorer 
                    {
                        var s = new EntitySelection();
                        foreach (var item in ret)
                        {
                            s.Add(Model.Instances[item]);
                        }
                        var sw = new Stopwatch();
                        sw.Start();
                        _parentWindow.DrawingControl.Selection = s;
                        Debug.WriteLine(sw.ElapsedMilliseconds);
                    }
                    continue;
                }

                m = Regex.Match(cmd, @"^(ObjectPlacement|OP) " +
                                     @"(?<EntityId>\d+)"
                                     , RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    if (ModelIsUnavailable) continue;
                    var entityId = Convert.ToInt32(m.Groups["EntityId"].Value);
                    var ent = Model.Instances[entityId];
                    if (ent == null)
                    {
                        ReportAdd($"Entity not found #{entityId}");
                        continue;
                    }
                    var sb = new TextHighliter();
                    ReportObjectPlacement(sb, ent, 0);
                    ReportAdd(sb);
                    continue;
                }

                m = Regex.Match(cmd, @"^(TransformGraph|TG) " +
                                   @"(?<EntityIds>[\d ,]+)"
                                   , RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    if (ModelIsUnavailable) continue;
                    var entityIds = m.Groups["EntityIds"].Value;
                    var v = entityIds.Split(new[] {' ', ','}, StringSplitOptions.RemoveEmptyEntries);

                    var sb = new TextHighliter();
                    
                    foreach (var entityIdString in v)
                    {
                        var entityId = Convert.ToInt32(entityIdString);
                        var ent = Model.Instances[entityId];
                        if (!(ent is IIfcProduct))
                        {
                            ReportAdd($"Entity not found #{entityId}");
                            continue;
                        }
                        ReportTransformGraph(sb, ent as IIfcProduct, 0);
                    }  
                    ReportAdd(sb);
                    continue;
                }
                
                m = Regex.Match(cmd, @"^region ?(?<mode>list|set|add|\?)? *(?<RegionName>.+)*$", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    if (ModelIsUnavailable) continue;
                    var mode = m.Groups["mode"].Value;
                    var rName = m.Groups["RegionName"].Value;
                    if (string.IsNullOrWhiteSpace(mode))
                    {
                        ReportAdd($"Region syntax is: ^region ?(?<mode>list|set|add|\\?)? *(?<RegionName>.+)*$");
                        continue;
                    }
                    if (mode == "?" || mode == "list")
                    {
                        using (var reader = Model.GeometryStore.BeginRead())
                        {
                            var allRegCollections = reader.ContextRegions;
                            ReportAdd($"Region Collections count: {allRegCollections.Count}");
                            foreach (var regionCollection in allRegCollections)
                            {
                                ReportAdd($"Region Collection (#{regionCollection.ContextLabel}) count: {regionCollection.Count}");
                                foreach (var r in regionCollection)
                                {
                                    ReportAdd($"Region\t'{r.Name}'\t{r.Population}\t{r.Size}\t{r.Centre}");
                                }
                            }
                        }
                    }
                    else
                    {
                        bool setOk = true;
                        var add = (mode == "add");
                        if (add && rName == "*")
                        {
                            using (var reader = Model.GeometryStore.BeginRead())
                            {
                                var allRegCollections = reader.ContextRegions;
                                foreach (var regionCollection in allRegCollections)
                                {
                                    foreach (var r in regionCollection)
                                    {
                                        setOk &= _parentWindow.DrawingControl.SetRegion(r.Name, add);
                                    }
                                }
                            }
                        }
                        else
                            setOk = _parentWindow.DrawingControl.SetRegion(rName, add);
                        if (setOk)
                        {
                            ReportAdd("Region set.");
                            ReportAdd(_parentWindow.DrawingControl.ModelPositions.Report());
                        }
                        else
                        {
                            ReportAdd($"Region \"{rName}\"not found.");
                        }
                    }
                    continue;
                }

                m = Regex.Match(cmd, @"^clip off$", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    if (ModelIsUnavailable) continue;
                    _parentWindow.DrawingControl.ClearCutPlane();
                    ReportAdd("Clip removed");
                    _parentWindow.Activate();
                    continue;
                }

                m = Regex.Match(cmd, @"^ModelFix$", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    if (ModelIsUnavailable) continue;
                    ReportAdd("Attempting model fix.");
                    var f = new Fixer();
                    var cnt = f.Fix(Model);
                    ReportAdd($"{cnt} interventions.");
                    continue;
                }

                m = Regex.Match(cmd, @"^clip (" +
                                     @"(?<elev>[-+]?([0-9]*\.)?[0-9]+) *$" +
                                     "|" +
                                     @"(?<px>[-+]?([0-9]*\.)?[0-9]+) *, *" +
                                     @"(?<py>[-+]?([0-9]*\.)?[0-9]+) *, *" +
                                     @"(?<pz>[-+]?([0-9]*\.)?[0-9]+) *, *" +
                                     @"(?<nx>[-+]?([0-9]*\.)?[0-9]+) *, *" +
                                     @"(?<ny>[-+]?([0-9]*\.)?[0-9]+) *, *" +
                                     @"(?<nz>[-+]?([0-9]*\.)?[0-9]+)" +
                                     "|" +
                                     @"(?<StoreyName>.+$)" +
                                     ")", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    double px = 0, py = 0, pz = 0;
                    double nx = 0, ny = 0, nz = -1;

                    if (m.Groups["elev"].Value != string.Empty)
                    {
                        pz = Convert.ToDouble(m.Groups["elev"].Value);
                    }
                    else if (m.Groups["StoreyName"].Value != string.Empty)
                    {
                        if (ModelIsUnavailable) continue;
                        var msg = "";
                        var storName = m.Groups["StoreyName"].Value;
                        var storey =
                            Model.Instances.OfType<IIfcBuildingStorey>().FirstOrDefault(x => x.Name == storName);
                        if (storey != null)
                        {
                            var placementTree = new XbimPlacementTree(storey.Model, App.ContextWcsAdjustment);
                            var trsf = XbimPlacementTree.GetTransform(storey, placementTree, new XbimGeometryEngine());
                            var off = trsf.OffsetZ;
                            var pt = new XbimPoint3D(0, 0, off);

                            var mcp = XbimMatrix3D.Copy(_parentWindow.DrawingControl.ModelPositions[storey.Model].Transform);
                           
                            var transformed = mcp.Transform(pt);
                            msg = $"Clip 1m above storey elevation {pt.Z} (View space height: {transformed.Z + 1})";
                            pz = transformed.Z + 1;
                            
                        }
                        if (msg == "")
                        {
                            ReportAdd($"Something wrong with storey name: '{storName}'");
                            ReportAdd("Names that should work are: ");
                            var strs = Model.Instances.OfType<IIfcBuildingStorey>();
                            foreach (var str in strs)
                            {
                                ReportAdd($" - '{str.Name}'");
                            }
                            continue;
                        }
                        ReportAdd(msg);
                    }
                    else
                    {
                        px = Convert.ToDouble(m.Groups["px"].Value);
                        py = Convert.ToDouble(m.Groups["py"].Value);
                        pz = Convert.ToDouble(m.Groups["pz"].Value);
                        nx = Convert.ToDouble(m.Groups["nx"].Value);
                        ny = Convert.ToDouble(m.Groups["ny"].Value);
                        nz = Convert.ToDouble(m.Groups["nz"].Value);
                    }

                    _parentWindow.DrawingControl.ClearCutPlane();
                    _parentWindow.DrawingControl.SetCutPlane(
                        px, py, pz,
                        nx, ny, nz
                        );

                    ReportAdd("Clip command sent");
                    _parentWindow.Activate();
                    continue;
                }
                
                // todo: layers are gone; needs cleanup
                //
                m = Regex.Match(cmd, @"^Visual (?<action>list|tree|on|off|mode)( (?<Name>[^ ]+))*",
                    RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    if (ModelIsUnavailable) continue;
                    var parName = m.Groups["Name"].Value;
                    if (m.Groups["action"].Value.ToLowerInvariant() == "list")
                    {
                        foreach (var item in _parentWindow.DrawingControl.ListItems(parName))
                        {
                            ReportAdd(item);
                        }
                        Report("OpaquesVisual3D", _parentWindow.DrawingControl.OpaquesVisual3D);
                        Report("TransparentsVisual3D", _parentWindow.DrawingControl.TransparentsVisual3D);
                        Report("Selection", _parentWindow.DrawingControl.HighlightedVisual, true);

                    }
                    else if (m.Groups["action"].Value.ToLowerInvariant() == "tree")
                    {
                        foreach (var item in _parentWindow.DrawingControl.LayersTree())
                        {
                            ReportAdd(item);
                        }
                    }
                    else if (m.Groups["action"].Value.ToLowerInvariant() == "mode")
                    {
                        // todo: restore

                        //parName = parName.ToLowerInvariant();
                        //var sb = new StringBuilder();

                        //// foreach model
                        //if (_parentWindow.DrawingControl.LayerStylerForceVersion1 || Model.GeometrySupportLevel == 1)
                        //    ReportAdd(string.Format(@"Current mode is {0}",
                        //        _parentWindow.DrawingControl.LayerStyler.GetType())
                        //        , Brushes.Green);
                        //else if (Model.GeometrySupportLevel == 2)
                        //    ReportAdd(string.Format(@"Current mode is {0}",
                        //        _parentWindow.DrawingControl.GeomSupport2LayerStyler.GetType()), Brushes.Green);
                        //else
                        //    ReportAdd(@"Visual mode not enabled on GeometrySupportLevel 0", Brushes.Red);
                        

                        //bool stylerSet = false;

                        //var v1 = TypesImplementingInterface(typeof (ILayerStyler));
                        //foreach (var instance in v1.Where(IsRealClass))
                        //{
                        //    if (instance.Name.ToLowerInvariant() == parName ||
                        //        instance.FullName.ToLowerInvariant() == parName)
                        //    {
                        //        _parentWindow.DrawingControl.LayerStylerForceVersion1 = true;
                        //        _parentWindow.DrawingControl.LayerStyler =
                        //            (ILayerStyler) Activator.CreateInstance(instance);
                        //        _parentWindow.DrawingControl.FederationLayerStyler =
                        //            (ILayerStyler)Activator.CreateInstance(instance);


                        //        _parentWindow.DrawingControl.ReloadModel(
                        //            options: DrawingControl3D.ModelRefreshOptions.ViewPreserveAll
                        //            );
                        //        ReportAdd("Visual mode set to " + instance.FullName + ".", Brushes.Orange);
                        //        stylerSet = true;
                        //        continue;
                        //    }
                        //    sb.AppendLine(" - " + instance.FullName);
                        //}
                        //var v2 = TypesImplementingInterface(typeof(ILayerStylerV2));
                        //foreach (var instance in v2.Where(IsRealClass))
                        //{
                        //    if (instance.Name.ToLowerInvariant() == parName ||
                        //        instance.FullName.ToLowerInvariant() == parName)
                        //    {
                        //        _parentWindow.DrawingControl.LayerStylerForceVersion1 = false;
                        //        _parentWindow.DrawingControl.GeomSupport2LayerStyler =
                        //            (ILayerStylerV2) Activator.CreateInstance(instance);
                        //        _parentWindow.DrawingControl.ReloadModel(
                        //            options: DrawingControl3D.ModelRefreshOptions.ViewPreserveAll
                        //            );
                        //        ReportAdd("Visual mode set to " + instance.FullName + ".", Brushes.Orange);
                        //        stylerSet = true;
                        //        continue;
                        //    }
                        //    sb.AppendLine(" - " + instance.FullName);
                        //}
                        //if (!stylerSet)
                        //    ReportAdd(string.Format("Nothing done; valid modes are:\r\n{0}", sb));
                    }
                    else
                    {
                        var bVis = m.Groups["action"].Value.ToLowerInvariant() == "on";
                        _parentWindow.DrawingControl.SetVisibility(parName, bVis);
                    }
                    continue;
                }
                m = Regex.Match(cmd, @"^test$", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    if (ModelIsUnavailable) continue;
                    _parentWindow.DrawingControl.DefaultLayerStyler = new BoundingBoxStyler(this.Logger);
                    _parentWindow.DrawingControl.ReloadModel();
                    //continue;

                    ReportAdd($"Testing Xbim3DModelContext creation.");
                    var w = new Stopwatch();
                    w.Restart();
                    var ccnt = Model.Instances.OfType<Xbim.Ifc2x3.RepresentationResource.IfcRepresentationContext>().ToList();
                    Debug.Write(ccnt.Count);
                    w.Stop();
                    ReportAdd($"Elapsed for ifc2x3.IfcRepresentationContext: {w.ElapsedMilliseconds} msec.");


                    w.Restart();
                    var ccnt2 = Model.Instances.OfType<IIfcGeometricRepresentationSubContext>().ToList();
                    Debug.Write(ccnt.Count);
                    w.Stop();
                    ReportAdd($"Elapsed for IIfcRepresentationContext: {w.ElapsedMilliseconds} msec.");

                    w.Restart();
                    var c = new Xbim3DModelContext(Model);
                    w.Stop();
                    var msg = c.GetRegions();
                    ReportAdd($"Elapsed for createcontext: {w.ElapsedMilliseconds} msec.");
                    ReportAdd($"regions: {msg.Count()}");

                    continue;
                }
                ReportAdd($"Command not understood: {cmd}.");
            }
        }

        internal void Execute(string cmd)
        {
            TxtCommand.Text = cmd;
            Execute();
        }

        private IEnumerable<Tuple<string, List<IXbimSolid>>> GetSolids(IPersistEntity entity)
        {
            // todo: cache methods by type
            var engine = new XbimGeometryEngine();
            var methods = typeof(XbimGeometryEngine).GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (var methodInfo in methods)
            {
                var pars = methodInfo.GetParameters().ToArray();
                if (pars.Length != 1) // only consider functinons with a single parameter
                    continue;
                if (methodInfo.ReturnParameter.ParameterType == typeof(bool))
                    continue; // excludes the equal function

                var firstParam = pars.FirstOrDefault();
                if (firstParam == null)
                    continue;
                if (!firstParam.ParameterType.IsInstanceOfType(entity))
                    continue;
                var functionShort = $"{methodInfo.Name}({firstParam.ParameterType.Name.Replace("IIfc", "Ifc")})";
                
                var getSolidRet = new Tuple<string, List<IXbimSolid>>( 
                    functionShort, new List<IXbimSolid>()
                    );

                try
                {
                    var ret = methodInfo.Invoke(engine, new object[] { entity });
                    if (ret != null)
                    {
                        var sol = ret as IXbimSolid;
                        var solset = ret as IXbimSolidSet;
                        if (sol != null)
                        {
                            getSolidRet.Item2.Add(sol);
                        }
                        else if (solset != null)
                        {
                            foreach (var subSol in solset)
                            {
                                getSolidRet.Item2.Add(subSol);
                                // ReportAdd($"    [{iCnt++}]: {subSol.GetType().Name} - Volume: {subSol.Volume}", Brushes.Green);
                            }
                        }
                    }
                    else
                    {
                        getSolidRet.Item2.Add(null);
                    }
                }
                catch (Exception ex)
                {
                    getSolidRet.Item2.Add(null);
                    var msg = $"  Failed on {functionShort} for #{entity.EntityLabel}. {ex.Message}";
                    ReportAdd(msg, Brushes.Red);
                }
                yield return getSolidRet;
            }
        }

        private void Report(string title, ModelVisual3D visualElement, bool triangulation = false)
        {
            ReportAdd(title, Brushes.Blue);
            if (visualElement.Content != null)
            {
                var as3D = visualElement.Content as GeometryModel3D;
                Report(as3D, 1, triangulation);
            }
            foreach (var visualElementChild in visualElement.Children.OfType<ModelVisual3D>())
            {
                Report(visualElementChild);
            }
        }

        private void Report(ModelVisual3D mv3d, int indent = 0)
        {
            var ind = new string('\t', indent);
            ReportAdd($"{ind}{mv3d.GetType().Name} isSealed:{mv3d.IsSealed} children: {mv3d.Children.Count}"
            );
            foreach (var child in mv3d.Children)
            {
                // Report(child, indent + 1);
            }
            if (mv3d.Content is Model3DGroup)
                Report((Model3DGroup)mv3d.Content, indent + 1);
        }

        private void Report(Model3DGroup content, int indent)
        {
            var ind = new string('\t', indent);
            ReportAdd($"{ind}{content.GetType().Name} isSealed:{content.IsSealed} children: {content.Children.Count}"
            );
            foreach (var child in content.Children.OfType<GeometryModel3D>())
            {
                Report(child, indent + 1);
            }
        }

        private void Report(GeometryModel3D content, int indent, bool triangulation = false)
        {
            var mRep = content.Material.GetType().Name;
            var mat = content.Material as DiffuseMaterial;
            Brush b = null;
            if (mat != null)
            {
                mRep = mat.Brush + " ";
                b = mat.Brush;
            }
            
            var ind = new string('\t', indent);
            var msg = $"{ind}{content.GetType().Name} isSealed:{content.IsSealed} Material: {mRep}";
            var rb = new TextHighliter();
            var txt = new List<string>() {msg};
            var bs = new List<Brush>() { Brushes.Black};
            
            if (b != null)
            {
                txt.Add("█████████"); // used to present colour
                bs.Add(b);
            }
            rb.AppendSpans(txt.ToArray(), bs.ToArray());
            ReportAdd(rb);
            if (content.Geometry is MeshGeometry3D)
                Report(content.Geometry as MeshGeometry3D, indent + 1, triangulation);

        }

        private void Report(MeshGeometry3D content, int indent, bool triangulation = false)
        {
            var ind = new string('\t', indent);
            ReportAdd(
                $"{ind}{content.GetType().Name} isSealed:{content.IsSealed}\tPositions:\t{content.Positions.Count}\tTriangleIndices:\t{content.TriangleIndices.Count}"
            );
            if (triangulation)
            {
                for (int i = 0; i < content.Positions.Count; i++)
                {
                    var p = content.Positions[i];
                    var n = content.Normals[i];
                    ReportAdd($"{p.X} {p.Y} {p.Z} {n.X} {n.Y} {n.Z}");
                }
            }
        }

        private void IfcZipAndDelete(string directoryName, bool subfolders)
        {
            var files = Directory.GetFiles(directoryName, "*.ifc", subfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            long l = 0;
            foreach (var file in files)
            {
                var ext = Path.GetExtension(file);
                if (ext != ".ifc")
                    continue;
                l += IfcZipAndDelete(file);
            }

            ReportAdd($"Total file delta: {l:N}");
        }

        private long IfcZipAndDelete(string fileName)
        {
            ReportAdd($"Processing file: {fileName}.");
            var newFile = Path.ChangeExtension(fileName, ".ifczip");
            if (fileName == newFile)
            {
                ReportAdd($"Nothing to do.");
                return 0;
            }
            ReportAdd($"Opening.");
            IfcStore model = null;
            try
            {
                model = IfcStore.Open(fileName, null, -1);
            }
            catch (Exception)
            {
                ReportAdd($"Error opening source file. Ignored.", Brushes.Red);;
            }
            if (model == null)
            {
                return 0;
            }

            ReportAdd($"Saving.");
            model.SaveAs(newFile, StorageType.IfcZip);
            model.Close();

            var fBefore = new FileInfo(fileName);
            var fAfter = new FileInfo(newFile);

            var diff = fAfter.Length - fBefore.Length;
            try
            {
                File.Delete(fileName);
            }
            catch (SystemException)
            {
                ReportAdd($"Error deleting source file.", Brushes.Red);
            }
            ReportAdd($"Completed. Delta is {diff:N}");
            return diff;
        }

        private void ReportTransformGraph(TextHighliter sb, IIfcProduct ent, int i)
        {
            var v = new TransformGraph(ent.Model);
            v.AddProduct(ent);
            sb.Append(
                $"=== #{ent.EntityLabel} ({ent.GetType().Name}) ",
                Brushes.Blue
                );
            sb.Append(string.Format("   Local matrix:"), Brushes.Black);
            ReportMatrix(sb, v[ent].LocalMatrix);
            sb.Append(string.Format("   World matrix:"), Brushes.Black);
            ReportMatrix(sb, v[ent].WorldMatrix());
        }

        private void ReportMatrix(TextHighliter sb, XbimMatrix3D matrix)
        {
            var frmt = "G7";
            sb.Append(
                $"   \t{matrix.M11.ToString(frmt),10}\t{matrix.M21.ToString(frmt),10}\t{matrix.M31.ToString(frmt),10}\t{matrix.OffsetX.ToString(frmt),10}", Brushes.Black);
            sb.Append(
                $"   \t{matrix.M12.ToString(frmt),10}\t{matrix.M22.ToString(frmt),10}\t{matrix.M32.ToString(frmt),10}\t{matrix.OffsetY.ToString(frmt),10}", Brushes.Black);
            sb.Append(
                $"   \t{matrix.M13.ToString(frmt),10}\t{matrix.M23.ToString(frmt),10}\t{matrix.M33.ToString(frmt),10}\t{matrix.OffsetZ.ToString(frmt),10}", Brushes.Black);
            sb.Append(
                $"   \t{matrix.M14.ToString(frmt),10}\t{matrix.M24.ToString(frmt),10}\t{matrix.M34.ToString(frmt),10}\t{matrix.M44.ToString(frmt),10}", Brushes.Black);
            sb.Append("", Brushes.Black);
        }

        private void ReportObjectPlacement(TextHighliter sb, IPersistEntity ent, int indentation)
        {
            
            var indentationHeader = new string('\t', indentation);

            if (ent is IIfcProduct)
            {
                var asprod = ent as IIfcProduct;
                sb.Append(
                    string.Format(indentationHeader + "=== #{0} ({1}) ", asprod.EntityLabel, asprod.GetType().Name),
                    Brushes.Blue
                );
                sb.Append(
                    string.Format(indentationHeader + "   ObjectPlacement:"),
                    Brushes.Black
                );
                ReportObjectPlacement(sb, asprod.ObjectPlacement, indentation + 1);
            }
            else if (ent is IIfcLocalPlacement)
            {
                var asLocalPlacement = ent as IIfcLocalPlacement;
                sb.Append(
                    string.Format(indentationHeader + "#{0} ({1}) ", asLocalPlacement.EntityLabel, asLocalPlacement.GetType().Name),
                    Brushes.Blue
                );
                sb.Append(
                    string.Format(indentationHeader + "   Placement:"),
                    Brushes.Black
                );
                ReportObjectPlacement(sb, asLocalPlacement.RelativePlacement, indentation + 1);
                if (asLocalPlacement.PlacementRelTo != null)
                {
                    sb.Append(
                        string.Format(indentationHeader + "   RelativeTo:"),
                        Brushes.Black
                    );
                    ReportObjectPlacement(sb, asLocalPlacement.PlacementRelTo, indentation + 1);
                }
            }
            else if (ent is IIfcAxis2Placement3D)
            {
                var asLocalPlacement = ent as IIfcAxis2Placement3D;
                sb.Append(
                    string.Format(indentationHeader + "#{0} ({1}) ", asLocalPlacement.EntityLabel, asLocalPlacement.GetType().Name),
                    Brushes.Blue
                );
                // props

                sb.Append(
                    string.Format(indentationHeader + "   Location: {0}, {1}, {2}",
                        asLocalPlacement.Location.X,
                        asLocalPlacement.Location.Y,
                        asLocalPlacement.Location.Z
                    ),
                    Brushes.Black
                );
                // ReportObjectPlacement(sb, asLocalPlacement.Location, indentation + 1);
                if (asLocalPlacement.Axis != null)
                    sb.Append(
                        string.Format(indentationHeader + "   Axis: {0}, {1}, {2}",
                            asLocalPlacement.Axis.X,
                            asLocalPlacement.Axis.Y,
                            asLocalPlacement.Axis.Z
                        ),
                        Brushes.Black
                    );
                // ReportObjectPlacement(sb, asLocalPlacement.Axis, indentation + 1);

                if (asLocalPlacement.RefDirection != null)
                    sb.Append(
                        string.Format(indentationHeader + "   RefDirection: {0}, {1}, {2}",
                            asLocalPlacement.RefDirection.X,
                            asLocalPlacement.RefDirection.Y,
                            asLocalPlacement.RefDirection.Z
                        ),
                        Brushes.Black
                    );
                //ReportObjectPlacement(sb, asLocalPlacement.RefDirection, indentation + 1);
            }
            else
            {
                if (ent == null)
                    return;
                sb.Append(
                    string.Format(indentationHeader + "Add management of {0} in code", ent.GetType().Name),
                    Brushes.Red
                );
            }
        }

        private IEnumerable<int> GetSelection(Match m)
        {
            var labels = GetEntityLabels(m);
            if (!string.IsNullOrEmpty(m.Groups["ri"].Value))
                labels = GetRepresentationItems(labels, m.Groups["ri"].Value);
            labels = labels.Distinct();
            return labels;
        }

        enum RepresentationItemSelectionMode
        {
            all,
            surfaceOrSolid,
            wires
        }

        private IEnumerable<int> GetRepresentationItems(IEnumerable<int> labels, string selectionMode)
        {
            selectionMode = selectionMode.Trim();
            var inQueue = new Queue<int>(labels);
            var outList = new List<int>();
            var mode = RepresentationItemSelectionMode.all;
            // representationitems|ri|surfacesolid|ss|wire|wi
            if (selectionMode == "surfacesolid" || selectionMode == "ss")
            {
                mode = RepresentationItemSelectionMode.surfaceOrSolid;
            }
            else if (selectionMode == "wire" || selectionMode == "wi")
            {
                mode = RepresentationItemSelectionMode.wires;
            }

            while (inQueue.Any())
            {
                var entityLabel = inQueue.Dequeue();
                var entity = Model.Instances[entityLabel];
                if (entity == null)
                    continue;
                EvaluateInclusion(entity, mode, outList);
                var ifcType = Model.Metadata.ExpressType(entity);
                var props = ifcType.Properties.Values;
                foreach (var expressMetaProperty in props)
                {
                    var t = expressMetaProperty.PropertyInfo.PropertyType;
                    var propVal = expressMetaProperty.PropertyInfo.GetValue(entity, null);

                    var v = EvaluateInclusion(propVal, mode, outList);
                    if (v != -1)
                        inQueue.Enqueue(v);

                    
                    else if (expressMetaProperty.EntityAttribute.IsEnumerable)
                    {
                        var propCollection = propVal as System.Collections.IEnumerable;
                        if (propCollection == null)
                            continue;
                        foreach (var item in propCollection)
                        {
                            var vEnum = EvaluateInclusion(item, mode, outList);
                            if (vEnum != -1)
                                inQueue.Enqueue(v);
                        }
                    }
                }
            }
            return outList;
        }
        
        private List<Type> _surfaceOrSolidTypes;
        private List<Type> _wireTypes;

        private void PopulateFilterTypes()
        {
            // todo: these lists needs to be revised
                     
            _surfaceOrSolidTypes = new List<Type>();
            _surfaceOrSolidTypes.AddRange(SchemaMetadatas["ifc2x3"].ExpressType(typeof(Xbim.Ifc2x3.GeometryResource.IfcSurface)).NonAbstractSubTypes.Select(x => x.Type));
            _surfaceOrSolidTypes.AddRange(SchemaMetadatas["ifc4"].ExpressType(typeof(Xbim.Ifc4.GeometryResource.IfcSurface)).NonAbstractSubTypes.Select(x => x.Type));


            _surfaceOrSolidTypes.AddRange(SchemaMetadatas["ifc2x3"].ExpressType(typeof(Xbim.Ifc2x3.GeometryResource.IfcSurface)).NonAbstractSubTypes.Select(x => x.Type));
            _surfaceOrSolidTypes.AddRange(SchemaMetadatas["ifc4"].ExpressType(typeof(Xbim.Ifc4.GeometryResource.IfcSurface)).NonAbstractSubTypes.Select(x => x.Type));

            _surfaceOrSolidTypes.AddRange(SchemaMetadatas["ifc2x3"].ExpressType(typeof(Xbim.Ifc2x3.GeometricModelResource.IfcCsgPrimitive3D)).NonAbstractSubTypes.Select(x => x.Type));
            _surfaceOrSolidTypes.AddRange(SchemaMetadatas["ifc4"].ExpressType(typeof(Xbim.Ifc4.GeometricModelResource.IfcCsgPrimitive3D)).NonAbstractSubTypes.Select(x => x.Type));
                                          
            _surfaceOrSolidTypes.AddRange(SchemaMetadatas["ifc2x3"].ExpressType(typeof(Xbim.Ifc2x3.GeometricModelResource.IfcBooleanResult)).NonAbstractSubTypes.Select(x => x.Type));
            _surfaceOrSolidTypes.AddRange(SchemaMetadatas["ifc4"].ExpressType(typeof(Xbim.Ifc4.GeometricModelResource.IfcBooleanResult)).NonAbstractSubTypes.Select(x => x.Type));
                                          
            _surfaceOrSolidTypes.AddRange(SchemaMetadatas["ifc2x3"].ExpressType(typeof(Xbim.Ifc2x3.GeometricModelResource.IfcHalfSpaceSolid)).NonAbstractSubTypes.Select(x => x.Type));
            _surfaceOrSolidTypes.AddRange(SchemaMetadatas["ifc4"].ExpressType(typeof(Xbim.Ifc4.GeometricModelResource.IfcHalfSpaceSolid)).NonAbstractSubTypes.Select(x => x.Type));
                                          
            _surfaceOrSolidTypes.AddRange(SchemaMetadatas["ifc2x3"].ExpressType(typeof(Xbim.Ifc2x3.GeometricModelResource.IfcSolidModel)).NonAbstractSubTypes.Select(x => x.Type));
            _surfaceOrSolidTypes.AddRange(SchemaMetadatas["ifc4"].ExpressType(typeof(Xbim.Ifc4.GeometricModelResource.IfcSolidModel)).NonAbstractSubTypes.Select(x => x.Type));
                                          
            _surfaceOrSolidTypes.AddRange(SchemaMetadatas["ifc2x3"].ExpressType(typeof(Xbim.Ifc2x3.GeometricModelResource.IfcFaceBasedSurfaceModel)).NonAbstractSubTypes.Select(x => x.Type));
            _surfaceOrSolidTypes.AddRange(SchemaMetadatas["ifc4"].ExpressType(typeof(Xbim.Ifc4.GeometricModelResource.IfcFaceBasedSurfaceModel)).NonAbstractSubTypes.Select(x => x.Type));

            _wireTypes = new List<Type>();
            _wireTypes.AddRange(SchemaMetadatas["ifc2x3"].ExpressType(typeof(Xbim.Ifc2x3.GeometryResource.IfcCurve)).NonAbstractSubTypes.Select(x => x.Type));
            _wireTypes.AddRange(SchemaMetadatas["ifc4"].ExpressType(typeof(Xbim.Ifc4.GeometryResource.IfcCurve)).NonAbstractSubTypes.Select(x => x.Type));

            _wireTypes.AddRange(SchemaMetadatas["ifc2x3"].ExpressType(typeof(Xbim.Ifc2x3.GeometryResource.IfcCompositeCurveSegment)).NonAbstractSubTypes.Select(x => x.Type));
            _wireTypes.AddRange(SchemaMetadatas["ifc4"].ExpressType(typeof(Xbim.Ifc4.GeometryResource.IfcCompositeCurveSegment)).NonAbstractSubTypes.Select(x => x.Type));
        }
        
        private int EvaluateInclusion(object entityO, RepresentationItemSelectionMode mode, List<int> outList)
        {
            var entity = entityO as IPersistEntity;
            if (entity == null)
                return -1;
            var t = entity.GetType();
           
            if (mode == RepresentationItemSelectionMode.all && (typeof(IIfcRepresentationItem)).IsAssignableFrom(t))
            {
                outList.Add(entity.EntityLabel);
                return entity.EntityLabel;
            }
            if (_surfaceOrSolidTypes == null || _wireTypes == null)
                PopulateFilterTypes();

            if (mode == RepresentationItemSelectionMode.surfaceOrSolid && _surfaceOrSolidTypes.Contains(t))
            {
                outList.Add(entity.EntityLabel);
                return entity.EntityLabel;
            }
            if (mode == RepresentationItemSelectionMode.wires && _wireTypes.Contains(t))
            {
                outList.Add(entity.EntityLabel);
                return entity.EntityLabel;
            }
            return entity.EntityLabel;
        }
        
        private IEnumerable<int> GetEntityLabels(Match m)
        {
            var top = m.Groups["top"].Value;
            var start = m.Groups["start"].Value;
            // top limit of returns
            var iTop = -1;
            if (top != string.Empty)
                iTop = Convert.ToInt32(top);
            var props = m.Groups["props"].Value;

            // transverse tree mode
            var transverseT = false;
            var transverse = m.Groups["tt"].Value;
            if (transverse != "")
                transverseT = true;

            IEnumerable<int> labels = ToIntarray(start, ',');
            if (!labels.Any())
            {
                // see if it's a type string instead;
                var subRe = new Regex(@"[\+\-]*([A-Za-z0-9\[\]]+)");
                var res = subRe.Matches(start);
                foreach (Match submatch in res)
                {
                    var modeAdd = !submatch.Value.Contains("-");
                    // the syntax could be IfcWall[10]
                    var sbi = new SquareBracketIndexer(submatch.Groups[1].Value);
                    IEnumerable<int> thisLabels = QueryEngine.EntititesForType(sbi.Property, Model);
                    thisLabels = sbi.GetItem(thisLabels);
                    labels = modeAdd
                        ? labels.Concat(thisLabels)
                        : labels.Where(t => !thisLabels.Contains(t));
                }
            }
            var ret = QueryEngine.RecursiveQuery(Model, props, labels, transverseT);
            if (iTop != -1)
                ret = ret.Take(iTop);
            return ret;
        }

        private void ReportAdd(TextHighliter th)
        {
            th.DropInto(TxtOut.Document);
            th.Clear();
        }

        private void ReportAdd(string text, Brush inColor = null)
        {
            var newP = new Paragraph(new Run(text));
            if (inColor != null)
            {
                newP.Foreground = inColor;
            }
            TxtOut.Document.Blocks.Add(newP);
        }

        private int[] ToIntarray(string value, char sep)
        {
            var sa = value.Split(new[] {sep}, StringSplitOptions.RemoveEmptyEntries);
            var ia = new List<int>();
            for (var i = 0; i < sa.Length; ++i)
            {
                if (sa[i].Contains('-'))
                {
                    var v = sa[i].Split('-');
                    if (v.Length != 2) 
                        continue;
                    int iS, iT;
                    if (!int.TryParse(v[0], out iS) || !int.TryParse(v[1], out iT)) 
                        continue;
                    if (iT < iS) 
                        continue;
                    for (var iC = iS; iC <= iT; iC++)
                    {
                        ia.Add(iC);
                    }
                }
                else
                {
                    int j;
                    var thisText = sa[i];
                    if (thisText.StartsWith("#"))
                        thisText = thisText.Substring(1);
                    if (int.TryParse(thisText, out j))
                    {
                        ia.Add(j);
                    }
                }
            }
            return ia.ToArray();
        }
        
        private void DisplayHelp()
        {
            var t = new TextHighliter();

            t.AppendFormat("Commands:");
            t.Append(
                "- select [count|list|typelist|full|short] [tt|transverse] [representationitems|ri|surfacesolid|ss|wire|wi] [hi|highlight] [svt|showvaluetype] <startingElement> [Property [Property...]]"
                , Brushes.Blue
                );
            t.Append(
                "    <startingElement>: <EntityLabel, <EntityLabel>> or <TypeIdentificator>[<+|-><TypeIdentificator>]",
                Brushes.Gray);
            t.Append("      <TypeIdentificator>: IfcTypeName[#]", Brushes.Gray);
            t.Append("      Examples:", Brushes.Gray);
            t.Append("        select 12,14", Brushes.Gray);
            t.Append("        select count IfcWall", Brushes.Gray);
            t.Append("        select typelist IfcWall-IfcWallStandardCase", Brushes.Gray);

            t.Append("    [Property] is a Property or Inverse name", Brushes.Gray);
            t.Append("    [highlight] puts the returned set in the viewer selection", Brushes.Gray);
            t.Append("    ", Brushes.Gray);
            t.Append("    Replacing the select command with geometryengine returns the geometry calls on the item", Brushes.Gray);
            t.Append("      Examples:", Brushes.Gray);
            t.Append("        ge 12,14", Brushes.Gray);
            
            t.Append("- EntityLabel <label> [recursion]" , Brushes.Blue);
            t.Append("    [recursion] is an int representing the depth of children to report", Brushes.Gray);

            t.Append("- IfcSchema [list|count|short|full] <TypeName>", Brushes.Blue);
            t.Append("    <TypeName> can contain wildcards", Brushes.Gray);
            t.Append("    use / in <TypeName> to select all root types", Brushes.Gray);
            
            t.Append("- Reload <EntityLabel,<EntityLabel>>", Brushes.Blue);
            t.Append("    <EntityLabel> filters the elements to load in the viewer.", Brushes.Gray);

            t.Append("- clip [off|<Elevation>|<px>, <py>, <pz>, <nx>, <ny>, <nz>|<Storey name>]", Brushes.Blue);
            t.Append("    Clipping the 3D model is still and unstable feature. Use with caution.", Brushes.Gray);
            
            t.Append("- ObjectPlacement <EntityLabel>", Brushes.Blue);
            t.Append("    Reports the place tree of an element.", Brushes.Gray);

            t.Append("- TransformGraph <EntityLabel,<EntityLabel>>", Brushes.Blue);
            t.Append("    Reports the transofrm graph for a set of elements.", Brushes.Gray);

            t.Append("- IfcZip <file|folder [/s]>", Brushes.Blue);
            t.Append("    Compresses ifc files to ifczip, the function is slow. ", Brushes.Gray);
            t.Append("    It goes through ifcstore.open rather than simple compression, ", Brushes.Gray);
            t.Append("    so it can be used totest for model correctness.", Brushes.Gray);

            t.Append("- Region <list|set|add> <Region name>", Brushes.Blue);
            t.Append("    'select the named region for display.", Brushes.Gray);
            t.Append("    'use 'region add *' to zoom to whole model.", Brushes.Gray);
                        
            t.AppendFormat("- Visual [list]");
            t.Append("    'Visual list' provides a list of the elements in the WPF visual tree with their respective size", Brushes.Gray);
            //t.Append("    'Visual list' provides a list of valid layer names", Brushes.Gray);
            //t.Append("    'Visual tree' provides a tree layer structure", Brushes.Gray);
            //t.Append("    'Visual mode ...' changes the mode of the layer tree structure", Brushes.Gray);
            //t.Append("      <ModeCommand> in: type, entity, oddeven or demo.", Brushes.Gray);

            t.Append("- clear [on|off]", Brushes.Blue);

            t.Append("- SelectionHighlighting [WholeMesh|Normals]", Brushes.Blue);
            t.Append("    defines the graphical style for selection highliting.", Brushes.Gray);

            t.Append("- SimplifyGUI", Brushes.Blue);
            t.Append("    opens a GUI for simplifying IFC files (useful for debugging purposes).", Brushes.Gray);

            t.AppendFormat("");
            t.AppendFormat("Notes:");
            t.AppendFormat("1. double slash (//) are the comments token and the remainder of lines is ignored.");
            t.AppendFormat("2. If a portion of text is selected, only selected text will be executed.");
            
            t.AppendFormat("");
            t.Append("Commands are executed on <ctrl>+<Enter> or pressing the Run button.", Brushes.OrangeRed);
            t.DropInto(TxtOut.Document);
        }


        /// <summary>
        /// Finds relevant classes through reflection by Namespace + Name query
        /// </summary>
        /// <param name="regExString">The regex string to be compared to the namespace</param>
        /// <returns>Enumerable string of full type name, with namespace</returns>
        private IEnumerable<string> MatchingTypes(string regExString)
        {
            HashSet<string> processed = new HashSet<string>();
            var re = new Regex(regExString, RegexOptions.IgnoreCase);
            foreach (var an in Assembly.GetExecutingAssembly().GetReferencedAssemblies())
            {
                var asm = Assembly.Load(an.ToString());
                foreach (var type in asm.GetTypes().Where(
                    t =>
                        t.Namespace != null &&
                        (
                            t.Namespace == "Xbim.XbimExtensions.SelectTypes"
                            ||
                            t.Namespace.StartsWith("Xbim.Ifc2x3.")
                            ||
                            t.Namespace.StartsWith("Xbim.Ifc4.")
                            ))
                    )
                {
                    if (regExString == "/")
                    {
                        if (type.BaseType == typeof(object))
                        {
                            if (!processed.Contains(type.FullName))
                            {
                                processed.Add(type.FullName);
                                yield return type.FullName;
                            }
                        }
                    }
                    else
                    {
                        if (re.IsMatch(type.FullName))
                        {
                            if (!processed.Contains(type.FullName))
                            {
                                processed.Add(type.FullName);
                                yield return type.FullName;
                            }
                        }
                    }
                }
            }
        }

        private  string PrepareRegex(string rex)
        {
            short ishort;
            if (short.TryParse(rex, out ishort))
            {
                // build the regex string from the typeid
                //
                var t = Model.Metadata.ExpressType(ishort);
                return  @".*\." + t.Name + "$";
            }

            rex = rex.Replace(".", @"\."); //escaped dot
            rex = rex.Replace("*", ".*");
            rex = rex.Replace("?", ".");
            return rex;
        }

        private string GetFriendlyTypeName(Type type)
        {
            using (var p = new CSharpCodeProvider())
            {
                var r = new CodeTypeReference(type);
                return p.GetTypeOutput(r);
            }
        }

        internal static Dictionary<string, ExpressMetaData> SchemaMetadatas => new Dictionary<string, ExpressMetaData>
        {
            {"ifc2x3", ExpressMetaData.GetMetadata(typeof(Xbim.Ifc2x3.SharedBldgElements.IfcWall).Module)},
            {"ifc4", ExpressMetaData.GetMetadata(typeof(Xbim.Ifc4.SharedBldgElements.IfcWall).Module)}
        };

        private TextHighliter ReportType(string type, int beVerbose, string indentationHeader = "")
        {
            Debug.WriteLine(type);
            var tarr = type.Split(new[] {"."}, StringSplitOptions.RemoveEmptyEntries);
            type = tarr[tarr.Length - 1];
            var schema = tarr[tarr.Length - 3].ToLowerInvariant();
            var sb = new TextHighliter();
           
            var ot = SchemaMetadatas[schema].ExpressType(type.ToUpper());
            if (ot != null)
            {
                sb.Append(
                    string.Format(indentationHeader + "=== {0}", ot.Name),
                    Brushes.Blue
                    );

                
                if (beVerbose > 0)
                {
                    sb.AppendFormat(indentationHeader + "Namespace: {0}", ot.Type.Namespace);
                    sb.AppendFormat(indentationHeader + "xbim.TypeId: {0}", ot.TypeId);
                    sb.AppendFormat(indentationHeader + "IsIndexed: {0}", ot.IndexedClass);
                }
                sb.DefaultBrush = Brushes.DarkOrange;
                var supertypes = new List<string>();
                var iterSuper = ot.SuperType;
                while (iterSuper != null)
                {
                    supertypes.Add(iterSuper.Name);
                    iterSuper = iterSuper.SuperType;
                }
                if (ot.SuperType != null)
                    sb.AppendFormat(indentationHeader + "Parents hierarchy: {0}",
                        string.Join(" => ", supertypes.ToArray()));
                if (ot.SubTypes.Count > 0)
                {
                    if (beVerbose > 1)
                    {
                        sb.DefaultBrush = null;
                        sb.AppendFormat(indentationHeader + "== Subtypes tree:");
                        sb.DefaultBrush = Brushes.DarkOrange;
                        var cnt = ChildTree(ot, sb, indentationHeader, 0);
                        sb.AppendFormat(indentationHeader + "count: {0}\r\n", cnt);
                    }
                    else
                    {
                        sb.DefaultBrush = null;
                        sb.AppendFormat(indentationHeader + "== Direct subtypes:");
                        sb.DefaultBrush = Brushes.DarkOrange;
                        foreach (var item in ot.SubTypes.OrderBy(x=>x.Name))
                        {
                            sb.AppendFormat(indentationHeader + "- {0}", item);
                        }
                        sb.AppendFormat(indentationHeader + "count: {0}\r\n", ot.SubTypes.Count);
                    }
                }
                if (beVerbose > 0)
                {
                    if (beVerbose > 1)
                    {
                        var allSub = ot.NonAbstractSubTypes;
                        sb.DefaultBrush = null;
                        sb.AppendFormat(indentationHeader + "== All Concrete subtypes:");
                        sb.DefaultBrush = Brushes.DarkOrange;
                        foreach (var item in allSub)
                        {
                            sb.AppendFormat(indentationHeader + "- {0}", item.Name);
                        }
                        sb.AppendFormat(indentationHeader + "count: {0}\r\n", allSub.Count());
                    }
                    

                    if (beVerbose > 1)
                    {
                        sb.DefaultBrush = null;
                        sb.AppendFormat(indentationHeader + "== Interfaces:");
                        sb.DefaultBrush = Brushes.DarkOrange;
                        foreach (var implementedName in ot.Type.GetInterfaces().Select(x => GetFriendlyTypeName(x)).OrderBy(y => y))
                        {
                            sb.AppendFormat(indentationHeader + "- {0}", implementedName);
                        }
                        sb.AppendFormat(indentationHeader + "count: {0}\r\n", ot.Type.GetInterfaces().Count());
                    }

                    sb.DefaultBrush = null;
                    // sb.DefaultBrush = Brushes.DimGray;
                    sb.AppendFormat(indentationHeader + "== Properties:");
                    sb.DefaultBrush = null;
                    var brushArray = new Brush[]
                    {
                        Brushes.DimGray,
                        Brushes.DarkGray,
                        Brushes.DimGray
                    };
                    foreach (var item in ot.Properties.Values)
                    {
                        var topParent = ot.SuperType;
                        var sTopParent = "";
                        while (topParent != null &&
                               topParent.Properties.Any(x => x.Value.PropertyInfo.Name == item.PropertyInfo.Name))
                        {
                            sTopParent = " \tfrom: " + topParent;
                            topParent = topParent.SuperType;
                        }
                        sb.AppendSpans(new[]
                            {
                                indentationHeader + "- " + item.PropertyInfo.Name + "\t\t",
                                CleanPropertyName(item.PropertyInfo.PropertyType.FullName),
                                sTopParent
                            },
                            brushArray);
                    }
                    sb.AppendFormat(indentationHeader + "count: {0}\r\n", ot.Properties.Count());

                    sb.AppendFormat(indentationHeader + "== Inverses:");
                    foreach (var item in ot.Inverses)
                    {
                        var topParent = ot.SuperType;
                        var sTopParent = "";
                        while (topParent != null &&
                               topParent.Inverses.Any(x => x.PropertyInfo.Name == item.PropertyInfo.Name))
                        {
                            sTopParent = " \tfrom: " + topParent;
                            topParent = topParent.SuperType;
                        }
                        //sb.AppendFormat(indentationHeader + "- {0}\t{1}{2}", item.PropertyInfo.Name, CleanPropertyName(item.PropertyInfo.PropertyType.FullName), sTopParent);
                        sb.AppendSpans(
                            new[]
                            {
                                indentationHeader + "- " + item.PropertyInfo.Name + "\t\t",
                                CleanPropertyName(item.PropertyInfo.PropertyType.FullName),
                                sTopParent
                            },
                            brushArray);
                    }
                    sb.AppendFormat(indentationHeader + "count: {0}\r\n", ot.Inverses.Count());
                }
                sb.DefaultBrush = null;
                if (beVerbose > 0)
                    sb.AppendFormat("");
            }
            else
            {
                // test to see if it's a select type...
                
                var ifcModule2 = SchemaMetadatas[schema].Module;
                var selectType = ifcModule2.GetTypes().FirstOrDefault(t => t.Name == type);
                
                

                if (selectType == null)
                    return sb;

                
                sb.AppendFormat(indentationHeader + "=== {1}.{0} is an Express Select type", type, schema);
                
                
                var selectSubTypes = ifcModule2.GetTypes().Where(
                    t => t.GetInterfaces().Contains(selectType)
                    ).ToList();

                // sub interfaces 
                var subInt = selectSubTypes.Where(x => x.IsInterface);

                // all the ones whose superclass in the list are children, need removing
                var toRemove = selectSubTypes.Where(x => x.BaseType != null && selectSubTypes.Contains(x.BaseType)).ToArray();
                selectSubTypes = selectSubTypes.Except(toRemove).ToList();
                toRemove = selectSubTypes.Where(x => x.GetInterfaces().Intersect(subInt).Any()).ToArray();
                selectSubTypes = selectSubTypes.Except(toRemove).ToList();

                // can't remember what the following did, it was connected with some code cleanup needed in Essentials
                //

                //// CommontIF sets up the infrastructure to check for common interfaces shared by the select type elements
                //if (beVerbose > 1)
                //{
                //    Type[] commontIf = null;
                //    foreach (var item in selectSubTypes)
                //    {
                //        if (commontIf == null)
                //            commontIf = item.GetInterfaces();
                //        else
                //        {
                //            var chk = item.GetInterfaces();
                //            for (var i = 0; i < commontIf.Length; i++)
                //            {
                //                if (!chk.Contains(commontIf[i]))
                //                {
                //                    commontIf[i] = null;
                //                }
                //            }
                //        }
                //    }

                //    var existingIf = selectType.GetInterfaces();
                //    sb.AppendFormat(indentationHeader + "Interfaces: {0}", existingIf.Length);
                //    foreach (var item in existingIf)
                //    {
                //        sb.AppendFormat(indentationHeader + "- {0}", item.Name);
                //    }
                //    // need to remove implemented interfaces from the ones shared 
                //    for (var i = 0; i < commontIf.Length; i++)
                //    {
                //        if (commontIf[i] == selectType)
                //            commontIf[i] = null;
                //        if (existingIf.Contains(commontIf[i]))
                //        {
                //            commontIf[i] = null;
                //        }
                //    }

                //    foreach (var item in commontIf.Where(item => item != null))
                //    {
                //        sb.AppendFormat(indentationHeader + "Missing Common Interface: {0}", item.Name);
                //    }
                //}

                // just list the names first
                foreach (var item in selectSubTypes)
                {
                    sb.Append(ReportType(item.FullName, 0, indentationHeader + "  "));
                }
                // only report subitmes in higher verbosity
                if (beVerbose > 1)
                {
                    foreach (var item in selectSubTypes)
                    {
                        sb.Append(ReportType(item.FullName, beVerbose, indentationHeader + "  "));
                    }
                    sb.AppendFormat("");
                }
            }
            return sb;
        }

        private static int ChildTree(ExpressType ot, TextHighliter sb, string indentationHeader, int indent)
        {
            int count = 0;
            var sSpace = new string('#', indent);
            // sSpace = sSpace.Replace(new string[] { " " }, "  ");
            foreach (var item in ot.SubTypes.OrderBy(x=>x.Name))
            {
                var isAbstract = item.Type.IsAbstract ? " (abstract)" : "";
                count++;
                sb.AppendFormat(indentationHeader + sSpace + "# {0} {1}", item, isAbstract);
                count += ChildTree(item, sb, indentationHeader, indent + 1);
            }
            return count;
        }

        private TextHighliter ReportEntity(int entityLabel, int recursiveDepth = 0, int indentationLevel = 0,
            bool verbose = false, bool showValueType = false)
        {
            // Debug.WriteLine("EL: " + EntityLabel.ToString());
            var sb = new TextHighliter();
            var indentationHeader = new string('\t', indentationLevel);
            try
            {
                var entity = Model.Instances[entityLabel];
                if (entity != null)
                {
                    var ifcType = Model.Metadata.ExpressType(entity);

                    sb.Append(
                        string.Format(indentationHeader + "=== {0} [#{1}]", ifcType, entityLabel),
                        Brushes.Blue
                        );
                    var props = ifcType.Properties.Values;
                    if (props.Count > 0)
                        sb.AppendFormat(indentationHeader + "Properties: {0}", props.Count);
                    foreach (var prop in props)
                    {
                        var propLabels = ReportProp(sb, indentationHeader, entity, prop, verbose, showValueType);

                        foreach (var propLabel in propLabels)
                        {
                            if (
                                propLabel != entityLabel &&
                                (recursiveDepth > 0 || recursiveDepth < 0)
                                && propLabel != 0
                                )
                            {
                                sb.Append(ReportEntity(propLabel, recursiveDepth - 1, indentationLevel + 1));
                            }
                        }
                    }
                    var invs = ifcType.Inverses;
                    if (invs.Count() > 0)
                        sb.AppendFormat(indentationHeader + "Inverses: {0}", invs.Count());
                    foreach (var inverse in invs)
                    {
                        ReportProp(sb, indentationHeader, entity, inverse, verbose, showValueType);
                    }

                    /*
                     * suspended until more geomtery primitives are exposed.
                     * 
                    if (entity is IfcProduct)
                    {
                        IfcProduct p = entity as IfcProduct;
                        IXbimGeometryModel ret = XbimMesher.GenerateGeometry(Model, p);
                        var factorCubicMetre = Math.Pow(Model.GetModelFactors.OneMetre, 3);
                        sb.AppendFormat("XbimVolume: {0}\r\n", ret.Volume / factorCubicMetre);
                        
                        // looks for the product shape without subtractions/additions
                        // XbimMesher.GenerateGeometry(model, 
                        foreach (var representation in p.Representation.Representations)
	                    {
                            if (representation.RepresentationIdentifier == "Body")
                            {
                                IXbimGeometryModel uncut = XbimMesher.GenerateGeometry(Model, representation);  
                                if (uncut != null)
                                    sb.AppendFormat("XbimUncutVolume: {0}\r\n", uncut.Volume / factorCubicMetre);
                            }
	                    }
                    }
                    */
                }
                else
                {
                    sb.AppendFormat(indentationHeader + "=== Entity #{0} is null", entityLabel);
                }
            }
            catch (Exception ex)
            {
                sb.AppendFormat(indentationHeader + "\r\nException Thrown: {0} ({1})\r\n{2}", ex.Message,
                    ex.GetType().ToString(), ex.StackTrace);
            }
            return sb;
        }

        private static IEnumerable<int> ReportProp(TextHighliter sb, string indentationHeader, IPersistEntity entity,
            ExpressMetaProperty prop, bool verbose, bool showPropType)
        {
            var retIds = new List<int>();
            var propName = prop.PropertyInfo.Name;
            var propType = prop.PropertyInfo.PropertyType;
            var shortTypeName = CleanPropertyName(propType.FullName);
            var propVal = prop.PropertyInfo.GetValue(entity, null) ?? "<null>";

            if (prop.EntityAttribute.IsEnumerable)
            {
                var propCollection = propVal as System.Collections.IEnumerable;
                
                if (propCollection != null)
                {
                    propVal = "<empty>"; // default that gets replaced if values are found.
                    var iCntProp = 0;
                    foreach (var item in propCollection)
                    {
                        iCntProp++;
                        if (iCntProp == 1)
                            propVal = ReportPropValue(item, ref retIds, showPropType);
                        else
                        {
                            if (iCntProp == 2)
                            {
                                propVal = "\r\n" + indentationHeader + "    " + propVal;
                            }
                            propVal += "\r\n" + indentationHeader + "    " + ReportPropValue(item, ref retIds, showPropType);
                        }
                    }
                    if (iCntProp > 2)
                        propVal += "\r\n" + indentationHeader + "    " + "Count: " + iCntProp;

                }
                else
                {
                    propVal = propVal + " [not an enumerable]";
                }
            }
            else
                propVal = ReportPropValue(propVal, ref retIds, showPropType);

            if (verbose)
                sb.AppendFormat(indentationHeader + "- {0} ({1}): {2}",
                    propName, // 0
                    shortTypeName, // 1
                    propVal // 2
                    );
            else
            {
                if ((string) propVal != "<null>" && (string) propVal != "<empty>")
                {
                    sb.AppendFormat(indentationHeader + "- {0}: {1}",
                        propName, // 0
                        propVal // 1
                        );
                }
            }
            return retIds;
        }

        private static string CleanPropertyName(string shortTypeName)
        {
            var m = Regex.Match(shortTypeName, @"^((?<Mod>.*)`\d\[\[)*Xbim\.(?<Type>[\w\.]*)");
            if (!m.Success) 
                return shortTypeName;
            shortTypeName = m.Groups["Type"].Value; // + m.Groups["Type"].Value + 
            if (m.Groups["Mod"].Value == string.Empty) 
                return shortTypeName;
            var getLast = m.Groups["Mod"].Value.Split(new[] {"."}, StringSplitOptions.RemoveEmptyEntries);
            shortTypeName += " (" + getLast[getLast.Length - 1] + ")";
            return shortTypeName;
        }

        private static string ReportPropValue(object propVal, ref List<int> retIds, bool showPropType = false)
        {
            var pe = propVal as IPersistEntity;
            var propLabel = 0;
            if (pe != null)
            {
                propLabel = pe.EntityLabel;
                retIds.Add(pe.EntityLabel);
            }
            var ret = propVal.ToString();
            if (ret == propVal.GetType().FullName)
            {
                ret = propVal.GetType().Name;
                showPropType = false;
            }
            ret +=
                (
                (propLabel != 0) 
                ? " [#" + propLabel + "]" 
                : ""
                );
            if (pe as Xbim.Ifc2x3.Interfaces.IIfcCartesianPoint != null)
            {
                var n = pe as Xbim.Ifc2x3.Interfaces.IIfcCartesianPoint;
                var vals = n.Coordinates.Select(x => x.Value);
                ret += "\t" + string.Join("\t,\t", vals);
            }
            if (showPropType)
            {
                ret += " (" + CleanPropertyName(propVal.GetType().FullName) + ")";
            }
            return ret;
        }

        #region "Plugin"
        
        /// <summary>
        /// Component's header text in the UI
        /// </summary>
        public string WindowTitle => "Commands";

        /// <summary>
        /// All bindings are to be established in this call
        /// </summary>
        /// <param name="mainWindow"></param>
        public void BindUi(IXbimXplorerPluginMasterWindow mainWindow)
        {
            _parentWindow = mainWindow;
            SetBinding(SelectedItemProperty,
                new Binding("SelectedItem") {Source = mainWindow, Mode = BindingMode.OneWay});
            SetBinding(ModelProperty, new Binding());
                // whole datacontext binding, see http://stackoverflow.com/questions/8343928/how-can-i-create-a-binding-in-code-behind-that-doesnt-specify-a-path
        }

        public IPersistEntity SelectedEntity
        {
            get { return (IPersistEntity) GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public static DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedEntity", typeof (IPersistEntity), typeof (wdwCommands),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, OnSelectedEntityChanged));

        public IfcStore Model
        {
            get { return (IfcStore) GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        public bool ModelIsUnavailable {
            get
            {
                if (Model == null)
                {
                    ReportAdd("This command requires an open model.", Brushes.Red);
                    return true;
                }
                return false;
            }
        }

        public static DependencyProperty ModelProperty =
            DependencyProperty.Register("Model", typeof (IfcStore), typeof (wdwCommands),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, OnSelectedEntityChanged));

        private static void OnSelectedEntityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // if any UI event should happen it needs to be specified here
            var ctrl = d as wdwCommands;
            if (ctrl == null) 
                return;
            switch (e.Property.Name)
            {
                case "Model":
                    // ctrl.ReportAdd("Model updated");
                    break;
                case "SelectedEntity":
                    if (e.NewValue == null)
                        ctrl.ReportAdd("No entitiy selected");
                    else
                    {
                        ctrl.ReportAdd(
                            $"Selected entity label is: {Math.Abs(((IPersistEntity) e.NewValue).EntityLabel)}");
                    }
                    break;
            }
        }

        #endregion

        private void cmdRun(object sender, RoutedEventArgs e)
        {
            Execute();
            e.Handled = true;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            var hyperlink = sender as Hyperlink;
            if (hyperlink == null)
                throw new ArgumentNullException();

            if (e.Uri.Host == "entitylabel")
            {
                var lab = e.Uri.AbsolutePath.Substring(1);
                int iLabel;
                if (int.TryParse(lab, out iLabel))
                {
                    _parentWindow.SelectedItem = Model.Instances[iLabel];
                }
            }
        }
    }
}