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

namespace Sweco.SIF.iMODValidator.Checks
{
    [TypeConverter(typeof(PropertySorter))]
    class DRNCheckSettings : CheckSettings
    {
        private string minLevel;
        private string maxLevel;
        private string minConductance;
        private string maxConductance;
        private UpscaleMethodEnum upscaleMethod;

        [Category("Warning-properties"), Description("The minimum valid drainlevel for this region"), PropertyOrder(20)]
        public string MinLevel
        {
            get { return minLevel; }
            set { minLevel = value; }
        }
        [Category("Warning-properties"), Description("The maximum valid drainlevel for this region"), PropertyOrder(21)]
        public string MaxLevel
        {
            get { return maxLevel; }
            set { maxLevel = value; }
        }
        [Category("Warning-properties"), Description("The minimum valid drainage conductance for this region"), PropertyOrder(22)]
        public string MinConductance
        {
            get { return minConductance; }
            set { minConductance = value; }
        }
        [Category("Warning-properties"), Description("The maximum valid drainage conductance for this region"), PropertyOrder(23)]
        public string MaxConductance
        {
            get { return maxConductance; }
            set { maxConductance = value; }
        }
        [Category("Scale-properties"), Description("Upscale method in case of resolution differences. Use 'Maximum' to only find coarse cells with problem for all finer cells inside"), PropertyOrder(30)]
        public UpscaleMethodEnum UpscaleMethod
        {
            get { return upscaleMethod; }
            set { upscaleMethod = value; }
        }

        public DRNCheckSettings(string checkName) : base(checkName)
        {
            minLevel = "-10";
            maxLevel = "500";
            minConductance = (0.001f).ToString();
            maxConductance = "10000";
            upscaleMethod = UpscaleMethodEnum.Maximum;
        }

        public override void LogSettings(Log log, int logIndentLevel = 0)
        {
            log.AddInfo("Minimum level: " + minLevel + " mNAP", logIndentLevel);
            log.AddInfo("Maximum level: " + maxLevel + " mNAP", logIndentLevel);
            log.AddInfo("MinConductance: " + MinConductance, logIndentLevel);
            log.AddInfo("MaxConductance: " + MaxConductance, logIndentLevel);
            log.AddInfo("Upscale method: " + upscaleMethod.ToString(), logIndentLevel);
        }
    }

    class DRNCheck : Check
    {
        public override string Abbreviation
        {
            get { return "DRN"; }
        }

        public override string Description
        {
            get { return "Checks drainlevel and conductance per drainagesystem"; }
        }

        private DRNCheckSettings settings;
        public override CheckSettings Settings
        {
            get { return settings; }
            set
            {
                if (value is DRNCheckSettings)
                {
                    settings = (DRNCheckSettings)value;
                }
            }
        }

        private CheckError InconsistentDRNFilesError1;
        private CheckError LevelAboveOLFError2;
        private CheckError NegativeConductanceError3;
        private CheckWarning ConductanceRangeWarning1;
        private CheckWarning LevelRangeWarning2;
        private CheckWarning LevelAboveSurfaceWarning3;
        private IDFLegend errorLegend;
        private IDFLegend warningLegend;

        public DRNCheck()
        {
            settings = new DRNCheckSettings(this.Name);

            // Define errors
            InconsistentDRNFilesError1 = new CheckError(1, "Inconsistent DRN-files", "Inconsistent DRN-files");
            LevelAboveOLFError2 = new CheckError(2, "Level above OLF", "The drainlevel is above the OLF level.");
            NegativeConductanceError3 = new CheckError(4, "Unexpected negative conductance");

            errorLegend = new IDFLegend("Legend for DRN-file check");
            errorLegend.AddClass(new ValueLegendClass(0, "No errors found.", Color.White));
            errorLegend.AddClass(InconsistentDRNFilesError1.CreateLegendValueClass(Color.Orange));
            errorLegend.AddClass(LevelAboveOLFError2.CreateLegendValueClass(Color.Red));
            errorLegend.AddClass(NegativeConductanceError3.CreateLegendValueClass(Color.DarkRed));
            errorLegend.AddUpperRangeClass(CombinedResultLabel, true);
            errorLegend.AddInbetweenClasses(CombinedResultLabel, true);

            // Define warnings
            ConductanceRangeWarning1 = new CheckWarning(1, "Conductance outside range", "Conductance outside the expected range ["
                + settings.MinConductance.ToString() + "," + settings.MaxConductance.ToString() + "]");
            LevelRangeWarning2 = new CheckWarning(2, "Level outside range", "Drainlevel outside the expected range ["
                + settings.MinLevel.ToString() + "," + settings.MaxLevel.ToString() + "]");
            LevelAboveSurfaceWarning3 = new CheckWarning(4, "Level above surface level", "The level value is above the surface level.");

            warningLegend = new IDFLegend("Legend for DRN-file check");
            warningLegend.AddClass(new ValueLegendClass(0, "No warnings found", Color.White));
            warningLegend.AddClass(ConductanceRangeWarning1.CreateLegendValueClass(Color.Orange, true));
            warningLegend.AddClass(LevelRangeWarning2.CreateLegendValueClass(Color.Red, true));
            warningLegend.AddClass(LevelAboveSurfaceWarning3.CreateLegendValueClass(Color.Blue, true));
            warningLegend.AddUpperRangeClass(CombinedResultLabel, true);
            warningLegend.AddInbetweenClasses(CombinedResultLabel, true);
        }

        public override void Run(Model model, CheckResultHandler resultHandler, Log log)
        {
            try
            {
                log.AddInfo("Checking DRN-package ...");
                settings.LogSettings(log, 1);
                RunDRNCheck1(model, resultHandler, log);
            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error in " + this.Name, ex);
            }
        }

        protected virtual void RunDRNCheck1(Model model, CheckResultHandler resultHandler, Log log)
        {
            // Retrieve DRN-package
            IDFPackage drnPackage = (IDFPackage)model.GetPackage(DRNPackage.DefaultKey);
            if (!IsPackageActive(drnPackage, DRNPackage.DefaultKey, log, 1))
            {
                return;
            }

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
                        log.AddInfo("OLF-check is defined to use DRN_L1 as OLF. DRN_L1 is skipped for the DRN-check...", 2);
                        olfIDFFile = drnPackage.GetIDFFile(0, DRNPackage.LevelPartIdx);
                    }
                    else
                    {
                        log.AddWarning(drnPackage.Key, model.RUNFilename, "No OLF-file defined for this model, OLF-checks are skipped ...", 2);
                    }
                }
            }
            IDFUpscaler olfUpscaler = new IDFUpscaler(olfIDFFile, settings.UpscaleMethod, resultHandler.Extent, GetIMODFilesPath(model), log, 2);

            // retrieve non-package surface level file
            IDFFile surfacelevelIDFFile = model.RetrieveSurfaceLevelFile(log, 1);
            if (surfacelevelIDFFile == null)
            {
                log.AddWarning("Maaiveld", null, "No surface level file defined, surface level check is skipped.", 1);
            }
            IDFUpscaler surfacelevelUpscaler = new IDFUpscaler(surfacelevelIDFFile, settings.UpscaleMethod, resultHandler.Extent, GetIMODFilesPath(model), log, 2);

            double levelErrorMargin = resultHandler.LevelErrorMargin;
            bool isSurfaceLevelNoDataWarningShown = false;

            foreach (int kper in drnPackage.StressPeriods(model, log, 1))
            {
                if ((kper >= resultHandler.MinKPER) && (kper <= resultHandler.MaxKPER))
                {
                    StressPeriod stressPeriod = model.RetrieveStressPeriod(kper);

                    int drnFirstLayerIdx = 0;
                    if (useDRNL1OLF)
                    {
                        drnFirstLayerIdx = 1;
                    }
                    // Process all DRN-entries
                    for (int drnEntryIdx = drnFirstLayerIdx; drnEntryIdx < drnPackage.GetEntryCount(kper); drnEntryIdx++)
                    {
                        log.AddInfo("Checking entry " + (drnEntryIdx + 1) + " with " + Name + " ...", 1);

                        // Retrieve IDF files for current layer
                        IDFFile drnConductanceIDFFile = drnPackage.GetIDFFile(drnEntryIdx, DRNPackage.ConductancePartIdx, kper);
                        IDFFile drnLevelIDFFile = drnPackage.GetIDFFile(drnEntryIdx, DRNPackage.LevelPartIdx, kper);
                        if ((drnConductanceIDFFile == null) || (drnLevelIDFFile == null))
                        {
                            log.AddError(drnPackage.Key, model.RUNFilename, "One or more DRN-files are missing, DRN-check is skipped");
                        }
                        else
                        {
                            // Prevent checking the same fileset twice
                            List<IDFFile> checkFileList = new List<IDFFile>() { drnConductanceIDFFile, drnLevelIDFFile };
                            int constantFileCount = IDFUtils.GetConstantIDFFileCount(checkFileList);
                            if (drnPackage.IsFileListPresent(checkFileList, resultHandler.MinKPER, kper))
                            {
                                log.AddInfo("files have been checked already", 2);
                            }
                            else
                            {
                                // Retrieve a surfacelevelfile with the same or coarser resolution than the DRN-file(s)
                                IDFFile scaledSurfacelevelIDFFile = surfacelevelUpscaler.RetrieveIDFFile(drnLevelIDFFile.XCellsize);
                                IDFFile scaledOLFIDFFile = olfUpscaler.RetrieveIDFFile(drnLevelIDFFile.XCellsize);

                                IDFCellIterator idfCellIterator = new IDFCellIterator(resultHandler.Extent);
                                idfCellIterator.AddIDFFile(drnConductanceIDFFile);
                                idfCellIterator.AddIDFFile(drnLevelIDFFile);

                                // Check that DRN-files have equal extent
                                idfCellIterator.CheckExtent(log, 2, LogLevel.Debug);
                                if (idfCellIterator.IsEmptyExtent())
                                {
                                    log.AddInfo("Check extent of DRN-files and/or surface level file", 2);
                                    return;
                                }
                                else
                                {
                                    idfCellIterator.AddIDFFile(scaledSurfacelevelIDFFile);
                                    idfCellIterator.AddIDFFile(scaledOLFIDFFile);

                                    // Create error IDFfiles for current layer
                                    CheckErrorLayer drnErrorLayer = CreateErrorLayer(resultHandler, drnPackage, "SYS" + (drnEntryIdx + 1), stressPeriod, drnEntryIdx + 1, idfCellIterator.XStepsize, errorLegend);
                                    drnErrorLayer.AddSourceFiles(idfCellIterator.GetIDFFiles());

                                    // Create warning IDFfiles for current layer
                                    CheckWarningLayer drnWarningLayer = CreateWarningLayer(resultHandler, drnPackage, "SYS" + (drnEntryIdx + 1), stressPeriod, drnEntryIdx + 1, idfCellIterator.XStepsize, warningLegend);
                                    drnWarningLayer.AddSourceFiles(idfCellIterator.GetIDFFiles());

                                    // Iterate through cells
                                    idfCellIterator.Reset();
                                    while (idfCellIterator.IsInsideExtent())
                                    {
                                        float surfacelevelValue = idfCellIterator.GetCellValue(scaledSurfacelevelIDFFile);
                                        float olfValue = idfCellIterator.GetCellValue(scaledOLFIDFFile);
                                        float drnLevelValue = idfCellIterator.GetCellValue(drnLevelIDFFile);
                                        float drnConductanceValue = idfCellIterator.GetCellValue(drnConductanceIDFFile);
                                        float x = idfCellIterator.X;
                                        float y = idfCellIterator.Y;

                                        float dataCount = 0;
                                        if (!drnLevelValue.Equals(drnLevelIDFFile.NoDataValue))
                                        {
                                            dataCount++;
                                            // Check drain level relative to surface level
                                            if ((scaledSurfacelevelIDFFile != null) && (!surfacelevelValue.Equals(scaledSurfacelevelIDFFile.NoDataValue)))
                                            {
                                                if (drnLevelValue > (surfacelevelValue + levelErrorMargin))
                                                {
                                                    resultHandler.AddCheckResult(drnWarningLayer, x, y, LevelAboveSurfaceWarning3);
                                                }
                                            }
                                            else if (!isSurfaceLevelNoDataWarningShown)
                                            {
                                                log.AddWarning("Surfacelevel has NoData-value for one or more DRN-cells, DRN-surfacelevel check skipped in these cases", 2);
                                                isSurfaceLevelNoDataWarningShown = true;
                                            }

                                            if ((scaledOLFIDFFile != null) && !olfValue.Equals(scaledOLFIDFFile.NoDataValue) && (drnLevelValue > (olfValue + levelErrorMargin)))
                                            {
                                                resultHandler.AddCheckResult(drnErrorLayer, x, y, LevelAboveOLFError2);
                                            }
                                        }
                                        if (!drnConductanceValue.Equals(drnConductanceIDFFile.NoDataValue))
                                        {
                                            dataCount++;
                                        }
                                        if (dataCount == drnPackage.MaxPartCount)
                                        {
                                            if (drnConductanceValue < 0)
                                            {
                                                resultHandler.AddCheckResult(drnErrorLayer, x, y, NegativeConductanceError3);
                                            }
                                        }
                                        else
                                        {
                                            // correct for constantFiles: since constant file have a value everywhere, correct for cells that have no other DRN-values defined
                                            if (constantFileCount > 0)
                                            {
                                                dataCount -= constantFileCount;
                                            }
                                            if ((dataCount > 0) && (dataCount < drnPackage.MaxPartCount))
                                            {
                                                resultHandler.AddCheckResult(drnErrorLayer, x, y, InconsistentDRNFilesError1);
                                            }
                                        }

                                        idfCellIterator.MoveNext();
                                    }

                                    // Write errorfiles and add files to error handler
                                    if (drnErrorLayer.HasResults())
                                    {
                                        drnErrorLayer.CompressLegend(CombinedResultLabel);
                                        drnErrorLayer.WriteResultFile(log);
                                        resultHandler.AddExtraMapFile(drnLevelIDFFile);
                                        resultHandler.AddExtraMapFile(drnConductanceIDFFile);
                                        resultHandler.AddExtraMapFile(surfacelevelIDFFile);
                                        resultHandler.AddExtraMapFile(scaledSurfacelevelIDFFile);
                                        resultHandler.AddExtraMapFile(olfIDFFile);
                                        resultHandler.AddExtraMapFile(scaledOLFIDFFile);
                                    }

                                    // Write warningfiles
                                    if ((drnWarningLayer != null) && (drnWarningLayer.HasResults()))
                                    {
                                        drnWarningLayer.CompressLegend(CombinedResultLabel);
                                        drnWarningLayer.WriteResultFile(log);
                                        resultHandler.AddExtraMapFile(drnLevelIDFFile);
                                        resultHandler.AddExtraMapFile(drnConductanceIDFFile);
                                    }

                                    drnErrorLayer.ReleaseMemory(true);
                                    drnPackage.ReleaseMemory(true);
                                }
                            }
                        }
                    }
                    drnPackage.ReleaseMemory(true);
                }
            }
            if (olfIDFFile != null)
            {
                olfIDFFile.ReleaseMemory(true);
            }
            if (surfacelevelIDFFile != null)
            {
                surfacelevelIDFFile.ReleaseMemory(true);
            }
        }
    }
}
