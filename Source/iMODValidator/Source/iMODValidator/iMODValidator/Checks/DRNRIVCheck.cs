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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using OrderedPropertyGrid;
using Sweco.SIF.iMOD.IDF;
using Sweco.SIF.Common;
using Sweco.SIF.iMODValidator.Models.Packages;
using Sweco.SIF.iMODValidator.Models;
using Sweco.SIF.iMODValidator.Checks.CheckResults;
using Sweco.SIF.iMODValidator.Models.Packages.Files;
using Sweco.SIF.iMODValidator.Models.Files;
using Sweco.SIF.iMOD.Legends;

namespace Sweco.SIF.iMODValidator.Checks
{
    [TypeConverter(typeof(PropertySorter))]
    class DRNRIVCheckSettings : CheckSettings
    {
        private UpscaleMethodEnum upscaleMethod;
        private bool isISGConverted;

        [Category("RIV-properties"), Description("Should ISG-files be converted and checked with the RIV-check?"), PropertyOrder(10)]
        public bool IsISGConverted
        {
            get { return isISGConverted; }
            set { isISGConverted = value; }
        }

        [Category("Scale-properties"), Description("Upscale method in case of resolution differences. Use 'Minimum' to only find coarse cells with problem for all finer cells inside"), PropertyOrder(10)]
        public UpscaleMethodEnum UpscaleMethod
        {
            get { return upscaleMethod; }
            set { upscaleMethod = value; }
        }

        public DRNRIVCheckSettings(string checkName) : base(checkName)
        {
            isISGConverted = true;
            upscaleMethod = UpscaleMethodEnum.Minimum;
        }

        public override void LogSettings(Log log, int logIndentLevel = 0)
        {
            log.AddInfo("IsISGConverted: " + isISGConverted, logIndentLevel);
            log.AddInfo("Upscale method: " + upscaleMethod.ToString(), logIndentLevel);
        }
    }

    class DRNRIVCheck : Check
    {
        public override string Abbreviation
        {
            get { return "DRN-RIV"; }
        }

        public override string Description
        {
            get { return "Checks drainlevel versus RIV-systems per drainagesystem"; }
        }

        private DRNRIVCheckSettings settings;
        public override CheckSettings Settings
        {
            get { return settings; }
            set
            {
                if (value is DRNRIVCheckSettings)
                {
                    settings = (DRNRIVCheckSettings)value;
                }
            }
        }

        private CheckWarning LevelBelowRIVStageWarning;
        private IDFLegend warningLegend;

        public DRNRIVCheck()
        {
            settings = new DRNRIVCheckSettings(this.Name);

            // Define warnings (currently no errors are defined)
            LevelBelowRIVStageWarning = new CheckWarning(1, "DRN-level below RIV-stage", "The drainlevel is below a RIV-stage level.");

            warningLegend = new IDFLegend("Legend for DRN-RIV check");
            warningLegend.AddClass(new ValueLegendClass(0, "No warnings found.", Color.White));
            warningLegend.AddClass(LevelBelowRIVStageWarning.CreateLegendValueClass(Color.Red));
            // warningLegend.AddDefaultUpperRangeClass();
            // warningLegend.AddInbetweenClasses(CombinedResultLabel, true);
        }

        public override void Run(Model model, CheckResultHandler resultHandler, Log log)
        {
            try
            {
                log.AddInfo("Checking DRN- and RIV-packages ...");
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

            // Check if DRN_L1 is used as OLF
            bool useDRNL1OLF = false;
            IDFPackage olfPackage = (IDFPackage)model.GetPackage(OLFPackage.DefaultKey);
            if ((olfPackage == null) || (!olfPackage.IsActive))
            {
                OLFCheck olfCheck = (OLFCheck)CheckManager.Instance.RetrieveCheck(typeof(OLFCheck));
                if (olfCheck != null)
                {
                    useDRNL1OLF = ((OLFCheckSettings)olfCheck.Settings).UseDRN_L1;
                    if (useDRNL1OLF)
                    {
                        log.AddInfo("OLF-check is defined to use DRN_L1 as OLF. DRN_L1 is skipped for the DRN-check...", 2);
                    }
                }
            }

            // retrieve RIV-package (if present) for comparison of levels
            IDFPackage rivPackage = (IDFPackage)model.GetPackage(RIVPackage.DefaultKey);
            ISGPackage isgPackage = (ISGPackage)model.GetPackage(ISGPackage.DefaultKey);
            if (!IsPackageActive(rivPackage))
            {
                if (!IsPackageActive(isgPackage, "ISG/RIV", log, 1))
                {
                    return;
                }
            }

            IDFPackage isgRIVPackage = null;
            if ((settings.IsISGConverted) && (isgPackage != null) && ISGRIVConverter.HasISGPackageEntries(isgPackage, model, resultHandler))
            {
                log.AddInfo("Converting ISG-files to RIV-files and applying RIV-check", 1);
                isgRIVPackage = ISGRIVConverter.ConvertISGtoRIVPackage(isgPackage, "ISGRIV", model, resultHandler, log, 2);
            }

            int maxRivEntryCount = 0;
            int maxDrnEntryCount = 0;
            for (int kper = resultHandler.MinKPER; kper <= model.NPER; kper++)
            {
                int rivEntryCount = rivPackage.GetEntryCount(kper);
                int drnEntryCount = drnPackage.GetEntryCount(kper);
                if (rivEntryCount > maxRivEntryCount)
                {
                    maxRivEntryCount = rivEntryCount;
                }
                if (drnEntryCount > maxDrnEntryCount)
                {
                    maxDrnEntryCount = drnEntryCount;
                }
            }
            List<string> drnRivPartAbbreviations = new List<string>();
            for (int entryIdx = 1; entryIdx <= maxRivEntryCount; entryIdx++)
            {
                drnRivPartAbbreviations.Add("RIV" + entryIdx);
            }
            CustomPackage drnRivPackage = new CustomPackage(Abbreviation, null, maxDrnEntryCount * maxRivEntryCount, drnRivPartAbbreviations.ToArray());

            float levelErrorMargin = resultHandler.LevelErrorMargin;
            int prevValidDRNKPER = 0;
            int prevValidRIVKPER = 0;
            List<List<IDFFile>> checkedFileLists = new List<List<IDFFile>>();
            for (int kper = resultHandler.MinKPER; (kper <= model.NPER) && (kper <= resultHandler.MaxKPER); kper++)
            {
                StressPeriod stressPeriod = model.RetrieveStressPeriod(kper);
                DoDRNRIVCheck(drnPackage, rivPackage, drnRivPackage, stressPeriod, model, resultHandler, checkedFileLists, useDRNL1OLF, prevValidDRNKPER, prevValidRIVKPER, levelErrorMargin, log);
            }

            if ((settings.IsISGConverted) && (isgPackage != null) && (isgRIVPackage != null) && isgRIVPackage.HasPackageFiles())
            {
                log.AddInfo("Checking converted ISG-files ... ", 1);
                int orgMinEntryNumber = resultHandler.MinEntryNumber;
                int orgMaxEntryNumber = resultHandler.MaxEntryNumber;
                resultHandler.MinEntryNumber = 1;
                resultHandler.MaxEntryNumber = 999;
                string orgDRNPackageKey = drnPackage.Key;
                drnRivPackage.Key = drnRivPackage.Key + "ISG";
                for (int kper = resultHandler.MinKPER; (kper <= model.NPER) && (kper <= resultHandler.MaxKPER); kper++)
                {
                    StressPeriod stressPeriod = model.RetrieveStressPeriod(kper);
                    DoDRNRIVCheck(drnPackage, isgRIVPackage, drnRivPackage, stressPeriod, model, resultHandler, checkedFileLists, useDRNL1OLF, prevValidDRNKPER, prevValidRIVKPER, levelErrorMargin, log, isgPackage);
                }
                resultHandler.MinEntryNumber = orgMinEntryNumber;
                resultHandler.MaxEntryNumber = orgMaxEntryNumber;
                drnPackage.Key = orgDRNPackageKey;
            }
        }

        public void DoDRNRIVCheck(IDFPackage drnPackage, IDFPackage rivPackage, CustomPackage drnRivPackage, StressPeriod stressPeriod, Model model, CheckResultHandler resultHandler, List<List<IDFFile>> checkedFileLists, bool useDRNL1OLF, int prevValidDRNKPER, int prevValidRIVKPER, float levelErrorMargin, Log log, ISGPackage isgPackage = null)
        {
            int kper = stressPeriod.KPER;
            if ((drnPackage.GetEntryCount(kper) > 0) || (rivPackage.GetEntryCount(kper) > 0))
            {
                if (model.NPER > 1)
                {
                    log.AddInfo("Checking stress period " + stressPeriod.KPER + " " + stressPeriod.SNAME + " ...", 1);
                }
                else
                {
                    log.AddInfo("Checking stress period " + stressPeriod.KPER + " " + stressPeriod.SNAME + " ...", 1);
                }

                int drnFirstLayerIdx = 0;
                if (useDRNL1OLF)
                {
                    drnFirstLayerIdx = 1;
                }

                // Retrieve timestep for which the DRN-files have been defined that correspond with this timestep
                int drnKPER = kper;
                if (drnPackage.GetEntryCount(kper) <= 0)
                {
                    drnKPER = prevValidDRNKPER;
                }
                else
                {
                    prevValidDRNKPER = drnKPER;
                }

                // Use last valid RIV-timestep in case of undefined RIV-timesteps
                int rivKPER = kper;
                if (rivPackage.GetEntryCount(kper) <= 0)
                {
                    rivKPER = prevValidRIVKPER;
                }
                else
                {
                    prevValidRIVKPER = rivKPER;
                }

                // Process all DRN-entries
                for (int drnEntryIdx = drnFirstLayerIdx; drnEntryIdx < drnPackage.GetEntryCount(drnKPER); drnEntryIdx++)
                {
                    log.AddInfo("Checking DRN entry " + (drnEntryIdx + 1) + " with " + Name + " ...", 1);

                    // Retrieve IDF files for current layer
                    PackageFile drnLevelPackageIDFFile = drnPackage.GetPackageFile(drnEntryIdx, DRNPackage.LevelPartIdx, drnKPER);
                    int ilay = drnLevelPackageIDFFile.ILAY;
                    IDFFile drnLevelIDFFile = drnPackage.GetIDFFile(drnEntryIdx, DRNPackage.LevelPartIdx, drnKPER);
                    if ((drnLevelIDFFile == null))
                    {
                        log.AddError("Drainlevel file is missing, DRN-check is skipped", 1);
                    }
                    else
                    {
                        // Create warning IDF-files for current layer
                        CheckWarningLayer drnWarningLayer = CreateWarningLayer(resultHandler, drnRivPackage, "SYS" + (drnEntryIdx + 1), stressPeriod, drnEntryIdx + 1, drnLevelIDFFile.XCellsize, warningLegend);
                        drnWarningLayer.AddSourceFile(drnLevelIDFFile);

                        // Process all RIV-layers/systems
                        for (int rivEntryIdx = 0; rivEntryIdx < rivPackage.GetEntryCount(rivKPER); rivEntryIdx++)
                        {
                            bool hasRivWarnings = false;
                            log.AddInfo("Checking DRN entry " + (drnEntryIdx + 1) + " versus RIV entry " + (rivEntryIdx + 1) + " ...", 2);
                            IDFFile rivStageIDFFile = rivPackage.GetIDFFile(rivEntryIdx, RIVPackage.StagePartIdx, rivKPER);

                            if (rivStageIDFFile != null)
                            {
                                // Prevent checking the same fileset twice
                                List<IDFFile> checkFileList = new List<IDFFile>() { drnLevelIDFFile, rivStageIDFFile };
                                if (IsIDFFileListInLists(checkedFileLists, checkFileList))
                                {
                                    log.AddInfo("files have been checked already: " + System.IO.Path.GetFileName(drnLevelIDFFile.Filename) + "," + System.IO.Path.GetFileName(rivStageIDFFile.Filename), 3);
                                }
                                else
                                {
                                    IDFFile rivCondIDFFile = rivPackage.GetIDFFile(rivEntryIdx, RIVPackage.ConductancePartIdx, rivKPER);
                                    IDFFile rivBottomIDFFile = rivPackage.GetIDFFile(rivEntryIdx, RIVPackage.BottomPartIdx, rivKPER);
                                    IDFFile rivInffactorIDFFile = rivPackage.GetIDFFile(rivEntryIdx, RIVPackage.InfFactorPartIdx, rivKPER);
                                    IDFUpscaler rivStageUpscaler = new IDFUpscaler(rivStageIDFFile, settings.UpscaleMethod, drnLevelIDFFile.Extent, FileUtils.EnsureFolderExists(GetIMODFilesPath(model), Name), log, 3);
                                    IDFUpscaler rivCondUpscaler = new IDFUpscaler(rivCondIDFFile, settings.UpscaleMethod, drnLevelIDFFile.Extent, FileUtils.EnsureFolderExists(GetIMODFilesPath(model), Name), log, 3);
                                    IDFUpscaler rivBottomUpscaler = new IDFUpscaler(rivBottomIDFFile, settings.UpscaleMethod, drnLevelIDFFile.Extent, FileUtils.EnsureFolderExists(GetIMODFilesPath(model), Name), log, 3);
                                    IDFUpscaler rivInffactorUpscaler = new IDFUpscaler(rivInffactorIDFFile, settings.UpscaleMethod, drnLevelIDFFile.Extent, FileUtils.EnsureFolderExists(GetIMODFilesPath(model), Name), log, 3);
                                    IDFFile scaledRIVStageIDFFile = rivStageUpscaler.RetrieveIDFFile(drnLevelIDFFile.XCellsize, drnLevelIDFFile.Extent);
                                    IDFFile scaledRIVCondIDFFile = rivCondUpscaler.RetrieveIDFFile(drnLevelIDFFile.XCellsize, drnLevelIDFFile.Extent);
                                    IDFFile scaledRIVBottomIDFFile = rivBottomUpscaler.RetrieveIDFFile(drnLevelIDFFile.XCellsize, drnLevelIDFFile.Extent);
                                    IDFFile scaledRIVInffactorIDFFile = rivInffactorUpscaler.RetrieveIDFFile(drnLevelIDFFile.XCellsize, drnLevelIDFFile.Extent);

                                    IDFCellIterator idfCellIterator = new IDFCellIterator(resultHandler.Extent);
                                    idfCellIterator.AddIDFFile(drnLevelIDFFile);
                                    idfCellIterator.AddIDFFile(scaledRIVStageIDFFile);
                                    idfCellIterator.AddIDFFile(scaledRIVCondIDFFile);
                                    idfCellIterator.AddIDFFile(scaledRIVBottomIDFFile);
                                    idfCellIterator.AddIDFFile(scaledRIVInffactorIDFFile);

                                    // Iterate through cells
                                    idfCellIterator.Reset();
                                    while (idfCellIterator.IsInsideExtent())
                                    {
                                        float drnLevelValue = idfCellIterator.GetCellValue(drnLevelIDFFile);
                                        float x = idfCellIterator.X;
                                        float y = idfCellIterator.Y;

                                        if (!drnLevelValue.Equals(drnLevelIDFFile.NoDataValue))
                                        {
                                            // Check drain level relative to currently checked available RIV-stage
                                            float rivStageValue = idfCellIterator.GetCellValue(scaledRIVStageIDFFile);
                                            float rivCondValue = idfCellIterator.GetCellValue(scaledRIVCondIDFFile);
                                            float rivBottomValue = idfCellIterator.GetCellValue(scaledRIVBottomIDFFile);
                                            float rivInffactorValue = idfCellIterator.GetCellValue(scaledRIVInffactorIDFFile);

                                            if (!rivStageValue.Equals(float.NaN) && !rivStageValue.Equals(scaledRIVStageIDFFile.NoDataValue)
                                                && !rivCondValue.Equals(float.NaN) && !rivCondValue.Equals(scaledRIVCondIDFFile.NoDataValue)
                                                && !rivBottomValue.Equals(float.NaN) && !rivBottomValue.Equals(scaledRIVBottomIDFFile.NoDataValue)
                                                && !rivInffactorValue.Equals(float.NaN) && !rivInffactorValue.Equals(scaledRIVInffactorIDFFile.NoDataValue))
                                            {
                                                // Only check infiltrating surface water
                                                if (rivStageValue > rivBottomValue + levelErrorMargin)
                                                {
                                                    if (rivStageValue > drnLevelValue + levelErrorMargin)
                                                    {
                                                        resultHandler.AddCheckResult(drnWarningLayer, x, y, LevelBelowRIVStageWarning);
                                                        hasRivWarnings = true;
                                                    }
                                                }
                                            }
                                        }
                                        idfCellIterator.MoveNext();
                                    }

                                    // Write warning files and add files to error handler
                                    if (hasRivWarnings)
                                    {
                                        resultHandler.AddExtraMapFile(drnLevelIDFFile);
                                        resultHandler.AddExtraMapFile(rivStageIDFFile);
                                        resultHandler.AddExtraMapFile(rivBottomIDFFile);
                                        resultHandler.AddExtraMapFile(rivCondIDFFile);
                                        resultHandler.AddExtraMapFile(rivInffactorIDFFile);
                                        if ((rivStageIDFFile != null) && (scaledRIVStageIDFFile != null) && !scaledRIVStageIDFFile.XCellsize.Equals(rivStageIDFFile.XCellsize))
                                        {
                                            resultHandler.AddExtraMapFile(scaledRIVStageIDFFile);
                                        }
                                        if (isgPackage != null)
                                        {
                                            string isgFilenamePart = Path.GetFileNameWithoutExtension(rivStageIDFFile.Filename).Replace(rivPackage.PartAbbreviations[RIVPackage.StagePartIdx] + "_", string.Empty);
                                            PackageFile isgPackageFile = isgPackage.GetPackageFile(isgFilenamePart, false, kper);
                                            resultHandler.AddExtraMapFile(isgPackageFile.IMODFile);
                                        }

                                        rivCondIDFFile.ReleaseMemory(true);
                                        rivBottomIDFFile.ReleaseMemory(true);
                                        rivInffactorIDFFile.ReleaseMemory(true);
                                        scaledRIVStageIDFFile.ReleaseMemory(true);
                                        scaledRIVCondIDFFile.ReleaseMemory(true);
                                        scaledRIVBottomIDFFile.ReleaseMemory(true);
                                        scaledRIVInffactorIDFFile.ReleaseMemory(true);
                                    }

                                    checkedFileLists.Add(checkFileList);
                                }

                                rivStageIDFFile.ReleaseMemory(true);
                            }
                        }

                        // Write warning files and add files to error handler
                        if (drnWarningLayer.HasResults())
                        {
                            drnWarningLayer.CompressLegend(CombinedResultLabel);
                            drnWarningLayer.WriteResultFile(log);
                        }

                        drnWarningLayer.ReleaseMemory(true);
                        drnPackage.ReleaseMemory(true);
                    }
                }
                drnPackage.ReleaseMemory(true);
                rivPackage.ReleaseMemory(true);
            }
        }
    }
}
