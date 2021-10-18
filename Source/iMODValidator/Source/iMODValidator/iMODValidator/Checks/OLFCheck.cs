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
    class OLFCheckSettings : CheckSettings
    {
        private string minLevel;
        private string maxLevel;
        private string distanceBelowSurface;
        private bool useDRN_L1;
        private UpscaleMethodEnum upscaleMethod;

        [Category("OLF-properties"), Description("An OLF-level below surface more than this distance generates a different error than less than this level"), PropertyOrder(10)]
        public string DistanceBelowSurface
        {
            get { return distanceBelowSurface; }
            set { distanceBelowSurface = value; }
        }
        [Category("OLF-properties"), Description("Set to true to use the DRN_L1 file as OLF-file when no OLF is explicitly defined in the runfile"), PropertyOrder(11)]
        public bool UseDRN_L1
        {
            get { return useDRN_L1; }
            set { useDRN_L1 = value; }
        }
        [Category("Scale-properties"), Description("Upscale method in case of resolution differences. Use 'Minimum' to only find coarse cells with problem for all finer cells inside"), PropertyOrder(20)]
        public UpscaleMethodEnum UpscaleMethod
        {
            get { return upscaleMethod; }
            set { upscaleMethod = value; }
        }

        [Category("Warning-properties"), Description("The minimum valid OLF-level for this region"), PropertyOrder(30)]
        public string MinLevel
        {
            get { return minLevel; }
            set { minLevel = value; }
        }
        [Category("Warning-properties"), Description("The maximum valid OLF-level for this region"), PropertyOrder(31)]
        public string MaxLevel
        {
            get { return maxLevel; }
            set { maxLevel = value; }
        }

        public OLFCheckSettings(string checkName)
            : base(checkName)
        {
            distanceBelowSurface = "1";
            minLevel = "-20";
            maxLevel = "175";
            useDRN_L1 = false;
            upscaleMethod = UpscaleMethodEnum.Minimum;
        }

        public override void LogSettings(Log log, int logIndentLevel = 0)
        {
            log.AddInfo("Distance below surface (legend class level): " + distanceBelowSurface + "m-mv", logIndentLevel);
            log.AddInfo("Minimum level: " + minLevel + " mNAP", logIndentLevel);
            log.AddInfo("Maximum level: " + maxLevel + " mNAP", logIndentLevel);
            log.AddInfo("Using DRN_L1: " + useDRN_L1.ToString(), logIndentLevel);
            log.AddInfo("Upscale method: " + upscaleMethod.ToString(), logIndentLevel);
        }
    }

    class OLFCheck : Check
    {
        public override string Abbreviation
        {
            get { return "OLF"; }
        }

        public override string Description
        {
            get { return "Checks if OLF is above surface level"; }
        }

        private OLFCheckSettings settings;
        public override CheckSettings Settings
        {
            get { return settings; }
            set
            {
                if (value is OLFCheckSettings)
                {
                    settings = (OLFCheckSettings)value;
                }
            }
        }

        private CheckError OLFDefinedLevelOrLessBelowSurfaceError;
        private CheckError OLFDefinedLevelOrMoreBelowSurfaceError;
        private CheckWarning RangeWarning;
        private IDFLegend errorLegend;
        private IDFLegend warningLegend;

        public OLFCheck()
        {
            settings = new OLFCheckSettings(this.Name);

            // Define errors
            OLFDefinedLevelOrLessBelowSurfaceError = new CheckError(1, "OLF < " + settings.DistanceBelowSurface + "m below surface level", "The OLF-value is less than " + settings.DistanceBelowSurface + " meter below the surface level.");
            OLFDefinedLevelOrMoreBelowSurfaceError = new CheckError(2, "OLF >= " + settings.DistanceBelowSurface + "m below surface level", "The OLF-value  is more than " + settings.DistanceBelowSurface + " meter below the surface level.");
            errorLegend = new IDFLegend("Legend for OLF-file check");
            errorLegend.AddClass(new ValueLegendClass(0, "No errors found.", Color.White));
            errorLegend.AddClass(OLFDefinedLevelOrLessBelowSurfaceError.CreateLegendValueClass(Color.Orange));
            errorLegend.AddClass(OLFDefinedLevelOrMoreBelowSurfaceError.CreateLegendValueClass(Color.DarkRed));
            //            errorLegend.AddInbetweenClasses(CombinedResultLabel, true);

            // Define warnings
            RangeWarning = new CheckWarning(1, "Level outside the expected range ["
                + settings.MinLevel.ToString() + "," + settings.MaxLevel.ToString() + "]");
            warningLegend = new IDFLegend("Legend for OLF-file check");
            warningLegend.AddClass(new ValueLegendClass(0, "No errors found", Color.White));
            warningLegend.AddClass(RangeWarning.CreateLegendValueClass(Color.Orange, true));
            //            errorLegend.AddInbetweenClasses(CombinedResultLabel, true);
        }

        public override void Run(Model model, CheckResultHandler resultHandler, Log log)
        {
            log.AddInfo("Checking OLF-package...");
            settings.LogSettings(log, 1);
            RunOLFCheck1(model, resultHandler, log);
        }

        private void RunOLFCheck1(Model model, CheckResultHandler resultHandler, Log log)
        {
            // Retrieve used packages
            IDFPackage olfPackage = (IDFPackage)model.GetPackage(OLFPackage.DefaultKey);
            if (olfPackage == null)
            {
                if (settings.UseDRN_L1)
                {
                    log.AddWarning("OLF-package is not active, using DRN_L1-file as OLF-file...", 1);
                }
                else
                {
                    log.AddWarning("OLF-package has not been found, check the logfile/runfile for errors.", 1);
                    return;
                }
            }
            else
            {
                if (!olfPackage.IsActive)
                {
                    if (settings.UseDRN_L1)
                    {
                        log.AddInfo("OLF-package is not active, using DRN_L1-file as OLF-file...", 1);
                        olfPackage = null;
                    }
                    else
                    {
                        log.AddWarning("OLF-package is not active. " + this.Name + " is skipped.", 1);
                        return;
                    }
                }
            }

            // TODO: Check all KPERs! It is currently fixed to kper = 0
            int kper = 0;

            IDFFile olfIDFFile = null;
            if (olfPackage != null)
            {
                olfIDFFile = olfPackage.GetIDFFile(0, OLFPackage.OLFPartIdx, kper);
                if (olfIDFFile == null)
                {
                    log.AddWarning("No OLF-file, check the logfile/runfile for errors.", 1);
                    return;
                }
            }
            else
            {
                // No OLF-file defined, check for optional use of DRN_L1 file
                if (settings.UseDRN_L1)
                {
                    IDFPackage drnPackage = (IDFPackage)model.GetPackage(DRNPackage.DefaultKey);
                    if ((drnPackage != null) && drnPackage.IsActive)
                    {
                        olfIDFFile = drnPackage.GetIDFFile(0, DRNPackage.LevelPartIdx, kper);
                        if (olfIDFFile == null)
                        {
                            log.AddWarning("DRN_L1 file not found, OLF-check is skipped", 1);
                            return;
                        }
                    }
                    else
                    {
                        log.AddWarning("No (active) DRN-package, OLF-check is skipped.", 1);
                        return;
                    }
                }
            }

            IDFFile surfacelevelIDFFile = model.RetrieveSurfaceLevelFile(log, 1);
            if (surfacelevelIDFFile == null)
            {
                log.AddWarning("No surface level file defined, check is skipped.", 1);
                return;
            }

            IDFUpscaler surfacelevelUpscaler = new IDFUpscaler(surfacelevelIDFFile, settings.UpscaleMethod, resultHandler.Extent, GetIMODFilesPath(model), log, 2);
            IDFFile scaledSurfacelevelIDFFile = surfacelevelUpscaler.RetrieveIDFFile(olfIDFFile.XCellsize);

            // Retrieve setting-values
            IDFFile distanceBelowSurfaceSettingIDFFile = settings.GetIDFFile(settings.DistanceBelowSurface, log, 1);
            IDFFile minLevelSettingIDFFile = settings.GetIDFFile(settings.MinLevel, log, 1);
            IDFFile maxLevelSettingIDFFile = settings.GetIDFFile(settings.MaxLevel, log, 1);

            double levelErrorMargin = resultHandler.LevelErrorMargin;
            IDFCellIterator idfCellIterator = new IDFCellIterator(resultHandler.Extent);
            idfCellIterator.AddIDFFile(scaledSurfacelevelIDFFile);
            idfCellIterator.AddIDFFile(olfIDFFile);

            idfCellIterator.AddIDFFile(distanceBelowSurfaceSettingIDFFile);
            idfCellIterator.AddIDFFile(minLevelSettingIDFFile);
            idfCellIterator.AddIDFFile(maxLevelSettingIDFFile);

            idfCellIterator.CheckExtent(log, 2);

            // Create error IDFfiles for current layer
            CheckErrorLayer errorLayer = CreateErrorLayer(resultHandler, olfPackage, kper, 1, idfCellIterator.XStepsize, errorLegend);
            errorLayer.AddSourceFiles(idfCellIterator.GetIDFFiles());

            // Create warning IDFfiles for current layer
            CheckWarningLayer warningLayer = CreateWarningLayer(resultHandler, olfPackage, kper, 1, idfCellIterator.XStepsize, warningLegend);
            errorLayer.AddSourceFiles(idfCellIterator.GetIDFFiles());

            // Iterate through cells
            idfCellIterator.Reset();
            while (idfCellIterator.IsInsideExtent())
            {
                float scaledSurfacelevelValue = idfCellIterator.GetCellValue(scaledSurfacelevelIDFFile);
                float olfValue = idfCellIterator.GetCellValue(olfIDFFile);

                float distanceBelowSurfaceSettingValue = idfCellIterator.GetCellValue(distanceBelowSurfaceSettingIDFFile);
                float minLevelSettingValue = idfCellIterator.GetCellValue(minLevelSettingIDFFile);
                float maxLevelSettingValue = idfCellIterator.GetCellValue(maxLevelSettingIDFFile);
                float x = idfCellIterator.X;
                float y = idfCellIterator.Y;

                if (!olfValue.Equals(olfIDFFile.NoDataValue))
                {
                    if (!scaledSurfacelevelValue.Equals(float.NaN) && !scaledSurfacelevelValue.Equals(surfacelevelIDFFile.NoDataValue))
                    {
                        if (olfValue < (scaledSurfacelevelValue - levelErrorMargin))
                        {
                            if ((scaledSurfacelevelValue - olfValue) < distanceBelowSurfaceSettingValue)
                            {
                                resultHandler.AddCheckResult(errorLayer, x, y, OLFDefinedLevelOrLessBelowSurfaceError);
                            }
                            else
                            {
                                resultHandler.AddCheckResult(errorLayer, x, y, OLFDefinedLevelOrMoreBelowSurfaceError);
                            }
                        }
                    }

                    // Check for a range warning
                    if ((olfValue < minLevelSettingValue) || (olfValue > maxLevelSettingValue))
                    {
                        resultHandler.AddCheckResult(warningLayer, x, y, RangeWarning);
                    }
                }

                idfCellIterator.MoveNext();
            }

            // Write OLF-bottom errors
            if (errorLayer.HasResults())
            {
                errorLayer.CompressLegend(CombinedResultLabel);
                errorLayer.WriteResultFile(log);
                if (surfacelevelIDFFile != null)
                {
                    resultHandler.AddExtraMapFile(surfacelevelIDFFile);
                    if ((surfacelevelIDFFile != null) && (scaledSurfacelevelIDFFile != null) && !scaledSurfacelevelIDFFile.XCellsize.Equals(surfacelevelIDFFile.XCellsize))
                    {
                        resultHandler.AddExtraMapFile(scaledSurfacelevelIDFFile);
                    }
                }
                resultHandler.AddExtraMapFile(olfIDFFile);
            }

            // Write OLF-bottom warnings
            if (warningLayer.HasResults())
            {
                warningLayer.CompressLegend(CombinedResultLabel);
                warningLayer.WriteResultFile(log);
                resultHandler.AddExtraMapFile(olfIDFFile);
                if ((surfacelevelIDFFile != null) && (scaledSurfacelevelIDFFile != null) && !scaledSurfacelevelIDFFile.XCellsize.Equals(surfacelevelIDFFile.XCellsize))
                {
                    resultHandler.AddExtraMapFile(scaledSurfacelevelIDFFile);
                }
            }
        }
    }
}
