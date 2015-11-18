using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using log4net;
using Xbim.Common.Exceptions;
using Xbim.COBieLiteUK;
using Xbim.FilterHelper;
using Xbim.Ifc2x3.ActorResource;
using Xbim.Ifc2x3.ExternalReferenceResource;
using Xbim.IO;
using Xbim.XbimExtensions.Interfaces;
using XbimExchanger.COBieLiteUkToIfc;
using XbimExchanger.IfcToCOBieLiteUK;

namespace XplorerPlugins.Cobie
{
    public class CobieLiteWorker
    {

        private static readonly ILog Log = LogManager.GetLogger("XplorerPlugins.Cobie.CobieLiteWorker");

        /// <summary>
        /// The worker
        /// </summary>
        public BackgroundWorker Worker
        { get; set; }


        /// <summary>
        /// Constructor
        /// </summary>
        public CobieLiteWorker()
        {
            Worker = new BackgroundWorker();
            Worker.WorkerReportsProgress = true;
            Worker.WorkerSupportsCancellation = false;
            Worker.DoWork += CobieLiteUKWorker;
        }

        /// <summary>
        /// Run the worker
        /// </summary>
        /// <param name="args"></param>
        internal void Run(Params args)
        {
            Worker.RunWorkerAsync(args);
        }

        /// <summary>
        /// DOWork function for worker, generate excel COBie
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CobieLiteUKWorker(object sender, DoWorkEventArgs e)
        {
            Params parameters = e.Argument as Params;
            if ((string.IsNullOrEmpty(parameters.ModelFile)) || (!File.Exists(parameters.ModelFile)))
            {
                Worker.ReportProgress(0, string.Format("That file doesn't exist: {0}.", parameters.ModelFile));
                return;
            }
            e.Result = GenerateFile(parameters); //returns the excel file name
        }

        /// <summary>
        /// Create XLS file from ifc/xbim files
        /// </summary>
        /// <param name="parameters">Params</param>
        private string GenerateFile(Params parameters)
        {
            string fileName = string.Empty;
            string exportType = parameters.ExportType.ToString();
            //string fileExt = Path.GetExtension(parameters.ModelFile);
            Stopwatch timer = new Stopwatch();
            timer.Start();

            var facilities = GenerateFacility(parameters);
            timer.Stop();
            Worker.ReportProgress(0, string.Format("Time to generate COBieLite data = {0} seconds", timer.Elapsed.TotalSeconds.ToString("F3")));
            timer.Reset();
            timer.Start();
            int index = 1;
            foreach (var facilityType in facilities)
            {
                fileName = Path.GetFileNameWithoutExtension(parameters.ModelFile) + ((facilities.Count == 1) ? "" : "(" + index.ToString() + ")");
                string path = Path.GetDirectoryName(parameters.ModelFile);
                fileName = Path.Combine(path, fileName);
                if (parameters.Log)
                {
                    string logFile = Path.ChangeExtension(fileName, ".log");
                    Worker.ReportProgress(0, string.Format("Creating validation log file: {0}", logFile));
                    using (var log = File.CreateText(logFile))
                    {
                        facilityType.ValidateUK2012(log, false);
                    }
                }
                if ((exportType.Equals("XLS", StringComparison.OrdinalIgnoreCase)) || (exportType.Equals("XLSX", StringComparison.OrdinalIgnoreCase)))
                {
                    fileName =  CreateExcelFile(parameters, fileName, facilityType);
                }
                else if (exportType.Equals("JSON", StringComparison.OrdinalIgnoreCase))
                {
                    fileName = CreateJsonFile(parameters, fileName, facilityType);
                }
                else if (exportType.Equals("XML", StringComparison.OrdinalIgnoreCase))
                {
                    fileName = CreateXmlFile(parameters, fileName, facilityType);
                }
                else if (exportType.Equals("IFC", StringComparison.OrdinalIgnoreCase))
                {
                    fileName = CreateIfcFile(parameters, fileName, facilityType);
                }
                index++; 
            }
            timer.Stop();
            Worker.ReportProgress(0, string.Format("Time to generate = {0} seconds", timer.Elapsed.TotalSeconds.ToString("F3")));

            Worker.ReportProgress(0, "Finished COBie Generation");
            return fileName;
        }

        private string CreateIfcFile(Params parameters, string fileName, Facility facility)
        {
            var ifcName = Path.ChangeExtension(fileName, ".ifc");
            if (File.Exists(ifcName))
            {
                DateTime now = DateTime.Now;
                ifcName = Path.GetDirectoryName(ifcName)+ "\\" + Path.GetFileNameWithoutExtension(ifcName) + "(" + DateTime.Now.ToString("HH-mm-ss") + ").ifc";
            }
            var xbimFile = Path.ChangeExtension(ifcName, "xbim");
            Worker.ReportProgress(0, string.Format("Creating file: {0}", xbimFile));
            facility.ReportProgress.Progress = Worker.ReportProgress;
            using (var ifcModel = XbimModel.CreateModel(xbimFile))
            {
                System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                ifcModel.Initialise(fvi.CompanyName, fvi.CompanyName, fvi.ProductName, fvi.CompanyName, fvi.ProductVersion);
                ifcModel.ReloadModelFactors();
                using (var txn = ifcModel.BeginTransaction("Convert from COBieLiteUK"))
                {
                    var coBieLiteUkToIfcExchanger = new CoBieLiteUkToIfcExchanger(facility, ifcModel);
                    coBieLiteUkToIfcExchanger.Convert();
                    txn.Commit();
                }
                Worker.ReportProgress(0, string.Format("Creating file: {0}", ifcName));
                ifcModel.SaveAs(ifcName, XbimStorageType.IFC);
                ifcModel.Close();
            }

            return ifcName;
        }

        /// <summary>
        /// Generate a Excel File
        /// </summary>
        /// <param name="parameters">Params</param>
        /// <param name="fileName">Root file name</param>
        /// <param name="facility">Facility</param>
        /// <returns>file name</returns>
        private string CreateExcelFile(Params parameters, string fileName, Facility facility)
        {
            ExcelTypeEnum excelType = (ExcelTypeEnum)Enum.Parse(typeof(ExcelTypeEnum), parameters.ExportType.ToString(),true);
            var excelName = Path.ChangeExtension(fileName, excelType == ExcelTypeEnum.XLS ? ".xls" : ".xlsx");
            Worker.ReportProgress(0, string.Format("Creating file: {0}", excelName));
            string msg;
            using (var file = File.Create(excelName))
            {
                facility.ReportProgress.Progress = Worker.ReportProgress;
                facility.WriteCobie(file, excelType, out msg, parameters.Filter, parameters.TemplateFile, true);
            }
            //_worker.ReportProgress(0, msg); //removed for now, kill app for some reason
            return excelName;
        }

        /// <summary>
        /// Generate a JSON File
        /// </summary>
        /// <param name="parameters">Params</param>
        /// <param name="fileName">Root file name</param>
        /// <param name="facility">Facility</param>
        /// <returns>file name</returns>
        private string CreateJsonFile(Params parameters, string fileName, Facility facility)
        {
            var jsonName = Path.ChangeExtension(fileName, ".json");
            Worker.ReportProgress(0, string.Format("Creating file: {0}", jsonName));
            facility.ReportProgress.Progress = Worker.ReportProgress;
            facility.WriteJson(jsonName, true);
            return jsonName;
        }

        /// <summary>
        /// Generate a XML File
        /// </summary>
        /// <param name="parameters">Params</param>
        /// <param name="fileName">Root file name</param>
        /// <param name="facility">Facility</param>
        /// <returns>file name</returns>
        private string CreateXmlFile(Params parameters, string fileName, Facility facility)
        {
            var xmlName = Path.ChangeExtension(fileName, ".xml");
            Worker.ReportProgress(0, string.Format("Creating file: {0}", xmlName));
            facility.ReportProgress.Progress = Worker.ReportProgress;
            facility.WriteXml(xmlName, true);
            return xmlName;
        }

        /// <summary>
        /// Generate the Facilities held within the Model
        /// </summary>
        /// <param name="parameters">Params</param>
        /// <returns>List of Facilities</returns>
        private List<Facility> GenerateFacility(Params parameters)
        {
            string fileExt = Path.GetExtension(parameters.ModelFile);

            //chsck if federated
            if (fileExt.Equals(".xbimf", StringComparison.OrdinalIgnoreCase))
            {
                return GenerateFedFacility(parameters);
            }
            if ((fileExt.Equals(".xls", StringComparison.OrdinalIgnoreCase)) || (fileExt.Equals(".xlsx", StringComparison.OrdinalIgnoreCase)))
            {
                return GeneratelExcelFacility(parameters);
            }
            if (fileExt.Equals(".json", StringComparison.OrdinalIgnoreCase))
            {
                return GeneratelJsonFacility(parameters);
            }
            if (fileExt.Equals(".xml", StringComparison.OrdinalIgnoreCase))
            {
                return GeneratelXmlFacility(parameters);
            }

            //not Federated 
            return GenerateFileFacility(parameters, fileExt);
        }

        /// <summary>
        /// Get the facility from the COBie Excel sheets
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private List<Facility> GeneratelExcelFacility(Params parameters)
        {
            var facilities = new List<Facility>();
            string msg;
            var facility = Facility.ReadCobie(parameters.ModelFile, out msg, parameters.TemplateFile);
            if (facility != null)
            {
                facilities.Add(facility);
            }
            return facilities;
        }

        /// <summary>
        /// Get the facility from the COBie Excel sheets
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private List<Facility> GeneratelJsonFacility(Params parameters)
        {
            var facilities = new List<Facility>();
             var facility = Facility.ReadJson(parameters.ModelFile);
            if (facility != null)
            {
                facilities.Add(facility);
            }
            return facilities;
        }
        /// <summary>
        /// Get the facility from the COBie Excel sheets
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private List<Facility> GeneratelXmlFacility(Params parameters)
        {
            var facilities = new List<Facility>();
            var facility = Facility.ReadXml(parameters.ModelFile);
            if (facility != null)
            {
                facilities.Add(facility);
            }
            return facilities;
        }
        /// <summary>
        /// Generate Facilities for a xbim or ifc type file
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="fileExt"></param>
        /// <returns></returns>
        private List<Facility> GenerateFileFacility(Params parameters, string fileExt)
        {
            var facilities = new List<Facility>();
            using (var model = new XbimModel())
            {
                if (fileExt.Equals(".xbim", StringComparison.OrdinalIgnoreCase))
                {
                    model.Open(parameters.ModelFile, Xbim.XbimExtensions.XbimDBAccess.Read, Worker.ReportProgress);
                }
                else
                {
                    var xbimFile = Path.ChangeExtension(parameters.ModelFile, "xbim");
                    model.CreateFrom(parameters.ModelFile, xbimFile, Worker.ReportProgress, true, true);

                }
                var ifcToCoBieLiteUkExchanger = new IfcToCOBieLiteUkExchanger(model, facilities, Worker.ReportProgress, parameters.Filter, parameters.ConfigFile, parameters.ExtId, parameters.SysMode);
                facilities = ifcToCoBieLiteUkExchanger.Convert();
            }
            return facilities;
        }

        private XbimModel _fedModelProxy;

        /// <summary>
        /// Map IfcActroRole to RoleFilter, if no match the RoleFilter.Unknown returned
        /// </summary>
        /// <param name="actorRole">IfcActorRole</param>
        /// <returns>RoleFilter</returns>
        private static RoleFilter MapActorRole(IfcActorRole actorRole)
        {
            var role = actorRole.Role;
            var userDefined = actorRole.UserDefinedRole;
            if (role == IfcRole.UserDefined)
            {
                RoleFilter filterRole;
                if (Enum.TryParse(userDefined, out filterRole))
                {
                    return filterRole;
                }
            }
            else
            {
                if (role == IfcRole.Architect)
                    return RoleFilter.Architectural;
                if (role == IfcRole.MechanicalEngineer)
                    return RoleFilter.Mechanical;
                if (role == IfcRole.ElectricalEngineer)
                    return RoleFilter.Electrical;
            }
            //Unhandled IfcRole
            //IfcRole.Supplier IfcRole.Manufacturer IfcRole.Contractor IfcRole.Subcontractor IfcRole.StructuralEngineer
            //IfcRole.CostEngineer IfcRole.Client IfcRole.BuildingOwner IfcRole.BuildingOperator IfcRole.ProjectManager
            //IfcRole.FacilitiesManager IfcRole.CivilEngineer IfcRole.ComissioningEngineer IfcRole.Engineer IfcRole.Consultant
            //IfcRole.ConstructionManager IfcRole.FieldConstructionManager  IfcRole.Owner IfcRole.Reseller
            return RoleFilter.Unknown;
        }

        /// <summary>
        /// Convert a list of IfcActorRole to set correct flags on the returned RoleFilter 
        /// </summary>
        /// <param name="actorRoles">list of IfcActorRole</param>
        /// <returns>RoleFilter</returns>
        public static RoleFilter GetRoleFilters(List<IfcActorRole> actorRoles)
        {
            RoleFilter roles = RoleFilter.Unknown; //reset to unknown

            //set selected roles
            int idx = 0;
            foreach (var item in actorRoles)
            {
                RoleFilter role = MapActorRole(item);
                if (!roles.HasFlag(role))
                {
                    roles |= role; //add flag to RoleFilter
                    //remove the inital RoleFilter.Unknown,  set in declaration unless found role is unknown
                    if ((idx == 0) && (role != RoleFilter.Unknown))
                    {
                        roles &= ~RoleFilter.Unknown;
                    }
                    idx++;
                }
            }
            return roles;
        }

        private Dictionary<XbimModel, RoleFilter> GetFileRoles()
        {
            Dictionary<XbimModel, RoleFilter> modelRoles = new Dictionary<XbimModel, RoleFilter>();
            foreach (var refModel in _fedModelProxy.ReferencedModels)
            {
                var doc = refModel.DocumentInformation;
                var owner = doc.DocumentOwner as IfcOrganization;
                if ((owner == null) || (owner.Roles == null)) 
                    continue;
                var docRoles = GetRoleFilters(owner.Roles.ToList());
                try
                {
                    var file = new FileInfo(refModel.Model.DatabaseName);
                    if (file.Exists)
                    {
                        modelRoles[refModel.Model] = docRoles;
                    }
                    else
                    {
                        Log.ErrorFormat("File path does not exist: {0}", doc.Name);
                    }
                }
                catch (ArgumentException ex)
                {
                    Log.Error(string.Format("File path is incorrect: {0}", doc.Name), ex);
                }
            }
            return modelRoles;
        }


        /// <summary>
        /// Generate Facilities for a xbimf federated file
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private List<Facility> GenerateFedFacility(Params parameters)
        {
            var facilities = new List<Facility>();
            try
            {

                if (_fedModelProxy.IsFederation)
                {
                    var fedFilters = parameters.Filter.SetFedModelFilter(GetFileRoles());
                    var rolesFacilities = new Dictionary<RoleFilter, List<Facility>>();
                    foreach (var item in fedFilters)
                    {
                        var ifcToCoBieLiteUkExchanger = new IfcToCOBieLiteUkExchanger(item.Key, new List<Facility>(),
                            Worker.ReportProgress, item.Value, parameters.ConfigFile, parameters.ExtId,
                            parameters.SysMode);
                        ifcToCoBieLiteUkExchanger.ReportProgress.Progress = Worker.ReportProgress;
                        var rolesFacility = ifcToCoBieLiteUkExchanger.Convert();

                        //facilities.AddRange(rolesFacility);
                        if (rolesFacilities.ContainsKey(item.Value.AppliedRoles))
                        {
                            rolesFacilities[item.Value.AppliedRoles].AddRange(rolesFacility);
                        }
                        else
                        {
                            rolesFacilities.Add(item.Value.AppliedRoles, rolesFacility);
                        }

                    }
                    var fedFacilities =
                        rolesFacilities.OrderByDescending(d => d.Key.HasFlag(RoleFilter.Architectural))
                            .SelectMany(p => p.Value)
                            .ToList(); //pull architectural roles facilities to the top
                    //fedFacilities = RolesFacilities.Values.SelectMany(f => f).OrderByDescending(f => f.AssetTypes.Count).ToList(); //pull facility with largest number of AssetTypes to the top
                    //fedFacilities = RolesFacilities.Values.SelectMany(f => f).ToList(); //flatten value lists
                    if (fedFacilities.Any())
                    {
                        var baseFacility = fedFacilities.First();
                        fedFacilities.RemoveAt(0);
                        if (parameters.Log)
                        {
                            var logfile = Path.ChangeExtension(parameters.ModelFile, "merge.log");
                            using (var sw = new StreamWriter(logfile))
                                //using (StreamWriter sw = new StreamWriter(Console.OpenStandardOutput())) //to debug console **slow**
                            {
                                sw.AutoFlush = true;
                                foreach (var mergeFacility in fedFacilities)
                                {
                                    baseFacility.Merge(mergeFacility, sw);
                                }
                            }
                        }
                        else
                        {
                            foreach (var mergeFacility in fedFacilities)
                            {
                                baseFacility.Merge(mergeFacility, null);
                            }
                        }
                        facilities.Add(baseFacility);
                    }

                }
                else
                {
                    throw new XbimException("Model is not Federated:");
                }

            }
            catch (ArgumentException Ex) //bad paths etc..
            {
                Worker.ReportProgress(0, Ex.Message);
            }
            catch (XbimException Ex) //not federated
            {
                Worker.ReportProgress(0, Ex.Message);
            }

            return facilities;
        }
    }

    /// <summary>
    /// Params Class, holds parameters for worker to access
    /// </summary>
    internal class Params
    {
        public string ModelFile
        { get; set; }
        public string TemplateFile
        { get; set; }
        public ExportTypeEnum ExportType
        { get; set; }
        public bool FlipFilter
        { get; set; }
        public bool OpenExcel
        { get; set; }
        public RoleFilter Roles
        { get; set; }
        public bool FilterOff
        { get; set; }
        public EntityIdentifierMode ExtId
        { get; set; }
        public SystemExtractionMode SysMode
        { get; set; }
        public OutPutFilters Filter
        { get; set; }
        public string ConfigFile
        { get; set; }
        public bool Log
        { get; set; }
    }
}
