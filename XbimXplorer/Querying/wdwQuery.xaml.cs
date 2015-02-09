using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Xbim.Ifc2x3.Kernel;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using Xbim.Presentation;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;
using Xbim.XbimExtensions.SelectTypes;
using XbimXplorer.PluginSystem;
using XbimGeometry.Interfaces;

namespace XbimXplorer.Querying
{
    /// <summary>
    /// Interaction logic for wdwQuery.xaml
    /// </summary>
    public partial class WdwQuery : xBimXplorerPluginWindow
    {
        

        /// <summary>
        /// 
        /// </summary>
        public WdwQuery()
        {
            InitializeComponent();
            DisplayHelp();
#if DEBUG
            // loads the last commands stored
            var fname = Path.Combine(Path.GetTempPath(), "xbimquerying.txt");
            if (File.Exists(fname))
            {
                using (StreamReader reader = File.OpenText(fname))
                {
                    string read = reader.ReadToEnd();
                    txtCommand.Text = read;
        }
            }
#endif
        }

        private XplorerMainWindow _parentWindow;

        private bool _bDoClear = true; 

        private void txtCommand_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && 
                (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                )
            {
#if DEBUG
                // stores the commands being launched
                var fname = Path.Combine(Path.GetTempPath(), "xbimquerying.txt");
                using (StreamWriter writer = File.CreateText(fname))
                {
                    writer.Write(txtCommand.Text);
                    writer.Flush();
                    writer.Close();
                }
#endif

                e.Handled = true;
                if (_bDoClear)
                    txtOut.Document = new FlowDocument();

                string[] commandArray = txtCommand.Text.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                if (txtCommand.SelectedText != string.Empty)
                    commandArray = txtCommand.SelectedText.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var cmdF in commandArray)
                {
                    ReportAdd("> " + cmdF, Brushes.ForestGreen);
                    var cmd = cmdF;
                    int i = cmd.IndexOf("//", StringComparison.Ordinal);
                    if (i > 0)
                    {
                        cmd = cmd.Substring(0, i);
                    }
                    if (cmd.TrimStart().StartsWith("//"))
                        continue;

                    // put here all commands that don't require a database open
                    var mdbclosed = Regex.Match(cmd, @"help", RegexOptions.IgnoreCase);
                    if (mdbclosed.Success)
                    {
                        DisplayHelp();
                        continue;
                    }

                    mdbclosed = Regex.Match(cmd, @"RefreshPlugins", RegexOptions.IgnoreCase);
                    if (mdbclosed.Success)
                    {
                        if (_parentWindow != null)
                            _parentWindow.RefreshPlugins();
                        continue;
                    }

                    mdbclosed = Regex.Match(cmd, @"xplorer", RegexOptions.IgnoreCase);
                    if (mdbclosed.Success)
                    {
                        if (_parentWindow != null)
                            _parentWindow.Focus();
                        else
                        {
                            // todo: bonghi: open the model in xplorer if needed.
                            XplorerMainWindow xp = new XplorerMainWindow();
                            _parentWindow = xp;
                            xp.Show();
                        }
                        continue;
                    }

                    mdbclosed = Regex.Match(cmd, @"clear *\b(?<mode>(on|off))*", RegexOptions.IgnoreCase);
                    if (mdbclosed.Success)
                    {
                        try
                        {
                            string option = mdbclosed.Groups["mode"].Value;

                            if (option == "")
                            {
                                txtOut.Document = new FlowDocument();
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
                        txtOut.Document = new FlowDocument();
                        continue;
                    }


                    if (Model == null)
                    {
                        ReportAdd("Plaese open a database.", Brushes.Red);
                        continue;
                    }

                    // all commands here
                    //
                    var m = Regex.Match(cmd, @"^(entitylabel|el) (?<el>\d+)(?<recursion> -*\d+)*", RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        int recursion = 0;
                        int v = Convert.ToInt32(m.Groups["el"].Value);
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
                        ReportAdd(string.Format("- ImplementationLevel: {0}", Model.Header.FileDescription.ImplementationLevel));
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
                    m = Regex.Match(cmd, @"^(SelectionHighlighting|sh) (?<mode>(wholemesh|normals|wireframe))+", RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        string mode = m.Groups["mode"].Value.ToLowerInvariant();
                        if (mode == "normals")
                        {
                            ReportAdd("Selection visual style set to 'Normals'");
                            _parentWindow.DrawingControl.SelectionHighlightMode = DrawingControl3D.SelectionHighlightModes.Normals;
                        }
                        else if (mode == "wholemesh")
                        {
                            ReportAdd("Selection visual style set to 'WholeMesh'");
                            _parentWindow.DrawingControl.SelectionHighlightMode = DrawingControl3D.SelectionHighlightModes.WholeMesh;
                        }
                        else if (mode == "wireframe")
                        {
                            ReportAdd("Selection visual style set to 'WireFrame'");
                            _parentWindow.DrawingControl.SelectionHighlightMode = DrawingControl3D.SelectionHighlightModes.WireFrame;
                        }
                        continue;
                    }

                    m = Regex.Match(cmd, @"^(IfcSchema|is) (?<mode>(list|count|short|full) )*(?<type>.+)", RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        string type = m.Groups["type"].Value;
                        string mode = m.Groups["mode"].Value;

                        if (type == "/")
                        {
                        }
                        else if (type == PrepareRegex(type)) // there's not a regex expression, we will prepare one assuming the search for a bare name.
                        {
                            type = @".*\." + type + "$"; // any character repeated then a dot then the name and the end of line
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
                            int  beVerbose = 1;
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
                        string start = m.Groups["entities"].Value;
                        IEnumerable<int> labels = toIntarray(start, ',');
                        if (labels.Count() > 0)
                        {
                            _parentWindow.DrawingControl.LoadGeometry(Model, labels);
                        }
                        else
                        {
                            _parentWindow.DrawingControl.LoadGeometry(Model);
                        }
                        continue;
                    }

                    m = Regex.Match(cmd, @"^(GeometryInfo|gi) (?<mode>(binary|viewer) )*(?<entities>([\d,]+|[^ ]+))", RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        string start = m.Groups["entities"].Value;
                        string mode = m.Groups["mode"].Value; 
                        IEnumerable<int> labels = toIntarray(start, ',');
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

                    m = Regex.Match(cmd, @"^(select|se) (?<mode>(count|list|typelist|short|full) )*(?<tt>(transverse|tt) )*(?<hi>(highlight|hi) )*(?<start>([\d,-]+|[^ ]+)) *(?<props>.*)", RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        string start = m.Groups["start"].Value;
                        string props = m.Groups["props"].Value;
                        string mode = m.Groups["mode"].Value;
                        
                        // transverse tree mode
                        bool transverseT = false;
                        string transverse = m.Groups["tt"].Value;
                        if (transverse != "")
                            transverseT = true;
                        
                        bool highlight = false;
                        string HighlightT = m.Groups["hi"].Value;
                        if (HighlightT != "")
                            highlight = true;                        

                        IEnumerable<int> labels = toIntarray(start, ',');
                        IEnumerable<int> ret = null;
                        if (!labels.Any())
                        {
                            // see if it's a type string instead;
                            Regex SubRe = new Regex(@"[\+\-]*([A-Za-z0-9\[\]]+)");
                            var res = SubRe.Matches(start);
                            foreach (Match submatch in res)
                            {
                                bool modeAdd = true;
                                if (submatch.Value.Contains("-"))
                                    modeAdd = false;
                                // the syntax could be IfcWall[10]
                                var sbi = new SquareBracketIndexer(submatch.Groups[1].Value);
                                IEnumerable<int> ThisLabels = QueryEngine.EntititesForType(sbi.Property, Model);
                                ThisLabels = sbi.getItem(ThisLabels);
                                if (modeAdd)
                                    labels = labels.Concat(ThisLabels);
                                else
                                    labels = labels.Where(t => !ThisLabels.Contains(t));
                        }
                        }
                        ret = QueryEngine.RecursiveQuery(Model, props, labels, transverseT);
                        
                        // textual report
                        if (mode.ToLower() == "count ")
                        {
                            ReportAdd(string.Format("Count: {0}", ret.Count()));
                        }
                        else if (mode.ToLower() == "list ")
                        {
                            foreach (var item in ret)
                            {
                                ReportAdd(item.ToString());
                            }
                        }
                        else if (mode.ToLower() == "typelist ")
                        {
                            foreach (var item in ret)
                            {
                                ReportAdd(item.ToString() + "\t" + Model.Instances[item].IfcType().Name);
                            }
                        }
                        else
                        {
                            bool BeVerbose = false;
                            if (mode.ToLower() == "short ")
                                BeVerbose = false;
                            if (mode.ToLower() == "full ")
                                BeVerbose = true;
                            foreach (var item in ret)
                            {
                                ReportAdd(ReportEntity(item, 0, Verbose: BeVerbose));
                            }
                        }
                        // visual selection
                        if (highlight)
                        {
                            EntitySelection s = new EntitySelection();
                            foreach (var item in ret)
                            {
                                s.Add(Model.Instances[item]);
                            }
                            _parentWindow.DrawingControl.Selection = s;
                        }
                        continue;
                    }
                            
                    m = Regex.Match(cmd, @"^zoom (" +
                        @"(?<RegionName>.+$)" +
                        ")", RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        string RName = m.Groups["RegionName"].Value;
                        var regionData = Model.GetGeometryData(XbimGeometryType.Region).FirstOrDefault();
                        if (regionData == null)
                        {
                            ReportAdd("data not found");
                        }
                        XbimRegionCollection regions = XbimRegionCollection.FromArray(regionData.ShapeData);
                        var reg = regions.Where(x => x.Name == RName).FirstOrDefault();
                        if (reg != null)
                        {
                            XbimMatrix3D mcp = XbimMatrix3D.Copy(_parentWindow.DrawingControl.WcsTransform);
                            var bb = reg.Centre;
                            var tC = mcp.Transform(reg.Centre);
                            var tS = mcp.Transform(reg.Size);
                            XbimRect3D r3d = new XbimRect3D(
                                tC.X - tS.X / 2, tC.Y - tS.Y / 2, tC.Z - tS.Z / 2,
                                tS.X, tS.X, tS.Z
                                );
                            _parentWindow.DrawingControl.ZoomTo(r3d);
                            _parentWindow.Activate();
                            continue;
                        }
                        else
                        {
                            ReportAdd(string.Format("Something wrong with region name: '{0}'", RName));
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
                            string msg = "";
                            string storName = m.Groups["StoreyName"].Value;
                            var storey = Model.Instances.OfType<Xbim.Ifc2x3.ProductExtension.IfcBuildingStorey>().Where(x => x.Name == storName).FirstOrDefault();
                            if (storey != null)
                            {
                                //get the object position data (should only be one)
                                XbimGeometryData geomdata = Model.GetGeometryData(storey.EntityLabel, XbimGeometryType.TransformOnly).FirstOrDefault();
                                if (geomdata != null)
                                {
                                    Xbim.Common.Geometry.XbimPoint3D pt = new Xbim.Common.Geometry.XbimPoint3D(0, 0, XbimMatrix3D.FromArray(geomdata.DataArray2).OffsetZ);
                                    Xbim.Common.Geometry.XbimMatrix3D mcp = Xbim.Common.Geometry.XbimMatrix3D.Copy(_parentWindow.DrawingControl.WcsTransform);
                                    var transformed = mcp.Transform(pt);
                                    msg = string.Format("Clip 1m above storey elevation {0} (height: {1})", pt.Z, transformed.Z + 1);
                                    pz = transformed.Z + 1;
                                }
                            }
                            if (msg == "")
                            {
                                ReportAdd(string.Format("Something wrong with storey name: '{0}'", storName));
                                ReportAdd("Names that should work are: ");
                                var strs = Model.Instances.OfType<Xbim.Ifc2x3.ProductExtension.IfcBuildingStorey>();
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
                        var st = _parentWindow.DrawingControl.LayerStyler as Xbim.Presentation.LayerStyling.LayerStylerTypeAndIFCStyleExtended;
                        if (st != null)
                        { 
                            string command = m.Groups["command"].Value;
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

                    m = Regex.Match(cmd, @"^Visual (?<action>list|tree|on|off|mode)( (?<Name>[^ ]+))*", RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        string Name = m.Groups["Name"].Value;
                        if (m.Groups["action"].Value.ToLowerInvariant() == "list")
                        {
                            foreach (var item in _parentWindow.DrawingControl.ListItems(Name))
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
                            string t = Name.ToLowerInvariant();
                            if (t == "type")
                            {
                                ReportAdd("Visual mode set to EntityType.");
                                _parentWindow.DrawingControl.LayerStyler = new Xbim.Presentation.LayerStyling.LayerStylerTypeAndIFCStyle();
                                _parentWindow.DrawingControl.ReloadModel(options: DrawingControl3D.ModelRefreshOptions.ViewPreserveAll);
                            }
                            else if (t == "entity")
                            {
                                ReportAdd("Visual mode set to EntityLabel.");
                                _parentWindow.DrawingControl.LayerStyler = new Xbim.Presentation.LayerStyling.LayerStylerPerEntity();
                                _parentWindow.DrawingControl.ReloadModel(options: DrawingControl3D.ModelRefreshOptions.ViewPreserveAll);
                            }
                            else if (t == "oddeven")
                            {
                                ReportAdd("Visual mode set to Odd/Even.");
                                _parentWindow.DrawingControl.LayerStyler = new Xbim.Presentation.LayerStyling.LayerStylerEvenOdd();
                                _parentWindow.DrawingControl.ReloadModel(options: DrawingControl3D.ModelRefreshOptions.ViewPreserveAll);
                            }
                            else if (t == "demo")
                            {
                                ReportAdd("Visual mode set to Demo.");
                                _parentWindow.DrawingControl.LayerStyler = new Xbim.Presentation.LayerStyling.LayerStylerTypeAndIFCStyleExtended();
                                _parentWindow.DrawingControl.ReloadModel(options: DrawingControl3D.ModelRefreshOptions.ViewPreserveAll);
                            }
                            else
                                ReportAdd(string.Format("mode not understood: {0}.", t));
                        }
                        else
                        {
                            bool bVis = false;
                            if (m.Groups["action"].Value.ToLowerInvariant() == "on")
                                bVis = true;
                            _parentWindow.DrawingControl.SetVisibility(Name, bVis);
                        }
                        continue;
                        }
                    m = Regex.Match(cmd, @"^SimplifyGUI$", RegexOptions.IgnoreCase);
                    if (m.Success)  
                    {
                        XbimXplorer.Simplify.IfcSimplify s = new Simplify.IfcSimplify();
                        s.Show();
                        continue;
                    }

                    m = Regex.Match(cmd, @"^test$", RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        if (SelectedEntity != null)
                            Debug.Write(SelectedEntity.EntityLabel);
                        else
                            Debug.Write(null);
                        
                        
                        continue;
                    }
                    ReportAdd(string.Format("Command not understood: {0}.", cmd));
                }
            }
        }

        private void ReportAdd(TextHighliter TH)
        {
            TH.DropInto(txtOut.Document);
            TH.Clear();
                }

        private void ReportAdd(string Text, Brush inColor = null)
        {
            Paragraph newP = new Paragraph(new Run(Text));
            if (inColor != null)
            {
                newP.Foreground = inColor;
            }
            txtOut.Document.Blocks.Add(newP);
        }

        int[] toIntarray(string value, char sep)
        {
            string[] sa = value.Split(new char[] { sep }, StringSplitOptions.RemoveEmptyEntries);
            List<int> ia = new List<int>();
            for (int i = 0; i < sa.Length; ++i)
            {
                if (sa[i].Contains('-'))
                {
                    var v = sa[i].Split('-');
                    if (v.Length == 2)
                    {
                        int iS, iT;
                        if (
                            int.TryParse(v[0], out iS) && 
                            int.TryParse(v[1], out iT) 
                            )
                        {
                            if (iT >= iS)
                            {
                                for (int iC = iS; iC <= iT; iC++)
                                {
                                    ia.Add(iC);
                                }
                            }
                        }
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

        private string RunTestCode(int i)
        {
            StringBuilder sb = new StringBuilder();
            byte[] bval = new byte[] {196};
            var eBase = Encoding.GetEncoding("iso-8859-1");
            var outV = eBase.GetChars(bval, 0, 1);

            bval = new byte[] { 0, 196 };
            var e16 = Encoding.GetEncoding("unicodeFFFE");
            var out2 = e16.GetChars(bval, 0, 2);

            //var v = Model.Instances.OfType<Xbim.Ifc2x3.MaterialResource.IfcMaterialLayerSetUsage>(true).Where(ent => ent.ForLayerSet.EntityLabel == i);
            //foreach (var item in v)
            //{
            //    sb.AppendFormat("{0}", item.EntityLabel);
            //}

            return sb.ToString();
        }

        private void DisplayHelp()
        {
            TextHighliter t = new TextHighliter();

            t.AppendFormat("Commands:");
            t.AppendFormat("- select [count|list|typelist|short] [transverse] [highlight] <startingElement> [Property [Property...]]");
            t.Append("    <startingElement>: <EntityLabel, <EntityLabel>> or <TypeIdentificator>[<+|-><TypeIdentificator>]", Brushes.Gray);
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
            
            t.AppendFormat("");
            t.Append("Commands are executed on <ctrl>+<Enter>.", Brushes.Blue);
            t.AppendFormat("double slash (//) are the comments token and the remainder of lines is ignored.");
            t.AppendFormat("If a portion of text is selected, only selected text will be executed.");

            t.DropInto(txtOut.Document);

        }

        static bool IsSubclassOfRawGeneric(Type generic, Type toCheck)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur)
                {
                    return true;
                }
                toCheck = toCheck.BaseType;
            }
            return false;
        }

        /// <summary>
        /// Finds relevant classes through reflection by Namespace + Name query
        /// </summary>
        /// <param name="RegExString">The regex string to be compared to the namespace</param>
        /// <returns>Enumerable string of full type name, with namespace</returns>
        private IEnumerable<string> MatchingTypes(string RegExString)
        {
            Regex re = new Regex(RegExString, RegexOptions.IgnoreCase);
            foreach (System.Reflection.AssemblyName an in System.Reflection.Assembly.GetExecutingAssembly().GetReferencedAssemblies())
            {
                System.Reflection.Assembly asm = System.Reflection.Assembly.Load(an.ToString());
                foreach (Type type in asm.GetTypes().Where(
                    t =>
                        t.Namespace != null &&
                        (
                        t.Namespace == "Xbim.XbimExtensions.SelectTypes"
                        ||
                        t.Namespace.StartsWith("Xbim.Ifc2x3.")
                        ))
                    )
                {
                    if (RegExString == "/")
                    {
                        if (type.BaseType == typeof(object))
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
            rex = rex.Replace(".", @"\."); //escaped dot
            rex = rex.Replace("*", ".*");
            rex = rex.Replace("?", ".");
            return rex;
        }

        private TextHighliter ReportType(string type, int beVerbose, string indentationHeader = "")
        {
            var tarr = type.Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries);
            type = tarr[tarr.Length - 1];


            TextHighliter sb = new TextHighliter();
            
            IfcType ot = IfcMetaData.IfcType(type.ToUpper());
            if (ot != null)
            {
                sb.Append(
                    string.Format(indentationHeader + "=== {0}", ot.Name),
                    Brushes.Blue
                    );

                sb.AppendFormat(indentationHeader + "Namespace: {0}", ot.Type.Namespace);
                // sb.AppendFormat(indentationHeader + "Xbim Type Id: {0}", ot.TypeId);
                sb.DefaultBrush = Brushes.DarkOrange;
                List<string> supertypes = new List<string>();
                var iterSuper = ot.IfcSuperType;
                while (iterSuper != null)
                {
                    supertypes.Add(iterSuper.Name);
                    iterSuper = iterSuper.IfcSuperType;
                }
                if (ot.IfcSuperType != null)
                    sb.AppendFormat(indentationHeader + "Parents hierarchy: {0}", string.Join(" => ", supertypes.ToArray()));
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
                    Brush[] brushArray = new Brush[]
                        {
                            Brushes.DimGray,
                            Brushes.DarkGray,
                            Brushes.DimGray
                        };
                    foreach (var item in ot.IfcProperties.Values)
                    {

                        var topParent = ot.IfcSuperType;
                        string sTopParent = "";
                        while (topParent != null && topParent.IfcProperties.Where(x => x.Value.PropertyInfo.Name == item.PropertyInfo.Name).Count() > 0)
                        {
                            sTopParent = " \tfrom: " + topParent.ToString();
                            topParent = topParent.IfcSuperType;
        }
                        sb.AppendSpans(
                            new string[] {
                                indentationHeader + "- " + item.PropertyInfo.Name + "\t\t",
                                CleanPropertyName(item.PropertyInfo.PropertyType.FullName),
                                sTopParent },
                            brushArray);


                        // sb.AppendFormat(\t{1}{2}", , , );
                    }
                    sb.AppendFormat(indentationHeader + "Inverses: {0}", ot.IfcInverses.Count());
                    foreach (var item in ot.IfcInverses)
        {
                        var topParent = ot.IfcSuperType;
                        string sTopParent = "";
                        while (topParent != null && topParent.IfcInverses.Where(x => x.PropertyInfo.Name == item.PropertyInfo.Name).Count() > 0)
                        {
                            sTopParent = " \tfrom: " + topParent.ToString();
                            topParent = topParent.IfcSuperType;
                        }
                        //sb.AppendFormat(indentationHeader + "- {0}\t{1}{2}", item.PropertyInfo.Name, CleanPropertyName(item.PropertyInfo.PropertyType.FullName), sTopParent);
                        sb.AppendSpans(
                            new string[] {
                                indentationHeader + "- " + item.PropertyInfo.Name + "\t\t",
                                CleanPropertyName(item.PropertyInfo.PropertyType.FullName),
                                sTopParent },
                            brushArray);
                    }
                }
                sb.DefaultBrush = null;
                sb.AppendFormat("");
            }
            else
            {
                // test to see if it's a select type...

                Module ifcModule2 = typeof(IfcMaterialSelect).Module;
                var SelectType = ifcModule2.GetTypes().Where(
                        t => t.Name.Contains(type)
                        ).FirstOrDefault();

                if (SelectType != null)
                {
                    sb.AppendFormat("=== {0} is a Select type", type);
                    Module ifcModule = typeof(IfcActor).Module;
                    IEnumerable<Type> SelectSubTypes = ifcModule.GetTypes().Where(
                            t => t.GetInterfaces().Contains(SelectType)
                            );

                    // CommontIF sets up the infrastructure to check for common interfaces shared by the select type elements
                    Type[] CommontIF = null;
                    foreach (var item in SelectSubTypes)
                    {
                        if (CommontIF == null)
                            CommontIF = item.GetInterfaces();
                        else
                        {
                            var chk = item.GetInterfaces();
                            for (int i = 0; i < CommontIF.Length; i++)
                            {
                                if (!chk.Contains(CommontIF[i]))
                                {
                                    CommontIF[i] = null;
                                }       
                            }
                        }   
                    }
                    
                    Type[] ExistingIF = SelectType.GetInterfaces();
                    sb.AppendFormat(indentationHeader + "Interfaces: {0}", ExistingIF.Length);
                    foreach (var item in ExistingIF)
                    {
                        sb.AppendFormat(indentationHeader + "- {0}", item.Name);
                    }
                    // need to remove implemented interfaces from the ones shared 
                    for (int i = 0; i < CommontIF.Length; i++)
                    {
                        if (CommontIF[i] == SelectType)
                            CommontIF[i] = null;
                        if (ExistingIF.Contains(CommontIF[i]))
                        {
                            CommontIF[i] = null;
                        }
                    }

                    foreach (var item in CommontIF)
                    {
                        if (item != null)
                            sb.AppendFormat(indentationHeader + "Missing Common Interface: {0}", item.Name);
                    }
                    if (beVerbose == 1)
                    {
                        foreach (var item in SelectSubTypes)
                        {
                            sb.Append(ReportType(item.Name, beVerbose, indentationHeader + "  "));
                        }
                    }
                    sb.AppendFormat("");
                }
        }

            return sb;
        }
        
        private void ChildTree(IfcType ot, TextHighliter sb, string indentationHeader, int Indent)
        {
            string sSpace = new string(' ', Indent * 2);
            // sSpace = sSpace.Replace(new string[] { " " }, "  ");
            foreach (var item in ot.IfcSubTypes)
            {
                string isAbstract = item.Type.IsAbstract ? " (abstract)" : "";
                sb.AppendFormat(indentationHeader + sSpace + "- {0} {1}", item, isAbstract);
                ChildTree(item, sb, indentationHeader, Indent + 1);    
            }
        }

        private TextHighliter ReportEntity(int EntityLabel, int RecursiveDepth = 0, int IndentationLevel = 0, bool Verbose = false)
        {
            // Debug.WriteLine("EL: " + EntityLabel.ToString());
            TextHighliter sb = new TextHighliter();
            string IndentationHeader = new String('\t', IndentationLevel);
            try
            {
                var entity = Model.Instances[EntityLabel];
                if (entity != null)
                {
                    IfcType ifcType = IfcMetaData.IfcType(entity);
                    
                    sb.Append(
                        string.Format(IndentationHeader + "=== {0} [#{1}]", ifcType, EntityLabel.ToString()),
                        Brushes.Blue
                        );
                    var props = ifcType.IfcProperties.Values;
                    if (props.Count > 0)
                        sb.AppendFormat(IndentationHeader + "Properties: {0}", props.Count);
                    foreach (var prop in props)
                    {
                        var PropLabels = ReportProp(sb, IndentationHeader, entity, prop, Verbose);

                        foreach (var PropLabel in PropLabels)
                        {
                            if (
                                PropLabel != EntityLabel &&
                                (RecursiveDepth > 0 || RecursiveDepth < 0)
                                && PropLabel != 0
                                )
                            {
                                sb.Append(ReportEntity(PropLabel, RecursiveDepth - 1, IndentationLevel + 1));
                            }
                        }
                    }
                    var Invs = ifcType.IfcInverses;
                    if (Invs.Count > 0)
                        sb.AppendFormat(IndentationHeader + "Inverses: {0}", Invs.Count);
                    foreach (var inverse in Invs)
                    {
                        ReportProp(sb, IndentationHeader, entity, inverse, Verbose);
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
                    sb.AppendFormat(IndentationHeader + "=== Entity #{0} is null", EntityLabel);
                }
            }
            catch (Exception ex)
            {
                sb.AppendFormat(IndentationHeader + "\r\nException Thrown: {0} ({1})\r\n{2}", ex.Message, ex.GetType().ToString(), ex.StackTrace);
            }
            return sb;
        }

        private static IEnumerable<int> ReportProp(TextHighliter sb, string IndentationHeader, IPersistIfcEntity entity, IfcMetaProperty prop, bool Verbose)
        {
            List<int> RetIds = new List<int>();
            string propName = prop.PropertyInfo.Name;
            Type propType = prop.PropertyInfo.PropertyType;
            string ShortTypeName = CleanPropertyName(propType.FullName);
            object propVal = prop.PropertyInfo.GetValue(entity, null);
            if (propVal == null)
                propVal = "<null>";

            if (prop.IfcAttribute.IsEnumerable)
            {
                IEnumerable<object> propCollection = propVal as IEnumerable<object>;
                propVal = propVal.ToString() + " [not an enumerable]";
                if (propCollection != null)
                {
                    propVal = "<empty>";
                    int iCntProp = 0;
                    foreach (var item in propCollection)
                    {
                        iCntProp++;
                        if (iCntProp == 1)
                            propVal = ReportPropValue(item, ref RetIds);
                        else
                        {
                            if (iCntProp == 2)
                            {
                                propVal = "\r\n" + IndentationHeader + "    " + propVal;
                            }
                            propVal += "\r\n" + IndentationHeader + "    " + ReportPropValue(item, ref RetIds);
                        }
                    }
                }
            }
            else
                propVal = ReportPropValue(propVal, ref RetIds);

            if (Verbose)
                sb.AppendFormat(IndentationHeader + "- {0} ({1}): {2}",
                propName,  // 0
                    ShortTypeName,  // 1
                propVal // 2
                );
            else
            {
                if ((string)propVal != "<null>" && (string)propVal != "<empty>")
                {
                    sb.AppendFormat(IndentationHeader + "- {0}: {1}",
                        propName,  // 0
                        propVal // 1
                        );
                }
            }
            return RetIds;
        }

        private static string CleanPropertyName(string ShortTypeName)
        {
            var m = Regex.Match(ShortTypeName, @"^((?<Mod>.*)`\d\[\[)*Xbim\.(?<Type>[\w\.]*)");
            if (m.Success)
            {
                ShortTypeName = m.Groups["Type"].Value; // + m.Groups["Type"].Value + 
                if (m.Groups["Mod"].Value != string.Empty)
                {
                    string[] GetLast = m.Groups["Mod"].Value.Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries);
                    ShortTypeName += " (" + GetLast[GetLast.Length - 1] + ")";
                }
            }
            return ShortTypeName;
        }

        private static string ReportPropValue(object propVal, ref List<int> RetIds)
        {
            IPersistIfcEntity pe = propVal as IPersistIfcEntity;
            int PropLabel = 0;
            if (pe != null)
            {
                RetIds.Add(pe.EntityLabel);
                pe.Activate(false);
            }
            string ret = propVal.ToString() + ((PropLabel != 0) ? " [#" + PropLabel.ToString() + "]" : "");
            return ret;
        }
        #region "Plugin"

        public string MenuText { get { return "Querying Window"; } }
        public string WindowTitle { get { return "Querying Window"; } }

        /// <summary>
        /// All bindings are to be established in this call
        /// </summary>
        /// <param name="mainWindow"></param>
        public void BindUI(XplorerMainWindow mainWindow)
        {
            _parentWindow = mainWindow;
            this.SetBinding(SelectedItemProperty, new Binding("SelectedItem") { Source = mainWindow, Mode = BindingMode.OneWay });
            this.SetBinding(ModelProperty, new Binding()); // whole datacontext binding, see http://stackoverflow.com/questions/8343928/how-can-i-create-a-binding-in-code-behind-that-doesnt-specify-a-path
        }

        // SelectedEntity
        public IPersistIfcEntity SelectedEntity
        {
            get { return (IPersistIfcEntity)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public static DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedEntity", typeof(IPersistIfcEntity), typeof(WdwQuery), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits,
                                                                      new PropertyChangedCallback(OnSelectedEntityChanged)));


        // Model
        public XbimModel Model
        {
            get { return (XbimModel)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        public static DependencyProperty ModelProperty =
            DependencyProperty.Register("Model", typeof(XbimModel), typeof(WdwQuery), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits,
                                                                      new PropertyChangedCallback(OnSelectedEntityChanged)));

        private static void OnSelectedEntityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // if any UI event should happen it needs to be specified here
            WdwQuery ctrl = d as WdwQuery;
            if (ctrl != null)
            {
                if (e.Property.Name == "Model")
                {
                    ctrl.ReportAdd("Model updated");
                }
                else if (e.Property.Name == "SelectedEntity")
                {
                    if (e.NewValue == null)
                        ctrl.ReportAdd("No entitiy selected");
                    else
                    {
                        ctrl.ReportAdd(
                            string.Format(
                                "Selected entity label is: {0}",
                                Math.Abs(((IPersistIfcEntity)e.NewValue).EntityLabel).ToString()
                                ));
                    }
                }
            }
        }

        public PluginWindowDefaultUIContainerEnum DefaultUIContainer
        { get { return PluginWindowDefaultUIContainerEnum.LayoutAnchorable; } }


        public PluginWindowDefaultUIShow DefaultUIActivation
        { get { return PluginWindowDefaultUIShow.onMenu; } }
        
        #endregion

        
    }
}
