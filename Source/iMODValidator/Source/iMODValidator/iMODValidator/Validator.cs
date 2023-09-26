// iMODValidator is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of iMODValidator.
// 
// iMODValidator is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// iMODValidator is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with iMODValidator. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD;
using Sweco.SIF.iMOD.IPF;
using Sweco.SIF.iMODValidator.Checks;
using Sweco.SIF.iMODValidator.Checks.CheckResults;
using Sweco.SIF.iMODValidator.Models;
using Sweco.SIF.iMODValidator.Models.Packages;
using Sweco.SIF.iMODValidator.Models.Packages.Files;
using Sweco.SIF.iMODValidator.Models.Runfiles;
using Sweco.SIF.iMODValidator.Results;
using Sweco.SIF.iMODValidator.Settings;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator
{
    /// <summary>
    /// Class for executing iMODValidator runs
    /// </summary>
    public class Validator
    {
        protected const float DefaultNoDataValue = -9999.0f;
        protected const string IMFFilenamePrefix = "ModelValidation";
        protected const string ResultTableFilenamePrefix = "ValidationStats";

        protected string toolname;
        public string ToolName
        {
            get { return toolname; }
            set
            {
                toolname = value;
                iMODValidatorSettingsManager.ApplicationName = toolname;
            }
        }
        public string ToolVersion { get; set; }

        protected CultureInfo cultureInfo = null;
        public CultureInfo CultureInfo
        {
            get { return cultureInfo; }
            set
            {
                cultureInfo = value;
                PackageFileFactory.CultureInfo = value;
            }
        }
        public Log Log { get; set; }
        public float NoDataValue { get; set; }
        public string OutputPath { get; set; }
        public string RUNFilename { get; set; }
        public bool IsModelValidated { get; set; }
        public SurfaceLevelMethod SurfaceLevelMethod { get; set; }
        public string SurfaceLevelFilename { get; set; }
        public float LevelErrorMargin { get; set; }
        public SplitValidationrunSettings.Options SplitValidationrunOption { get; set; }
        public bool UseSparseGrids { get; set; }
        public bool IsIMODOpened { get; set; }
        public bool IsResultSheetOpened { get; set; }
        public bool IsRelativePathIMFAdded { get; set; }
        public ExtentMethod ExtentType { get; set; }
        public Extent Extent { get; set; }
        public float SummaryMinCellsize { get; set; }
        public string OutputFilenameSubString { get; set; }
        public int MinILAY { get; set; }
        public int MaxILAY { get; set; }
        protected int minKPER;
        public int MinKPER
        {
            get { return minKPER; }
            set
            {
                if (value > 0)
                {
                    minKPER = value;
                }
                else
                {
                    minKPER = 0;
                }
            }
        }
        protected int maxKPER;
        public int MaxKPER
        {
            get { return maxKPER; }
            set
            {
                if (value >= 0)
                {
                    maxKPER = value;
                }
                else
                {
                    maxKPER = int.MaxValue;
                }
            }
        }

        protected Model Model { get; set; }

        public Validator(Log log)
        {
            cultureInfo = new CultureInfo("en-GB", false);
            this.Log = log;
            this.NoDataValue = DefaultNoDataValue;
            this.ExtentType = ExtentMethod.PackageFileExtent;
            this.Extent = null;
            this.minKPER = 0;
            this.maxKPER = int.MaxValue;
            this.MinILAY = 0;
            this.MaxILAY = int.MaxValue;
            this.SplitValidationrunOption = SplitValidationrunSettings.Options.None;
            this.OutputFilenameSubString = null;
            this.Model = null;
        }

        public virtual void Run()
        {
            CheckManager.Instance.ResetAbortActions();
            try
            {
                Initialize();

                CheckManager.Instance.CheckForAbort();

                // Read model and data
                Model = ReadModel();
                Extent = RetrieveExtent(Model, ExtentType);
                ReadModelData(Model);

                // Start model validation if requested
                if (IsModelValidated)
                {
                    if (SplitValidationrunOption.Equals(SplitValidationrunSettings.Options.Yearly) && (Model.StartDate == null))
                    {
                        Log.AddWarning("SpitValidationRunOption Yearly is only allowed when model has a startdate defined in the runfile, ignoring option Yearly");
                    }
                    if (SplitValidationrunOption.Equals(SplitValidationrunSettings.Options.Yearly) && (Model.StartDate != null))
                    {
                        // Split validation run in smaller runs
                        int fullMinKPER = minKPER;
                        int fullMaxKPER = maxKPER;
                        string baseOutputPath = OutputPath;
                        minKPER = fullMinKPER;
                        if (this.IsIMODOpened)
                        {
                            Log.AddInfo("Note: starting iMOD is turned off for Yearly split-validator-run option");
                        }
                        this.IsIMODOpened = false;
                        this.IsResultSheetOpened = false;

                        while ((minKPER <= fullMaxKPER) && (minKPER <= Model.NPER))
                        {
                            // Calculate end timestep for this run
                            DateTime currentStartDate = Model.GetStressPeriodDate((DateTime)Model.StartDate, minKPER);
                            DateTime currentEndDate = new DateTime(currentStartDate.Year + 1, 1, 1).Subtract(new TimeSpan(1, 0, 0, 0));
                            maxKPER = minKPER + (currentEndDate - currentStartDate).Days;
                            if (maxKPER > fullMaxKPER)
                            {
                                maxKPER = fullMaxKPER;
                            }

                            if ((fullMaxKPER - fullMinKPER) > 366)
                            {
                                // If more than ome year has to be checked, create a subdir per year
                                OutputPath = Path.Combine(baseOutputPath, currentStartDate.Year.ToString());
                                Model.ToolOutputPath = OutputPath;
                                if (!Directory.Exists(OutputPath))
                                {
                                    Directory.CreateDirectory(OutputPath);
                                }
                                OutputFilenameSubString = "_" + currentStartDate.Year.ToString(); // +((outputFilenameSubString == null) ? string.Empty : outputFilenameSubString);
                            }

                            // Start run                                
                            Log.AddInfo("Splitted validation run for year " + currentStartDate.Year.ToString() + ", timesteps " + minKPER + " - " + maxKPER);
                            ValidateModel(Model);

                            // Write current intermediate logfile 
                            Log.Flush();

                            // Calculate new start timestep
                            minKPER = maxKPER + 1;
                        }
                    }
                    else
                    {
                        ValidateModel(Model);
                    }
                }
            }
            catch (ToolException ex)
            {
                throw new ToolException("Error while running validator", ex);
            }
            // let other exceptions pass, but first free package memory
            catch (Exception ex)
            {
                if (Model != null)
                {
                    Model.ReleaseAllPackageMemory();
                }
                throw new Exception("Error while running validator", ex);
            }
        }

        private void Initialize()
        {
            PropagateSettings();
            CheckFileExistance();
            ISGRIVConverter.ClearCache();
            LogSettings(Log);

            // Write current intermediate logfile 
            Log.Flush();
        }

        protected void PropagateSettings()
        {
            IPFFile.IsWarnedForColumnMismatch = iMODValidatorSettingsManager.Settings.UseIPFWarningForColumnMismatch;
            IPFFile.IsWarnedForExistingPoints = iMODValidatorSettingsManager.Settings.UseIPFWarningForExistingPoints;
            IPFFile.UserDefinedListSeperators = iMODValidatorSettingsManager.Settings.DefaultIPFListSeperators;
            IPFTimeseries.DefaultListSeperators = iMODValidatorSettingsManager.Settings.DefaultIPFListSeperators;
        }

        protected void CheckFileExistance()
        {
            if (!Directory.Exists(OutputPath))
            {
                Directory.CreateDirectory(OutputPath);
            }

            if (!File.Exists(RUNFilename))
            {
                throw new Exception("Specified runfile doesn't exist: " + RUNFilename);
            }
        }

        protected void LogSettings(Log log)
        {
            if (log == null)
            {
                throw new Exception("Please add Log object to Validator instance");
            }

            log.AddInfo("Using extent method: " + ExtentType.ToString() + " (see below for actually used extent)");
            log.AddInfo("Using surfacelevel method: " + SurfaceLevelMethod.ToString());
            if (SurfaceLevelMethod == SurfaceLevelMethod.UseFilename)
            {
                log.AddInfo("Surfacelevel filename: " + SurfaceLevelFilename);
            }
            log.AddInfo("Level-error margin: " + LevelErrorMargin + "m");
            log.AddInfo("Checked timesteps: " + minKPER + " - " + maxKPER);
            log.AddInfo("Split validationrun option: " + SplitValidationrunOption.ToString());
            log.AddInfo("Checked layers: " + MinILAY + " - " + MaxILAY);
            log.AddInfo("Minimum summary-IDF cellsize: " + SummaryMinCellsize + "m");
            if (UseSparseGrids)
            {
                log.AddInfo("Sparse matrices: sparse matrices are use to store IDF-data");
            }
            log.AddInfo(string.Empty);

            log.AddMessage(LogLevel.Debug, "Allocated memory at start of validation: " + GC.GetTotalMemory(true) / 1000000 + "Mb");
            log.AddMessage(LogLevel.Debug, string.Empty);
        }

        protected Model ReadModel()
        {
            Runfile runfile = new V5Runfile(RUNFilename);
            Model model = runfile.ReadModel(Log, maxKPER);
            model.ToolOutputPath = OutputPath;

            Log.AddMessage(LogLevel.Debug, "Allocated memory after reading base runfile: " + GC.GetTotalMemory(true) / 1000000 + "Mb");
            Log.AddMessage(LogLevel.Debug, string.Empty);

            return model;
        }

        /// <summary>
        /// Actually read all data from packages, using lazy loading mechanism
        /// </summary>
        /// <param name="model"></param>
        protected void ReadModelData(Model model)
        {
            // Actually read all data from modelfiles
            Log.AddMessage(LogLevel.Trace, "Reading packagefiles...");
            foreach (Package package in model.Packages)
            {
                // read packages using lazy loading mechanism
                package.ReadFiles(Log, iMODValidatorSettingsManager.Settings.UseLazyLoading, model.GetExtent(), 2);
            }
            Log.AddMessage(LogLevel.Debug, "Allocated memory after reading base packagefiles: " + GC.GetTotalMemory(true) / 1000000 + "Mb");
            Log.AddMessage(LogLevel.Trace, string.Empty);

            // Retrieve surface level filename
            model.SurfaceLevelFilename = RetrieveSurfaceLevelFilename(SurfaceLevelMethod, model, Log);
        }

        protected virtual void ValidateModel(Model model)
        {
            Log.AddInfo(string.Empty);
            Log.AddInfo("Starting validation...");

            // Now run the available checks
            CheckResultHandler resultHandler = new CheckResultHandler(model, NoDataValue, Extent, SIFTool.Instance.ToolName + "-tool" + ((SIFTool.Instance.ToolVersion != null) ? (", " + SIFTool.Instance.ToolVersion) : string.Empty));
            // resultHandler.OutputPath = CheckManager.Instance.GetiMODFilesPath(model);
            SetResultHandlerSettings(resultHandler);

            List<Check> activeChecks = new List<Check>();
            foreach (Check check in CheckManager.Instance.Checks)
            {
                CheckManager.Instance.CheckForAbort();

                if (check.IsActive)
                {
                    Log.AddMessage(LogLevel.Debug, "Allocated memory before check " + check.Name + ": " + GC.GetTotalMemory(false) / 1000000 + "Mb");
                    activeChecks.Add(check);
                    check.Reset();
                    check.Run(model, resultHandler, Log);
                    if (iMODValidatorSettingsManager.Settings.UseLazyLoading)
                    {
                        model.ReleaseAllPackageMemory(Log);
                    }
                    Log.AddMessage(LogLevel.Debug, "Allocated memory after check " + check.Name + ": " + GC.GetTotalMemory(true) / 1000000 + "Mb");
                }
            }

            if (activeChecks.Count == 0)
            {
                Log.AddWarning("No checks have been selected");
            }
            if (activeChecks.Count == 1)
            {
                // prefix current outputFilenameSubstring with the name of the single check
                OutputFilenameSubString = "_" + activeChecks[0].Abbreviation + ((OutputFilenameSubString == null) ? string.Empty : OutputFilenameSubString);
            }

            CheckManager.Instance.CheckForAbort();

            string imfFilename = IMFFilenamePrefix;
            string fullResultTableFilenamePrefix = ResultTableFilenamePrefix;
            if ((OutputFilenameSubString != null) && !OutputFilenameSubString.Equals(string.Empty))
            {
                imfFilename += OutputFilenameSubString;
                fullResultTableFilenamePrefix += OutputFilenameSubString;
            }
            imfFilename += ".IMF";
            resultHandler.WriteResults(imfFilename, fullResultTableFilenamePrefix, "No validation problems found.", IsRelativePathIMFAdded, IsIMODOpened, IsResultSheetOpened, Log);

            if (Log.Errors.Count > 0)
            {
                Log.AddInfo("There were " + Log.Errors.Count + " unexpected errors during validation, this may affect the result.");
            }
            if (Log.Warnings.Count > 0)
            {
                Log.AddInfo("There were " + Log.Warnings.Count + " unexpected warnings during validation, this may affect the result.");
            }
        }


        /// <summary>
        /// Sets userdefined options for resulthandler: MaxKPER, LevelErrorMargin, MinEntryNumber, UseSparseGrids, SummaryMinCellSize
        /// </summary>
        /// <param name="resultHandler"></param>
        protected void SetResultHandlerSettings(ResultHandler resultHandler)
        {
            resultHandler.LevelErrorMargin = LevelErrorMargin;
            resultHandler.MinKPER = (minKPER >= 0) ? minKPER : 0;
            resultHandler.MaxKPER = (maxKPER >= 0) ? maxKPER : resultHandler.Model.NPER;
            if (resultHandler.Model.IsSteadyStateModel())
            {
                resultHandler.MaxKPER = 0;
            }
            resultHandler.MinEntryNumber = MinILAY;
            resultHandler.MaxEntryNumber = MaxILAY;
            resultHandler.UseSparseGrids = UseSparseGrids;
            resultHandler.SummaryMinCellsize = SummaryMinCellsize;
        }

        protected Extent RetrieveExtent(Model model, ExtentMethod extentType)
        {
            Extent extent = null;
            switch (extentType)
            {
                case ExtentMethod.ModelExtent:
                    extent = model.GetExtent();
                    break;
                case ExtentMethod.PackageFileExtent:
                    extent = model.GetPackageExtent();
                    break;
                case ExtentMethod.CustomExtent:
                    // Keep custom extent that already has been set via the Extent-property 
                    extent = this.Extent;
                    if (extent == null)
                    {
                        throw new Exception("No extent specified for custom extent");
                    }
                    break;
                default:
                    throw new Exception("Invalid ExtentType: " + extentType);
            }

            if (extent.IsValidExtent())
            {
                Log.AddInfo("Using extent " + extent.ToString());
                return extent;
            }
            else
            {
                throw new Exception("Invalid extent used: " + extent.ToString());
            }
        }

        protected string RetrieveSurfaceLevelFilename(SurfaceLevelMethod surfaceLevelMethod, Model model, Log log, bool areWarningsShown = true)
        {
            string surfaceLevelFilename = null;

            switch (surfaceLevelMethod)
            {
                case SurfaceLevelMethod.Smart:
                    log.AddInfo("Smart surface level selection has been specified. Trying several methods.");
                    // First try to find file in most likely DBASE path
                    // TODO

                    // If not found, try MetaSWAP method
                    if (surfaceLevelFilename == null)
                    {
                        surfaceLevelFilename = RetrieveSurfaceLevelFilename(SurfaceLevelMethod.UseMetaSWAP, model, log, false);
                    }
                    if (surfaceLevelFilename == null)
                    {
                        surfaceLevelFilename = RetrieveSurfaceLevelFilename(SurfaceLevelMethod.UseOLF, model, log, false);
                    }
                    if (surfaceLevelFilename == null)
                    {
                        surfaceLevelFilename = RetrieveSurfaceLevelFilename(SurfaceLevelMethod.UseFilename, model, log, false);
                    }
                    break;
                case SurfaceLevelMethod.UseMetaSWAP:
                    CAPPackage capPackage = (CAPPackage)model.GetPackage(CAPPackage.DefaultKey);
                    IMODFile surfaceLevelIDFFile = (capPackage != null) ? capPackage.GetIMODFile(CAPPackage.GetRunFileV5EntryIdx(CAPEntryCode.SEV)) : null;
                    if ((capPackage != null) && (surfaceLevelIDFFile != null))
                    {
                        surfaceLevelFilename = surfaceLevelIDFFile.Filename;
                        if (File.Exists(surfaceLevelFilename))
                        {
                            log.AddInfo("Using CAP surface elevation as model surface level: " + surfaceLevelFilename);
                        }
                        else
                        {
                            log.AddError(capPackage.Key, surfaceLevelFilename, "Unexisting CAP surfacelevel filename: " + surfaceLevelFilename);
                            surfaceLevelFilename = null;
                        }
                    }
                    else
                    {
                        if (areWarningsShown)
                        {
                            log.AddWarning("Surfacelevel", null, "CAP-package or surface elevation file not defined, no file available for use as surfacelevel.");
                        }
                    }
                    break;
                case SurfaceLevelMethod.UseOLF:
                    IDFPackage olfPackage = (IDFPackage)model.GetPackage(OLFPackage.DefaultKey);
                    if ((olfPackage != null) && (olfPackage.GetIDFPackageFile(0) != null))
                    {
                        surfaceLevelFilename = olfPackage.GetIDFPackageFile(0).FName;
                        if (File.Exists(surfaceLevelFilename))
                        {
                            log.AddInfo("Using OLF-file as surface level file: " + surfaceLevelFilename);
                        }
                        else
                        {
                            log.AddError(olfPackage.Key, surfaceLevelFilename, "Unexisting OLF filename: " + surfaceLevelFilename);
                            surfaceLevelFilename = null;
                        }
                    }
                    else
                    {
                        if (areWarningsShown)
                        {
                            log.AddWarning("Surfacelevel", model.Runfilename, "OLF-package or file not defined, no surface elevationn available for use as surfacelevel.");
                        }
                    }
                    break;
                case SurfaceLevelMethod.UseFilename:
                    surfaceLevelFilename = this.SurfaceLevelFilename;

                    if (this.SurfaceLevelFilename == null)
                    {
                        log.AddError("Surfacelevel", null, "Unspecified surfacelevel filename in selected method 'UseFilename'");
                        surfaceLevelFilename = null;
                    }
                    else if (File.Exists(this.SurfaceLevelFilename))
                    {
                        log.AddInfo("Using surface level file: " + surfaceLevelFilename);
                    }
                    else
                    {
                        log.AddError("Surfacelevel", surfaceLevelFilename, "Unexisting surfacelevel filename: " + surfaceLevelFilename);
                        surfaceLevelFilename = null;
                    }

                    break;
                default:
                    break;
            }

            return surfaceLevelFilename;
        }

        /// <summary>
        /// Load settings, from a specified file location or (if settingsFilename is null) from the default location
        /// </summary>
        /// <param name="settingsFilename"></param>
        public virtual void LoadSettings(string settingsFilename = null)
        {
            try
            {
                CultureInfo englishCultureInfo = new CultureInfo("en-GB", false);

                // Load settings from file
                iMODValidatorSettingsManager.LoadMainSettings(settingsFilename);

                NoDataValue = iMODValidatorSettingsManager.Settings.DefaultNoDataValue;

                // Retrieve output settings
                IsIMODOpened = iMODValidatorSettingsManager.Settings.IsIMODOpened;
                IsResultSheetOpened = iMODValidatorSettingsManager.Settings.IsExcelOpened;

                // Retrieve and check extent settings
                if (iMODValidatorSettingsManager.Settings.UseCustomExtentMethod)
                {
                    float urx = iMODValidatorSettingsManager.Settings.DefaultCustomExtentURX;
                    float ury = iMODValidatorSettingsManager.Settings.DefaultCustomExtentURY;
                    float llx = iMODValidatorSettingsManager.Settings.DefaultCustomExtentLLX;
                    float lly = iMODValidatorSettingsManager.Settings.DefaultCustomExtentLLY;
                    ExtentType = ExtentMethod.CustomExtent;
                    Extent = new Extent(llx, lly, urx, ury);
                }
                else if (iMODValidatorSettingsManager.Settings.UsePackageFileExtentMethod)
                {
                    ExtentType = ExtentMethod.PackageFileExtent;
                }
                else
                {
                    ExtentType = ExtentMethod.ModelExtent;
                }

                // Set default surface level file and method
                try
                {
                    SurfaceLevelFilename = iMODValidatorSettingsManager.Settings.DefaultSurfaceLevelFilename;
                    if (!Path.IsPathRooted(SurfaceLevelFilename))
                    {
                        SurfaceLevelFilename = Path.Combine(Directory.GetCurrentDirectory(), SurfaceLevelFilename);
                    }
                    if (!File.Exists(SurfaceLevelFilename))
                    {
                        // ignore for now
                    }
                }
                catch (Exception)
                {
                    // ignore
                }
                if (iMODValidatorSettingsManager.Settings.UseSmartSurfaceLevelMethod)
                {
                    SurfaceLevelMethod = SurfaceLevelMethod.Smart;
                    SurfaceLevelFilename = null;
                }
                else if (iMODValidatorSettingsManager.Settings.UseMetaSWAPSurfaceLevelMethod)
                {
                    SurfaceLevelMethod = SurfaceLevelMethod.UseMetaSWAP;
                    SurfaceLevelFilename = null;
                }
                else if (iMODValidatorSettingsManager.Settings.UseOLFSurfaceLevelMethod)
                {
                    SurfaceLevelMethod = SurfaceLevelMethod.UseOLF;
                    SurfaceLevelFilename = null;
                }
                else if (iMODValidatorSettingsManager.Settings.UseFileSurfaceLevelMethod)
                {
                    if ((iMODValidatorSettingsManager.Settings.DefaultSurfaceLevelFilename != null) && (iMODValidatorSettingsManager.Settings.DefaultSurfaceLevelFilename != string.Empty))
                    {
                        SurfaceLevelMethod = SurfaceLevelMethod.UseFilename;
                        SurfaceLevelFilename = iMODValidatorSettingsManager.Settings.DefaultSurfaceLevelFilename;
                    }
                    else
                    {
                        SurfaceLevelMethod = SurfaceLevelMethod.Smart;
                        SurfaceLevelFilename = null;
                    }
                }
                else
                {
                    SurfaceLevelMethod = SurfaceLevelMethod.Smart;
                    SurfaceLevelFilename = null;
                }

                // Retrieve and check error margin settings
                float levelErrorMargin = iMODValidatorSettingsManager.Settings.LevelErrorMargin;
                this.LevelErrorMargin = (levelErrorMargin > 0) ? levelErrorMargin : 0;

                // Retrieve min/max timestep setting
                int minTimeStep = -1;
                if (int.TryParse(iMODValidatorSettingsManager.Settings.MinTimestep, out minTimeStep))
                {
                    minKPER = minTimeStep;
                }
                int maxTimeStep = -1;
                if (int.TryParse(iMODValidatorSettingsManager.Settings.MaxTimestep, out maxTimeStep))
                {
                    maxKPER = maxTimeStep;
                }

                SplitValidationrunOption = iMODValidatorSettingsManager.Settings.SplitValidationrunOption;

                // Retrieve ilay settings
                if (int.TryParse(iMODValidatorSettingsManager.Settings.MinILAY, out int minILAYValue))
                {
                    MinILAY = minILAYValue;
                }
                if (int.TryParse(iMODValidatorSettingsManager.Settings.MaxILAY, out int maxILAYValue))
                {
                    MaxILAY = maxILAYValue;
                }

                IPFFile.UserDefinedListSeperators = iMODValidatorSettingsManager.Settings.DefaultIPFListSeperators;

                UseSparseGrids = iMODValidatorSettingsManager.Settings.UseSparseMatrix;
                IsRelativePathIMFAdded = iMODValidatorSettingsManager.Settings.IsRelativePathIMFAdded;
                // iMODValidatorSettingsManager.Settings.UseIPFWarningForExistingPoints is stored in settings object
                // iMODValidatorSettingsManager.Settings.UseIPFWarningForColumnMismatch is stored in settings object

                // Retrieve summary min cellsize settings
                SummaryMinCellsize = iMODValidatorSettingsManager.Settings.DefaultSummaryMinCellSize;
            }
            catch (Exception ex)
            {
                if (settingsFilename == null)
                {
                    throw new Exception("Could not load settings from default location", ex);
                }
                else
                {
                    throw new Exception("Could not load settings from: " + settingsFilename, ex);
                }
            }
        }
    }
}
