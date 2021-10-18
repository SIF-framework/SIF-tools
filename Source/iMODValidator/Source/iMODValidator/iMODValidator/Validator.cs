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
        protected float noDataValue;
        public float NoDataValue
        {
            get { return noDataValue; }
            set { noDataValue = value; }
        }
        protected string outputPath;
        public string OutputPath
        {
            get { return outputPath; }
            set { outputPath = value; }
        }
        protected string runfilename;
        public string Runfilename
        {
            get { return runfilename; }
            set { runfilename = value; }
        }
        protected bool isModelValidated;
        public bool IsModelValidated
        {
            get { return isModelValidated; }
            set { isModelValidated = value; }
        }
        protected bool isModelCorrected;
        public bool IsModelCorrected
        {
            get { return isModelCorrected; }
            set { isModelCorrected = value; }
        }
        protected SurfaceLevelMethod surfaceLevelMethod;
        public SurfaceLevelMethod SurfaceLevelMethod
        {
            get { return surfaceLevelMethod; }
            set { surfaceLevelMethod = value; }
        }
        protected string surfaceLevelFilename;
        public string SurfaceLevelFilename
        {
            get { return surfaceLevelFilename; }
            set { surfaceLevelFilename = value; }
        }
        protected float levelErrorMargin;
        public float LevelErrorMargin
        {
            get { return levelErrorMargin; }
            set { levelErrorMargin = value; }
        }
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
        protected SplitValidationrunSettings.Options splitValidationrunOption;
        public SplitValidationrunSettings.Options SplitValidationrunOption
        {
            get { return splitValidationrunOption; }
            set
            {
                splitValidationrunOption = value;
            }
        }
        protected int maxKPER;
        public int MaxKPER
        {
            get { return maxKPER; }
            set
            {
                if (value > 0)
                {
                    maxKPER = value;
                }
                else
                {
                    maxKPER = int.MaxValue;
                }
            }
        }
        protected int minILAY;
        public int MinILAY
        {
            get { return minILAY; }
            set { minILAY = value; }
        }
        protected int maxILAY;
        public int MaxILAY
        {
            get { return maxILAY; }
            set { maxILAY = value; }
        }
        protected bool useSparseGrids;
        public bool UseSparseGrids
        {
            get { return useSparseGrids; }
            set { useSparseGrids = value; }
        }
        protected bool isIModOpened;
        public bool IsIModOpened
        {
            get { return isIModOpened; }
            set { isIModOpened = value; }
        }
        protected bool isResultSheetOpened;
        public bool IsResultSheetOpened
        {
            get { return isResultSheetOpened; }
            set { isResultSheetOpened = value; }
        }
        protected bool isRelativePathIMFAdded;
        public bool IsRelativePathIMFAdded
        {
            get { return isRelativePathIMFAdded; }
            set { isRelativePathIMFAdded = value; }
        }
        protected ExtentMethod extentType;
        public ExtentMethod ExtentType
        {
            get { return extentType; }
            set { extentType = value; }
        }
        protected Extent extent;
        public Extent Extent
        {
            get { return extent; }
            set { extent = value; }
        }
        protected float summaryMinCellsize;
        public float SummaryMinCellsize
        {
            get { return summaryMinCellsize; }
            set { summaryMinCellsize = value; }
        }
        protected string outputFilenameSubString;
        public string OutputFilenameSubString
        {
            get { return outputFilenameSubString; }
            set { outputFilenameSubString = value; }
        }

        protected Model model = null;

        public Validator()
        {
            cultureInfo = new CultureInfo("en-GB", false);
            this.noDataValue = DefaultNoDataValue;
            this.extentType = ExtentMethod.PackageFileExtent;
            this.extent = null;
            this.minKPER = 0;
            this.maxKPER = int.MaxValue;
            this.minILAY = 0;
            this.maxILAY = int.MaxValue;
            this.splitValidationrunOption = SplitValidationrunSettings.Options.None;
            this.outputFilenameSubString = null;
        }

        public virtual void Run(Log log)
        {
            PropagateSettings();
            CheckManager.Instance.ResetAbortActions();
            try
            {
                CheckFileExistance();
                LogSettings(log);

                // Write current intermediate logfile 
                log.Flush();

                if (isModelValidated)
                {
                    CheckManager.Instance.CheckForAbort();
                    model = ReadModel(log);
                    ReadModelData(model, log);
                    Extent = RetrieveExtent(model, extentType, log);
                }

                // Start model validation if requested
                if (isModelValidated)
                {
                    if (splitValidationrunOption.Equals(SplitValidationrunSettings.Options.Yearly) && (model.StartDate == null))
                    {
                        log.AddWarning("SpitValidationRunOption Yearly is only allowed when model has a startdate defined in the runfile, ignoring option Yearly");
                    }
                    if (splitValidationrunOption.Equals(SplitValidationrunSettings.Options.Yearly) && (model.StartDate != null))
                    {
                        // Split validation run in smaller runs
                        int fullMinKPER = minKPER;
                        int fullMaxKPER = maxKPER;
                        string baseOutputPath = outputPath;
                        minKPER = fullMinKPER;
                        if (this.IsIModOpened)
                        {
                            log.AddInfo("Note: starting iMOD is turned off for Yearly split-validator-run option");
                        }
                        this.isIModOpened = false;
                        this.isResultSheetOpened = false;
                        while ((minKPER <= fullMaxKPER) && (minKPER <= model.NPER))
                        {
                            // Calculate end timestep for this run
                            DateTime currentStartDate = Model.GetStressPeriodDate((DateTime)model.StartDate, minKPER);
                            DateTime currentEndDate = new DateTime(currentStartDate.Year + 1, 1, 1).Subtract(new TimeSpan(1, 0, 0, 0));
                            maxKPER = minKPER + (currentEndDate - currentStartDate).Days;
                            if (maxKPER > fullMaxKPER)
                            {
                                maxKPER = fullMaxKPER;
                            }

                            if ((fullMaxKPER - fullMinKPER) > 366)
                            {
                                // If more than ome year has to be checked, create a subdir per year
                                outputPath = Path.Combine(baseOutputPath, currentStartDate.Year.ToString());
                                model.ToolOutputPath = outputPath;
                                if (!Directory.Exists(outputPath))
                                {
                                    Directory.CreateDirectory(outputPath);
                                }
                                outputFilenameSubString = "_" + currentStartDate.Year.ToString(); // +((outputFilenameSubString == null) ? string.Empty : outputFilenameSubString);
                            }

                            // Start run                                
                            CheckManager.Instance.CheckForAbort();
                            log.AddInfo("Splitted validation run for year " + currentStartDate.Year.ToString() + ", timesteps " + minKPER + " - " + maxKPER);
                            ValidateModel(model, log);

                            // Write current intermediate logfile 
                            log.Flush();

                            // Calculate new start timestep
                            minKPER = maxKPER + 1;
                        }
                    }
                    else
                    {
                        CheckManager.Instance.CheckForAbort();
                        ValidateModel(model, log);
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
                if (model != null)
                {
                    model.ReleaseAllPackageMemory();
                }
                throw new Exception("Error during validation", ex);
            }
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
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            if (!File.Exists(runfilename))
            {
                throw new Exception("Specified runfile doesn't exist: " + runfilename);
            }
        }

        protected void LogSettings(Log log)
        {
            log.AddInfo("Basemodel runfilename: " + runfilename);
            log.AddInfo("Using extent method: " + extentType.ToString() + " (see below for actually used extent)");
            log.AddInfo("Using surfacelevel method: " + surfaceLevelMethod.ToString());
            if (surfaceLevelMethod == SurfaceLevelMethod.UseFilename)
            {
                log.AddInfo("Surfacelevel filename: " + surfaceLevelFilename);
            }
            log.AddInfo("Level-error margin: " + levelErrorMargin + "m");
            log.AddInfo("Checked timesteps: " + minKPER + " - " + maxKPER);
            log.AddInfo("Split validationrun option: " + splitValidationrunOption.ToString());
            log.AddInfo("Checked layers: " + minILAY + " - " + maxILAY);
            log.AddInfo("Minimum summary-IDF cellsize: " + summaryMinCellsize + "m");
            if (useSparseGrids)
            {
                log.AddInfo("Sparse matrices: sparse matrices are use to store IDF-data");
            }
            log.AddInfo(string.Empty);

            log.AddMessage(LogLevel.Debug, "Allocated memory at start of validation: " + GC.GetTotalMemory(true) / 1000000 + "Mb");
            log.AddMessage(LogLevel.Debug, string.Empty);
        }

        protected Model ReadModel(Log log)
        {
            Runfile runfile = new V5Runfile(runfilename);
            Model model = runfile.ReadModel(log, maxKPER);
            model.ToolOutputPath = outputPath;

            log.AddMessage(LogLevel.Debug, "Allocated memory after reading base runfile: " + GC.GetTotalMemory(true) / 1000000 + "Mb");
            log.AddMessage(LogLevel.Debug, string.Empty);

            return model;
        }

        protected void ReadModelData(Model model, Log log)
        {
            // Actually read all data from modelfiles
            log.AddMessage(LogLevel.Trace, "Reading packagefiles...");
            foreach (Package package in model.Packages)
            {
                // read packages using lazy loading mechanism
                package.ReadFiles(log, iMODValidatorSettingsManager.Settings.UseLazyLoading, 2);
            }
            log.AddMessage(LogLevel.Debug, "Allocated memory after reading base packagefiles: " + GC.GetTotalMemory(true) / 1000000 + "Mb");
            log.AddMessage(LogLevel.Trace, string.Empty);

            // Retrieve surface level filename
            model.SurfaceLevelFilename = RetrieveSurfaceLevelFilename(surfaceLevelMethod, model, log);
        }

        /// <summary>
        /// Sets userdefined options for resulthandler: MaxKPER, LevelErrorMargin, MinEntryNumber, UseSparseGrids, SummaryMinCellSize
        /// </summary>
        /// <param name="resultHandler"></param>
        protected void SetResultHandlerSettings(ResultHandler resultHandler)
        {
            resultHandler.LevelErrorMargin = levelErrorMargin;
            resultHandler.MinKPER = (minKPER >= 0) ? minKPER : 0;
            resultHandler.MaxKPER = (maxKPER >= 0) ? maxKPER : resultHandler.Model.NPER;
            resultHandler.MinEntryNumber = minILAY;
            resultHandler.MaxEntryNumber = maxILAY;
            resultHandler.UseSparseGrids = useSparseGrids;
            resultHandler.SummaryMinCellsize = summaryMinCellsize;
        }

        protected Extent RetrieveExtent(Model model, ExtentMethod extentType, Log log)
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
                    extent = this.extent;
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
                log.AddInfo("Using extent " + extent.ToString(), 1);
                log.AddInfo(string.Empty);
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
                    if ((capPackage != null) && (capPackage.GetSurfaceLevelFilename() != null))
                    {
                        surfaceLevelFilename = capPackage.GetSurfaceLevelFilename();
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
                    surfaceLevelFilename = this.surfaceLevelFilename;

                    if (this.surfaceLevelFilename == null)
                    {
                        log.AddError("Surfacelevel", null, "Unspecified surfacelevel filename in selected method 'UseFilename'");
                        surfaceLevelFilename = null;
                    }
                    else if (File.Exists(this.surfaceLevelFilename))
                    {
                        log.AddInfo("Using specfied file as surface level file: " + surfaceLevelFilename);
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


        private void ValidateModel(Model model, Log log)
        {
            log.AddInfo("Starting validation...");

            // Now run the available checks
            CheckResultHandler resultHandler = new CheckResultHandler(model, noDataValue, extent, ToolName + "-tool" + ((ToolVersion != null) ? (", " + ToolVersion) : string.Empty));
            // resultHandler.OutputPath = CheckManager.Instance.GetiMODFilesPath(model);
            SetResultHandlerSettings(resultHandler);

            List<Check> activeChecks = new List<Check>();
            foreach (Check check in CheckManager.Instance.Checks)
            {
                CheckManager.Instance.CheckForAbort();

                if (check.IsActive)
                {
                    log.AddMessage(LogLevel.Debug, "Allocated memory before check " + check.Name + ": " + GC.GetTotalMemory(false) / 1000000 + "Mb");
                    activeChecks.Add(check);
                    check.Reset();
                    check.IsModelCorrected = isModelCorrected;
                    check.Run(model, resultHandler, log);
                    if (iMODValidatorSettingsManager.Settings.UseLazyLoading)
                    {
                        model.ReleaseAllPackageMemory(log);
                    }
                    log.AddMessage(LogLevel.Debug, "Allocated memory after check " + check.Name + ": " + GC.GetTotalMemory(true) / 1000000 + "Mb");
                }
            }

            if (activeChecks.Count == 0)
            {
                log.AddWarning("No checks have been selected");
            }
            if (activeChecks.Count == 1)
            {
                // prefix current outputFilenameSubstring with the name of the single check
                outputFilenameSubString = "_" + activeChecks[0].Abbreviation + ((outputFilenameSubString == null) ? string.Empty : outputFilenameSubString);
            }

            CheckManager.Instance.CheckForAbort();

            string imfFilename = IMFFilenamePrefix;
            string fullResultTableFilenamePrefix = ResultTableFilenamePrefix;
            if ((outputFilenameSubString != null) && !outputFilenameSubString.Equals(string.Empty))
            {
                imfFilename += outputFilenameSubString;
                fullResultTableFilenamePrefix += outputFilenameSubString;
            }
            imfFilename += ".IMF";
            resultHandler.WriteResults(imfFilename, fullResultTableFilenamePrefix, "No validation problems found.", isRelativePathIMFAdded, isIModOpened, isResultSheetOpened, log);

            if (log.Errors.Count > 0)
            {
                log.AddInfo("There were " + log.Errors.Count + " unexpected errors during validation, this may affect the result.");
            }
            if (log.Warnings.Count > 0)
            {
                log.AddInfo("There were " + log.Warnings.Count + " unexpected warnings during validation, this may affect the result.");
            }
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

                noDataValue = iMODValidatorSettingsManager.Settings.DefaultNoDataValue;

                // Retrieve output settings
                isIModOpened = iMODValidatorSettingsManager.Settings.IsIModOpened;
                isResultSheetOpened = iMODValidatorSettingsManager.Settings.IsExcelOpened;

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
                    surfaceLevelFilename = iMODValidatorSettingsManager.Settings.DefaultSurfaceLevelFilename;
                    if (!Path.IsPathRooted(surfaceLevelFilename))
                    {
                        surfaceLevelFilename = Path.Combine(Directory.GetCurrentDirectory(), surfaceLevelFilename);
                    }
                    if (!File.Exists(surfaceLevelFilename))
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
                    surfaceLevelMethod = SurfaceLevelMethod.Smart;
                    surfaceLevelFilename = null;
                }
                else if (iMODValidatorSettingsManager.Settings.UseMetaSWAPSurfaceLevelMethod)
                {
                    surfaceLevelMethod = SurfaceLevelMethod.UseMetaSWAP;
                    surfaceLevelFilename = null;
                }
                else if (iMODValidatorSettingsManager.Settings.UseOLFSurfaceLevelMethod)
                {
                    surfaceLevelMethod = SurfaceLevelMethod.UseOLF;
                    surfaceLevelFilename = null;
                }
                else if (iMODValidatorSettingsManager.Settings.UseFileSurfaceLevelMethod)
                {
                    if ((iMODValidatorSettingsManager.Settings.DefaultSurfaceLevelFilename != null) && (iMODValidatorSettingsManager.Settings.DefaultSurfaceLevelFilename != string.Empty))
                    {
                        surfaceLevelMethod = SurfaceLevelMethod.UseFilename;
                        surfaceLevelFilename = iMODValidatorSettingsManager.Settings.DefaultSurfaceLevelFilename;
                    }
                    else
                    {
                        surfaceLevelMethod = SurfaceLevelMethod.Smart;
                        surfaceLevelFilename = null;
                    }
                }
                else
                {
                    surfaceLevelMethod = SurfaceLevelMethod.Smart;
                    surfaceLevelFilename = null;
                }

                // Retrieve and check error margin settings
                float levelErrorMargin = iMODValidatorSettingsManager.Settings.LevelErrorMargin;
                this.levelErrorMargin = (levelErrorMargin > 0) ? levelErrorMargin : 0;

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

                splitValidationrunOption = iMODValidatorSettingsManager.Settings.SplitValidationrunOption;

                // Retrieve ilay settings
                int minILAYValue = 1;
                if (int.TryParse(iMODValidatorSettingsManager.Settings.MinILAY, out minILAYValue))
                {
                    minILAY = minILAYValue;
                }
                int maxILAYValue = 1;
                if (int.TryParse(iMODValidatorSettingsManager.Settings.MaxILAY, out maxILAYValue))
                {
                    maxILAY = maxILAYValue;
                }

                useSparseGrids = iMODValidatorSettingsManager.Settings.UseSparseMatrix;
                isRelativePathIMFAdded = iMODValidatorSettingsManager.Settings.IsRelativePathIMFAdded;
                // iMODValidatorSettingsManager.Settings.UseIPFWarningForExistingPoints is stored in settings object
                // iMODValidatorSettingsManager.Settings.UseIPFWarningForColumnMismatch is stored in settings object

                // Retrieve summary min cellsize settings
                summaryMinCellsize = iMODValidatorSettingsManager.Settings.DefaultSummaryMinCellSize;
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
