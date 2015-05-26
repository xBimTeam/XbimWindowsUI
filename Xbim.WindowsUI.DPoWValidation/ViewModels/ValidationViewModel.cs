using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Practices.Unity;
using Xbim.COBieLiteUK;
using Xbim.IO;
using Xbim.WindowsUI.DPoWValidation.Commands;
using Xbim.WindowsUI.DPoWValidation.Extensions;
using Xbim.WindowsUI.DPoWValidation.Injection;
using Xbim.WindowsUI.DPoWValidation.IO;
using Xbim.WindowsUI.DPoWValidation.Models;
using Xbim.XbimExtensions;
using cobieUKValidation = Xbim.CobieLiteUK.Validation;

namespace Xbim.WindowsUI.DPoWValidation.ViewModels
{
    public class ValidationViewModel: INotifyPropertyChanged
    {
        public SelectFileCommand SelectRequirement { get; set; }
        public SelectFileCommand SelectSubmission { get; set; }

        public SubmittedFacilitySaveCommand SaveModelFacility { get; set; }

        public ValidateCommand Validate { get; set; }

        public ValidateAndSaveCommand ValidateAndSave { get; set; }

        public FacilitySaveCommand ExportFacility { get; set; }

        public bool IsWorking { get; set; }

        public bool FilesCanChange
        {
            get { return !IsWorking; }
        }

        public string RequirementFileSource
        {
            get { return RequirementFileInfo.File; }
            set
            {
                RequirementFileInfo.File = value;
                Validate.ChangesHappened();
            }
        }

        private Facility _requirementFacility;

        internal Facility RequirementFacility
        {
            get { return _requirementFacility; }
            set
            {
                _requirementFacility = value; 
                RequirementFacilityVm = new DpoWFacilityViewModel(_requirementFacility);
                
                if (PropertyChanged == null)
                    return;
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(@"RequirementFacilityVM"));
            }
        }

        public DpoWFacilityViewModel RequirementFacilityVm { get; private set; }

        private Facility _submissionFacility;

        internal Facility SubmissionFacility
        {
            get { return _submissionFacility; }
            set
            {
                _submissionFacility = value;
                SubmissionFacilityVm = new DpoWFacilityViewModel(_submissionFacility);
                
                if (PropertyChanged == null)
                    return;
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(@"SubmissionFacilityVM"));
            }
        }

        public DpoWFacilityViewModel SubmissionFacilityVm { get; private set; }

        private Facility _validationFacility;

        internal Facility ValidationFacility
        {
            get { return _validationFacility; }
            set
            {
                _validationFacility = value;
                ValidationFacilityVm = new DpoWFacilityViewModel(_validationFacility);
                ExportFacility = new FacilitySaveCommand(this);
                
                if (PropertyChanged == null)
                    return;

                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(@"ValidationFacilityVM")); // notiffy that the VM has also changed
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(@"ValidationFacility"));
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(@"ExportFacility"));
            }
        }

        public DpoWFacilityViewModel ValidationFacilityVm { get; private set; }

        public string SubmissionFileSource
        {
            get { return SubmissionFileInfo.File; }
            set
            {
                SubmissionFileInfo.File = value;
                Validate.ChangesHappened();
            }
        }

        public string ReportFileSource
        {
            get { return ReportFileInfo.File; }
            set
            {
                ReportFileInfo.File = value;
                Validate.ChangesHappened();
            }
        }

        internal SourceFile RequirementFileInfo = new SourceFile();
        internal SourceFile SubmissionFileInfo = new SourceFile();
        internal SourceFile ReportFileInfo = new SourceFile();

        
        public ValidationViewModel()
        {
            IsWorking = false;
            SelectRequirement = new SelectFileCommand(RequirementFileInfo, this);
            SelectSubmission = new SelectFileCommand(SubmissionFileInfo, this) {IncludeIfc = true};
            SelectReport = new SelectReportFileCommand(ReportFileInfo, this);
            

            ExportOnValidated = false;
            OpenOnExported = false;

            Validate = new ValidateCommand(this);
            ExportFacility = new FacilitySaveCommand(this);
            ValidateAndSave = new ValidateAndSaveCommand(this);
            SaveModelFacility = new SubmittedFacilitySaveCommand(this);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        internal void FilesUpdate()
        {
            if (PropertyChanged == null) 
                return;
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(@"RequirementFileSource"));
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(@"SubmissionFileSource"));
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(@"ReportFileSource"));

            SaveModelFacility.ChangesHappened();
            Validate.ChangesHappened();
            ValidateAndSave.ChangesHappened();
        }

        internal void ExecuteSaveCobie()
        {
            ActivityStatus = "Loading submission file";
            LoadSubmissionFile(SubmissionFileSource);
        }

        internal void ExecuteValidation()
        {
            IsWorking = true;
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(@"FilesCanChange"));
            SelectRequirement.ChangesHappened();
            SelectSubmission.ChangesHappened();
            SelectReport.ChangesHappened();

            ActivityStatus = "Loading requirement file";
            var fReader = new FacilityReader();
            RequirementFacility = fReader.LoadFacility(RequirementFileSource);
            
            ActivityStatus = "Loading submission file";
            LoadSubmissionFile(SubmissionFileSource);
        }

        private string _openedModelFileName;
        private string _temporaryXbimFileName;

        private BackgroundWorker _worker;

        private void OpenSubmissionCobieFile(object s, DoWorkEventArgs args)
        {
            var cobieFilename = args.Argument as string;
            if (string.IsNullOrEmpty(cobieFilename))
                return;
            if (!File.Exists(cobieFilename))
                return;
            
            switch (Path.GetExtension(cobieFilename.ToLowerInvariant()))
            {
                case ".json": 
                    SubmissionFacility = Facility.ReadJson(cobieFilename);
                    break;
                case ".xml":
                    SubmissionFacility = Facility.ReadXml(cobieFilename);
                    break;
                case ".xls":
                case ".xlsx":
                    string msg;
                    SubmissionFacility = Facility.ReadCobie(cobieFilename, out msg);
                    break;
            }
            args.Result = SubmissionFacility;
        }

        public SelectReportFileCommand SelectReport { get; set; }

        private void OpenIfcFile(object s, DoWorkEventArgs args)
        {
            var worker = s as BackgroundWorker;
            var ifcFilename = args.Argument as string;

            var model = new XbimModel();
            try
            {
                _temporaryXbimFileName = Path.GetTempFileName();
                _openedModelFileName = ifcFilename;

                if (worker != null)
                {
                    model.CreateFrom(ifcFilename, _temporaryXbimFileName, worker.ReportProgress, true);
                    if (worker.CancellationPending) // if a cancellation has been requested then don't open the resulting file
                    {
                        try
                        {
                            model.Close();
                            if (File.Exists(_temporaryXbimFileName))
                                File.Delete(_temporaryXbimFileName); 
                            _temporaryXbimFileName = null;
                            _openedModelFileName = null;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                        return;
                    }
                }
                args.Result = model;
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Error reading " + ifcFilename);
                var indent = "\t";
                while (ex != null)
                {
                    sb.AppendLine(indent + ex.Message);
                    ex = ex.InnerException;
                    indent += "\t";
                }
                args.Result = new Exception(sb.ToString());
            }
        }

        private void OpenXbimFile(object s, DoWorkEventArgs args)
        {
            var worker = s as BackgroundWorker;
            var fileName = args.Argument as string;
            var model = new XbimModel();
            try
            {
                const XbimDBAccess dbAccessMode = XbimDBAccess.Read;
                if (worker != null)
                {
                    model.Open(fileName, dbAccessMode, worker.ReportProgress); //load entities into the model

                    if (model.IsFederation)
                    {
                        // needs to open the federation in rw mode
                        model.Close();
                        model.Open(fileName, XbimDBAccess.ReadWrite, worker.ReportProgress);
                        // federations need to be opened in read/write for the editor to work

                        // sets a convenient integer to all children for model identification
                        // this is used by the federated model selection mechanisms.
                        // 
                        var i = 0;
                        foreach (var item in model.AllModels)
                        {
                            item.Tag = i++;
                        }
                    }
                }
                args.Result = model;
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Error reading " + fileName);
                var indent = "\t";
                while (ex != null)
                {
                    sb.AppendLine(indent + ex.Message);
                    ex = ex.InnerException;
                    indent += "\t";
                }
                args.Result = new Exception(sb.ToString());
            }
        }

        private string _activityStatus;
        public string ActivityStatus
        {
            get { return _activityStatus; }

            set
            {
                _activityStatus = value;
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(@"ActivityStatus"));
            }
        }

        private int _activityProgress;
        public int ActivityProgress
        {
            get { return _activityProgress; }
            set
            {
                _activityProgress = value;
                if (PropertyChanged != null)
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(@"ActivityProgress"));
            }
        }
        public string ActivityDescription { get; set; }

        private XbimModel _model;

        private void CreateWorker()
        {
            _worker = new BackgroundWorker
            {
                WorkerReportsProgress = true, 
                WorkerSupportsCancellation = true
            };
            _worker.ProgressChanged += delegate(object s, ProgressChangedEventArgs args)
            {
                ActivityProgress = args.ProgressPercentage;
                ActivityStatus = (string) args.UserState;
                Debug.WriteLine("{0}% {1}", args.ProgressPercentage, (string) args.UserState);
            };

            _worker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs args)
            {
                if (args.Result is XbimModel) //all ok
                {
                    _model = args.Result as XbimModel;
                    ActivityProgress = 0;
                    // prepare the facility
                    SubmissionFacility = FacilityFromIfcConverter.FacilityFromModel(_model);

                    if (SubmissionFacility == null)
                        return;
                    var jsonFileName = Path.ChangeExtension(SubmissionFileSource, "json");
                    if (!File.Exists(jsonFileName))
                        SubmissionFacility.WriteJson(jsonFileName);

                    ValidateLoadedFacilities();
                }
                else if (args.Result is Facility) //all ok; this is the model facility
                {
                    ValidateLoadedFacilities();
                }
                else //we have a problem
                {
                    var errMsg = args.Result as String;
                    if (!string.IsNullOrEmpty(errMsg))
                    {
                        ActivityStatus = "Error Opening File";
                    }
                    if (args.Result is Exception)
                    {
                        var sb = new StringBuilder();
                        var ex = args.Result as Exception;
                        var indent = "";
                        while (ex != null)
                        {
                            sb.AppendFormat("{0}{1}\n", indent, ex.Message);
                            ex = ex.InnerException;
                            indent += "\t";
                        }
                        ActivityStatus = "Error Opening Ifc File\r\n\r\n" + sb;
                    }
                    ActivityProgress = 0;
                    ActivityStatus = "Error/Ready";
                }
            };
        }
        
        [Dependency]
        public ISaveFileSelector FileSelector { get; set; }

        private void ValidateLoadedFacilities()
        {
            if (RequirementFacility == null && SubmissionFacility != null)
            {
                // I want to save the cobie here.
                var filters = new List<string>
                {
                    "COBie excel|*.xlsx", 
                    "COBie binary excel|*.xls"
                };
                
                FileSelector.Filter = string.Join("|", filters.ToArray());
                FileSelector.Title = "Select destination file";

                var ret = FileSelector.ShowDialog();
                if (ret != DialogResult.OK) 
                    return;
                
                string msg;
                SubmissionFacility.WriteCobie(FileSelector.FileName, out msg);
                if (OpenOnExported && File.Exists(FileSelector.FileName))
                {
                    Process.Start(FileSelector.FileName);
                }
            }
            else if (RequirementFacility != null && SubmissionFacility != null)
            {
                ActivityStatus = "Validation in progress";
                var f = new cobieUKValidation.FacilityValidator();
                ValidationFacility = f.Validate(RequirementFacility, SubmissionFacility);
                ActivityStatus = "Validation completed";
                if (ExportOnValidated)
                {
                    ExportValidatedFacility();
                }
            }
        }

        private void CloseAndDeleteTemporaryFiles()
        {
            try
            {
                if (_worker != null && _worker.IsBusy)
                    _worker.CancelAsync(); //tell it to stop
                
                _openedModelFileName = null;
                if (_model == null) 
                    return;
                _model.Dispose();
                _model = null;
            }
            finally
            {
                if (!(_worker != null && _worker.IsBusy && _worker.CancellationPending)) //it is still busy but has been cancelled 
                {
                    if (!string.IsNullOrWhiteSpace(_temporaryXbimFileName) && File.Exists(_temporaryXbimFileName))
                        File.Delete(_temporaryXbimFileName);
                    _temporaryXbimFileName = null;
                } //else do nothing it will be cleared up in the worker thread
            }
        }

        public void LoadSubmissionFile(string modelFileName)
        {
            var fInfo = new FileInfo(modelFileName);
            if (!fInfo.Exists) // file does not exist; do nothing
                return;
            
            // there's no going back; if it fails after this point the current file should be closed anyway
            CloseAndDeleteTemporaryFiles();
            _openedModelFileName = modelFileName.ToLower();
            
            CreateWorker();

            var ext = fInfo.Extension.ToLower();
            switch (ext)
            {
                case ".json": 
                case ".xml":
                case ".xls":
                case ".xlsx": 
                    _worker.DoWork += OpenSubmissionCobieFile;
                    _worker.RunWorkerAsync(modelFileName);
                    break;
                case ".ifc": //it is an Ifc File
                case ".ifcxml": //it is an IfcXml File
                case ".ifczip": //it is a xip file containing xbim or ifc File
                case ".zip": //it is a xip file containing xbim or ifc File
                    _worker.DoWork += OpenIfcFile;
                    _worker.RunWorkerAsync(modelFileName);
                    break;
                case ".xbimf":
                case ".xbim": //it is an xbim File, just open it in the main thread
                    _worker.DoWork += OpenXbimFile;
                    _worker.RunWorkerAsync(modelFileName);
                    break;
            }
        }

        internal void ExportValidatedFacility()
        {
            if (File.Exists(ReportFileInfo.FileInfo.FullName))
                File.Delete(ReportFileInfo.FileInfo.FullName);
            var thread = new Thread(WorkThreadFunction);
            thread.Start();
            thread.Join();
            if (OpenOnExported && File.Exists(ReportFileInfo.FileInfo.FullName))
            {
                Process.Start(ReportFileInfo.FileInfo.FullName);
            }
        }

        public void WorkThreadFunction()
        {
            try
            {
                ActivityStatus = ValidationFacility.ExportFacility(ReportFileInfo.FileInfo);
            }
            catch (Exception ex)
            {
                ActivityStatus = @"Error.\r\n\r\n" + ex.Message;
            }
        }

        public bool ExportOnValidated { get; set; }

        public bool OpenOnExported { get; set; }
    }
}