using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Xbim.Common.Geometry;
using Xbim.Geometry.Engine.Interop;
using Xbim.Ifc2x3.GeometricConstraintResource;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using Xbim.Presentation;
using Xbim.Presentation.LayerStyling;
using Xbim.Presentation.LayerStylingV2;
using Xbim.Presentation.XplorerPluginSystem;
using Xbim.XbimExtensions.Interfaces;
using Xbim.XbimExtensions.SelectTypes;
using XbimGeometry.Interfaces;
using XbimXplorer.PluginSystem;
using XbimXplorer.Simplify;

// todo: see if gemini is a good candidate for a network based ui experience in xbim.
// https://github.com/tgjones/gemini
//

namespace XbimXplorer.Querying
{
    /// <summary>
    /// Interaction logic for wdwQuery.xaml
    /// </summary>
    [XplorerUiElement(PluginWindowUiContainerEnum.LayoutAnchorable, PluginWindowActivation.OnMenu, "View/Developer/Commands")]
    public partial class WdwQuery : IXbimXplorerPluginWindow
    {
        /// <summary>
        /// WindowsUI
        /// </summary>
        public WdwQuery()
        {
            InitializeComponent();
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

        private void Execute() {
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

                mdbclosed = Regex.Match(cmd, @"^Plugin Refresh$", RegexOptions.IgnoreCase);
                if (mdbclosed.Success)
                {
                    if (_parentWindow != null)
                        _parentWindow.RefreshPlugins();
                    continue;
                }

                mdbclosed = Regex.Match(cmd, @"^Plugin Config$", RegexOptions.IgnoreCase);
                if (mdbclosed.Success)
                {
                    var pc = new PluginsConfig();
                    pc.Show();
                    continue;
                }

                mdbclosed = Regex.Match(cmd, @"^Plugin Load (?<assemblyName>.+)$", RegexOptions.IgnoreCase);
                if (mdbclosed.Success)
                {
                    var assemblyName = mdbclosed.Groups["assemblyName"].Value;
                    if (File.Exists(assemblyName))
                        if (_parentWindow != null && _parentWindow is XplorerMainWindow)
                            ((XplorerMainWindow)_parentWindow).LoadPlugin(assemblyName);
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

                mdbclosed = Regex.Match(cmd, @"^convert (?<resource>.+)", RegexOptions.IgnoreCase);
                if (mdbclosed.Success)
                {
                    var resource = mdbclosed.Groups["resource"].Value;
                    if (Directory.Exists(resource))
                    {
                        ConvertDirectory(resource);
                    }
                    else if (File.Exists(resource))
                    {
                         ConvertFile(new FileInfo(resource));
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
                        ReportAdd(string.Format(" - {0}", asmName.FullName), Brushes.Black);
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
                            ReportAdd(string.Format("Autoclear not changed ({0} is not a valid option).", option));
                            continue;
                        }
                        ReportAdd(string.Format("Autoclear set to {0}", option.ToLower()));
                        continue;
                    }
// ReSharper disable once EmptyGeneralCatchClause
                    catch
                    {
                    }
                    TxtOut.Document = new FlowDocument();
                    continue;
                }


                if (Model == null)
                {
                    ReportAdd("Plaese open a database.", Brushes.Red);
                    continue;
                }

                // all commands here
                //
                var m = Regex.Match(cmd, @"^(entitylabel|el) (?<el>\d+)(?<recursion> -*\d+)*",
                    RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    var recursion = 0;
                    var v = Convert.ToInt32(m.Groups["el"].Value);
                    try
                    {
                        recursion = Convert.ToInt32(m.Groups["recursion"].Value);
                    }
// ReSharper disable once EmptyGeneralCatchClause
                    catch
                    {
                    }

                    ReportAdd(ReportEntity(v, recursion));
                    continue;
                }



                m = Regex.Match(cmd, @"^(Header|he)$", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    if (Model.Header == null)
                    {
                        ReportAdd("Model header is not defined.", Brushes.Red);
                        continue;
                    }
                    ReportAdd("FileDescription:");
                    foreach (var item in Model.Header.FileDescription.Description)
                    {
                        ReportAdd(string.Format("- Description: {0}", item));
                    }
                    ReportAdd(string.Format("- ImplementationLevel: {0}",
                        Model.Header.FileDescription.ImplementationLevel));
                    ReportAdd(string.Format("- EntityCount: {0}", Model.Header.FileDescription.EntityCount));

                    ReportAdd("FileName:");
                    ReportAdd(string.Format("- Name: {0}", Model.Header.FileName.Name));
                    ReportAdd(string.Format("- TimeStamp: {0}", Model.Header.FileName.TimeStamp));
                    foreach (var item in Model.Header.FileName.Organization)
                    {
                        ReportAdd(string.Format("- Organization: {0}", item));
                    }
                    ReportAdd(string.Format("- OriginatingSystem: {0}", Model.Header.FileName.OriginatingSystem));
                    ReportAdd(string.Format("- PreprocessorVersion: {0}", Model.Header.FileName.PreprocessorVersion));
                    foreach (var item in Model.Header.FileName.AuthorName)
                    {
                        ReportAdd(string.Format("- AuthorName: {0}", item));
                    }

                    ReportAdd(string.Format("- AuthorizationName: {0}", Model.Header.FileName.AuthorizationName));
                    foreach (var item in Model.Header.FileName.AuthorizationMailingAddress)
                    {
                        ReportAdd(string.Format("- AuthorizationMailingAddress: {0}", item));
                    }

                    ReportAdd("FileSchema:");
                    foreach (var item in Model.Header.FileSchema.Schemas)
                    {
                        ReportAdd(string.Format("- Schema: {0}", item));
                    }
                    continue;
                }

                // SelectionHighlighting [WholeMesh|Normals]
                m = Regex.Match(cmd, @"^(SelectionHighlighting|sh) (?<mode>(wholemesh|normals|wireframe))+",
                    RegexOptions.IgnoreCase);
                if (m.Success)
                {
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

                m = Regex.Match(cmd, @"^(IfcSchema|is) (?<mode>(list|count|short|full) )*(?<type>\w+)[ ]*",
                    RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    var type = m.Groups["type"].Value;
                    var mode = m.Groups["mode"].Value;

                    if (type == "/")
                    {
                        // this is a magic case handled by the matchingType
                    }
                    else if (type == PrepareRegex(type))
                        // there's not a regex expression, we will prepare one assuming the search for a bare name.
                    {
                        type = @".*\." + type + "$";
                        // any character repeated then a dot then the name and the end of line
                    }
                    else
                        type = PrepareRegex(type);

                    var typeList = MatchingTypes(type);


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

                m = Regex.Match(cmd, @"^(reload|re) *(?<entities>([\d,]+|[^ ]+))", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    var start = m.Groups["entities"].Value;
                    IEnumerable<int> labels = ToIntarray(start, ',');
                    if (labels.Any())
                    {
                        _parentWindow.DrawingControl.LoadGeometry(Model, labels);
                    }
                    else
                    {
                        _parentWindow.DrawingControl.LoadGeometry(Model);
                    }
                    continue;
                }

                m = Regex.Match(cmd, @"^(GeometryInfo|gi) (?<mode>(binary|viewer) )*(?<entities>([\d,]+|[^ ]+))",
                    RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    var start = m.Groups["entities"].Value;
                    var mode = m.Groups["mode"].Value;
                    IEnumerable<int> labels = ToIntarray(start, ',');
                    foreach (var item in labels)
                    {
                        ReportAdd("Geometry for: " + item, Brushes.Green);
                        ReportAdd(GeomQuerying.GeomInfoBoundBox(Model, item));
                        ReportAdd(GeomQuerying.GeomLayers(Model, item, _parentWindow.DrawingControl.Scenes));
                        if (mode == "binary ")
                        {
                            ReportAdd(GeomQuerying.GeomInfoMesh(Model, item));
                        }
                        if (mode == "viewer ")
                        {
                            ReportAdd(
                                GeomQuerying.Viewerdata(_parentWindow.DrawingControl, Model, item)
                                );
                        }
                    }
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
                    var labels = GetSelection(m);
                    if (labels.Any())
                    {
                        var engine = new XbimGeometryEngine();
                        foreach (var label in labels)
                        {
                            var entity = Model.Instances[label];
                            if (entity == null)
                            {
                                ReportAdd($"=== Entity {label} not found in model.", Brushes.Red);
                                continue;
                            }
                            ReportAdd($"=== Geometry functions for {entity.GetType().Name} #{label}", Brushes.Blue);
                            // todo: cache methods by type
                            var methods =
                                typeof(XbimGeometryEngine).GetMethods(BindingFlags.Public | BindingFlags.Instance);
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
                                var functionShort = $"{methodInfo.Name}({firstParam.ParameterType.Name})";
                                ReportAdd($"- {functionShort}");
                                try
                                {
                                    var ret = methodInfo.Invoke(engine, new object[] { entity });
                                    if (ret != null)
                                    {
                                        var sol = ret as IXbimSolid;
                                        if (sol != null)
                                        {
                                            if (sol.IsValid)
                                                ReportAdd($"  ok, returned {ret.GetType().Name}", Brushes.Green);
                                            else
                                                ReportAdd($"  Err, returned {ret.GetType().Name} (not valid)", Brushes.Red);
                                        }
                                        else
                                        {
                                            ReportAdd($"  ok, returned {ret.GetType().Name} ({ret})", Brushes.Green);
                                        }
                                    }
                                    else
                                    {
                                        ReportAdd($"  returned value is null (function return type is ${methodInfo.ReturnType.Name})", Brushes.Orange);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    var msg = $"  Failed on {functionShort} for #{label}. {ex.Message}";
                                    ReportAdd(msg, Brushes.Red);
                                }
                            }
                        }
                    }
                    continue;
                }
                
                m = Regex.Match(cmd,
                    @"^(select|se) " +
                    @"(top (?<top>\d+) )*" +
                    @"(?<mode>(count|list|typelist|short|full) )*" +
                    @"(?<tt>(transverse|tt) )*" +
                    @"(?<hi>(highlight|hi) )*" +
                    @"(?<svt>(showvaluetype|svt) )*" +
                    @"(?<start>([\d,-]+|[^ ]+)) *(?<props>.*)",
                    RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    var start = m.Groups["start"].Value;
                    var props = m.Groups["props"].Value;
                    var mode = m.Groups["mode"].Value;
                    var svt = m.Groups["svt"].Value;
                    var top = m.Groups["top"].Value;
                    var iTop = -1;
                    if (top != string.Empty)
                        iTop = Convert.ToInt32(top);


                    // transverse tree mode
                    var transverseT = false;
                    var transverse = m.Groups["tt"].Value;
                    if (transverse != "")
                        transverseT = true;

                    var highlight = false;
                    var highlightT = m.Groups["hi"].Value;
                    if (highlightT != "")
                        highlight = true;

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

                    // textual report
                    switch (mode.ToLower())
                    {
                        case "count ":
                            ReportAdd(string.Format("Count: {0}", ret.Count()));
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
                                ReportAdd(item + "\t" + Model.Instances[item].IfcType().Name);
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
                    // visual selection
                    if (highlight)
                    {
                        var s = new EntitySelection();
                        foreach (var item in ret)
                        {
                            s.Add(Model.Instances[item]);
                        }
                        var t = new Stopwatch();
                        t.Start();
                        _parentWindow.DrawingControl.Selection = s;
                        t.Stop();
                        Debug.WriteLine("SelectionHighlight: \t" + t.ElapsedMilliseconds);

                    }
                    continue;
                }

                m = Regex.Match(cmd, @"^(ObjectPlacement|OP) " +
                                     @"(?<EntityId>\d+)"
                                     , RegexOptions.IgnoreCase);
                if (m.Success)
                {
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
                    var entityIds = m.Groups["EntityIds"].Value;
                    var v = entityIds.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

                    var sb = new TextHighliter();

                    foreach (var entityIdString in v)
                    {
                        var entityId = Convert.ToInt32(entityIdString);
                        var ent = Model.Instances[entityId];
                        if (!(ent is IfcProduct))
                        {
                            ReportAdd($"Entity not found #{entityId}");
                            continue;
                        }
                        ReportTransformGraph(sb, ent as IfcProduct, 0);
                    }
                    ReportAdd(sb);
                    continue;
                }

                m = Regex.Match(cmd, @"^zoom (" +
                                     @"(?<RegionName>.+$)" +
                                     ")", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    var rName = m.Groups["RegionName"].Value;
                    var regionData = Model.GetGeometryData(XbimGeometryType.Region).FirstOrDefault();
                    if (regionData == null)
                    {
                        ReportAdd("data not found");
                    }
                    var regions = XbimRegionCollection.FromArray(regionData.ShapeData);
                    var reg = regions.FirstOrDefault(x => x.Name == rName);
                    if (reg != null)
                    {
                        var mcp = XbimMatrix3D.Copy(_parentWindow.DrawingControl.ModelPositions[Model].Transfrom);
                        var tC = mcp.Transform(reg.Centre);
                        var tS = mcp.Transform(reg.Size);
                        var r3D = new XbimRect3D(
                            tC.X - tS.X/2, tC.Y - tS.Y/2, tC.Z - tS.Z/2,
                            tS.X, tS.X, tS.Z
                            );
                        _parentWindow.DrawingControl.ZoomTo(r3D);
                        _parentWindow.Activate();
                        continue;
                    }
                    else
                    {
                        ReportAdd(string.Format("Something wrong with region name: '{0}'", rName));
                        ReportAdd("Names that should work are: ");
                        foreach (var str in regions)
                        {
                            ReportAdd(string.Format(" - '{0}'", str.Name));
                        }
                        continue;
                    }
                }

                m = Regex.Match(cmd, @"^clip off$", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    _parentWindow.DrawingControl.ClearCutPlane();
                    ReportAdd("Clip removed");
                    _parentWindow.Activate();
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
                        var msg = "";
                        var storName = m.Groups["StoreyName"].Value;
                        var storey =
                            Model.Instances.OfType<IfcBuildingStorey>().FirstOrDefault(x => x.Name == storName);
                        if (storey != null)
                        {

                            //get the object position data (should only be one)
                            if (Model.GeometrySupportLevel == 1)
                            {
                                var geomdata =
                                    Model.GetGeometryData(storey.EntityLabel, XbimGeometryType.TransformOnly)
                                        .FirstOrDefault();
                                if (geomdata != null)
                                {
                                    var pt = new XbimPoint3D(0, 0,
                                        XbimMatrix3D.FromArray(geomdata.DataArray2).OffsetZ);
                                    var mcp = XbimMatrix3D.Copy(_parentWindow.DrawingControl.ModelPositions[Model].Transfrom);
                                    var transformed = mcp.Transform(pt);
                                    msg = string.Format("Clip 1m above storey elevation {0} (View space height: {1})", pt.Z, transformed.Z + 1);
                                    pz = transformed.Z + 1;
                                }
                            }
                            else if (Model.GeometrySupportLevel == 2)
                            {
                                var v = new TransformGraph(Model);
                                v.AddProduct(storey);
                                var v2 = v[storey].WorldMatrix();
                                var pt = new XbimPoint3D(0, 0, v2.OffsetZ);

                                var mcp = XbimMatrix3D.Copy(_parentWindow.DrawingControl.ModelPositions[Model].Transfrom);
                                var transformed = mcp.Transform(pt);
                                msg = string.Format("Clip 1m above storey elevation {0} (View space height: {1})", pt.Z, transformed.Z + 1);
                                pz = transformed.Z + 1;
                            }
                        }
                        if (msg == "")
                        {
                            ReportAdd(string.Format("Something wrong with storey name: '{0}'", storName));
                            ReportAdd("Names that should work are: ");
                            var strs = Model.Instances.OfType<IfcBuildingStorey>();
                            foreach (var str in strs)
                            {
                                ReportAdd(string.Format(" - '{0}'", str.Name));
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

                m = Regex.Match(cmd, @"^Styler (?<command>.+)", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    var st = _parentWindow.DrawingControl.LayerStyler as LayerStylerTypeAndIfcStyleExtended;
                    if (st != null)
                    {
                        var command = m.Groups["command"].Value;
                        ReportAdd(
                            st.SendCommand(command, _parentWindow.DrawingControl.Selection)
                            );
                        _parentWindow.DrawingControl.ReloadModel();
                    }
                    else
                    {
                        ReportAdd("Command not valid under current styler configuration.");
                    }
                    continue;
                }

                m = Regex.Match(cmd, @"^Visual (?<action>list|tree|on|off|mode)( (?<Name>[^ ]+))*",
                    RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    var parName = m.Groups["Name"].Value;
                    if (m.Groups["action"].Value.ToLowerInvariant() == "list")
                    {
                        foreach (var item in _parentWindow.DrawingControl.ListItems(parName))
                        {
                            ReportAdd(item);
                        }
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
                        parName = parName.ToLowerInvariant();
                        var sb = new StringBuilder();

                        // foreach model
                        if (_parentWindow.DrawingControl.LayerStylerForceVersion1 || Model.GeometrySupportLevel == 1)
                            ReportAdd(string.Format(@"Current mode is {0}",
                                _parentWindow.DrawingControl.LayerStyler.GetType())
                                , Brushes.Green);
                        else if (Model.GeometrySupportLevel == 2)
                            ReportAdd(string.Format(@"Current mode is {0}",
                                _parentWindow.DrawingControl.GeomSupport2LayerStyler.GetType()), Brushes.Green);
                        else
                            ReportAdd(@"Visual mode not enabled on GeometrySupportLevel 0", Brushes.Red);
                        

                        bool stylerSet = false;

                        var v1 = TypesImplementingInterface(typeof (ILayerStyler));
                        foreach (var instance in v1.Where(IsRealClass))
                        {
                            if (instance.Name.ToLowerInvariant() == parName ||
                                instance.FullName.ToLowerInvariant() == parName)
                            {
                                _parentWindow.DrawingControl.LayerStylerForceVersion1 = true;
                                _parentWindow.DrawingControl.LayerStyler =
                                    (ILayerStyler) Activator.CreateInstance(instance);
                                _parentWindow.DrawingControl.FederationLayerStyler =
                                    (ILayerStyler)Activator.CreateInstance(instance);


                                _parentWindow.DrawingControl.ReloadModel(
                                    options: DrawingControl3D.ModelRefreshOptions.ViewPreserveAll
                                    );
                                ReportAdd("Visual mode set to " + instance.FullName + ".", Brushes.Orange);
                                stylerSet = true;
                                continue;
                            }
                            sb.AppendLine(" - " + instance.FullName);
                        }
                        var v2 = TypesImplementingInterface(typeof(ILayerStylerV2));
                        foreach (var instance in v2.Where(IsRealClass))
                        {
                            if (instance.Name.ToLowerInvariant() == parName ||
                                instance.FullName.ToLowerInvariant() == parName)
                            {
                                _parentWindow.DrawingControl.LayerStylerForceVersion1 = false;
                                _parentWindow.DrawingControl.GeomSupport2LayerStyler =
                                    (ILayerStylerV2) Activator.CreateInstance(instance);
                                _parentWindow.DrawingControl.ReloadModel(
                                    options: DrawingControl3D.ModelRefreshOptions.ViewPreserveAll
                                    );
                                ReportAdd("Visual mode set to " + instance.FullName + ".", Brushes.Orange);
                                stylerSet = true;
                                continue;
                            }
                            sb.AppendLine(" - " + instance.FullName);
                        }
                        if (!stylerSet)
                            ReportAdd(string.Format("Nothing done; valid modes are:\r\n{0}", sb));
                    }
                    else
                    {
                        var bVis = m.Groups["action"].Value.ToLowerInvariant() == "on";
                        _parentWindow.DrawingControl.SetVisibility(parName, bVis);
                    }
                    continue;
                }
                m = Regex.Match(cmd, @"^SimplifyGUI$", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    var s = new IfcSimplify();
                    s.Show();
                    continue;
                }

                m = Regex.Match(cmd, @"^test$", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    var recursiveElement = Model.Instances[582800] as IfcBuildingStorey;
                    foreach (
                        var prod in recursiveElement.ContainsElements.SelectMany(container => container.RelatedElements)
                    )
                    {
                        Debug.WriteLine(prod.EntityLabel);
                    }


                    continue;
                }
                ReportAdd(string.Format("Command not understood: {0}.", cmd));
            }
        }
        
        private void ReportObjectPlacement(TextHighliter sb, IPersistIfcEntity ent, int indentation)
        {

            var indentationHeader = new string('\t', indentation);

            if (ent is IfcProduct)
            {
                var asprod = ent as IfcProduct;
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
            else if (ent is IfcLocalPlacement)
            {
                var asLocalPlacement = ent as IfcLocalPlacement;
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
            else if (ent is IfcAxis2Placement3D)
            {
                var asLocalPlacement = ent as IfcAxis2Placement3D;
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
                var ifcType = IfcMetaData.IfcType(entity);
                var props = ifcType.IfcProperties.Values;
                foreach (var expressMetaProperty in props)
                {
                    var t = expressMetaProperty.PropertyInfo.PropertyType;
                    var propVal = expressMetaProperty.PropertyInfo.GetValue(entity, null);

                    var v = EvaluateInclusion(propVal, mode, outList);
                    if (v != -1)
                        inQueue.Enqueue(v);
                    else if (expressMetaProperty.IfcAttribute.IsEnumerable)
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
            //_surfaceOrSolidTypes.AddRange(ExpressType(typeof(Xbim.Ifc2x3.GeometryResource.IfcSurface)).NonAbstractSubTypes.Select(x => x.Type));
            //_surfaceOrSolidTypes.AddRange(SchemaMetadatas["ifc4"].ExpressType(typeof(Xbim.Ifc4.GeometryResource.IfcSurface)).NonAbstractSubTypes.Select(x => x.Type));


            //_surfaceOrSolidTypes.AddRange(SchemaMetadatas["ifc2x3"].ExpressType(typeof(Xbim.Ifc2x3.GeometryResource.IfcSurface)).NonAbstractSubTypes.Select(x => x.Type));
            //_surfaceOrSolidTypes.AddRange(SchemaMetadatas["ifc4"].ExpressType(typeof(Xbim.Ifc4.GeometryResource.IfcSurface)).NonAbstractSubTypes.Select(x => x.Type));

            //_surfaceOrSolidTypes.AddRange(SchemaMetadatas["ifc2x3"].ExpressType(typeof(Xbim.Ifc2x3.GeometricModelResource.IfcCsgPrimitive3D)).NonAbstractSubTypes.Select(x => x.Type));
            //_surfaceOrSolidTypes.AddRange(SchemaMetadatas["ifc4"].ExpressType(typeof(Xbim.Ifc4.GeometricModelResource.IfcCsgPrimitive3D)).NonAbstractSubTypes.Select(x => x.Type));

            //_surfaceOrSolidTypes.AddRange(SchemaMetadatas["ifc2x3"].ExpressType(typeof(Xbim.Ifc2x3.GeometricModelResource.IfcBooleanResult)).NonAbstractSubTypes.Select(x => x.Type));
            //_surfaceOrSolidTypes.AddRange(SchemaMetadatas["ifc4"].ExpressType(typeof(Xbim.Ifc4.GeometricModelResource.IfcBooleanResult)).NonAbstractSubTypes.Select(x => x.Type));

            //_surfaceOrSolidTypes.AddRange(SchemaMetadatas["ifc2x3"].ExpressType(typeof(Xbim.Ifc2x3.GeometricModelResource.IfcHalfSpaceSolid)).NonAbstractSubTypes.Select(x => x.Type));
            //_surfaceOrSolidTypes.AddRange(SchemaMetadatas["ifc4"].ExpressType(typeof(Xbim.Ifc4.GeometricModelResource.IfcHalfSpaceSolid)).NonAbstractSubTypes.Select(x => x.Type));

            //_surfaceOrSolidTypes.AddRange(SchemaMetadatas["ifc2x3"].ExpressType(typeof(Xbim.Ifc2x3.GeometricModelResource.IfcSolidModel)).NonAbstractSubTypes.Select(x => x.Type));
            //_surfaceOrSolidTypes.AddRange(SchemaMetadatas["ifc4"].ExpressType(typeof(Xbim.Ifc4.GeometricModelResource.IfcSolidModel)).NonAbstractSubTypes.Select(x => x.Type));

            //_surfaceOrSolidTypes.AddRange(SchemaMetadatas["ifc2x3"].ExpressType(typeof(Xbim.Ifc2x3.GeometricModelResource.IfcFaceBasedSurfaceModel)).NonAbstractSubTypes.Select(x => x.Type));
            //_surfaceOrSolidTypes.AddRange(SchemaMetadatas["ifc4"].ExpressType(typeof(Xbim.Ifc4.GeometricModelResource.IfcFaceBasedSurfaceModel)).NonAbstractSubTypes.Select(x => x.Type));

            _wireTypes = new List<Type>();
            //_wireTypes.AddRange(SchemaMetadatas["ifc2x3"].ExpressType(typeof(Xbim.Ifc2x3.GeometryResource.IfcCurve)).NonAbstractSubTypes.Select(x => x.Type));
            //_wireTypes.AddRange(SchemaMetadatas["ifc4"].ExpressType(typeof(Xbim.Ifc4.GeometryResource.IfcCurve)).NonAbstractSubTypes.Select(x => x.Type));

            //_wireTypes.AddRange(SchemaMetadatas["ifc2x3"].ExpressType(typeof(Xbim.Ifc2x3.GeometryResource.IfcCompositeCurveSegment)).NonAbstractSubTypes.Select(x => x.Type));
            //_wireTypes.AddRange(SchemaMetadatas["ifc4"].ExpressType(typeof(Xbim.Ifc4.GeometryResource.IfcCompositeCurveSegment)).NonAbstractSubTypes.Select(x => x.Type));
        }


        private int EvaluateInclusion(object entityO, RepresentationItemSelectionMode mode, List<int> outList)
        {
            var entity = entityO as IPersistIfcEntity;
            if (entity == null)
                return -1;
            var t = entity.GetType();

            if (mode == RepresentationItemSelectionMode.all && (typeof(IfcRepresentationItem)).IsAssignableFrom(t))
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

        private void ConvertDirectory(string resource)
        {
            ReportAdd($"Attempting to convert directory {resource}", Brushes.Black);
            var di = new DirectoryInfo(resource);
            foreach (var ifcfile in di.GetFiles("*.ifc"))
            {
                ConvertFile(ifcfile);
            }
        }

        private void ConvertFile(FileInfo ifcfile)
        {
            var ximfilename = new FileInfo(Path.ChangeExtension(ifcfile.FullName, "xbim"));
            if (!ximfilename.Exists)
                ConvertIt(ifcfile.FullName, ximfilename.FullName);

        }

        private void ConvertIt(string origFileName, string xbimFileName)
        {
            ReportAdd($"Attempting to convert file.\r\n\torigin: {origFileName}\r\n\tdestination: {xbimFileName}", Brushes.Black);
            using (var model = new XbimModel())
            {
                ReportAdd("\t\tParsing");
                model.CreateFrom(origFileName, xbimFileName, null, true);
                ReportAdd("\t\tMeshing");
                var geomContext = new Xbim3DModelContext(model);
                geomContext.CreateContext(XbimGeometryType.PolyhedronBinary, adjustWCS: false);
                model.Close();
                ReportAdd("\t\tFinished");
            }
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
                    if (int.TryParse(sa[i], out j))
                    {
                        ia.Add(j);
                    }
                }
            }
            return ia.ToArray();
        }

        private void ReportTransformGraph(TextHighliter sb, IfcProduct ent, int i)
        {
            var v = new TransformGraph(Model);
            v.AddProduct(ent);

            // var pt = new XbimPoint3D(0, 0, v2.OffsetZ);

            sb.Append(
                string.Format("=== #{0} ({1}) ", ent.EntityLabel, ent.GetType().Name),
                Brushes.Blue
                );
            sb.Append(
                    string.Format("   Local matrix:"),
                    Brushes.Black
                );
            ReportMatrix(sb, v[ent].LocalMatrix);

            sb.Append(
                    string.Format("   World matrix:"),
                    Brushes.Black
                );
            ReportMatrix(sb, v[ent].WorldMatrix());
        }

        private void ReportMatrix(TextHighliter sb, XbimMatrix3D matrix)
        {
            var frmt = "G7";
            sb.Append(string.Format("   \t{0,10}\t{1,10}\t{2,10}\t{3,10}", matrix.M11.ToString(frmt), matrix.M21.ToString(frmt), matrix.M31.ToString(frmt), matrix.OffsetX.ToString(frmt)), Brushes.Black);
            sb.Append(string.Format("   \t{0,10}\t{1,10}\t{2,10}\t{3,10}", matrix.M12.ToString(frmt), matrix.M22.ToString(frmt), matrix.M32.ToString(frmt), matrix.OffsetY.ToString(frmt)), Brushes.Black);
            sb.Append(string.Format("   \t{0,10}\t{1,10}\t{2,10}\t{3,10}", matrix.M13.ToString(frmt), matrix.M23.ToString(frmt), matrix.M33.ToString(frmt), matrix.OffsetZ.ToString(frmt)), Brushes.Black);
            sb.Append(string.Format("   \t{0,10}\t{1,10}\t{2,10}\t{3,10}", matrix.M14.ToString(frmt), matrix.M24.ToString(frmt), matrix.M34.ToString(frmt), matrix.M44.ToString(frmt)), Brushes.Black);
            sb.Append("", Brushes.Black);
        }

        private void DisplayHelp()
        {
            var t = new TextHighliter();

            t.AppendFormat("Commands:");
            t.AppendFormat(
                "- select [count|list|typelist|full|short] [tt|transverse] [hi|highlight] [svt|showvaluetype] <startingElement> [Property [Property...]]");
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

            t.AppendFormat("- EntityLabel label [recursion]");
            t.Append("    [recursion] is an int representing the depth of children to report", Brushes.Gray);

            t.AppendFormat("- IfcSchema [list|count|short|full] <TypeName>");
            t.Append("    <TypeName> can contain wildcards", Brushes.Gray);
            t.Append("    use / in <TypeName> to select all root types", Brushes.Gray);

            t.AppendFormat("- GeometryInfo [binary|viewer] <EntityLabel,<EntityLabel>>");
            t.Append("    Provide textual information on meshes.", Brushes.Gray);

            t.AppendFormat("- Reload <EntityLabel,<EntityLabel>>");
            t.Append("    <EntityLabel> filters the elements to load in the viewer.", Brushes.Gray);

            t.AppendFormat("- clip [off|<Elevation>|<px>, <py>, <pz>, <nx>, <ny>, <nz>|<Storey name>]");
            t.Append("    Clipping the 3D model is still and unstable feature. Use with caution.", Brushes.Gray);

            t.AppendFormat("- ObjectPlacement <EntityLabel>");
            t.Append("    Displays the place tree of an element.", Brushes.Gray);
            
            t.AppendFormat("- zoom <Region name>");
            t.Append("    'zoom ?' provides a list of valid region names", Brushes.Gray);

            t.AppendFormat("- Visual [list|tree|[on|off <name>]|mode <ModeCommand>]");
            t.Append("    'Visual list' provides a list of valid layer names", Brushes.Gray);
            t.Append("    'Visual tree' provides a tree layer structure", Brushes.Gray);
            t.Append("    'Visual mode ...' changes the mode of the layer tree structure", Brushes.Gray);
            t.Append("      <ModeCommand> in: type, entity, oddeven or demo.", Brushes.Gray);


            t.AppendFormat("- clear [on|off]");

            t.AppendFormat("- SelectionHighlighting [WholeMesh|Normals]");
            t.Append("    defines the graphical style for selection highliting.", Brushes.Gray);

            t.AppendFormat("- SimplifyGUI");
            t.Append("    opens a GUI for simplifying IFC files (useful for debugging purposes).", Brushes.Gray);

            t.AppendFormat("- Convert <FileName|FolderName>");
            t.Append("    converts ifc files to xbim", Brushes.Gray);


            t.AppendFormat("");
            t.Append("Commands are executed on <ctrl>+<Enter>.", Brushes.Blue);
            t.AppendFormat("double slash (//) are the comments token and the remainder of lines is ignored.");
            t.AppendFormat("If a portion of text is selected, only selected text will be executed.");

            t.DropInto(TxtOut.Document);
        }


        /// <summary>
        /// Finds relevant classes through reflection by Namespace + Name query
        /// </summary>
        /// <param name="regExString">The regex string to be compared to the namespace</param>
        /// <returns>Enumerable string of full type name, with namespace</returns>
        private IEnumerable<string> MatchingTypes(string regExString)
        {
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
                            ))
                    )
                {
                    if (regExString == "/")
                    {
                        if (type.BaseType == typeof (object))
                            yield return type.FullName;
                    }
                    else
                    {
                        if (re.IsMatch(type.FullName))
                            yield return type.FullName;
                    }
                }
            }
        }

        private static string PrepareRegex(string rex)
        {
            short ishort;
            if (short.TryParse(rex, out ishort))
            {
                // build the regex string from the typeid
                //
                var t = IfcMetaData.IfcType(ishort);
                return  @".*\." + t.Name + "$";
            }

            rex = rex.Replace(".", @"\."); //escaped dot
            rex = rex.Replace("*", ".*");
            rex = rex.Replace("?", ".");
            return rex;
        }

        private TextHighliter ReportType(string type, int beVerbose, string indentationHeader = "")
        {
            var tarr = type.Split(new[] {"."}, StringSplitOptions.RemoveEmptyEntries);
            type = tarr[tarr.Length - 1];


            var sb = new TextHighliter();

            var ot = IfcMetaData.IfcType(type.ToUpper());
            if (ot != null)
            {
                sb.Append(
                    string.Format(indentationHeader + "=== {0}", ot.Name),
                    Brushes.Blue
                    );

                sb.AppendFormat(indentationHeader + "Namespace: {0}", ot.Type.Namespace);
                // sb.AppendFormat(indentationHeader + "Xbim Type Id: {0}", ot.TypeId);
                sb.DefaultBrush = Brushes.DarkOrange;
                var supertypes = new List<string>();
                var iterSuper = ot.IfcSuperType;
                while (iterSuper != null)
                {
                    supertypes.Add(iterSuper.Name);
                    iterSuper = iterSuper.IfcSuperType;
                }
                if (ot.IfcSuperType != null)
                    sb.AppendFormat(indentationHeader + "Parents hierarchy: {0}",
                        string.Join(" => ", supertypes.ToArray()));
                if (ot.IfcSubTypes.Count > 0)
                {
                    if (beVerbose > 1)
                    {
                        sb.DefaultBrush = null;
                        sb.AppendFormat(indentationHeader + "Subtypes tree:");
                        sb.DefaultBrush = Brushes.DarkOrange;
                        ChildTree(ot, sb, indentationHeader, 0);
                    }
                    else
                    {
                        sb.DefaultBrush = null;
                        sb.AppendFormat(indentationHeader + "Subtypes: {0}", ot.IfcSubTypes.Count);
                        sb.DefaultBrush = Brushes.DarkOrange;
                        foreach (var item in ot.IfcSubTypes)
                        {
                            sb.AppendFormat(indentationHeader + "- {0}", item);
                        }
                    }
                }
                if (beVerbose > 0)
                {
                    if (beVerbose > 1)
                    {
                        var allSub = ot.NonAbstractSubTypes;
                        sb.DefaultBrush = null;
                        sb.AppendFormat(indentationHeader + "All non abstract subtypes: {0}", allSub.Count());
                        sb.DefaultBrush = Brushes.DarkOrange;
                        foreach (var item in allSub)
                        {
                            sb.AppendFormat(indentationHeader + "- {0}", item.Name);
                        }
                    }
                    sb.DefaultBrush = null;
                    sb.AppendFormat(indentationHeader + "xbim.TypeId: {0}", ot.TypeId);
                    sb.AppendFormat(indentationHeader + "Interfaces: {0}", ot.Type.GetInterfaces().Count());
                    sb.DefaultBrush = Brushes.DarkOrange;
                    foreach (var item in ot.Type.GetInterfaces())
                    {
                        sb.AppendFormat(indentationHeader + "- {0}", item.Name);
                    }

                    sb.DefaultBrush = null;
                    // sb.DefaultBrush = Brushes.DimGray;
                    sb.AppendFormat(indentationHeader + "Properties: {0}", ot.IfcProperties.Count());
                    sb.DefaultBrush = null;
                    var brushArray = new Brush[]
                    {
                        Brushes.DimGray,
                        Brushes.DarkGray,
                        Brushes.DimGray
                    };
                    foreach (var item in ot.IfcProperties.Values)
                    {

                        var topParent = ot.IfcSuperType;
                        var sTopParent = "";
                        while (topParent != null &&
                               topParent.IfcProperties.Any(x => x.Value.PropertyInfo.Name == item.PropertyInfo.Name))
                        {
                            sTopParent = " \tfrom: " + topParent;
                            topParent = topParent.IfcSuperType;
                        }
                        sb.AppendSpans(
                            new[]
                            {
                                indentationHeader + "- " + item.PropertyInfo.Name + "\t\t",
                                CleanPropertyName(item.PropertyInfo.PropertyType.FullName),
                                sTopParent
                            },
                            brushArray);


                        // sb.AppendFormat(\t{1}{2}", , , );
                    }
                    sb.AppendFormat(indentationHeader + "Inverses: {0}", ot.IfcInverses.Count());
                    foreach (var item in ot.IfcInverses)
                    {
                        var topParent = ot.IfcSuperType;
                        var sTopParent = "";
                        while (topParent != null &&
                               topParent.IfcInverses.Any(x => x.PropertyInfo.Name == item.PropertyInfo.Name))
                        {
                            sTopParent = " \tfrom: " + topParent;
                            topParent = topParent.IfcSuperType;
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
                }
                sb.DefaultBrush = null;
                sb.AppendFormat("");
            }
            else
            {
                // test to see if it's a select type...

                var ifcModule2 = typeof (IfcMaterialSelect).Module;
                var selectType = ifcModule2.GetTypes().FirstOrDefault(t => t.Name.Contains(type));

                if (selectType == null)
                    return sb;
                sb.AppendFormat("=== {0} is a Select type", type);
                var ifcModule = typeof (IfcActor).Module;
                var selectSubTypes = ifcModule.GetTypes().Where(
                    t => t.GetInterfaces().Contains(selectType)
                    ).ToArray();

                // CommontIF sets up the infrastructure to check for common interfaces shared by the select type elements
                Type[] commontIf = null;
                foreach (var item in selectSubTypes)
                {
                    if (commontIf == null)
                        commontIf = item.GetInterfaces();
                    else
                    {
                        var chk = item.GetInterfaces();
                        for (var i = 0; i < commontIf.Length; i++)
                        {
                            if (!chk.Contains(commontIf[i]))
                            {
                                commontIf[i] = null;
                            }
                        }
                    }
                }

                var existingIf = selectType.GetInterfaces();
                sb.AppendFormat(indentationHeader + "Interfaces: {0}", existingIf.Length);
                foreach (var item in existingIf)
                {
                    sb.AppendFormat(indentationHeader + "- {0}", item.Name);
                }
                // need to remove implemented interfaces from the ones shared 
                for (var i = 0; i < commontIf.Length; i++)
                {
                    if (commontIf[i] == selectType)
                        commontIf[i] = null;
                    if (existingIf.Contains(commontIf[i]))
                    {
                        commontIf[i] = null;
                    }
                }

                foreach (var item in commontIf.Where(item => item != null))
                {
                    sb.AppendFormat(indentationHeader + "Missing Common Interface: {0}", item.Name);
                }
                if (beVerbose == 1)
                {
                    foreach (var item in selectSubTypes)
                    {
                        sb.Append(ReportType(item.Name, beVerbose, indentationHeader + "  "));
                    }
                }
                sb.AppendFormat("");
            }

            return sb;
        }

        private static void ChildTree(IfcType ot, TextHighliter sb, string indentationHeader, int indent)
        {
            var sSpace = new string(' ', indent*2);
            // sSpace = sSpace.Replace(new string[] { " " }, "  ");
            foreach (var item in ot.IfcSubTypes)
            {
                var isAbstract = item.Type.IsAbstract ? " (abstract)" : "";
                sb.AppendFormat(indentationHeader + sSpace + "- {0} {1}", item, isAbstract);
                ChildTree(item, sb, indentationHeader, indent + 1);
            }
        }

        private TextHighliter ReportEntity(int entityLabel, int recursiveDepth = 0, int indentationLevel = 0,
            bool verbose = false, bool showValueType = false)
        {
            // Debug.WriteLine("EL: " + EntityLabel.ToString());
            var sb = new TextHighliter();
            var indentationHeader = new String('\t', indentationLevel);
            try
            {
                var entity = Model.Instances[entityLabel];
                if (entity != null)
                {
                    var ifcType = IfcMetaData.IfcType(entity);

                    sb.Append(
                        string.Format(indentationHeader + "=== {0} [#{1}]", ifcType, entityLabel),
                        Brushes.Blue
                        );
                    var props = ifcType.IfcProperties.Values;
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
                    var invs = ifcType.IfcInverses;
                    if (invs.Count > 0)
                        sb.AppendFormat(indentationHeader + "Inverses: {0}", invs.Count);
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

        private static IEnumerable<int> ReportProp(TextHighliter sb, string indentationHeader, IPersistIfcEntity entity,
            IfcMetaProperty prop, bool verbose, bool showPropType)
        {
            var retIds = new List<int>();
            var propName = prop.PropertyInfo.Name;
            var propType = prop.PropertyInfo.PropertyType;
            var shortTypeName = CleanPropertyName(propType.FullName);
            var propVal = prop.PropertyInfo.GetValue(entity, null) ?? "<null>";

            if (prop.IfcAttribute.IsEnumerable)
            {
                var propCollection = propVal as IEnumerable<object>;
                propVal = propVal + " [not an enumerable]";
                if (propCollection != null)
                {
                    propVal = "<empty>";
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
            var pe = propVal as IPersistIfcEntity;
            var propLabel = 0;
            if (pe != null)
            {
                propLabel = pe.EntityLabel;
                retIds.Add(pe.EntityLabel);
                pe.Activate(false);
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
        public string WindowTitle
        {
            get { return "Commands"; }
        }

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

        // SelectedEntity
        /// <summary>
        /// 
        /// </summary>
        public IPersistIfcEntity SelectedEntity
        {
            get { return (IPersistIfcEntity) GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public static DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedEntity", typeof (IPersistIfcEntity), typeof (WdwQuery),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, OnSelectedEntityChanged));


        // Model
        /// <summary>
        /// 
        /// </summary>
        public XbimModel Model
        {
            get { return (XbimModel) GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public static DependencyProperty ModelProperty =
            DependencyProperty.Register("Model", typeof (XbimModel), typeof (WdwQuery),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, OnSelectedEntityChanged));

        private static void OnSelectedEntityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // if any UI event should happen it needs to be specified here
            var ctrl = d as WdwQuery;
            if (ctrl == null) 
                return;
            switch (e.Property.Name)
            {
                case "Model":
                    ctrl.ReportAdd("Model updated");
                    break;
                case "SelectedEntity":
                    if (e.NewValue == null)
                        ctrl.ReportAdd("No entitiy selected");
                    else
                    {
                        ctrl.ReportAdd(
                            string.Format(
                                "Selected entity label is: {0}",
                                Math.Abs(((IPersistIfcEntity) e.NewValue).EntityLabel)
                                ));
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
    }
}