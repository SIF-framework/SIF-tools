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
using OrderedPropertyGrid;
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD;
using Sweco.SIF.iMOD.IDF;
using Sweco.SIF.iMOD.Legends;
using Sweco.SIF.iMODValidator.Checks.CheckResults;
using Sweco.SIF.iMODValidator.Models;
using Sweco.SIF.iMODValidator.Models.Files;
using Sweco.SIF.iMODValidator.Models.Packages;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Checks
{
    [TypeConverter(typeof(PropertySorter))]
    class MetaSWAPCheckSettings : CheckSettings
    {
        [Category("Warning-properties"), Description("The minimum valid rootzone depth (cm) for this region"), PropertyOrder(10)]
        public string MinRTZDepth { get; set; }

        [Category("Warning-properties"), Description("The maximum valid rootzone depth (cm) for this region"), PropertyOrder(11)]
        public string MaxRTZDepth { get; set; }

        [Category("Warning-properties"), Description("The name(s) for paved urban area in luse_svat.inp, seperated by ';'"), PropertyOrder(20)]
        public string LuseUBANames { get; set; }

        [Category("Warning-properties"), Description("The name(s) for wetted area in luse_svat.inp, seperated by ';'"), PropertyOrder(21)]
        public string LuseWTANames { get; set; }

        [Category("Warning-properties"), Description("The substring(s) for which DRN-files are excluded for check with wetted area, seperated by ';'"), PropertyOrder(30)]
        public string ExcludedDRNFileSubstrings { get; set; }

        [Category("Warning-properties"), Description("Specify if ISG-files should be converted to RIV for calculation of wetted areä"), PropertyOrder(30)]
        public bool IsISGConverted { get; set; }

        [Category("Warning-properties"), Description("The minimum resistance (d) for RIV/DRN, used to calculate minimum wetted area from RIV/DRN-conductance; use 0 to ignore"), PropertyOrder(31)]
        public string MinRIVResistance { get; set; }

        [Category("Warning-properties"), Description("The maximum valid wetted area percentage (%) when RIV/DRN not exists; use 100 to ignore"), PropertyOrder(32)]
        public string MaxWTARIVPercentage { get; set; }

        [Category("Warning-properties"), Description("The minimum valid ponding depth (m)"), PropertyOrder(33)]
        public string MinPondingDepth { get; set; }

        [Category("Warning-properties"), Description("The maximum valid ponding depth (m)"), PropertyOrder(34)]
        public string MaxPondingDepth { get; set; }

        [Category("Warning-properties"), Description("Specifies if meteo data is checked"), PropertyOrder(40)]
        public bool IsMeteoChecked { get; set; }

        [Category("Warning-properties"), Description("The minimum valid precipitation (mm/d) for this region"), PropertyOrder(41)]
        public string MinPrecipitationValue { get; set; }

        [Category("Warning-properties"), Description("The maximum valid precipitation (mm/d) for this region"), PropertyOrder(42)]
        public string MaxPrecipitationValue { get; set; }

        [Category("Warning-properties"), Description("The minimum valid evapotranspiration (mm/d) for this region"), PropertyOrder(43)]
        public string MinEvapotranspirationValue { get; set; }

        [Category("Warning-properties"), Description("The maximum valid evapotranspiration (mm/d) for this region"), PropertyOrder(44)]
        public string MaxEvapotranspirationValue { get; set; }

        [Category("Warning-properties"), Description("The maximum number of files with meteo issues to report"), PropertyOrder(45)]
        public int MaxMeteoIssueFiles { get; set; }

        public MetaSWAPCheckSettings(string checkName) : base(checkName)
        {
            MinRTZDepth = "5";
            MaxRTZDepth = "120";
            LuseUBANames = "stedelijk bebouwd"; // Multiple names can be defined, seperated by semicolon ';'
            LuseWTANames = "zoet water;zout water";             // Multiple names can be defined, seperated by semicolon ';'
            ExcludedDRNFileSubstrings = "buis;buizen";
            IsISGConverted = true;
            MinRIVResistance = "0.5";
            MaxWTARIVPercentage = "100";
            MinPondingDepth = "0.00";
            MaxPondingDepth = "100";
            MinPrecipitationValue = "0";
            MaxPrecipitationValue = "150";
            MinEvapotranspirationValue = "0";
            MaxEvapotranspirationValue = "5";
            IsMeteoChecked = true;
            MaxMeteoIssueFiles = 10;
        }

        public override void LogSettings(Log log, int logIndentLevel = 0)
        {
            log.AddInfo("Minimum rootzone depth value: " + MinRTZDepth, logIndentLevel);
            log.AddInfo("Maximum rootzone depth value: " + MaxRTZDepth, logIndentLevel);
            log.AddInfo("Urban area name in LUSE table: " + LuseUBANames, logIndentLevel);
            log.AddInfo("Wetted area name in LUSE table " + LuseWTANames, logIndentLevel);
            log.AddInfo("Exclude DRN files substring: " + ExcludedDRNFileSubstrings, logIndentLevel);
            log.AddInfo("Minimum wetted area in relation to surface water packages: " + MinRIVResistance, logIndentLevel);
            log.AddInfo("Maximum wetted area in relation to surface water packages:" + MaxWTARIVPercentage, logIndentLevel);
            log.AddInfo("Minimum Ponding depth: " + MinPondingDepth, logIndentLevel);
            log.AddInfo("Maximum Ponding depth: " + MaxPondingDepth, logIndentLevel);
            log.AddInfo("Minimum precipitation: " + MinPrecipitationValue, logIndentLevel);
            log.AddInfo("Maximum precipitation: " + MaxPrecipitationValue, logIndentLevel);
            log.AddInfo("Minimum evapotranspiration: " + MinEvapotranspirationValue, logIndentLevel);
            log.AddInfo("Maximum evapotranspiration: " + MaxEvapotranspirationValue, logIndentLevel);

            if (IsMeteoChecked)
            {
                log.AddInfo("Meteo checks are switched on", logIndentLevel);
            }
            else
            {
                log.AddInfo("Meteo checks are switched of", logIndentLevel);
            }

        }
    }

    /// <summary>
    /// Main class for MetaSWAP-checks
    /// </summary>
    class MetaSWAPCheck : Check
    {
        public override string Abbreviation
        {
            get { return "MetaSWAP"; }
        }

        public override string Description
        {
            get { return "Checks MetaSWAP input"; }
        }

        protected MetaSWAPCheckSettings settings;
        public override CheckSettings Settings
        {
            get { return settings; }
            set
            {
                if (value is MetaSWAPCheckSettings)
                {
                    settings = (MetaSWAPCheckSettings)value;
                }
            }
        }

        public MetaSWAPCheck()
        {
            settings = new MetaSWAPCheckSettings(this.Name);
        }

        /// <summary>
        /// Start all MetaSWAP-checks for specified model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="resultHandler"></param>
        /// <param name="log"></param>
        public override void Run(Model model, CheckResultHandler resultHandler, Log log)
        {
            log.AddInfo("Checking CAP-package ...");

            CAPPackage capPackage = (CAPPackage)model.GetPackage(CAPPackage.DefaultKey);
            if ((capPackage == null) || !capPackage.IsActive)
            {
                log.AddWarning(this.Name, model.Runfilename, "CAP-package is not active. " + this.Name + " is skipped.", 1);
                return;
            }

            settings.LogSettings(log, 1);

            MetaSWAPLandUseCheck landuseSubCheck = new MetaSWAPLandUseCheck(this, settings);
            landuseSubCheck.Run(model, resultHandler, log);

            if (settings.IsMeteoChecked)
            {
                MetaSWAPMeteoCheck meteoCheck = new MetaSWAPMeteoCheck(this, settings);
                meteoCheck.Run(model, resultHandler, log);
            }
        }

        /// <summary>
        /// Create CheckErrorLayer with IDF-files as the underlying resultFile
        /// </summary>
        /// <param name="resultHandler"></param>
        /// <param name="capSubPackage"></param>
        /// <param name="kper"></param>
        /// <param name="entryIdx"></param>
        /// <param name="cellsize"></param>
        /// <param name="errorLegend"></param>
        /// <returns></returns>
        internal CheckErrorLayer CreateErrorLayer(CheckResultHandler resultHandler, CAPSubPackage capSubPackage, int kper, int entryIdx, float cellsize, IDFLegend errorLegend)
        {
            // Allow MetaSWAPSubChecks in this file to create error layers
            return base.CreateErrorLayer(resultHandler, capSubPackage, null, kper, entryIdx, cellsize, errorLegend);
        }

        /// <summary>
        /// Create CheckWarningLayer with IPF-files as the underlying resultFile
        /// </summary>
        /// <param name="resultHandler"></param>
        /// <param name="capSubPackage"></param>
        /// <param name="kper"></param>
        /// <param name="entryIdx"></param>
        /// <param name="cellsize"></param>
        /// <param name="warningLegend"></param>
        /// <returns></returns>
        internal CheckWarningLayer CreateWarningLayer(CheckResultHandler resultHandler, CAPSubPackage capSubPackage, int kper, int entryIdx, float cellsize, IDFLegend warningLegend)
        {
            // Allow MetaSWAPSubChecks in this file to create warning layers
            return base.CreateWarningLayer(resultHandler, capSubPackage, null, kper, entryIdx, cellsize, warningLegend);
        }
    }

    /// <summary>
    /// Class for Landuse subchecks of MetaSWAP
    /// </summary>
    internal class MetaSWAPLandUseCheck
    {
        private const string MergedSurfaceWaterPrefix = "MergedSWConductance";

        private MetaSWAPCheck check = null;
        private MetaSWAPCheckSettings settings = null;

        private IDFLegend rtzSFUErrorLegend = null;
        private IDFLegend luseErrorLegend = null;
        private IDFLegend pduPDRErrorLegend = null;

        private IDFLegend rtzSFUWarningLegend = null;
        private IDFLegend luseWarningLegend = null;
        private IDFLegend pduPDRWarningLegend = null;
        private int kper = 0;
        private int entryIdx = 0;

        private Log Log { get; set; }
        private CAPPackage capPackage = null;
        IDFCellIterator idfCellIterator = null;

        private CheckErrorLayer rtzSFUErrorLayer = null;
        private CheckErrorLayer luseErrorLayer = null;
        private CheckErrorLayer pduPDRErrorLayer = null;

        private CheckWarningLayer rtzSFUWarningLayer = null;
        private CheckWarningLayer luseWarningLayer = null;
        private CheckWarningLayer pduPDRWarningLayer = null;

        private float ubaCellArea = -1;         // area of UBA-cells (m2), which defines maximum UBA-values
        private float wtaCellArea = -1;         // area of UBA-cells (m2), which defines maximum WTA-values
        private List<int> ubaLUSECodes = null;  // LUSE-codes for UBA
        private List<int> wtaLUSECodes = null;  // LUSE-codes for WTA
        private int minSFUCode = -9999;         // Minimum SFU-code in MetaSWAP database
        private int maxSFUCode = -9999;         // Maximum SFU-code in MetaSWAP database

        Dictionary<string, int> luseCodeDictionary = null;

        // (Constant) IDF-files for settings
        private IDFFile MinRTZIDFFile { get; set; }
        private IDFFile MaxRTZIDFFile { get; set; }
        private IDFFile MinSurfaceWaterResistanceIDFFile { get; set; }
        private IDFFile MaxWTARIVPercIDFFile { get; set; }
        private IDFFile MinPondingDepthIDFFile { get; set; }
        private IDFFile MaxPondingDepthIDFFile { get; set; }

        // Landuse inputfiles
        private IDFFile rtzIDFFile = null;
        private IDFFile sfuIDFFile = null;
        private IDFFile luseIDFFile = null;
        private IDFFile ubaIDFFile = null;
        private IDFFile wtaIDFFile = null;
        private IDFFile pduIDFFile = null;
        private IDFFile pdrIDFFile = null;
        private IDFFile mergedSurfacewaterIDFFile = null;
        private List<IMODFile> mergedSurfacewaterIDFFiles = null;

        // errors
        private CheckError InvalidRTZError { get; set; }
        private CheckError InvalidSFUError { get; set; }
        private CheckError InvalidLUSEError { get; set; }
        private CheckError InvalidUBAError { get; set; }
        private CheckError InvalidWTAError { get; set; }
        private CheckError UBAWTAAreaError { get; set; }
        private CheckError InvalidPDUError { get; set; }
        private CheckError InvalidPDRError { get; set; }
        private CheckError PDUWTAMatchError { get; set; }
        private CheckError PDRWTAMatchError { get; set; }

        // warnings
        private CheckWarning RTZRangeWarning { get; set; }
        private CheckWarning LUSEUBAMatchWarning { get; set; }
        private CheckWarning RIVWTAMatchWarning { get; set; }
        private CheckWarning UBAWTAOverflowWarning { get; set; }
        private CheckWarning PDURangeWarning { get; set; }
        private CheckWarning PDRRangeWarning { get; set; }

        /// <summary>
        /// Store specified settings and initialize MetaSWAPCheck object
        /// </summary>
        /// <param name="check"></param>
        /// <param name="model"></param>
        /// <param name="resultHandler"></param>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        public MetaSWAPLandUseCheck(MetaSWAPCheck check, MetaSWAPCheckSettings settings)
        {
            this.check = check;
            this.settings = settings;

            Initialize();
        }

        /// <summary>
        /// Initialize MetaSWAPCheck object
        /// </summary>
        protected void Initialize()
        {
            // Define legends and results 
            rtzSFUErrorLegend = new IDFLegend("Legend for RTZ and SFU MetaSWAP-file check");
            luseErrorLegend = new IDFLegend("Legend for LUSE, UBA and WTA MetaSWAP-file check");
            pduPDRErrorLegend = new IDFLegend("Legend for PDU and PDR MetaSWAP-file check");

            rtzSFUWarningLegend = new IDFLegend("Legend for RTZ and SFU MetaSWAP-file check");
            luseWarningLegend = new IDFLegend("Legend for LUSE, UBA and WTA MetaSWAP-file check");
            pduPDRWarningLegend = new IDFLegend("Legend for PDU and PDR  MetaSWAP-file check");

            InvalidRTZError = new CheckError(1, "Invalid RTZ-value");
            InvalidSFUError = new CheckError(2, "Invalid SFU-value / para_sim mismatch");

            InvalidLUSEError = new CheckError(1, "Invalid LUSE-value / luse_svat mismatch");
            InvalidUBAError = new CheckError(2, "Invalid UBA-value");
            InvalidWTAError = new CheckError(4, "Invalid WTA-value");
            UBAWTAAreaError = new CheckError(8, "Invalid UBA+WTA area");

            InvalidPDUError = new CheckError(1, "Invalid PDU-value");
            InvalidPDRError = new CheckError(2, "Invalid PDR-value");
            PDUWTAMatchError = new CheckError(4, "PDU-WTA mismatch");
            PDRWTAMatchError = new CheckError(8, "PDR-WTA mismatch");

            // Define warnings
            RTZRangeWarning = new CheckWarning(1, "Rootzone depth outside the expected range [" + settings.MinRTZDepth.ToString() + "," + settings.MaxRTZDepth + "]");
            LUSEUBAMatchWarning = new CheckWarning(1, "LUSE-UBA mismatch");
            RIVWTAMatchWarning = new CheckWarning(2, "RIV-WTA mismatch");
            UBAWTAOverflowWarning = new CheckWarning(4, "UBA+WTA overflow");
            PDURangeWarning = new CheckWarning(1, "PDU outside expected range [" + settings.MinPondingDepth + "," + settings.MaxPondingDepth + "]");
            PDRRangeWarning = new CheckWarning(2, "PDR outside expected range [" + settings.MinPondingDepth + "," + settings.MaxPondingDepth + "]");

            // Create legends for errors
            rtzSFUErrorLegend.AddClass(new ValueLegendClass(0, "No errors found.", Color.White));
            rtzSFUErrorLegend.AddClass(InvalidRTZError.CreateLegendValueClass(Color.Yellow, true));
            rtzSFUErrorLegend.AddClass(InvalidSFUError.CreateLegendValueClass(Color.Red, true));
            rtzSFUErrorLegend.AddUpperRangeClass(Check.CombinedResultLabel, true);

            luseErrorLegend.AddClass(new ValueLegendClass(0, "No errors found.", Color.White));
            luseErrorLegend.AddClass(InvalidLUSEError.CreateLegendValueClass(Color.Orange, true));
            luseErrorLegend.AddClass(InvalidUBAError.CreateLegendValueClass(Color.Yellow, true));
            luseErrorLegend.AddClass(InvalidWTAError.CreateLegendValueClass(Color.Red, true));
            luseErrorLegend.AddClass(UBAWTAAreaError.CreateLegendValueClass(Color.Blue, true));
            luseErrorLegend.AddUpperRangeClass(Check.CombinedResultLabel, true);
            luseErrorLegend.AddInbetweenClasses(Check.CombinedResultLabel, true);

            pduPDRErrorLegend.AddClass(new ValueLegendClass(0, "No errors found.", Color.White));
            pduPDRErrorLegend.AddClass(InvalidPDUError.CreateLegendValueClass(Color.Orange, true));
            pduPDRErrorLegend.AddClass(InvalidPDRError.CreateLegendValueClass(Color.Yellow, true));
            pduPDRErrorLegend.AddClass(PDUWTAMatchError.CreateLegendValueClass(Color.Red, true));
            pduPDRErrorLegend.AddClass(PDRWTAMatchError.CreateLegendValueClass(Color.Blue, true));
            pduPDRErrorLegend.AddUpperRangeClass(Check.CombinedResultLabel, true);
            pduPDRErrorLegend.AddInbetweenClasses(Check.CombinedResultLabel, true);

            // Create legends for warnings
            rtzSFUWarningLegend.AddClass(new ValueLegendClass(0, "No errors found", Color.White));
            rtzSFUWarningLegend.AddClass(RTZRangeWarning.CreateLegendValueClass(Color.Red, true));

            luseWarningLegend.AddClass(new ValueLegendClass(0, "No errors found", Color.White));
            luseWarningLegend.AddClass(LUSEUBAMatchWarning.CreateLegendValueClass(Color.Orange, true));
            luseWarningLegend.AddClass(RIVWTAMatchWarning.CreateLegendValueClass(Color.Blue, true));
            luseWarningLegend.AddClass(UBAWTAOverflowWarning.CreateLegendValueClass(Color.Red, true));
            luseWarningLegend.AddUpperRangeClass(Check.CombinedResultLabel, true);
            luseWarningLegend.AddInbetweenClasses(Check.CombinedResultLabel, true);

            pduPDRWarningLegend.AddClass(new ValueLegendClass(0, "No errors found", Color.White));
            pduPDRWarningLegend.AddClass(PDURangeWarning.CreateLegendValueClass(Color.Orange, true));
            pduPDRWarningLegend.AddClass(PDRRangeWarning.CreateLegendValueClass(Color.Red, true));
            pduPDRWarningLegend.AddUpperRangeClass(Check.CombinedResultLabel, true);
            pduPDRWarningLegend.AddInbetweenClasses(Check.CombinedResultLabel, true);
        }

        /// <summary>
        /// Checks for MetaSWAP LandUse input (LUSE, RTZ, SFU, UBA, WTA, PDU, PDR)
        /// </summary>
        /// <param name="model"></param>
        /// <param name="resultHandler"></param>
        /// <param name="log"></param>
        public void Run(Model model, CheckResultHandler resultHandler, Log log)
        {
            this.Log = log;
            log.AddInfo("Starting MetaSWAP landuse checks ...", 1);

            // Check if packages exist
            RetrievePackages(model, log);

            // Retrieve settingfiles 
            RetrieveSettingsFiles(log);

            // Retrieve landuse input files
            log.AddInfo("Retrieving landuse input files ...", 2);
            RetrieveInputFiles();

            // Get Soil physical unit codes
            log.AddInfo("Retrieving soil physical unit (SFU) codes from MetaSWAP-DB via para_sim.inp ...", 2);
            List<int> sfuCodes = GetSFUCodes();
            if ((sfuCodes == null) || (sfuCodes.Count == 0))
            {
                // SFU-codes not found, some error occurred, skip test
                log.AddError(check.Name, "para_sim.inp", "No SFU-codes found, MetaSWAP-DB may be missing. SFU-checks are skipped.", 3);

                minSFUCode = -1;
                maxSFUCode = -1;
            }
            else
            {
                minSFUCode = sfuCodes.Min();
                maxSFUCode = sfuCodes.Max();
            }

            // Get Landuse codes
            log.AddInfo("Retrieving land use codes from luse_svat.inp ...", 2);
            luseCodeDictionary = GetLUSECodes();
            ubaLUSECodes = GetUBALUSECodes(luseCodeDictionary);
            wtaLUSECodes = GetWTALUSECodes(luseCodeDictionary);
            log.AddInfo("Land use codes for UBA: " + CommonUtils.ToString(ubaLUSECodes, SIFTool.EnglishCultureInfo), 3);
            log.AddInfo("Land use codes for WTA: " + CommonUtils.ToString(wtaLUSECodes, SIFTool.EnglishCultureInfo), 3);

            // Check that UBA/WTA-file is not a constant file. This doesn't make sense for area
            if (ubaIDFFile is ConstantIDFFile)
            {
                log.AddWarning(check.Name, Path.GetFileName(ubaIDFFile.Filename), "UBA is a constant IDF-file, cellsize 100 is assumed; UBA-checks may give invalid results", 3);
            }
            if (wtaIDFFile is ConstantIDFFile)
            {
                log.AddWarning(check.Name, Path.GetFileName(wtaIDFFile.Filename), "WTA is a constant IDF-file, cellsize 100 is assumed; WTA-checks may give invalid results", 3);
            }

            ubaCellArea = ubaIDFFile.XCellsize * ubaIDFFile.YCellsize;
            wtaCellArea = wtaIDFFile.XCellsize * wtaIDFFile.YCellsize;

            // Combine surface water area
            log.AddInfo("Combining surface water input files ...", 2);
            MergeSurfaceWater(model, resultHandler, (wtaIDFFile is ConstantIDFFile) ? 100 : wtaIDFFile.XCellsize, settings, log, 3);

            // Initialize cell iterator
            InitializeIDFCellIterator(resultHandler.Extent, log);

            // Create errors IDFfiles for current layer                 
            //errorLayer = this.check.CreateErrorLayer(resultHandler, capPackage, kper, entryIdx, idfCellIterator.XStepsize, errorLegend);
            CAPSubPackage rtzSFUSubPackage = CAPSubPackage.CreateInstance(capPackage.Key, "RTZ-SFU");
            CAPSubPackage luseSubPackage = CAPSubPackage.CreateInstance(capPackage.Key, "LUSE");
            CAPSubPackage pduPDRSubPackage = CAPSubPackage.CreateInstance(capPackage.Key, "PDU-PDR");

            // Create error and warning IDF-files for current layer     
            rtzSFUErrorLayer = check.CreateErrorLayer(resultHandler, rtzSFUSubPackage, kper, entryIdx, idfCellIterator.XStepsize, rtzSFUErrorLegend);
            luseErrorLayer = check.CreateErrorLayer(resultHandler, luseSubPackage, kper, entryIdx, idfCellIterator.XStepsize, luseErrorLegend);
            pduPDRErrorLayer = check.CreateErrorLayer(resultHandler, pduPDRSubPackage, kper, entryIdx, idfCellIterator.XStepsize, pduPDRErrorLegend);

            rtzSFUWarningLayer = check.CreateWarningLayer(resultHandler, rtzSFUSubPackage, kper, entryIdx, idfCellIterator.XStepsize, rtzSFUWarningLegend);
            luseWarningLayer = check.CreateWarningLayer(resultHandler, luseSubPackage, kper, entryIdx, idfCellIterator.XStepsize, luseWarningLegend);
            pduPDRWarningLayer = check.CreateWarningLayer(resultHandler, pduPDRSubPackage, kper, entryIdx, idfCellIterator.XStepsize, pduPDRWarningLegend);

            while (idfCellIterator.IsInsideExtent())
            {
                float luseValue = idfCellIterator.GetCellValue(luseIDFFile);
                float wtaValue = idfCellIterator.GetCellValue(wtaIDFFile);
                float ubaValue = idfCellIterator.GetCellValue(ubaIDFFile);

                float x = idfCellIterator.X;
                float y = idfCellIterator.Y;

                // Retrieve urban area and wetted area percentage in current cell
                float ubaPercentage = (ubaValue / ubaCellArea) * 100;
                float wtaPercentage = (wtaValue / wtaCellArea) * 100;

                // Check for invalid rootzone depth
                CheckRTZ(x, y, resultHandler);

                // Check for invalid soil physical unit code values
                CheckSFU(x, y, resultHandler);

                // Check for invalid land use grid
                CheckLUSE(x, y, luseValue, resultHandler);

                CheckUBAWTA(x, y, ubaPercentage, wtaPercentage, resultHandler);

                // Check for invalid urban area grid, and not matching with LUSE                                                                                          
                CheckUBAvsLUSE(x, y, ubaValue, ubaPercentage, luseValue, resultHandler);

                // Check for invalid wetted area grid, and not matching with LUSE
                CheckWTAvsUBA(x, y, wtaValue, wtaPercentage, ubaPercentage, resultHandler);

                // Check match between WTA (wetted area) and surface water packages (RIV, DRN, todo: ISG)
                CheckWTAvsRIV(x, y, wtaValue, wtaPercentage, resultHandler);

                // Check WTA (wetted area) in relation to PDR and PDU
                CheckPDUPDR(x, y, resultHandler);

                idfCellIterator.MoveNext();
            }

            // Write combined surface water IDF-file when related error/warning message exists
            List<int> surfacewaterErrorCodeList = new List<int>(); // Note: currently no error exist that indicate mismatch
            List<int> surfacewaterWarningCodeList = new List<int>();
            surfacewaterWarningCodeList.Add(RIVWTAMatchWarning.ResultValue);

            // Write MetaSWAP error files
            if (rtzSFUErrorLayer.HasResults())
            {
                rtzSFUErrorLayer.CompressLegend(Check.CombinedResultLabel);
                rtzSFUErrorLayer.WriteResultFile(log);
            }
            if (luseErrorLayer.HasResults())
            {
                if (luseErrorLayer.ContainsResult(surfacewaterErrorCodeList) || luseWarningLayer.ContainsResult(surfacewaterWarningCodeList))
                {
                    resultHandler.AddExtraMapFile(wtaIDFFile);
                    resultHandler.AddExtraMapFile(mergedSurfacewaterIDFFile);
                    resultHandler.AddExtraMapFiles(mergedSurfacewaterIDFFiles);
                }

                luseErrorLayer.CompressLegend(Check.CombinedResultLabel);
                luseErrorLayer.WriteResultFile(log);
            }
            if (pduPDRErrorLayer.HasResults())
            {
                pduPDRErrorLayer.CompressLegend(Check.CombinedResultLabel);
                pduPDRErrorLayer.WriteResultFile(log);
            }

            // Write MetaSWAP warning files
            if (rtzSFUWarningLayer.HasResults())
            {
                rtzSFUWarningLayer.CompressLegend(Check.CombinedResultLabel);
                rtzSFUWarningLayer.WriteResultFile(log);
            }
            if (luseWarningLayer.HasResults())
            {
                luseWarningLayer.CompressLegend(Check.CombinedResultLabel);
                luseWarningLayer.WriteResultFile(log);
            }
            if (pduPDRWarningLayer.HasResults())
            {
                pduPDRWarningLayer.CompressLegend(Check.CombinedResultLabel);
                pduPDRWarningLayer.WriteResultFile(log);
            }
        }

        private void CheckRTZ(float x, float y, CheckResultHandler resultHandler)
        {
            if (rtzIDFFile != null)
            {
                float rtzValue = idfCellIterator.GetCellValue(rtzIDFFile);
                float minRTZValue = idfCellIterator.GetCellValue(MinRTZIDFFile);
                float maxRTZValue = idfCellIterator.GetCellValue(MaxRTZIDFFile);

                if (rtzValue.Equals(rtzIDFFile.NoDataValue) || rtzValue < 0)
                {
                    // Rootzone is either NoData or negative, both are not allowed
                    resultHandler.AddCheckResult(rtzSFUErrorLayer, x, y, InvalidRTZError);
                    resultHandler.AddExtraMapFile(rtzIDFFile);
                }
                else if (rtzValue < minRTZValue || rtzValue > maxRTZValue)
                {
                    resultHandler.AddCheckResult(rtzSFUWarningLayer, x, y, RTZRangeWarning);
                    resultHandler.AddExtraMapFile(rtzIDFFile);
                }
            }
        }

        private void CheckSFU(float x, float y, CheckResultHandler resultHandler)
        {
            if (sfuIDFFile != null)
            {
                float sfuValue = idfCellIterator.GetCellValue(sfuIDFFile);
                if (sfuValue.Equals(sfuIDFFile.NoDataValue) || (sfuValue < 0) || !int.TryParse(sfuValue.ToString(), out int sfuIntCode))
                {
                    // Rootzone is either NoData, negative or not an integer; all are never allowed
                    resultHandler.AddCheckResult(rtzSFUErrorLayer, x, y, InvalidSFUError);
                    resultHandler.AddExtraMapFile(sfuIDFFile);
                }
                else if (minSFUCode > sfuIntCode || maxSFUCode < sfuIntCode)
                {
                    // Value does not correspond with the min/max-values that have been retrieved from the MetaSWAP-database
                    resultHandler.AddCheckResult(rtzSFUErrorLayer, x, y, InvalidSFUError);
                    resultHandler.AddExtraMapFile(sfuIDFFile);
                }
            }
        }

        private void CheckLUSE(float x, float y, float luseValue, CheckResultHandler resultHandler)
        {
            if (luseValue.Equals(luseIDFFile.NoDataValue) || (luseValue < 0) || !int.TryParse(luseValue.ToString(), out int luseIntCode))
            {
                // Rootzone is either NoData, negative or not an integer; all are never allowed
                resultHandler.AddCheckResult(luseErrorLayer, x, y, InvalidLUSEError);
                resultHandler.AddExtraMapFile(luseIDFFile);
            }
            else if (!luseCodeDictionary.ContainsValue(luseIntCode))
            {
                // Value does not correspond with LUSE-values from luse_svat.inp
                resultHandler.AddCheckResult(luseErrorLayer, x, y, InvalidLUSEError);
                resultHandler.AddExtraMapFile(luseIDFFile);
            }
        }

        private void CheckUBAWTA(float x, float y, float ubaPercentage, float wtaPercentage, CheckResultHandler resultHandler)
        {
            float summedPercentage = (float)Math.Round((ubaPercentage + wtaPercentage), 2);
            if (summedPercentage > 100)
            {
                resultHandler.AddCheckResult(luseErrorLayer, x, y, UBAWTAAreaError);
                resultHandler.AddExtraMapFile(ubaIDFFile);
                resultHandler.AddExtraMapFile(wtaIDFFile);
            }
        }

        private void CheckUBAvsLUSE(float x, float y, float ubaValue, float ubaPercentage, float luseValue, CheckResultHandler resultHandler)
        {
            if (ubaValue.Equals(ubaIDFFile.NoDataValue) || (ubaValue < 0) || (ubaPercentage > 100))
            {
                resultHandler.AddCheckResult(luseErrorLayer, x, y, InvalidUBAError);
                resultHandler.AddExtraMapFile(ubaIDFFile);
            }
            else if (ubaLUSECodes.Contains((int)luseValue))
            {
                // An urban LUSE-code is found for current cell. Check that UBA percentage is 100%.
                // LUSE-code should be for the remaining part of the cell that is not paved.
                if (!ubaPercentage.Equals(100))
                {
                    resultHandler.AddCheckResult(luseWarningLayer, x, y, LUSEUBAMatchWarning);
                    resultHandler.AddExtraMapFile(ubaIDFFile);
                    resultHandler.AddExtraMapFile(luseIDFFile);
                }
            }
        }

        private void CheckWTAvsUBA(float x, float y, float wtaValue, float wtaPercentage, float ubaPercentage, CheckResultHandler resultHandler)
        {
            if (wtaValue.Equals(wtaIDFFile.NoDataValue) || (wtaValue < 0) || (wtaPercentage > 100))
            {
                // WTA area is not allowed to be NoData for MetaSWAP, also and negative area or area larger than cellsize are invalid
                resultHandler.AddCheckResult(luseErrorLayer, x, y, InvalidWTAError);
                resultHandler.AddExtraMapFile(wtaIDFFile);
            }
            else
            {
                double summedPercentage = Math.Round(wtaPercentage + ubaPercentage, 1);
                if (summedPercentage > 100)
                {
                    // Sum of WTA and UBA should never be larger than cell area
                    resultHandler.AddCheckResult(luseWarningLayer, x, y, UBAWTAOverflowWarning);
                    resultHandler.AddExtraMapFile(wtaIDFFile);
                    resultHandler.AddExtraMapFile(ubaIDFFile);
                }
            }
        }

        private void CheckWTAvsRIV(float x, float y, float wtaValue, float wtaPercentageValue, CheckResultHandler resultHandler)
        {
            if (mergedSurfacewaterIDFFile != null)
            {
                float minResistance = idfCellIterator.GetCellValue(MinSurfaceWaterResistanceIDFFile);
                float mergedSurfaceWaterConductance = idfCellIterator.GetCellValue(mergedSurfacewaterIDFFile);
                float maxWTARIVPercentage = idfCellIterator.GetCellValue(MaxWTARIVPercIDFFile);
                float minWTAValue = mergedSurfaceWaterConductance * minResistance;

                bool hasSurfacewater = !mergedSurfaceWaterConductance.Equals(mergedSurfacewaterIDFFile.NoDataValue) && (mergedSurfaceWaterConductance > 0);
                if (hasSurfacewater)
                {
                    if (wtaValue < minWTAValue)
                    {
                        resultHandler.AddCheckResult(luseWarningLayer, x, y, RIVWTAMatchWarning);
                        resultHandler.AddExtraMapFile(wtaIDFFile);
                        resultHandler.AddExtraMapFile(mergedSurfacewaterIDFFile);
                        resultHandler.AddExtraMapFiles(mergedSurfacewaterIDFFiles);
                    }
                }
                else
                {
                    // Note: it is valid to have WTA > 0 and RIV/DRN/ISG-area = 0. So an error can not be raised here.
                    // E.g. isolated surface water may not drain/infiltrate and will not be in RIV/DRN/ISG-packages, but can evaporate

                    // If it is known that no isolated surface water is present in the model, maxWTARIVPercentage can be used to define warning level
                    if (wtaPercentageValue > maxWTARIVPercentage)
                    {
                        resultHandler.AddCheckResult(luseWarningLayer, x, y, RIVWTAMatchWarning);
                        resultHandler.AddExtraMapFile(wtaIDFFile);
                        resultHandler.AddExtraMapFile(MaxWTARIVPercIDFFile);
                    }
                }
            }
        }

        private void CheckPDUPDR(float x, float y, CheckResultHandler resultHandler)
        {
            float pduValue = idfCellIterator.GetCellValue(pduIDFFile);
            float pdrValue = idfCellIterator.GetCellValue(pdrIDFFile);
            float minPondingDepthValue = idfCellIterator.GetCellValue(MinPondingDepthIDFFile);
            float maxPondingDepthValue = idfCellIterator.GetCellValue(MaxPondingDepthIDFFile);

            if (pduValue < 0)
            {
                resultHandler.AddCheckResult(pduPDRErrorLayer, x, y, InvalidPDUError);
                resultHandler.AddExtraMapFile(pduIDFFile);
            }
            else if ((pduValue < minPondingDepthValue) || (pduValue > maxPondingDepthValue))
            {
                resultHandler.AddCheckResult(pduPDRWarningLayer, x, y, PDURangeWarning);
                resultHandler.AddExtraMapFile(pduIDFFile);
            }

            if (pdrValue < 0)
            {
                resultHandler.AddCheckResult(pduPDRErrorLayer, x, y, InvalidPDRError);
                resultHandler.AddExtraMapFile(pdrIDFFile);
            }
            else if ((pdrValue < minPondingDepthValue) || (pdrValue > minPondingDepthValue))
            {
                resultHandler.AddCheckResult(pduPDRWarningLayer, x, y, PDRRangeWarning);
                resultHandler.AddExtraMapFile(pdrIDFFile);
            }
        }

        private void MergeSurfaceWater(Model model, CheckResultHandler resultHandler, float cellsize, MetaSWAPCheckSettings settings, Log log, int logIndentLevel)
        {
            mergedSurfacewaterIDFFile = null;
            mergedSurfacewaterIDFFiles = new List<IMODFile>();

            IDFPackage rivPackage = (IDFPackage)model.GetPackage(RIVPackage.DefaultKey);
            IDFPackage drnPackage = (IDFPackage)model.GetPackage(DRNPackage.DefaultKey);
            if (rivPackage != null)
            {
                log.AddInfo("Merging surface water from RIV-package ...", logIndentLevel);
                MergeSurfaceWaterCells(ref mergedSurfacewaterIDFFile, mergedSurfacewaterIDFFiles, rivPackage, model, resultHandler, cellsize, null, RIVPackage.ConductancePartIdx, log, logIndentLevel + 1);
            }

            if (drnPackage != null)
            {
                MergeSurfaceWaterCells(ref mergedSurfacewaterIDFFile, mergedSurfacewaterIDFFiles, drnPackage, model, resultHandler, cellsize, settings.ExcludedDRNFileSubstrings, DRNPackage.ConductancePartIdx, log, logIndentLevel + 1);
                log.AddInfo("Merging surface water from DRN-package ...", logIndentLevel);
            }

            if (settings.IsISGConverted)
            {
                ISGPackage isgPackage = (ISGPackage)model.GetPackage(ISGPackage.DefaultKey);
                if (isgPackage != null)
                {
                    log.AddInfo("Merging surface water from (converted) ISG-package ...", logIndentLevel);
                    IDFPackage isgRIVPackage = ISGRIVConverter.ConvertISGtoRIVPackage(isgPackage, "ISGRIV", model, resultHandler, log, logIndentLevel + 1);

                    MergeSurfaceWaterCells(ref mergedSurfacewaterIDFFile, mergedSurfacewaterIDFFiles, isgRIVPackage, model, resultHandler, cellsize, null, RIVPackage.ConductancePartIdx, log, logIndentLevel + 1);
                }
            }

            if (mergedSurfacewaterIDFFile != null)
            {
                mergedSurfacewaterIDFFile.WriteFile(mergedSurfacewaterIDFFile.Filename);
            }
        }

        /// <summary>
        /// Add all surfacewater cells from specified package to given IDF-file. Exclude filenames that contain one of the specified substrings.
        /// Note: cell that contains a surface water cell will be set to value 1, cells without surface water are set to NoData.
        /// </summary>
        /// <param name="mergedSurfacewaterIDFFile">updated IDF-file with all surface water</param>
        /// <param name="sourceIDFFiles">list of IDF-files that is updated with surface water IDf-file of this merge operation</param>
        /// <param name="packageKey"></param>
        /// <param name="mergedPackage"></param>
        /// <param name="model"></param>
        /// <param name="resultHandler"></param>
        /// <param name="cellsize"></param>
        /// <param name="excludedFileSubstringsString">semicolon-seperated substrings to exclude from merge</param>
        /// <param name="partIdx"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"
        private void MergeSurfaceWaterCells(ref IDFFile mergedSurfacewaterIDFFile, List<IMODFile> sourceIDFFiles, IDFPackage mergedPackage, Model model, CheckResultHandler resultHandler, float cellsize, string excludedFileSubstringsString, int partIdx, Log log, int logIndentLevel)
        {
            if (mergedPackage == null)
            {
                return;
            }

            if (!mergedPackage.IsActive)
            {
                log.AddInfo(mergedPackage.Key.ToString() + "-package is not active. " + check.Name + " is skipped.", logIndentLevel);
                return;
            }

            // For each entry in this package search timesteps for first IDF-file (TODO: add option to merge non-NoData-cells of IDF-files in all timesteps)
            string[] excludeSubstrings = (excludedFileSubstringsString != null) ? excludedFileSubstringsString.Split(';') : null;
            for (int entryIdx = 0; entryIdx <= mergedPackage.GetEntryCount(); entryIdx++)
            {
                // Search for first existing surfacewater file in all timesteps
                int kperIdx = 0;
                IDFFile entryIDFFile = null;
                while ((entryIDFFile == null) && (kperIdx <= model.NPER) && (kperIdx <= resultHandler.MaxKPER))
                {
                    entryIDFFile = mergedPackage.GetIDFFile(entryIdx, partIdx, kperIdx);
                    kperIdx++;
                }

                if (entryIDFFile != null)
                {
                    if (!sourceIDFFiles.Contains(entryIDFFile))
                    {
                        sourceIDFFiles.Add(entryIDFFile);
                    }

                    if (!ContainsSubstring(Path.GetFileName(entryIDFFile.Filename), excludeSubstrings))
                    {
                        if (mergedSurfacewaterIDFFile == null)
                        {
                            // Create empty merged IDF-file from first entry IDF-file
                            // Initialize IDF-file with merged surface water: ensure cellsize is equal to specified cellsize (which should come from the WTA IDF-file)
                            string mergedFilename = Path.Combine(resultHandler.OutputPath, "checks-imodfiles", check.Name, MergedSurfaceWaterPrefix + cellsize + ".IDF");
                            mergedSurfacewaterIDFFile = new IDFFile(mergedFilename, wtaIDFFile.Extent, cellsize, wtaIDFFile.NoDataValue);
                        }

                        // Ensure extents match
                        if (!mergedSurfacewaterIDFFile.Extent.Contains(entryIDFFile.Extent))
                        {
                            mergedSurfacewaterIDFFile = mergedSurfacewaterIDFFile.EnlargeIDF(entryIDFFile.Extent);
                        }

                        if (!entryIDFFile.Extent.Equals(mergedSurfacewaterIDFFile.Extent))
                        {
                            entryIDFFile = entryIDFFile.EnlargeIDF(mergedSurfacewaterIDFFile.Extent);
                        }

                        // Ensure cellsize matches with specified cellsize (WTA-cellsize)
                        if (entryIDFFile.XCellsize > cellsize)
                        {
                            entryIDFFile = entryIDFFile.ScaleDown(cellsize, DownscaleMethodEnum.Divide);
                        }
                        else if (entryIDFFile.XCellsize < cellsize)
                        {
                            entryIDFFile = entryIDFFile.ScaleUp(cellsize, UpscaleMethodEnum.Sum);
                        }

                        // Add conductance of entry IDF-file to merged file. Use value 0 for NoData when adding values from entry IDF-file
                        // Preserve current filename (which will be changed because of the added IDF-file
                        mergedSurfacewaterIDFFile.NoDataCalculationValue = 0;
                        entryIDFFile.NoDataCalculationValue = 0;
                        string filename = mergedSurfacewaterIDFFile.Filename;
                        mergedSurfacewaterIDFFile += entryIDFFile;
                        // mergedSurfacewaterIDFFile.WriteFile(@"C:\Data\Tools\TmpSIFToolsKJ\SIF-tools\iMODValidator\Test\Deelmodel-TestKvdH-TKI1\Model\RUNFILES\BASIS1_TA-RUN\MERGEDSW.IDF");
                        mergedSurfacewaterIDFFile.Filename = filename;
                    }
                }
                else
                {
                    log.AddWarning(check.Name, mergedPackage.Key, mergedPackage.Key + "-package does not contain any IDF-files and is skipped for " + check.Name, 3);
                }
            }
        }

        /// <summary>
        /// Checks if given string contains one of the specified substrings
        /// </summary>
        /// <param name="someString"></param>
        /// <param name="subStrings"></param>
        /// <returns></returns>
        private bool ContainsSubstring(string someString, string[] subStrings)
        {
            if ((someString != null) && (subStrings != null))
            {
                for (int idx = 0; idx < subStrings.Length; idx++)
                {
                    if (someString.Contains(subStrings[idx]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void RetrievePackages(Model model, Log log)
        {
            capPackage = (CAPPackage)model.GetPackage(CAPPackage.DefaultKey);

            IDFPackage rivPackage = (IDFPackage)model.GetPackage(RIVPackage.DefaultKey);
            if (rivPackage == null || !rivPackage.IsActive)
            {
                log.AddWarning(check.Name, model.Runfilename, "RIV-package is not active. " + check.Name + " is skipped.", 1);
                return;
            }

            IDFPackage drnPackage = (IDFPackage)model.GetPackage(DRNPackage.DefaultKey);
            if (drnPackage == null || !drnPackage.IsActive)
            {
                log.AddWarning(check.Name, model.Runfilename, "DRN-package is not active. " + check.Name + " is skipped.", 1);
                return;
            }
        }

        private void RetrieveSettingsFiles(Log log)
        {
            MinRTZIDFFile = settings.GetIDFFile(settings.MinRTZDepth, log, 1);
            MaxRTZIDFFile = settings.GetIDFFile(settings.MaxRTZDepth, log, 1);

            MinSurfaceWaterResistanceIDFFile = settings.GetIDFFile(settings.MinRIVResistance.ToString(), log, 1);
            MaxWTARIVPercIDFFile = settings.GetIDFFile(settings.MaxWTARIVPercentage.ToString(), log, 1);

            MinPondingDepthIDFFile = settings.GetIDFFile(settings.MinPondingDepth.ToString(), log, 1);
            MaxPondingDepthIDFFile = settings.GetIDFFile(settings.MaxPondingDepth.ToString(), log, 1);
        }

        private void RetrieveInputFiles()
        {
            rtzIDFFile = (IDFFile)capPackage.GetIMODFile(CAPPackage.GetEntryIdx(CAPEntryCode.RTZ));
            sfuIDFFile = (IDFFile)capPackage.GetIMODFile(CAPPackage.GetEntryIdx(CAPEntryCode.SFU));
            luseIDFFile = (IDFFile)capPackage.GetIMODFile(CAPPackage.GetEntryIdx(CAPEntryCode.LUSE));
            ubaIDFFile = (IDFFile)capPackage.GetIMODFile(CAPPackage.GetEntryIdx(CAPEntryCode.UBA));
            wtaIDFFile = (IDFFile)capPackage.GetIMODFile(CAPPackage.GetEntryIdx(CAPEntryCode.WTA));
            pduIDFFile = (IDFFile)capPackage.GetIMODFile(CAPPackage.GetEntryIdx(CAPEntryCode.PDU));
            pdrIDFFile = (IDFFile)capPackage.GetIMODFile(CAPPackage.GetEntryIdx(CAPEntryCode.PDR));
        }

        /// <summary>
        /// Retrieve SFU-codes (as a list of integers) from para_sim.inp
        /// </summary>
        /// <returns></returns>
        private List<int> GetSFUCodes()
        {
            string parasimFilename = capPackage.GetExtraFilename("para_sim.inp");
            List<int> sfuCodes = new List<int>();
            string[] sfuDirectories = null;
            string dataBaseSFUDir = null;

            try
            {
                StreamReader sr = new StreamReader(parasimFilename);

                bool endOfStream = true;
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    if (line.Contains("unsa_svat_path"))
                    {
                        int startIdx = line.IndexOf('=') + 1;
                        int endIdx = line.IndexOf('!');
                        string substring = line.Substring(startIdx, endIdx - startIdx - 1).Replace('"', ' ');
                        dataBaseSFUDir = substring.Trim();
                        //string test4 = line.
                        endOfStream = false;
                        break;
                    }
                }
                if (endOfStream)
                {
                    Log.AddError(check.Name, "para_sim.inp", "Reference to unsa_svat_path is missing in para_sim.inp", 3);
                    return sfuCodes;
                }
                sr.Close();
            }
            catch (ToolException ex)
            {
                throw new Exception("Error while reading para_sim.inp " + parasimFilename, ex);
            }

            try
            {
                sfuDirectories = Directory.GetDirectories(dataBaseSFUDir);
            }
            catch
            {
                Log.AddError(check.Name, "para_sim.inp", "unsa_svat_path in para_sim.inp does not exist: " + dataBaseSFUDir, 3);
                return sfuCodes;
            }

            foreach (string sfuDirectory in sfuDirectories)
            {
                int sfuCode = -9999;
                string subdir = Path.GetFileName(sfuDirectory);
                if (int.TryParse(subdir, out sfuCode))
                {
                    sfuCodes.Add(sfuCode);
                }
            }
            return sfuCodes;
        }

        /// <summary>
        /// Retrieve LUSE-codes (as a name-integer dictionary) from luse_svat.inp
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, int> GetLUSECodes()
        {
            string luseFilename = capPackage.GetExtraFilename("luse_svat.inp");
            Dictionary<string, int> luseCodeDictionary = new Dictionary<string, int>();
            try
            {
                StreamReader sr = new StreamReader(luseFilename);

                int lineCount = 1;
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    if (int.TryParse(line.Substring(0, 6).Trim(), out int luseCode))
                    {
                        string luseDescription = line.Substring(7, 19).Trim();
                        luseCodeDictionary.Add(luseDescription, luseCode);
                    }
                    else
                    {
                        Log.AddError(check.Name, "para_sim.inp", "Land use code in luse_svat.inp in line " + lineCount.ToString() + "is not an integer: " + line.Substring(26, 6).Trim(), 3);
                        return luseCodeDictionary;
                    }
                    lineCount++;
                }
                sr.Close();
            }
            catch (ToolException ex)
            {
                throw new Exception("Error while reading luse_svat.inp " + luseFilename, ex);
            }
            return luseCodeDictionary;
        }

        /// <summary>
        /// Retrieve UBA-codes from LUSE-table luse_svat.inp
        /// </summary>
        /// <param name="luseCodeDictionary"></param>
        /// <returns></returns>
        private List<int> GetUBALUSECodes(Dictionary<string, int> luseCodeDictionary)
        {
            List<int> urbanLUSECodes = new List<int>();
            string[] urbanAreaNames = settings.LuseUBANames.Split(';');
            foreach (string urbanAreaName in urbanAreaNames)
            {
                if (luseCodeDictionary.ContainsKey(urbanAreaName.Trim()))
                {
                    urbanLUSECodes.Add(luseCodeDictionary[urbanAreaName.Trim()]);
                }
                else
                {
                    throw new Exception("Name for specified urban LUSE-entry does not exist in luse_svat.inp for the name: " + urbanAreaName);
                }
            }
            return urbanLUSECodes;
        }

        /// <summary>
        /// Retrieve WTA-codes from LUSE-table luse_svat.inp
        /// </summary>
        /// <param name="luseCodeDictionary"></param>
        /// <returns></returns>
        private List<int> GetWTALUSECodes(Dictionary<string, int> luseCodeDictionary)
        {
            List<int> wettedLUSECodes = new List<int>();
            string[] wettedAreaNames = settings.LuseWTANames.Split(';');
            foreach (string wettedAreaName in wettedAreaNames)
            {
                if (luseCodeDictionary.ContainsKey(wettedAreaName.Trim()))
                {
                    wettedLUSECodes.Add(luseCodeDictionary[wettedAreaName.Trim()]);
                }
                else
                {
                    throw new Exception("Wetted area does not exist in luse_svat.inp for the name: " + wettedAreaName);
                }
            }
            return wettedLUSECodes;
        }

        private void InitializeIDFCellIterator(Extent extent, Log log)
        {
            idfCellIterator = new IDFCellIterator(extent);

            // Grid runfile input
            idfCellIterator.AddIDFFile(rtzIDFFile);
            idfCellIterator.AddIDFFile(sfuIDFFile);

            idfCellIterator.AddIDFFile(luseIDFFile);
            idfCellIterator.AddIDFFile(ubaIDFFile);
            idfCellIterator.AddIDFFile(wtaIDFFile);
            idfCellIterator.AddIDFFile(mergedSurfacewaterIDFFile);

            // Settings input
            idfCellIterator.AddIDFFile(MinRTZIDFFile);
            idfCellIterator.AddIDFFile(MaxRTZIDFFile);

            idfCellIterator.AddIDFFile(MinSurfaceWaterResistanceIDFFile);
            idfCellIterator.AddIDFFile(MaxWTARIVPercIDFFile);

            idfCellIterator.AddIDFFile(MinPondingDepthIDFFile);
            idfCellIterator.AddIDFFile(MaxPondingDepthIDFFile);
            idfCellIterator.CheckExtent(log, 2, LogLevel.Warning);
            idfCellIterator.Reset();
        }
    }

    internal class MetaSWAPMeteoCheck
    {
        private MetaSWAPCheck check = null;
        private MetaSWAPCheckSettings settings = null;

        private IDFLegend errorLegend = new IDFLegend("Legend for Meteo input MetaSWAP-file check");
        private IDFLegend warningLegend = new IDFLegend("Legend for Meteo input MetaSWAP-file check");
        private int kper = 0;
        private int entryIdx = 0;

        private Log Log { get; set; }
        private CheckErrorLayer errorLayer = null;
        private CheckWarningLayer warningLayer = null;
        private CAPPackage capPackage = null;
        private IDFCellIterator idfCellIterator = null;

        // meteo inputfiles
        private IDFFile precipitationIDFFile = null;
        private IDFFile evapotranspirationIDFFile = null;

        // errors
        private CheckError InvalidPrecipitationError { get; set; }
        private CheckError InvalidEvapotranspirationError { get; set; }

        // warnings
        private CheckWarning PrecipitationRangeWarning { get; set; }
        private CheckWarning EvapotranspirationRangeWarning { get; set; }

        // settings files
        private IDFFile minPrecipitationSettingsIDFFile { get; set; }
        private IDFFile maxPrecipitationSettingsIDFFile { get; set; }
        private IDFFile minEvapotranspirationSettingsIDFFile { get; set; }
        private IDFFile maxEvapotranspirationSettingsIDFFile { get; set; }

        public MetaSWAPMeteoCheck(MetaSWAPCheck check, MetaSWAPCheckSettings settings)
        {
            this.check = check;
            this.settings = settings;

            Initialize();
        }

        protected void Initialize()
        {
            // Define legends and results 
            InvalidPrecipitationError = new CheckError(1, "Precipitation has invalid value");
            InvalidEvapotranspirationError = new CheckError(2, "Evapotranspiration has invalid value");

            errorLegend.AddClass(new ValueLegendClass(0, "No errors found.", Color.White));
            errorLegend.AddClass(InvalidPrecipitationError.CreateLegendValueClass(Color.Yellow, true));
            errorLegend.AddClass(InvalidEvapotranspirationError.CreateLegendValueClass(Color.Orange, true));
            errorLegend.AddUpperRangeClass(Check.CombinedResultLabel, true);

            // Define warnings
            PrecipitationRangeWarning = new CheckWarning(1, "Precipitation outside the expected range ["
              + settings.MinPrecipitationValue.ToString() + "," + settings.MaxPrecipitationValue + "]");
            EvapotranspirationRangeWarning = new CheckWarning(2, "Evapotranspiration outside the expected range ["
              + settings.MinEvapotranspirationValue.ToString() + "," + settings.MaxEvapotranspirationValue + "]");

            warningLegend.AddClass(new ValueLegendClass(0, "No errors found", Color.White));
            warningLegend.AddClass(PrecipitationRangeWarning.CreateLegendValueClass(Color.Yellow, true));
            warningLegend.AddClass(EvapotranspirationRangeWarning.CreateLegendValueClass(Color.Orange, true));

            warningLegend.AddUpperRangeClass(Check.CombinedResultLabel, true);
        }

        /// <summary>
        /// Checks for MetaSWAP Meteo input
        /// </summary>
        /// <param name="model"></param>
        /// <param name="resultHandler"></param>
        /// <param name="log"></param>
        public void Run(Model model, CheckResultHandler resultHandler, Log log)
        {
            this.Log = log;

            log.AddInfo("Starting metaSWAP meteo checks ...", 1);

            // Check if CAP-package exists and is active
            capPackage = (CAPPackage)model.GetPackage(CAPPackage.DefaultKey);

            // Retrieve settingfiles 
            RetrieveSettingsFiles(log);

            // Retrieve landuse input files
            log.AddInfo("Retrieving meteo files ...", 2);
            List<IDFFile[]> meteoFiles = ReadMeteoFiles(model, resultHandler.OutputPath, log);
            if ((meteoFiles == null) || (meteoFiles.Count == 0))
            {
                log.AddError(check.Name, "Meteo", "No meteo files found, check is skipped", 1);
                return;
            }

            // Check all KPERs for meteo input (mete_grid.inp)
            int issueFileCount = 0;
            for (kper = resultHandler.MinKPER; (kper <= model.NPER) && (kper <= resultHandler.MaxKPER); kper++)
            {
                if (model.NPER > 1)
                {
                    log.AddMessage(LogLevel.Info, "Checking stressperiod " + kper + " " + Model.GetStressPeriodString(model.StartDate, kper) + "...", 2);
                }
                else
                {
                    log.AddMessage(LogLevel.Trace, "Checking stressperiod " + kper + " " + Model.GetStressPeriodString(model.StartDate, kper) + "...", 2);
                }

                const int precipitationIdx = 0;
                const int evapotranspirationIdx = 1;
                precipitationIDFFile = meteoFiles[kper][precipitationIdx];
                evapotranspirationIDFFile = meteoFiles[kper][evapotranspirationIdx];

                //  double levelErrorMargin = resultHandler.LevelErrorMargin;

                // Initialize cell iterator
                InitializeIDFCellIterator(resultHandler, log);

                // Create error IDF-files for current layer                  
                CAPSubPackage meteoSubPackage = CAPSubPackage.CreateInstance(capPackage.Key, "meteo");
                errorLayer = check.CreateErrorLayer(resultHandler, meteoSubPackage, kper, entryIdx, idfCellIterator.XStepsize, errorLegend);

                // Create warning IDFfiles for current layer     
                warningLayer = check.CreateWarningLayer(resultHandler, meteoSubPackage, kper, entryIdx, idfCellIterator.XStepsize, warningLegend);

                // Iterate through cells
                int issueCount = 0;
                while (idfCellIterator.IsInsideExtent())
                {
                    float x = idfCellIterator.X;
                    float y = idfCellIterator.Y;

                    // Check precipitation values
                    issueCount += CheckPrecipitation(x, y, resultHandler);

                    // Check evapotranspiration values
                    issueCount += CheckEvapotranspiration(x, y, resultHandler);

                    idfCellIterator.MoveNext();
                }

                if (issueCount > 0)
                {
                    // Write precipitation/evapotranspiration IDF-file if related error/warning message exists
                    List<int> precipitationErrorCodeList = new List<int>();
                    List<int> precipitationWarningCodeList = new List<int>();
                    precipitationErrorCodeList.Add(InvalidPrecipitationError.ResultValue);
                    precipitationWarningCodeList.Add(PrecipitationRangeWarning.ResultValue);
                    if (errorLayer.ContainsResult(precipitationErrorCodeList) || warningLayer.ContainsResult(precipitationWarningCodeList))
                    {
                        if (!File.Exists(precipitationIDFFile.Filename))
                        {
                            precipitationIDFFile.WriteFile(precipitationIDFFile.Filename);
                        }
                        resultHandler.AddExtraMapFile(precipitationIDFFile);
                    }
                    List<int> evapotranspirationErrorCodeList = new List<int>();
                    List<int> evapotranspirationWarningCodeList = new List<int>();
                    evapotranspirationErrorCodeList.Add(InvalidEvapotranspirationError.ResultValue);
                    evapotranspirationWarningCodeList.Add(EvapotranspirationRangeWarning.ResultValue);
                    if (errorLayer.ContainsResult(evapotranspirationErrorCodeList) || warningLayer.ContainsResult(evapotranspirationWarningCodeList))
                    {
                        if (!File.Exists(evapotranspirationIDFFile.Filename))
                        {
                            evapotranspirationIDFFile.WriteFile(evapotranspirationIDFFile.Filename);
                        }
                        resultHandler.AddExtraMapFile(evapotranspirationIDFFile);
                    }

                    // Write MetaSWAP errors
                    if (errorLayer.HasResults())
                    {
                        errorLayer.CompressLegend(Check.CombinedResultLabel);
                        errorLayer.WriteResultFile(log);
                    }

                    // Write MetaSWAP warnings
                    if (warningLayer.HasResults())
                    {
                        warningLayer.CompressLegend(Check.CombinedResultLabel);
                        warningLayer.WriteResultFile(log);
                    }

                    issueFileCount++;
                    if (issueFileCount >= settings.MaxMeteoIssueFiles)
                    {
                        return;
                    }
                }
            }
        }

        private void RetrieveSettingsFiles(Log log)
        {
            minPrecipitationSettingsIDFFile = settings.GetIDFFile(settings.MinPrecipitationValue, log, 1);
            maxPrecipitationSettingsIDFFile = settings.GetIDFFile(settings.MaxPrecipitationValue, log, 1);
            minEvapotranspirationSettingsIDFFile = settings.GetIDFFile(settings.MinEvapotranspirationValue, log, 1);
            maxEvapotranspirationSettingsIDFFile = settings.GetIDFFile(settings.MaxEvapotranspirationValue, log, 1);
        }

        private List<IDFFile[]> ReadMeteoFiles(Model model, string outputPath, Log log)
        {
            List<IDFFile[]> meteoFiles = new List<IDFFile[]>();
            precipitationIDFFile = null;
            evapotranspirationIDFFile = null;
            int day = 0;
            int year = 0;
            int dayYear = -9990;
            string[] lineParts = null;
            int lineCount = 1;

            string meteGridFilename = capPackage.GetExtraFilename("mete_grid.inp");
            StreamReader sr = null;
            try
            {
                sr = new StreamReader(meteGridFilename);

                bool hasMissingPrecFiles = false;
                bool hasMissingEvapFiles = false;
                while (!sr.EndOfStream)
                {
                    lineParts = sr.ReadLine().Split(',');

                    day = 0;
                    if (!int.TryParse(lineParts[0], out day))
                    {
                        Log.AddError(check.Name, "para_sim.inp", "Value day is not an integer: " + lineParts[0] + " in mete_grid.inp in line " + lineCount.ToString(), 3);
                        continue;
                    }
                    else if (day < 0 || day > 366)
                    {
                        Log.AddError(check.Name, "para_sim.inp", "Value day is out of range (0-365): " + lineParts[0] + " in mete_grid.inp in line " + lineCount.ToString(), 3);
                        continue;
                    }
                    year = 0;
                    if (!int.TryParse(lineParts[1], out year))
                    {
                        Log.AddError(check.Name, "para_sim.inp", "Value year is not an integer: " + lineParts[1] + " in mete_grid.inp in line " + lineCount.ToString(), 3);
                        continue;
                    }
                    else if (year < 0 || year > 9999)
                    {
                        Log.AddError(check.Name, "para_sim.inp", "Value year is out of range (1-366): " + lineParts[1] + " in mete_grid.inp in line " + lineCount.ToString(), 3);
                        continue;
                    }

                    dayYear = year * 1000 + day;

                    // Assume meteo grids have ASC format
                    bool isMissingMeteoFile = false;

                    string precFilename = lineParts[2].Replace("\"", string.Empty).Replace("'", string.Empty);
                    if (!File.Exists(precFilename))
                    {
                        if (!hasMissingPrecFiles)
                        {
                            log.AddError(check.Name, Path.GetFileName(precFilename), "Meteo file not found: " + precFilename + ". Note: other missing meteo files are not logged.", 3);
                            isMissingMeteoFile = true;
                            hasMissingPrecFiles = true;
                        }
                    }
                    else
                    {
                        try
                        {
                            precipitationIDFFile = IDFFile.ReadFile(precFilename);
                            precipitationIDFFile.Filename = Path.Combine(outputPath, "checks-imodfiles", check.Name, Path.GetFileName(precFilename));
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("Could not read precipitation file '" + precFilename + "' in mete_grip.inp at line " + lineCount + ": " + ex.GetBaseException().Message);
                        }
                    }

                    string evapFilename = lineParts[3].Replace("\"", string.Empty).Replace("'", string.Empty);
                    if (!File.Exists(evapFilename))
                    {
                        if (!hasMissingEvapFiles)
                        {
                            log.AddError(check.Name, Path.GetFileName(evapFilename), "Meteo file not found: " + precFilename + ". Note: other missing meteo files are not logged.", 3);
                            hasMissingEvapFiles = true;
                            isMissingMeteoFile = true;
                        }
                    }
                    else
                    {
                        try
                        {
                            evapotranspirationIDFFile = IDFFile.ReadFile(evapFilename);
                            evapotranspirationIDFFile.Filename = Path.Combine(outputPath, "checks-imodfiles", check.Name, Path.GetFileName(evapotranspirationIDFFile.Filename));
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("Could not read evapotranspiration file '" + evapFilename + "' in mete_grip.inp at line " + lineCount, ex);
                        }
                    }

                    if (!isMissingMeteoFile)
                    {
                        IDFFile[] meteoArray = { precipitationIDFFile, evapotranspirationIDFFile };
                        meteoFiles.Add(meteoArray);
                        lineCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while reading mete_grip.inp in line: " + lineCount, ex);
            }
            finally
            {
                if (sr != null)
                {
                    sr.Close();
                }
            }

            // Check for equal number of timesteps and modelfiles. ToDo: check starting timestep in para_sim.inp, startdate of model, etc.
            //if (meteoFiles.Count() != model.NPER)
            //{
            //    log.AddError("Number of meteo input grids ( " + meteoFiles.Count() + " ) and modelsteps (" + model.NPER + ") do not match", 2);
            //}

            return meteoFiles;
        }

        private void InitializeIDFCellIterator(CheckResultHandler resultHandler, Log log)
        {
            idfCellIterator = new IDFCellIterator(resultHandler.Extent);

            idfCellIterator.AddIDFFile(precipitationIDFFile);
            idfCellIterator.AddIDFFile(evapotranspirationIDFFile);
            idfCellIterator.AddIDFFile(minPrecipitationSettingsIDFFile);
            idfCellIterator.AddIDFFile(maxPrecipitationSettingsIDFFile);
            idfCellIterator.AddIDFFile(minEvapotranspirationSettingsIDFFile);
            idfCellIterator.AddIDFFile(maxEvapotranspirationSettingsIDFFile);
            string message = idfCellIterator.CheckExtent();
            if (message != null)
            {
                log.AddWarning(check.Name, "Meteo", message, 2);
            }
            idfCellIterator.Reset();
        }

        /// <summary>
        /// Check precipitation. Return 1 if an issue was found, 0 otherwise.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private int CheckPrecipitation(float x, float y, CheckResultHandler resultHandler)
        {
            float precipitationValue = idfCellIterator.GetCellValue(precipitationIDFFile);
            float minPrecipitationValue = idfCellIterator.GetCellValue(minPrecipitationSettingsIDFFile);
            float maxPrecipitationValue = idfCellIterator.GetCellValue(maxPrecipitationSettingsIDFFile);

            if (precipitationValue.Equals(precipitationIDFFile.NoDataValue) || (precipitationValue < 0))
            {
                resultHandler.AddCheckResult(errorLayer, x, y, InvalidPrecipitationError);
                resultHandler.AddExtraMapFile(precipitationIDFFile);
                return 1;
            }
            else if ((precipitationValue < minPrecipitationValue) || (precipitationValue > maxPrecipitationValue))
            {
                resultHandler.AddCheckResult(warningLayer, x, y, PrecipitationRangeWarning);
                resultHandler.AddExtraMapFile(precipitationIDFFile);
                return 1;
            }

            return 0;
        }

        /// <summary>
        /// Check evapotranspiration. Return 1 if an issue was found, 0 otherwise.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private int CheckEvapotranspiration(float x, float y, CheckResultHandler resultHandler)
        {
            float evapotranspirationValue = idfCellIterator.GetCellValue(evapotranspirationIDFFile);
            float minEvapotranspirationValue = idfCellIterator.GetCellValue(minEvapotranspirationSettingsIDFFile);
            float maxEvapotranspirationValue = idfCellIterator.GetCellValue(maxEvapotranspirationSettingsIDFFile);

            if (evapotranspirationValue.Equals(evapotranspirationIDFFile.NoDataValue) || (evapotranspirationValue < 0))
            {
                resultHandler.AddCheckResult(errorLayer, x, y, InvalidEvapotranspirationError);
                resultHandler.AddExtraMapFile(evapotranspirationIDFFile);
                return 1;
            }
            else if ((evapotranspirationValue < minEvapotranspirationValue) || (evapotranspirationValue > maxEvapotranspirationValue))
            {
                resultHandler.AddCheckResult(warningLayer, x, y, EvapotranspirationRangeWarning);
                resultHandler.AddExtraMapFile(evapotranspirationIDFFile);
                return 1;
            }

            return 0;
        }
    }
}
