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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Checks
{
    [TypeConverter(typeof(PropertySorter))]
    class BNDCheckSettings : CheckSettings
    {
        private string minLevel;
        private string maxLevel;
        private UpscaleMethodEnum upscaleMethod;

        [Category("Scale-properties"), Description("Upscale method in case of resolution differences. Use 'Minimum' to only find coarse cells with problem for all finer cells inside"), PropertyOrder(20)]
        public UpscaleMethodEnum UpscaleMethod
        {
            get { return upscaleMethod; }
            set { upscaleMethod = value; }
        }

        [Category("Warning-properties"), Description("The minimum valid BND-value for this region"), PropertyOrder(30)]
        public string MinValue
        {
            get { return minLevel; }
            set { minLevel = value; }
        }
        [Category("Warning-properties"), Description("The maximum valid BND-value for this region"), PropertyOrder(31)]
        public string MaxValue
        {
            get { return maxLevel; }
            set { maxLevel = value; }
        }

        public BNDCheckSettings(string checkName) : base(checkName)
        {
            minLevel = "-2";
            maxLevel = "2";
            upscaleMethod = UpscaleMethodEnum.Minimum;
        }

        public override void LogSettings(Log log, int logIndentLevel = 0)
        {
            log.AddInfo("Minimum BND value: " + minLevel, logIndentLevel);
            log.AddInfo("Maximum BND value: " + maxLevel, logIndentLevel);
            log.AddInfo("Upscale method: " + upscaleMethod.ToString(), logIndentLevel);
        }
    }

    class BNDCheck : Check
    {
        public override string Abbreviation
        {
            get { return "BND"; }
        }

        public override string Description
        {
            get { return "Checks BND value and if SHD and CHD are defined for boundary cells"; }
        }

        private BNDCheckSettings settings;
        public override CheckSettings Settings
        {
            get { return settings; }
            set
            {
                if (value is BNDCheckSettings)
                {
                    settings = (BNDCheckSettings)value;
                }
            }
        }

        public BNDCheck()
        {
            settings = new BNDCheckSettings(this.Name);
        }

        private Log log;
        private Model model;
        private IDFPackage chdPackage = null;
        private IDFPackage fhbPackage = null;
        private IDFPackage bndPackage = null;
        private IDFPackage shdPackage = null;

        private CheckError SHDNoDataError { get; set; }
        private CheckError CHDNoDataError { get; set; }
        private CheckError InvalidBNDError { get; set; }
        private CheckError FHBNoDataError { get; set; }
        private IDFLegend errorLegend = new IDFLegend("Legend for BND-file check");

        private CheckWarning BNDValueRangeWarning { get; set; }
        private IDFLegend warningLegend = new IDFLegend("Legend for BND-file check");

        private IDFFile MinBNDSettingIDFFile { get; set; }
        private IDFFile MaxBNDSettingIDFFile { get; set; }

        public override void Run(Model model, CheckResultHandler resultHandler, Log log)
        {
            log.AddInfo("Checking BND-package ...");

            bndPackage = (BNDPackage)model.GetPackage(BNDPackage.DefaultKey);
            if ((bndPackage == null) || !bndPackage.IsActive)
            {
                log.AddWarning(this.Name, model.Runfilename, "BND-package is not active. " + this.Name + " is skipped.", 1);
                return;
            }

            settings.LogSettings(log, 1);
            RunBNDCheck1(model, resultHandler, log);
        }
        /// <summary>
        /// Checks for: 1) BND value within range and integer, 2) SHD/CHD defined for BND=-1 value, 3) FHB defined for BND=-2 value, 4) CHD-values for inactive cells
        /// </summary>
        /// <param name="model"></param>
        /// <param name="resultHandler"></param>
        /// <param name="log"></param>
        private void RunBNDCheck1(Model model, CheckResultHandler resultHandler, Log log)
        {
            // Define results, errors and warnings
            Initialize(model, log);

            // Retrieve other packages 
            shdPackage = (IDFPackage)model.GetPackage(SHDPackage.DefaultKey);
            chdPackage = (IDFPackage)model.GetPackage(CHDPackage.DefaultKey);
            fhbPackage = (IDFPackage)model.GetPackage(FHBPackage.DefaultKey);

            // When SHD-package is missing, use IDF-files from CHD-package
            if (shdPackage == null)
            {
                log.AddWarning("SHD-package is missing, using IDF-files from CHD-package for SHD-check");
                shdPackage = chdPackage;
            }
            // Retrieve settingfiles 
            RetrieveSettings();

            // Process all BND-entries
            int kper = 0;
            for (int entryIdx = resultHandler.MinEntryNumber - 1; (entryIdx < bndPackage.GetEntryCount(kper)) && (entryIdx < resultHandler.MaxEntryNumber); entryIdx++)
            {
                log.AddInfo("Checking entry " + (entryIdx + 1) + " with " + Name + " ...", 1);

                //BND check for value within range and check for SHD
                IDFFile bndIDFFile = bndPackage.GetIDFFile(entryIdx);
                IDFFile SHDIDFFile;
                try
                {
                    SHDIDFFile = shdPackage.GetIDFFile(entryIdx);
                }
                catch
                {
                    throw new ToolException("No SHD IDF-file found for entry: " + (entryIdx + 1));
                }

                double levelErrorMargin = resultHandler.LevelErrorMargin;
                IDFCellIterator idfCellIterator = new IDFCellIterator(resultHandler.Extent);
                idfCellIterator.AddIDFFile(bndIDFFile);
                idfCellIterator.AddIDFFile(SHDIDFFile);

                idfCellIterator.AddIDFFile(MinBNDSettingIDFFile);
                idfCellIterator.AddIDFFile(MaxBNDSettingIDFFile);
                idfCellIterator.CheckExtent(log, 2, LogLevel.Warning);

                // Create warning IDFfiles for current layer        
                CheckWarningLayer warningLayer = CreateWarningLayer(resultHandler, bndPackage, null, kper, entryIdx, idfCellIterator.XStepsize, warningLegend);

                // Create errors IDFfiles for current layer                  
                CheckErrorLayer errorLayer = CreateErrorLayer(resultHandler, bndPackage, null, 0, 1, idfCellIterator.XStepsize, warningLegend);

                // Check SHD/BND for first KPER. Iterate through all cells
                idfCellIterator.Reset();
                while (idfCellIterator.IsInsideExtent())
                {
                    float bndValue = idfCellIterator.GetCellValue(bndIDFFile);
                    float shdValue = idfCellIterator.GetCellValue(SHDIDFFile);
                    float minBNDSettingValue = idfCellIterator.GetCellValue(MinBNDSettingIDFFile);
                    float maxBNDSettingValue = idfCellIterator.GetCellValue(MaxBNDSettingIDFFile);
                    float x = idfCellIterator.X;
                    float y = idfCellIterator.Y;

                    if (shdValue.Equals(SHDIDFFile.NoDataValue))
                    {
                        if ((bndValue != 0) && !bndValue.Equals(bndIDFFile.NoDataValue))
                        {
                            resultHandler.AddCheckResult(errorLayer, x, y, SHDNoDataError);
                            if (!errorLayer.SourceFiles.Contains(SHDIDFFile))
                            {
                                errorLayer.AddSourceFile(SHDIDFFile);
                            }
                        }
                    }

                    if (!Math.Truncate(bndValue).Equals(bndValue) || bndValue.Equals(bndIDFFile.NoDataValue))
                    {
                        resultHandler.AddCheckResult(errorLayer, x, y, InvalidBNDError);
                    }
                    else if (bndValue < minBNDSettingValue || bndValue > maxBNDSettingValue)
                    {
                        resultHandler.AddCheckResult(warningLayer, x, y, BNDValueRangeWarning);
                    }

                    idfCellIterator.MoveNext();
                }

                // Check all KPERs for CHD and FHB
                for (kper = resultHandler.MinKPER; (kper <= model.NPER) && (kper <= resultHandler.MaxKPER); kper++)
                {
                    log.AddMessage((model.NPER > 1) ? LogLevel.Info : LogLevel.Trace, "Checking stressperiod " + kper + " " + Model.GetStressPeriodString(model.StartDate, kper) + "...", 1);

                    // Check CHD-package for all cells and current KPER
                    if (chdPackage != null)
                    {
                        IDFFile chdIDFFile = null;
                        if (chdPackage != null)
                        {
                            try
                            {
                                chdIDFFile = chdPackage.GetIDFFile(entryIdx, 0, kper);
                            }
                            catch
                            {
                                log.AddWarning("CHD-file not found for entry: " + (entryIdx + 1), 1); //continue for BND=-1
                            }
                        }

                        idfCellIterator = new IDFCellIterator(resultHandler.Extent);
                        idfCellIterator.AddIDFFile(bndIDFFile);
                        idfCellIterator.AddIDFFile(chdIDFFile);
                        idfCellIterator.CheckExtent(log, 2, LogLevel.Warning);

                        idfCellIterator.Reset();
                        while (idfCellIterator.IsInsideExtent())
                        {
                            float bndValue = idfCellIterator.GetCellValue(bndIDFFile);
                            float chdValue = idfCellIterator.GetCellValue(chdIDFFile);
                            float x = idfCellIterator.X;
                            float y = idfCellIterator.Y;

                            if ((bndValue < 0))
                            {
                                if (chdValue.Equals(chdIDFFile.NoDataValue))
                                {
                                    if ((fhbPackage == null) || !bndValue.Equals(-2))
                                    {
                                        resultHandler.AddCheckResult(errorLayer, x, y, CHDNoDataError);
                                        resultHandler.AddExtraMapFile(chdIDFFile);
                                    }
                                    else
                                    {
                                        // Ignore; when FHB-package is defined value -2 is valid for BND
                                    }
                                }
                            }

                            idfCellIterator.MoveNext();
                        }
                    }

                    // Check FHB-package for all cells and current KPER
                    if (fhbPackage != null)
                    {
                        IDFFile fhbHeadIDFFile = null;
                        IDFFile fhbFlowIDFFile = null;
                        if (fhbPackage != null)
                        {
                            try
                            {
                                fhbHeadIDFFile = (IDFFile)fhbPackage.GetIMODFile(entryIdx, FHBPackage.HeadPartIdx, kper);
                                fhbFlowIDFFile = (IDFFile)fhbPackage.GetIMODFile(entryIdx, FHBPackage.FlowPartIdx, kper);
                            }
                            catch
                            {
                                log.AddWarning("No FHB-file found for entry: " + (entryIdx + 1) + ", BND-check is skipped.", 1);
                                return;
                            }
                        }

                        idfCellIterator.Reset();
                        while (idfCellIterator.IsInsideExtent())
                        {
                            float bndValue = idfCellIterator.GetCellValue(bndIDFFile);
                            float fhbHeadValue = idfCellIterator.GetCellValue(fhbHeadIDFFile);
                            float fhbFlowValue = idfCellIterator.GetCellValue(fhbFlowIDFFile);
                            float x = idfCellIterator.X;
                            float y = idfCellIterator.Y;

                            // Check for FHB boundary cells without FHB-values
                            if (bndValue.Equals(-2) || bndValue.Equals(2))
                            {
                                if (fhbHeadValue.Equals(fhbHeadIDFFile.NoDataValue) || fhbFlowValue.Equals(fhbFlowIDFFile.NoDataValue))
                                {
                                    resultHandler.AddCheckResult(errorLayer, x, y, FHBNoDataError);
                                    resultHandler.AddExtraMapFile(fhbHeadIDFFile);
                                    resultHandler.AddExtraMapFile(fhbFlowIDFFile);
                                }
                            }

                            idfCellIterator.MoveNext();
                        }
                    }
                }

                // Write BND errors
                if (errorLayer.HasResults())
                {
                    errorLayer.AddSourceFile(bndIDFFile);
                    errorLayer.CompressLegend(CombinedResultLabel);
                    errorLayer.WriteResultFile(log);
                }

                // Write BND warnings
                if (warningLayer.HasResults())
                {
                    warningLayer.AddSourceFile(bndIDFFile);
                    warningLayer.CompressLegend(CombinedResultLabel);
                    warningLayer.WriteResultFile(log);
                }
            }
        }


        private void Initialize(Model model, Log log)
        {
            this.model = model;
            this.log = log;

            SHDNoDataError = new CheckError(1, "BND = -1 but SHD is NoData");
            CHDNoDataError = new CheckError(2, "BND = -1 but CHD is NoData");
            InvalidBNDError = new CheckError(4, "BND is no integer");
            FHBNoDataError = new CheckError(8, "BND = -2/2 but FHB is NoData");

            errorLegend.AddClass(new ValueLegendClass(0, "No errors found.", Color.White));
            errorLegend.AddClass(SHDNoDataError.CreateLegendValueClass(Color.Gold, true));
            errorLegend.AddClass(CHDNoDataError.CreateLegendValueClass(Color.Orange, true));
            errorLegend.AddClass(InvalidBNDError.CreateLegendValueClass(Color.Red, true));
            errorLegend.AddClass(FHBNoDataError.CreateLegendValueClass(Color.DarkRed, true));
            errorLegend.AddUpperRangeClass(CombinedResultLabel, true);
            errorLegend.AddInbetweenClasses(CombinedResultLabel, true);

            BNDValueRangeWarning = new CheckWarning(1, "Level outside the expected range ["
             + settings.MinValue.ToString() + "," + settings.MaxValue.ToString() + "]");

            warningLegend.AddClass(new ValueLegendClass(0, "No errors found", Color.White));
            warningLegend.AddClass(BNDValueRangeWarning.CreateLegendValueClass(Color.DarkRed, true));
            warningLegend.AddUpperRangeClass(CombinedResultLabel, true);
            warningLegend.AddInbetweenClasses(CombinedResultLabel, true);
        }

        private void RetrieveSettings()
        {
            MinBNDSettingIDFFile = settings.GetIDFFile(settings.MinValue, log, 1);
            MaxBNDSettingIDFFile = settings.GetIDFFile(settings.MaxValue, log, 1);
        }

    }
}

