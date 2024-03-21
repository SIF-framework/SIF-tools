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
using Sweco.SIF.iMOD.IDF;
using Sweco.SIF.iMOD.Legends;
using Sweco.SIF.iMODValidator.Checks.CheckResults;
using Sweco.SIF.iMODValidator.Models;
using Sweco.SIF.iMODValidator.Models.Files;
using Sweco.SIF.iMODValidator.Models.Packages;
using Sweco.SIF.iMODValidator.Models.Packages.Files;
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
    class RIVCheckSettings : CheckSettings
    {
        private string minConductanceRatioForLevelChecks;
        private string stageMinLevel;
        private string stageMaxLevel;
        private string stageMinBelowSurfaceDistance;
        private string stageMaxBelowSurfaceDistance;
        private string bottomMinBelowStageDistance;
        private string bottomMaxBelowStageDistance;
        private string hydraulicResistanceMinValue;
        private string hydraulicResistanceMaxValue;
        private string infFactorMinValue;
        private string infFactorMaxValue;
        private UpscaleMethodEnum upscaleMethod;
        private bool isISGConverted;

        [Category("RIV-properties"), Description("Should ISG-files be converted and checked with the RIV-check?"), PropertyOrder(1)]
        public bool IsISGConverted
        {
            get { return isISGConverted; }
            set { isISGConverted = value; }
        }

        [Category("RIV-properties"), Description("The ratio [0,1] for calculating the minimum conductance for level-checks (i.e. bottom against surface level or stage against OLF). This minimum conductance for any modelcell is the ratio mulitplied with the surface of the cell. With this setting surface water that occupies just a small part of a cell can be ignored,"), PropertyOrder(2)]
        public string MinConductanceRatioForLevelChecks
        {
            get { return minConductanceRatioForLevelChecks; }
            set
            {
                float minCondValue;
                if (float.TryParse(value, System.Globalization.NumberStyles.Float, englishCultureInfo, out minCondValue))
                {
                    minConductanceRatioForLevelChecks = value;
                }
                else
                {
                    // ignore invalid format
                }
            }
        }

        [Category("Warning-properties"), Description("The minimum valid Stage-level for this region"), PropertyOrder(10)]
        public string StageMinLevel
        {
            get { return stageMinLevel; }
            set { stageMinLevel = value; }
        }
        [Category("Warning-properties"), Description("The maximum valid Stage-level for this region"), PropertyOrder(11)]
        public string StageMaxLevel
        {
            get { return stageMaxLevel; }
            set { stageMaxLevel = value; }
        }
        [Category("Warning-properties"), Description("The minimum valid distance from surface level to stage for this region"), PropertyOrder(12)]
        public string StageMinBelowSurfaceDistance
        {
            get { return stageMinBelowSurfaceDistance; }
            set { stageMinBelowSurfaceDistance = value; }
        }
        [Category("Warning-properties"), Description("The maximum valid distance from surface level to stage for this region"), PropertyOrder(13)]
        public string StageMaxBelowSurfaceDistance
        {
            get { return stageMaxBelowSurfaceDistance; }
            set { stageMaxBelowSurfaceDistance = value; }
        }
        [Category("Warning-properties"), Description("The minimum valid distance from stage to bottom for this region. Can be negative for models that allow stage < bottomheight."), PropertyOrder(20)]
        public string BottomMinBelowStageDistance
        {
            get { return bottomMinBelowStageDistance; }
            set { bottomMinBelowStageDistance = value; }
        }
        [Category("Warning-properties"), Description("The maximum valid distance from stage to bottom for this region"), PropertyOrder(21)]
        public string BottomMaxBelowStageDistance
        {
            get { return bottomMaxBelowStageDistance; }
            set { bottomMaxBelowStageDistance = value; }
        }
        [Category("Warning-properties"), Description("The minimum valid hydraulic resistance (d) for this region. Used for calculating the maximum conductance."), PropertyOrder(30)]
        public string HydraulicResistanceMinValue
        {
            get { return hydraulicResistanceMinValue; }
            set { hydraulicResistanceMinValue = value; }
        }
        [Category("Warning-properties"), Description("The maximum valid hydraulic resistance (d) for this region. Used for calculating the minimum conductance."), PropertyOrder(31)]
        public string HydraulicResistanceMaxValue
        {
            get { return hydraulicResistanceMaxValue; }
            set { hydraulicResistanceMaxValue = value; }
        }
        [Category("Warning-properties"), Description("The minimum valid infiltrationfactor for this region"), PropertyOrder(40)]
        public string InfFactorMinValue
        {
            get { return infFactorMinValue; }
            set { infFactorMinValue = value; }
        }
        [Category("Warning-properties"), Description("The maximum valid infiltrationfactor for this region"), PropertyOrder(41)]
        public string InfFactorMaxValue
        {
            get { return infFactorMaxValue; }
            set { infFactorMaxValue = value; }
        }
        [Category("Scale-properties"), Description("Upscale method in case of resolution differences. Use 'Maximum' to only find coarse cells with problem for all finer cells inside"), PropertyOrder(10)]
        public UpscaleMethodEnum UpscaleMethod
        {
            get { return upscaleMethod; }
            set { upscaleMethod = value; }
        }

        public RIVCheckSettings(string checkName) : base(checkName)
        {
            isISGConverted = true;
            minConductanceRatioForLevelChecks = "0";
            StageMinLevel = "-10";
            StageMaxLevel = "500";
            StageMinBelowSurfaceDistance = "0";
            StageMaxBelowSurfaceDistance = ((float)10f).ToString(englishCultureInfo);    // Depends on the region, 10m is for areas with a large gradient
            BottomMinBelowStageDistance = "0";                                           // Can be negative for models that allow stage < bottomheight
            BottomMaxBelowStageDistance = "50";                                          // Set default level on the safe side (absolute maximum whould be around 5m)
            HydraulicResistanceMinValue = ((float)0.1f).ToString(englishCultureInfo);
            HydraulicResistanceMaxValue = "1000";
            InfFactorMinValue = "0";
            InfFactorMaxValue = "1";
            upscaleMethod = UpscaleMethodEnum.Minimum;
        }

        public override void LogSettings(Log log, int logIndentLevel = 0)
        {
            log.AddInfo("IsISGConverted: " + isISGConverted, logIndentLevel);
            log.AddInfo("MinConductanceRatioForLevelChecks: " + minConductanceRatioForLevelChecks, logIndentLevel);
            log.AddInfo("Minimum stage level: " + stageMinLevel + " mNAP", logIndentLevel);
            log.AddInfo("Maximum stage level: " + stageMaxLevel + " mNAP", logIndentLevel);
            log.AddInfo("StageMinBelowSurfaceDistance: " + StageMinBelowSurfaceDistance + " m-mv", logIndentLevel);
            log.AddInfo("StageMaxBelowSurfaceDistance: " + StageMaxBelowSurfaceDistance + " m-mv", logIndentLevel);
            log.AddInfo("BottomMinBelowStageDistance: " + BottomMinBelowStageDistance + " m", logIndentLevel);
            log.AddInfo("BottomMaxBelowStageDistance: " + BottomMaxBelowStageDistance + " m", logIndentLevel);
            log.AddInfo("HydraulicResistanceMinValue: " + HydraulicResistanceMinValue + " m", logIndentLevel);
            log.AddInfo("HydraulicResistanceMaxValue: " + HydraulicResistanceMaxValue + " m", logIndentLevel);
            log.AddInfo("InfFactorMinValue: " + InfFactorMinValue, logIndentLevel);
            log.AddInfo("InfFactorMaxValue: " + InfFactorMaxValue, logIndentLevel);
            log.AddInfo("Upscale method: " + upscaleMethod.ToString(), logIndentLevel);
        }
    }

    public class RIVCheck : Check
    {
        private CheckError InconsistentRIVFilesError;
        private CheckError ZeroValueError;
        private CheckError NegativeValueError;
        private CheckError StageAboveOLFError;
        private CheckError InfiltrationFactorError;
        private CheckWarning BotAboveSurfaceWarning;
        private CheckWarning AbsoluteStageRangeWarning;
        private CheckWarning RelativeStageRangeWarning;
        private CheckWarning RelativeBottomRangeWarning;
        private CheckWarning ConductanceRangeWarning;
        private CheckWarning InfiltrationFactorRangeWarning;

        protected IDFLegend errorLegend;
        protected IDFLegend warningLegend;

        public override string Abbreviation
        {
            get { return "RIV"; }
        }

        public override string Description
        {
            get { return "Checks consistincy of bottom, conductance, stage and inffactor files per system"; }
        }

        private RIVCheckSettings settings;
        public override CheckSettings Settings
        {
            get { return settings; }
            set
            {
                if (value is RIVCheckSettings)
                {
                    settings = (RIVCheckSettings)value;
                }
            }
        }

        public RIVCheck()
        {
            settings = new RIVCheckSettings(this.Name);

            // Define errors
            InconsistentRIVFilesError = new CheckError(1, "Inconsistent RIV-files", "Inconsistent RIV-files");
            ZeroValueError = new CheckError(2, "Unexpected zero value", "The value is not, but should be, greater than zero.");
            NegativeValueError = new CheckError(4, "Unexpected negative value");
            StageAboveOLFError = new CheckError(8, "Stage level above OLF", "The stage level is above the OLF level.");
            InfiltrationFactorError = new CheckError(16, "Invalid infiltrationfactor", "The infiltrationfactor should between -1 and 1");

            errorLegend = new IDFLegend("Legend for RIV-file check");
            errorLegend.AddClass(new ValueLegendClass(0, "No errors found.", Color.White));
            errorLegend.AddClass(InconsistentRIVFilesError.CreateLegendValueClass(Color.Yellow));
            errorLegend.AddClass(ZeroValueError.CreateLegendValueClass(Color.Red));
            errorLegend.AddClass(NegativeValueError.CreateLegendValueClass(Color.DarkRed));
            errorLegend.AddClass(StageAboveOLFError.CreateLegendValueClass(Color.Purple));
            errorLegend.AddClass(InfiltrationFactorError.CreateLegendValueClass(Color.DeepPink));
            errorLegend.AddUpperRangeClass(CombinedResultLabel, true);
            errorLegend.AddInbetweenClasses(CombinedResultLabel, true);

            // Define warnings
            AbsoluteStageRangeWarning = new CheckWarning(1, "Stage outside range", "Stage outside the expected range ["
                + settings.StageMinLevel.ToString() + "," + settings.StageMaxLevel.ToString() + "]");
            RelativeStageRangeWarning = new CheckWarning(2, "Stage-surface outside range", "Distance stage to surface level outside the expected range ["
                + settings.StageMinBelowSurfaceDistance.ToString() + ";" + settings.StageMaxBelowSurfaceDistance.ToString() + "]");
            BotAboveSurfaceWarning = new CheckWarning(4, "Bottom above surface level", "The bottom value is above the surface level.");
            RelativeBottomRangeWarning = new CheckWarning(8, "Bottom-stage outside range", "Distance bottom to stage outside expected range ["
                + settings.BottomMinBelowStageDistance.ToString() + ";" + settings.BottomMaxBelowStageDistance.ToString() + "]");
            ConductanceRangeWarning = new CheckWarning(16, "Conductance outside range", "Conductance outside range based on defined range for hydraulic resistance ["
                + settings.HydraulicResistanceMinValue.ToString() + ";" + settings.HydraulicResistanceMaxValue.ToString() + "]");
            InfiltrationFactorRangeWarning = new CheckWarning(32, "Inffactor outside range", "Infiltrationfactor outside expected range ["
                + settings.InfFactorMinValue.ToString() + ";" + settings.InfFactorMaxValue.ToString() + "]");
            warningLegend = new IDFLegend("Legend for RIV-file check");
            warningLegend.AddClass(new ValueLegendClass(0, "No warnings found", Color.White));
            warningLegend.AddClass(AbsoluteStageRangeWarning.CreateLegendValueClass(Color.Pink, true));
            warningLegend.AddClass(RelativeStageRangeWarning.CreateLegendValueClass(Color.LightBlue, true));
            warningLegend.AddClass(BotAboveSurfaceWarning.CreateLegendValueClass(Color.Blue, true));
            warningLegend.AddClass(RelativeBottomRangeWarning.CreateLegendValueClass(Color.Orange, true));
            warningLegend.AddClass(ConductanceRangeWarning.CreateLegendValueClass(Color.Brown, true));
            warningLegend.AddClass(InfiltrationFactorRangeWarning.CreateLegendValueClass(Color.Red, true));
            warningLegend.AddUpperRangeClass(CombinedResultLabel, true);
            warningLegend.AddInbetweenClasses(CombinedResultLabel, true);
        }

        public override void Run(Model model, CheckResultHandler resultHandler, Log log)
        {
            try
            {
                log.AddInfo("Checking RIV-package ...");

                IDFPackage rivPackage = (IDFPackage)model.GetPackage(RIVPackage.DefaultKey);
                if (!IsPackageActive(rivPackage, RIVPackage.DefaultKey, log, 1))
                {
                    return;
                }

                settings.LogSettings(log, 1);
                RunRIVCheck1(model, resultHandler, log);
            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error in " + this.Name, ex);
            }
        }

        protected virtual void RunRIVCheck1(Model model, CheckResultHandler resultHandler, Log log)
        {
            // Retrieve RIV-package
            IDFPackage rivPackage = (IDFPackage)model.GetPackage(RIVPackage.DefaultKey);

            // Retrieve OLF package
            bool useDRNL1OLF = false;
            IDFPackage olfPackage = (IDFPackage)model.GetPackage(OLFPackage.DefaultKey);
            IDFFile olfIDFFile = null;
            if ((olfPackage != null) && (olfPackage.IsActive))
            {
                olfIDFFile = olfPackage.GetIDFFile(0);
            }
            else
            {
                olfPackage = null;
                OLFCheck olfCheck = (OLFCheck)CheckManager.Instance.RetrieveCheck(typeof(OLFCheck));
                if (olfCheck != null)
                {
                    useDRNL1OLF = ((OLFCheckSettings)olfCheck.Settings).UseDRN_L1;
                    if (useDRNL1OLF)
                    {
                        log.AddInfo("OLF-check is defined to use DRN_L1 as OLF. DRN_L1 is skipped for the DRN-check ...", 2);

                        // Retrieve DRN-package
                        IDFPackage drnPackage = (IDFPackage)model.GetPackage(DRNPackage.DefaultKey);
                        if ((drnPackage == null) || !drnPackage.IsActive)
                        {
                            log.AddWarning(this.Name, model.RUNFilename, "DRN-package is not active. " + this.Name + " is skipped.", 2);
                            return;
                        }

                        olfIDFFile = drnPackage.GetIDFFile(0, DRNPackage.LevelPartIdx);
                    }
                    else if (model.IsSteadyStateModel())
                    {
                        log.AddWarning(rivPackage.Key, model.RUNFilename, "No OLF-file defined for this model, OLF-checks are skipped ...", 2);
                    }
                }
            }
            IDFUpscaler olfUpscaler = new IDFUpscaler(olfIDFFile, settings.UpscaleMethod, resultHandler.Extent, GetIMODFilesPath(model), log, 2);

            // retrieve non-package surface level file
            IDFFile surfacelevelIDFFile = model.RetrieveSurfaceLevelFile(log, 1);
            if (surfacelevelIDFFile == null)
            {
                log.AddWarning("No surface level file defined, surface level check is skipped.", 1);
            }
            IDFUpscaler surfacelevelUpscaler = new IDFUpscaler(surfacelevelIDFFile, settings.UpscaleMethod, resultHandler.Extent, GetIMODFilesPath(model), log, 2);

            float levelErrorMargin = resultHandler.LevelErrorMargin;
            bool isSurfaceLevelNoDataWarningShown = false;
            log.AddInfo("Checking RIV-files ... ", 1);
            for (int kper = resultHandler.MinKPER; (kper <= model.NPER) && (kper <= resultHandler.MaxKPER); kper++)
            {
                CheckRIVPackage(rivPackage, kper, model, resultHandler, surfacelevelIDFFile, surfacelevelUpscaler, olfIDFFile, olfUpscaler, levelErrorMargin, isSurfaceLevelNoDataWarningShown, log, 1);
            }

            ISGPackage isgPackage = (ISGPackage)model.GetPackage(ISGPackage.DefaultKey);
            if ((settings.IsISGConverted) && (isgPackage != null) && ISGRIVConverter.HasISGPackageEntries(isgPackage, model, resultHandler))
            {
                log.AddInfo("Converting ISG-files to RIV-files and applying RIV-check", 1);
                IDFPackage isgRIVPackage = ISGRIVConverter.ConvertISGtoRIVPackage(isgPackage, "ISGRIV", model, resultHandler, log, 2);

                log.AddInfo("Checking converted ISG-files ... ", 1);
                int orgMinEntryNumber = resultHandler.MinEntryNumber;
                int orgMaxEntryNumber = resultHandler.MaxEntryNumber;
                resultHandler.MinEntryNumber = 1;
                resultHandler.MaxEntryNumber = 999;
                for (int kper = resultHandler.MinKPER; (kper <= model.NPER) && (kper <= resultHandler.MaxKPER); kper++)
                {
                    CheckRIVPackage(isgRIVPackage, kper, model, resultHandler, surfacelevelIDFFile, surfacelevelUpscaler, olfIDFFile, olfUpscaler, levelErrorMargin, isSurfaceLevelNoDataWarningShown, log, 1, isgPackage);
                }
                resultHandler.MinEntryNumber = orgMinEntryNumber;
                resultHandler.MaxEntryNumber = orgMaxEntryNumber;
            }

            olfUpscaler.ReleaseMemory(false);
            surfacelevelUpscaler.ReleaseMemory(true);
            if (olfPackage != null)
            {
                olfPackage.ReleaseMemory(true);
            }
        }

        private void CheckRIVPackage(IDFPackage rivPackage, int kper, Model model, CheckResultHandler resultHandler, IDFFile surfacelevelIDFFile, IDFUpscaler surfacelevelUpscaler, IDFFile olfIDFFile, IDFUpscaler olfUpscaler, float levelErrorMargin, bool isSurfaceLevelNoDataWarningShown, Log log, int logIndentLevel, ISGPackage isgPackage = null)
        {
            StressPeriod stressPeriod = model.RetrieveStressPeriod(kper);

            if (rivPackage.GetEntryCount(kper) > 0)
            {
                if (model.NPER > 1)
                {
                    log.AddInfo("Checking stress period " + kper + " " + model.RetrieveSNAME(kper) + " ...", logIndentLevel);
                }
                else
                {
                    log.AddInfo("Checking stress period " + kper + " " + model.RetrieveSNAME(kper) + " ...", logIndentLevel);
                }

                // Process all specified modellayers within the current period
                HandlePreprocessing();
                bool hasInconsistentRIVFiles = false;
                for (int entryIdx = resultHandler.MinEntryNumber - 1; (entryIdx < resultHandler.MaxEntryNumber) && (entryIdx < rivPackage.GetEntryCount(kper)); entryIdx++)
                {
                    log.AddInfo("Checking entry " + (entryIdx + 1) + " with " + Name + " ...", logIndentLevel);

                    // Retrieve IDF files for current layer
                    IDFPackageFile rivConductancePackageIDFFile = rivPackage.GetIDFPackageFile(entryIdx, RIVPackage.ConductancePartIdx, kper);
                    IDFFile rivConductanceIDFFile = (rivConductancePackageIDFFile != null) ? rivConductancePackageIDFFile.IDFFile : null;
                    IDFFile rivStageIDFFile = rivPackage.GetIDFFile(entryIdx, RIVPackage.StagePartIdx, kper);
                    IDFFile rivBottomIDFFile = rivPackage.GetIDFFile(entryIdx, RIVPackage.BottomPartIdx, kper);
                    IDFFile rivInfFactorIDFFile = rivPackage.GetIDFFile(entryIdx, RIVPackage.InfFactorPartIdx, kper);

                    if ((rivConductanceIDFFile == null) || (rivStageIDFFile == null) || (rivBottomIDFFile == null) || (rivInfFactorIDFFile == null))
                    {
                        log.AddWarning("One or more RIV-files are missing, RIV-check is skipped", logIndentLevel + 1);
                    }
                    else
                    {
                        // Prevent checking the same fileset twice
                        List<IDFFile> checkFileList = new List<IDFFile>() { rivConductanceIDFFile, rivStageIDFFile, rivBottomIDFFile, rivInfFactorIDFFile };
                        int constantFileCount = IDFUtils.GetConstantIDFFileCount(checkFileList);
                        if (rivPackage.IsFileListPresent(checkFileList, resultHandler.MinKPER, kper))
                        {
                            log.AddInfo("files have been checked already", logIndentLevel + 1);
                        }
                        else
                        {
                            int ilay = rivConductancePackageIDFFile.ILAY;

                            // Retrieve a surfacelevelfile with the same or coarser resolution than the DRN-file(s)
                            IDFFile scaledSurfacelevelIDFFile = surfacelevelUpscaler.RetrieveIDFFile(rivStageIDFFile.XCellsize);
                            IDFFile scaledOLFIDFFile = olfUpscaler.RetrieveIDFFile(rivStageIDFFile.XCellsize);

                            // Retrieve setting-values
                            IDFFile minConductanceRatioForLevelChecksSettingIDFFile = settings.GetIDFFile(settings.MinConductanceRatioForLevelChecks, log, logIndentLevel + 1);
                            IDFFile stageMinSettingIDFFile = settings.GetIDFFile(settings.StageMinLevel, log, logIndentLevel + 1);
                            IDFFile stageMaxSettingIDFFile = settings.GetIDFFile(settings.StageMaxLevel, log, logIndentLevel + 1);
                            IDFFile stageMinBelowSurfaceSettingIDFFile = settings.GetIDFFile(settings.StageMinBelowSurfaceDistance, log, logIndentLevel + 1);
                            IDFFile stageMaxBelowSurfaceSettingIDFFile = settings.GetIDFFile(settings.StageMaxBelowSurfaceDistance, log, logIndentLevel + 1);
                            IDFFile bottomMinBelowStageSettingIDFFile = settings.GetIDFFile(settings.BottomMinBelowStageDistance, log, logIndentLevel + 1);
                            IDFFile bottomMaxBelowStageSettingIDFFile = settings.GetIDFFile(settings.BottomMaxBelowStageDistance, log, logIndentLevel + 1);
                            IDFFile hydraulicResistanceMinSettingIDFFile = settings.GetIDFFile(settings.HydraulicResistanceMinValue, log, logIndentLevel + 1);
                            IDFFile hydraulicResistanceMaxSettingIDFFile = settings.GetIDFFile(settings.HydraulicResistanceMaxValue, log, logIndentLevel + 1);
                            IDFFile infFactorMinSettingIDFFile = settings.GetIDFFile(settings.InfFactorMinValue, log, logIndentLevel + 1);
                            IDFFile infFactorMaxSettingIDFFile = settings.GetIDFFile(settings.InfFactorMaxValue, log, logIndentLevel + 1);

                            IDFCellIterator idfCellIterator = new IDFCellIterator(resultHandler.Extent);
                            idfCellIterator.AddIDFFile(rivStageIDFFile);
                            idfCellIterator.AddIDFFile(rivBottomIDFFile);
                            idfCellIterator.AddIDFFile(rivConductanceIDFFile);
                            idfCellIterator.AddIDFFile(rivInfFactorIDFFile);

                            // Check that ANI-files have equal extent
                            idfCellIterator.CheckExtent(log, logIndentLevel + 1, LogLevel.Debug);
                            if (idfCellIterator.IsEmptyExtent())
                            {
                                return;
                            }

                            // Add other files that are used for checking
                            idfCellIterator.AddIDFFile(minConductanceRatioForLevelChecksSettingIDFFile);
                            idfCellIterator.AddIDFFile(scaledSurfacelevelIDFFile);
                            idfCellIterator.AddIDFFile(scaledOLFIDFFile);
                            idfCellIterator.AddIDFFile(stageMinSettingIDFFile);
                            idfCellIterator.AddIDFFile(stageMaxSettingIDFFile);
                            idfCellIterator.AddIDFFile(stageMinBelowSurfaceSettingIDFFile);
                            idfCellIterator.AddIDFFile(stageMaxBelowSurfaceSettingIDFFile);
                            idfCellIterator.AddIDFFile(bottomMinBelowStageSettingIDFFile);
                            idfCellIterator.AddIDFFile(bottomMaxBelowStageSettingIDFFile);
                            idfCellIterator.AddIDFFile(hydraulicResistanceMinSettingIDFFile);
                            idfCellIterator.AddIDFFile(hydraulicResistanceMaxSettingIDFFile);
                            idfCellIterator.AddIDFFile(infFactorMinSettingIDFFile);
                            idfCellIterator.AddIDFFile(infFactorMaxSettingIDFFile);

                            // Create error IDFfiles for current layer
                            CheckErrorLayer rivErrorLayer = CreateErrorLayer(resultHandler, rivPackage, "SYS" + (entryIdx + 1), stressPeriod, entryIdx + 1, idfCellIterator.XStepsize, errorLegend);
                            rivErrorLayer.AddSourceFiles(idfCellIterator.GetIDFFiles());

                            // Create warning IDFfiles for current layer
                            CheckWarningLayer rivWarningLayer = CreateWarningLayer(resultHandler, rivPackage, "SYS" + (entryIdx + 1), stressPeriod, entryIdx + 1, idfCellIterator.XStepsize, warningLegend);
                            rivWarningLayer.AddSourceFiles(idfCellIterator.GetIDFFiles());

                            float cellSurface = rivConductanceIDFFile.XCellsize * rivConductanceIDFFile.YCellsize;

                            // Iterate through cells
                            idfCellIterator.Reset();
                            while (idfCellIterator.IsInsideExtent())
                            {
                                float scaledSurfacelevelValue = idfCellIterator.GetCellValue(scaledSurfacelevelIDFFile);
                                float scaledOLFValue = idfCellIterator.GetCellValue(scaledOLFIDFFile);
                                float rivStageValue = idfCellIterator.GetCellValue(rivStageIDFFile);
                                float rivBottomValue = idfCellIterator.GetCellValue(rivBottomIDFFile);
                                float rivConductanceValue = idfCellIterator.GetCellValue(rivConductanceIDFFile);
                                float rivInfFactorValue = idfCellIterator.GetCellValue(rivInfFactorIDFFile);
                                float minConductanceForLevelChecksValue = cellSurface * idfCellIterator.GetCellValue(minConductanceRatioForLevelChecksSettingIDFFile);
                                float stageMinLevel = idfCellIterator.GetCellValue(stageMinSettingIDFFile);
                                float stageMaxLevel = idfCellIterator.GetCellValue(stageMaxSettingIDFFile);
                                float stageMinBelowSurfaceDistance = idfCellIterator.GetCellValue(stageMinBelowSurfaceSettingIDFFile);
                                float stageMaxBelowSurfaceDistance = idfCellIterator.GetCellValue(stageMaxBelowSurfaceSettingIDFFile);
                                float bottomMinBelowStageDistance = idfCellIterator.GetCellValue(bottomMinBelowStageSettingIDFFile);
                                float bottomMaxBelowStageDistance = idfCellIterator.GetCellValue(bottomMaxBelowStageSettingIDFFile);
                                float conductanceMinValue = 1 / idfCellIterator.GetCellValue(hydraulicResistanceMaxSettingIDFFile);
                                float conductanceMaxValue = (rivConductanceIDFFile.XCellsize * rivConductanceIDFFile.YCellsize) / idfCellIterator.GetCellValue(hydraulicResistanceMinSettingIDFFile);
                                float infFactorMinValue = idfCellIterator.GetCellValue(infFactorMinSettingIDFFile);
                                float infFactorMaxValue = idfCellIterator.GetCellValue(infFactorMaxSettingIDFFile);
                                float x = idfCellIterator.X;
                                float y = idfCellIterator.Y;

                                float dataCount = 0;
                                if (!rivBottomValue.Equals(float.NaN) && !rivBottomValue.Equals(rivBottomIDFFile.NoDataValue))
                                {
                                    dataCount++;
                                    if (!scaledSurfacelevelValue.Equals(float.NaN) && (!scaledSurfacelevelValue.Equals(scaledSurfacelevelIDFFile.NoDataValue)))
                                    {
                                        if ((rivBottomValue > scaledSurfacelevelValue + levelErrorMargin) && (rivConductanceValue >= minConductanceForLevelChecksValue))
                                        {
                                            resultHandler.AddCheckResult(rivWarningLayer, x, y, BotAboveSurfaceWarning);
                                        }
                                    }
                                    else if (!isSurfaceLevelNoDataWarningShown)
                                    {
                                        log.AddWarning(this.Name, null, "Surfacelevel has NoData-value for one or more RIV-cells, RIV-surfacelevel check skipped in these cases", logIndentLevel + 1);
                                        isSurfaceLevelNoDataWarningShown = true;
                                    }
                                }
                                if (!rivStageValue.Equals(float.NaN) && !rivStageValue.Equals(rivStageIDFFile.NoDataValue))
                                {
                                    dataCount++;

                                    if ((rivStageValue < stageMinLevel) || (rivStageValue > stageMaxLevel))
                                    {
                                        resultHandler.AddCheckResult(rivWarningLayer, x, y, AbsoluteStageRangeWarning);
                                    }

                                    if (!scaledSurfacelevelValue.Equals(float.NaN) && !scaledSurfacelevelValue.Equals(scaledSurfacelevelIDFFile.NoDataValue))
                                    {
                                        if (((rivStageValue > (scaledSurfacelevelValue - stageMinBelowSurfaceDistance)) ||
                                            (rivStageValue < (scaledSurfacelevelValue - stageMaxBelowSurfaceDistance))) && (rivConductanceValue >= minConductanceForLevelChecksValue))
                                        {
                                            resultHandler.AddCheckResult(rivWarningLayer, x, y, RelativeStageRangeWarning);
                                        }
                                    }

                                    if (!rivBottomValue.Equals(float.NaN) && !rivBottomValue.Equals(rivBottomIDFFile.NoDataValue))
                                    {
                                        if ((rivBottomValue > (rivStageValue - bottomMinBelowStageDistance)) ||
                                            (rivBottomValue < (rivStageValue - bottomMaxBelowStageDistance)))
                                        {
                                            resultHandler.AddCheckResult(rivWarningLayer, x, y, RelativeBottomRangeWarning);
                                        }
                                    }
                                    if (!scaledOLFValue.Equals(float.NaN) && !scaledOLFValue.Equals(scaledOLFIDFFile.NoDataValue) && (rivStageValue > scaledOLFValue + levelErrorMargin) && (rivConductanceValue >= minConductanceForLevelChecksValue))
                                    {
                                        resultHandler.AddCheckResult(rivErrorLayer, x, y, StageAboveOLFError);
                                    }
                                }
                                if (!rivConductanceValue.Equals(float.NaN) && !rivConductanceValue.Equals(rivConductanceIDFFile.NoDataValue))
                                {
                                    dataCount++;

                                    if ((rivConductanceValue < conductanceMinValue) || (rivConductanceValue > conductanceMaxValue))
                                    {
                                        resultHandler.AddCheckResult(rivWarningLayer, x, y, ConductanceRangeWarning);
                                    }
                                }
                                if (!rivInfFactorValue.Equals(float.NaN) && !rivInfFactorValue.Equals(rivInfFactorIDFFile.NoDataValue))
                                {
                                    dataCount++;
                                    // Round to number of decimals that are visible in iMOD
                                    rivInfFactorValue = (float) Math.Round(rivInfFactorValue, 7);
                                    if ((rivInfFactorValue < 0.0f) || (rivInfFactorValue > 1.0f)) // According to the runfile doc, the factor can be between -1 and 0, but for now this is seen as an error
                                    {
                                        resultHandler.AddCheckResult(rivErrorLayer, x, y, InfiltrationFactorError);
                                    }
                                    else
                                    {
                                        if ((rivInfFactorValue < infFactorMinValue) || (rivInfFactorValue > infFactorMaxValue))
                                        {
                                            resultHandler.AddCheckResult(rivWarningLayer, x, y, InfiltrationFactorRangeWarning);
                                        }
                                    }
                                }
                                if (dataCount == rivPackage.MaxPartCount)
                                {
                                    //if ((rivInfFactorValue == 0) || (rivConductanceValue == 0))
                                    //{
                                    //    resultHandler.AddError(rivErrorLayerIdx, x, y, ZeroValueError);
                                    //}
                                    // Check for negative conductance (note: inffactor may be negative, see runfile documentation)
                                    if (rivConductanceValue < 0)
                                    {
                                        resultHandler.AddCheckResult(rivErrorLayer, x, y, NegativeValueError);
                                    }
                                }
                                else
                                {
                                    // correct for constantFiles: since constant file have a value everywhere, correct for cells that have no other RIV-values defined
                                    if (constantFileCount > 0)
                                    {
                                        dataCount -= constantFileCount;
                                    }
                                    if ((dataCount > 0) && (dataCount < rivPackage.MaxPartCount))
                                    {
                                        resultHandler.AddCheckResult(rivErrorLayer, x, y, InconsistentRIVFilesError);
                                        hasInconsistentRIVFiles = true;
                                    }
                                }

                                idfCellIterator.MoveNext();
                            }

                            // Write errorfiles and add files to error handler
                            if (rivErrorLayer.HasResults())
                            {
                                rivErrorLayer.CompressLegend(CombinedResultLabel);
                                rivErrorLayer.WriteResultFile(log);
                                resultHandler.AddExtraMapFile(olfIDFFile);
                                if ((olfIDFFile != null) && (scaledOLFIDFFile != null) && !olfIDFFile.XCellsize.Equals(scaledOLFIDFFile.XCellsize))
                                {
                                    resultHandler.AddExtraMapFile(scaledOLFIDFFile);
                                }
                                resultHandler.AddExtraMapFile(rivStageIDFFile);
                                resultHandler.AddExtraMapFile(rivBottomIDFFile);
                                resultHandler.AddExtraMapFile(rivConductanceIDFFile);
                                resultHandler.AddExtraMapFile(rivInfFactorIDFFile);
                                if (isgPackage != null)
                                {
                                    string isgFilenamePart = Path.GetFileNameWithoutExtension(rivStageIDFFile.Filename).Replace(rivPackage.PartAbbreviations[RIVPackage.StagePartIdx] + "_", string.Empty);
                                    PackageFile isgPackageFile = isgPackage.GetPackageFile(isgFilenamePart, false, kper);
                                    resultHandler.AddExtraMapFile(isgPackageFile.IMODFile);
                                }
                            }

                            // Write warningfiles and add files to error handler
                            if (rivWarningLayer.HasResults())
                            {
                                rivWarningLayer.CompressLegend(CombinedResultLabel);
                                rivWarningLayer.WriteResultFile(log);
                                resultHandler.AddExtraMapFile(surfacelevelIDFFile);
                                if ((surfacelevelIDFFile != null) && (scaledSurfacelevelIDFFile != null) && !surfacelevelIDFFile.XCellsize.Equals(scaledSurfacelevelIDFFile.XCellsize))
                                {
                                    resultHandler.AddExtraMapFile(scaledSurfacelevelIDFFile);
                                }
                                resultHandler.AddExtraMapFile(rivStageIDFFile);
                                resultHandler.AddExtraMapFile(rivBottomIDFFile);
                                resultHandler.AddExtraMapFile(rivConductanceIDFFile);
                                resultHandler.AddExtraMapFile(rivInfFactorIDFFile);
                                if (isgPackage != null)
                                {
                                    string isgFilenamePart = Path.GetFileNameWithoutExtension(rivStageIDFFile.Filename).Replace(rivPackage.PartAbbreviations[RIVPackage.StagePartIdx] + "_", string.Empty);
                                    PackageFile isgPackageFile = isgPackage.GetPackageFile(isgFilenamePart, false, kper);
                                    resultHandler.AddExtraMapFile(isgPackageFile.IMODFile);
                                }
                            }

                            stageMinSettingIDFFile.ReleaseMemory(false);
                            stageMaxSettingIDFFile.ReleaseMemory(false);
                            stageMinBelowSurfaceSettingIDFFile.ReleaseMemory(false);
                            stageMaxBelowSurfaceSettingIDFFile.ReleaseMemory(false);
                            bottomMinBelowStageSettingIDFFile.ReleaseMemory(false);
                            bottomMaxBelowStageSettingIDFFile.ReleaseMemory(false);
                            hydraulicResistanceMinSettingIDFFile.ReleaseMemory(false);
                            hydraulicResistanceMaxSettingIDFFile.ReleaseMemory(false);
                            infFactorMinSettingIDFFile.ReleaseMemory(false);
                            infFactorMaxSettingIDFFile.ReleaseMemory(true);

                            rivErrorLayer.ReleaseMemory(false);
                            rivWarningLayer.ReleaseMemory(true);

                            HandleEntryPostProcessing(model, rivPackage, checkFileList, entryIdx, ilay, kper, resultHandler.Extent, hasInconsistentRIVFiles, log);

                            rivStageIDFFile.ReleaseMemory(false);
                            rivBottomIDFFile.ReleaseMemory(false);
                            rivConductanceIDFFile.ReleaseMemory(false);
                            rivInfFactorIDFFile.ReleaseMemory(true);

                        }
                    }
                    rivPackage.ReleaseMemory(true);
                }

                DoFinalPostProcessing(model, kper);
                rivPackage.ReleaseMemory(true);
            }
            GC.Collect();
        }

        /// <summary>
        /// Resets internal administration of corrections: to be defined in subclass
        /// </summary>
        protected virtual void HandlePreprocessing()
        {
            // Currently nothing is done here
        }

        /// <summary>
        /// Handke postprocessing after RIV-entry has been checked
        /// </summary>
        /// <param name="model"></param>
        /// <param name="package"></param>
        /// <param name="idfFiles">IDF-files in order as defined for package</param>
        /// <param name="entryIdx"></param>
        /// <param name="ilay"></param>
        /// <param name="kper"></param>
        /// <param name="extent"></param>
        /// <param name="hasInconsistentRIVFiles"<
        /// <param name="log"></param>
        protected virtual void HandleEntryPostProcessing(Model model, Package package, List<IDFFile> idfFiles, int entryIdx, int ilay, int kper, Extent extent, bool hasInconsistentRIVFiles, Log log)
        {
            // Currently nothing is done here
        }

        /// <summary>
        /// Does postprocessing after individual files have been processed and written
        /// </summary>
        /// <param name="model"></param>
        /// <param name="kper"></param>
        protected virtual void DoFinalPostProcessing(Model model, int kper)
        {
            // Currently nothing is done here
        }
    }
}
