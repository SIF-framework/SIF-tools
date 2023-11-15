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
    class RCHCheckSettings : CheckSettings
    {
        [Category("Scale-properties"), Description("Upscale method in case of resolution differences. Use 'Minimum' to only find coarse cells with problem for all finer cells inside"), PropertyOrder(20)]
        public UpscaleMethodEnum UpscaleMethod { get; set; }

        [Category("Warning-properties"), Description("The minimum valid RCH-level for this region"), PropertyOrder(30)]
        public string MinRCH { get; set; }

        [Category("Warning-properties"), Description("The maximum valid RCH-level for this region"), PropertyOrder(31)]
        public string MaxRCH { get; set; }

        public RCHCheckSettings(string checkName) : base(checkName)
        {
            MinRCH = "0";
            MaxRCH = "150";
            UpscaleMethod = UpscaleMethodEnum.Minimum;
        }

        public override void LogSettings(Log log, int logIndentLevel = 0)
        {
            log.AddInfo("Minimum RCH value: " + MinRCH, logIndentLevel);
            log.AddInfo("Maximum RCH value: " + MaxRCH, logIndentLevel);
            log.AddInfo("Upscale method: " + UpscaleMethod.ToString(), logIndentLevel);
        }
    }

    class RCHCheck : Check
    {
        public override string Abbreviation
        {
            get { return "RCH"; }
        }

        public override string Description
        {
            get { return "Checks RCH value for range and NoData values"; }
        }

        private RCHCheckSettings settings;
        public override CheckSettings Settings
        {
            get { return settings; }
            set
            {
                if (value is RCHCheckSettings)
                {
                    settings = (RCHCheckSettings)value;
                }
            }
        }

        public RCHCheck()
        {
            settings = new RCHCheckSettings(this.Name);
        }

        public override void Run(Model model, CheckResultHandler resultHandler, Log log)
        {
            log.AddInfo("Checking RCH-package ...");

            IDFPackage rchPackage = (IDFPackage)model.GetPackage(RCHPackage.DefaultKey);
            if (rchPackage == null || !rchPackage.IsActive)
            {
                log.AddWarning(this.Name, model.Runfilename, "RCH-package is not active. " + this.Name + " is skipped.", 1);
                return;
            }

            settings.LogSettings(log, 1);
            RunRCHCheck1(model, resultHandler, log);
        }
        /// <summary>
        /// Check for RCH-value within range and NoData-values inside model area
        /// </summary>
        /// <param name="model"></param>
        /// <param name="resultHandler"></param>
        /// <param name="log"></param>
        private void RunRCHCheck1(Model model, CheckResultHandler resultHandler, Log log)
        {
            IDFPackage rchPackage = (IDFPackage)model.GetPackage(RCHPackage.DefaultKey);
            IDFPackage bndPackage = (IDFPackage)model.GetPackage(BNDPackage.DefaultKey);

            ////////////////////////////////
            // Define legends and results //
            ////////////////////////////////

            CheckError NoDataRCHError = new CheckError(1, "RCH = NoData");

            IDFLegend errorLegend = new IDFLegend("Legend for RCH-file check");
            errorLegend.AddClass(new ValueLegendClass(0, "No errors found.", Color.White));
            errorLegend.AddClass(NoDataRCHError.CreateLegendValueClass(Color.Gold));
            // errorLegend.AddUpperRangeClass(CombinedResultLabel, true);
            // errorLegend.AddInbetweenClasses(CombinedResultLabel, true);

            CheckWarning RangeWarning = new CheckWarning(1, "RCH-value outside expected range ["
             + settings.MinRCH.ToString() + "," + settings.MaxRCH.ToString() + "]");

            IDFLegend warningLegend = new IDFLegend("Legend for RCH-file check");
            warningLegend.AddClass(new ValueLegendClass(0, "No errors found", Color.White));
            warningLegend.AddClass(RangeWarning.CreateLegendValueClass(Color.DarkRed, true));
            //warningLegend.AddInbetweenClasses(CombinedResultLabel, true);
            //warningLegend.AddUpperRangeClass(CombinedResultLabel, true);

            ///////////////////////////
            // Retrieve settingfiles //
            ///////////////////////////
            IDFFile minRCHSettingIDFFile = settings.GetIDFFile(settings.MinRCH, log, 1);
            IDFFile maxRCHSettingIDFFile = settings.GetIDFFile(settings.MaxRCH, log, 1);

            // Assume first BND-file defines all active (<> 0) cells
            IDFFile bndIDFFile = (bndPackage != null) ? bndPackage.GetIDFFile(0) : null;

            // Process all RCH-entries
            int kper = 0;
            for (int entryIdx = resultHandler.MinEntryNumber - 1; (entryIdx < rchPackage.GetEntryCount(kper)) && (entryIdx < resultHandler.MaxEntryNumber); entryIdx++)
            {
                log.AddInfo("Checking entry " + (entryIdx + 1) + " with " + Name + " ...", 1);

                IDFFile rchIDFFile = rchPackage.GetIDFFile(entryIdx);
                if (rchIDFFile == null)
                {
                    log.AddError(this.Name, "RCH-file", "RCH-file not found: " + rchPackage.GetPackageFile(entryIdx).FName);
                    return;
                }

                IDFCellIterator idfCellIterator = new IDFCellIterator(resultHandler.Extent);
                idfCellIterator.AddIDFFile(rchIDFFile);
                idfCellIterator.AddIDFFile(bndIDFFile);

                idfCellIterator.AddIDFFile(minRCHSettingIDFFile);
                idfCellIterator.AddIDFFile(maxRCHSettingIDFFile);
                idfCellIterator.CheckExtent(log, 2, LogLevel.Warning);

                // Create warning IDFfiles for current layer        
                CheckWarningLayer warningLayer = CreateWarningLayer(resultHandler, rchPackage, null, kper, entryIdx, idfCellIterator.XStepsize, warningLegend);

                // Create errors IDFfiles for current layer                  
                CheckErrorLayer errorLayer = CreateErrorLayer(resultHandler, rchPackage, null, 0, 1, idfCellIterator.XStepsize, warningLegend);

                // Iterate through cells
                idfCellIterator.Reset();
                while (idfCellIterator.IsInsideExtent())
                {
                    float rchValue = idfCellIterator.GetCellValue(rchIDFFile);
                    float bndValue = idfCellIterator.GetCellValue(bndIDFFile);
                    float minRCHSettingValue = idfCellIterator.GetCellValue(minRCHSettingIDFFile);
                    float maxRCHSettingValue = idfCellIterator.GetCellValue(maxRCHSettingIDFFile);
                    float x = idfCellIterator.X;
                    float y = idfCellIterator.Y;

                    if (rchValue.Equals(rchIDFFile.NoDataValue))
                    {
                        if (!bndValue.Equals(0f) && !bndValue.Equals(bndIDFFile.NoDataValue))
                        {
                            resultHandler.AddCheckResult(errorLayer, x, y, NoDataRCHError);
                        }
                    }
                    else if (rchValue < minRCHSettingValue || rchValue > maxRCHSettingValue)
                    {
                        resultHandler.AddCheckResult(warningLayer, x, y, RangeWarning);
                    }

                    idfCellIterator.MoveNext();
                }

                // Write RCH errors and warnings
                if (errorLayer.HasResults())
                {
                    errorLayer.CompressLegend(CombinedResultLabel);
                    errorLayer.WriteResultFile(log);
                    resultHandler.AddExtraMapFile(rchIDFFile);
                }

                // Write RCH-bottom warnings
                if (warningLayer.HasResults())
                {
                    warningLayer.CompressLegend(CombinedResultLabel);
                    warningLayer.WriteResultFile(log);
                    resultHandler.AddExtraMapFile(rchIDFFile);
                }
            }
        }
    }
}
