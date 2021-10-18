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
using Sweco.SIF.iMODValidator.Models.Packages;
using Sweco.SIF.Statistics;
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
    class TOPBOTCheckSettings : CheckSettings
    {
        private string minLevel;
        private string maxLevel;
        [Category("TOPBOT-properties"), Description("The minimum valid height for this region"), PropertyOrder(10)]
        public string MinLevel
        {
            get { return minLevel; }
            set { minLevel = value; }
        }
        [Category("TOPBOT-properties"), Description("The maximum valid height for this region"), PropertyOrder(11)]
        public string MaxLevel
        {
            get { return maxLevel; }
            set { maxLevel = value; }
        }
        private bool isOutlierChecked;
        [Category("TOPBOT-properties"), Description("Specifies that outliers should be searched"), PropertyOrder(15)]
        public bool IsOutlierChecked
        {
            get { return isOutlierChecked; }
            set { isOutlierChecked = value; }
        }
        private OutlierMethodEnum outlierMethod;
        [Category("TOPBOT-properties"), Description("The method for identifying spatial outliers"), PropertyOrder(16)]
        public OutlierMethodEnum OutlierMethod
        {
            get { return outlierMethod; }
            set { outlierMethod = value; }
        }
        private OutlierBaseRangeEnum outlierMethodBaseRange;
        [Category("TOPBOT-properties"), Description("The valid base range for identifying outliers"), PropertyOrder(17)]
        public OutlierBaseRangeEnum OutlierMethodBaseRange
        {
            get { return outlierMethodBaseRange; }
            set { outlierMethodBaseRange = value; }
        }
        private float outlierMethodMultiplier;
        [Category("TOPBOT-properties"), Description("The factor to multiply the base range"), PropertyOrder(18)]
        public float OutlierMethodMultiplier
        {
            get { return outlierMethodMultiplier; }
            set { outlierMethodMultiplier = value; }
        }

        public TOPBOTCheckSettings(string checkName)
            : base(checkName)
        {
            minLevel = "-1250";
            maxLevel = "175";
            isOutlierChecked = false;
            outlierMethod = OutlierMethodEnum.IQR;
            outlierMethodBaseRange = OutlierBaseRangeEnum.Pct75_25;
            outlierMethodMultiplier = 3.95f;
        }

        public override void LogSettings(Log log, int logIndentLevel = 0)
        {
            log.AddInfo("Minimum level: " + minLevel + "mNAP", logIndentLevel);
            log.AddInfo("Maximum level: " + maxLevel + "mNAP", logIndentLevel);
            if (isOutlierChecked)
            {
                log.AddInfo("Outlier method: " + outlierMethod.ToString(), logIndentLevel);
                log.AddInfo("Outlier base range method: " + outlierMethodBaseRange.ToString(), logIndentLevel);
                log.AddInfo("Outlier base range multiplier: " + outlierMethodMultiplier.ToString(), logIndentLevel);
            }
            else
            {
                log.AddInfo("Outliers are not checked");
            }
        }
    }

    class TOPBOTCheck : Check
    {
        public override string Abbreviation
        {
            get { return "TOP-BOT"; }
        }

        public override string Description
        {
            get { return "Checks per layer if top is above bottom"; }
        }

        private TOPBOTCheckSettings settings;
        public override CheckSettings Settings
        {
            get { return settings; }
            set
            {
                if (value is TOPBOTCheckSettings)
                {
                    settings = (TOPBOTCheckSettings)value;
                }
            }
        }

        private CheckError TopBelowBottomError;
        private CheckError BottomBelowTopError;
        private CheckError NoDataTopBottomError;
        private CheckWarning HeightRangeWarning;
        private CheckWarning OutlierWarning;
        private IDFLegend errorLegend;
        private IDFLegend warningLegend;

        public TOPBOTCheck()
        {
            settings = new TOPBOTCheckSettings(this.Name);
        }

        private void createCheckResultAndLegends()
        {
            // Define errors
            TopBelowBottomError = new CheckError(1, "Top below bottom", "Top is below bottom of this layer");
            BottomBelowTopError = new CheckError(2, "Bottom below lowertop", "Bottom is below top of lower layer");
            NoDataTopBottomError = new CheckError(4, "NoData Top or bottom", "Only top or bottom has NoData-value");
            errorLegend = new IDFLegend("Legend for TOP- and BOT-file check");
            errorLegend.AddClass(new ValueLegendClass(0, "No problems found", Color.White));
            errorLegend.AddClass(TopBelowBottomError.CreateLegendValueClass(Color.Orange));
            errorLegend.AddClass(BottomBelowTopError.CreateLegendValueClass(Color.Red));
            errorLegend.AddClass(NoDataTopBottomError.CreateLegendValueClass(Color.DarkRed));
            errorLegend.AddUpperRangeClass(CombinedResultLabel, true);
            errorLegend.AddInbetweenClasses(CombinedResultLabel, true);

            // Define warnings
            HeightRangeWarning = new CheckWarning(1, "Level outside the expected range ["
                + settings.MinLevel.ToString() + "," + settings.MaxLevel.ToString() + "]");
            warningLegend = new IDFLegend("Legend for TOP- and BOT-file check");
            warningLegend.AddClass(new ValueLegendClass(0, "No problems found", Color.White));
            warningLegend.AddClass(HeightRangeWarning.CreateLegendValueClass(Color.Orange));
            if (settings.IsOutlierChecked)
            {
                OutlierWarning = new CheckWarning(4, "Outlier");
                warningLegend.AddClass(OutlierWarning.CreateLegendValueClass(Color.Red, true));
            }
            warningLegend.AddInbetweenClasses(CombinedResultLabel, true);
        }

        public override void Run(Model model, CheckResultHandler resultHandler, Log log)
        {
            createCheckResultAndLegends();

            log.AddInfo("Checking TOP- and BOT-packages...");
            settings.LogSettings(log, 1);

            // Retrieve used packages
            TOPPackage topPackage = (TOPPackage)model.GetPackage(TOPPackage.DefaultKey);
            BOTPackage botPackage = (BOTPackage)model.GetPackage(BOTPackage.DefaultKey);

            // Check used packages
            if ((topPackage == null) || (botPackage == null))
            {
                log.AddInfo("TOP and/or BOT-package are not available for this model, check is skipped.", 1);
                return;
            }

            // Retrieve IDF settingfiles
            IDFFile minLevelSettingIDFFile = settings.GetIDFFile(settings.MinLevel, log, 1);
            IDFFile maxLevelSettingIDFFile = settings.GetIDFFile(settings.MaxLevel, log, 1);

            // Process all entries
            for (int entryIdx = resultHandler.MinEntryNumber - 1; (entryIdx < topPackage.GetEntryCount()) && (entryIdx < resultHandler.MaxEntryNumber); entryIdx++)
            {
                log.AddInfo("Checking entry " + (entryIdx + 1) + " with " + Name + " ...", 1);

                // Retrieve IDF files for current layer
                //long mem1 = GC.GetTotalMemory(true) / 1000000;
                IDFFile topIDFFile = topPackage.GetIDFFile(entryIdx);
                //long mem6 = GC.GetTotalMemory(true) / 1000000;
                IDFFile botIDFFile = botPackage.GetIDFFile(entryIdx);
                //long mem7 = GC.GetTotalMemory(true) / 1000000;
                if ((topIDFFile != null) && (botIDFFile != null))
                {
                    // Retrieve statistics for outliers
                    log.AddInfo("Computing statistics...", 1);
                    IDFStatistics topStats = new IDFStatistics(topIDFFile);
                    IDFStatistics botStats = new IDFStatistics(botIDFFile);
                    bool isOutlierChecked = settings.IsOutlierChecked;
                    double topOutlierRangeLowerValue = double.NaN;
                    double topOutlierRangeUpperValue = double.NaN;
                    double botOutlierRangeLowerValue = double.NaN;
                    double botOutlierRangeUpperValue = double.NaN;
                    if (isOutlierChecked)
                    {
                        topStats.ComputeOutlierStatistics(settings.OutlierMethod, settings.OutlierMethodBaseRange, settings.OutlierMethodMultiplier);
                        topStats.ReleaseValuesMemory();
                        botStats.ComputeOutlierStatistics(settings.OutlierMethod, settings.OutlierMethodBaseRange, settings.OutlierMethodMultiplier);
                        botStats.ReleaseValuesMemory();
                        topOutlierRangeLowerValue = topStats.OutlierRangeLowerValue;
                        topOutlierRangeUpperValue = topStats.OutlierRangeUpperValue;
                        botOutlierRangeLowerValue = botStats.OutlierRangeLowerValue;
                        botOutlierRangeUpperValue = botStats.OutlierRangeUpperValue;
                    }
                    //long mem22 = GC.GetTotalMemory(true) / 1000000;

                    float[][] topValues = topIDFFile.Values;
                    float[][] botValues = botIDFFile.Values;
                    IDFFile lowerTopIDFFile = null;
                    float[][] lowerTopValues = null;
                    if (entryIdx < topPackage.GetEntryCount() - 1)
                    {
                        lowerTopIDFFile = topPackage.GetIDFFile(entryIdx + 1);
                        lowerTopValues = (lowerTopIDFFile != null) ? lowerTopIDFFile.Values : null;
                    }

                    //long mem36 = GC.GetTotalMemory(true) / 1000000;

                    // Create error IDFfile for current layer
                    CheckErrorLayer errorLayer = CreateErrorLayer(resultHandler, botPackage, 1, entryIdx + 1, botIDFFile.XCellsize, errorLegend);
                    errorLayer.AddSourceFiles(new List<IDFFile>() { topIDFFile, botIDFFile });

                    // Create warning IDFfile for current layer
                    CheckWarningLayer warningLayer = CreateWarningLayer(resultHandler, botPackage, 1, entryIdx + 1, botIDFFile.XCellsize, warningLegend);
                    errorLayer.AddSourceFiles(new List<IDFFile>() { topIDFFile, botIDFFile });

                    // Compare extent and cellsizes
                    if (!topIDFFile.HasEqualExtentAndCellsize(botIDFFile))
                    {
                        log.AddError(topPackage.Key, topIDFFile.Filename, "Unexpected mismatch in extent/cellsize found for TOP- and BOT-files in layer " + (entryIdx + 1), 2);
                        log.AddInfo("Ensure TOP- and BOT-files have equal extent.", 3);
                        return;
                    }

                    // Process all cells for the current layer
                    Extent minExtent = topIDFFile.Extent;
                    minExtent = minExtent.Clip(botIDFFile.Extent);
                    minExtent = minExtent.Clip(resultHandler.Extent);

                    // Process all cells for the current layer
                    int firstRowIdx = topIDFFile.GetRowIdx(minExtent.ury);
                    int firstColIdx = topIDFFile.GetColIdx(minExtent.llx);
                    int lastRowIdx = topIDFFile.GetRowIdx(minExtent.lly + 1);
                    int lastColIdx = topIDFFile.GetColIdx(minExtent.urx - 1);
                    for (int rowIdx = firstRowIdx; rowIdx <= lastRowIdx; rowIdx++)
                    {
                        for (int colIdx = firstColIdx; colIdx <= lastColIdx; colIdx++)
                        {
                            float topValue = topValues[rowIdx][colIdx];
                            float botValue = botValues[rowIdx][colIdx];
                            float x = topIDFFile.GetX(colIdx);
                            float y = topIDFFile.GetY(rowIdx);

                            if ((topIDFFile != null) && (botIDFFile != null))
                            {
                                if (!topValue.Equals(topIDFFile.NoDataValue) && !botValue.Equals(botIDFFile.NoDataValue))
                                {
                                    float minLevel = minLevelSettingIDFFile.GetValue(x, y);
                                    float maxLevel = maxLevelSettingIDFFile.GetValue(x, y);

                                    if (topValue < botValue)
                                    {
                                        resultHandler.AddCheckResult(errorLayer, x, y, TopBelowBottomError);
                                    }

                                    if ((topValue < minLevel) || (topValue > maxLevel)
                                        || (botValue < minLevel) || (botValue > maxLevel))
                                    {
                                        resultHandler.AddCheckResult(warningLayer, x, y, HeightRangeWarning);
                                    }

                                    if (isOutlierChecked)
                                    {
                                        if ((topValue < topOutlierRangeLowerValue) || (topValue > topOutlierRangeUpperValue)
                                            || (botValue < botOutlierRangeLowerValue) || (botValue > botOutlierRangeUpperValue))
                                        {
                                            resultHandler.AddCheckResult(warningLayer, x, y, OutlierWarning);
                                        }
                                    }
                                }
                                else if (!(topValue.Equals(topIDFFile.NoDataValue) && botValue.Equals(botIDFFile.NoDataValue)))
                                {
                                    resultHandler.AddCheckResult(errorLayer, x, y, NoDataTopBottomError);
                                }
                            }

                            if ((lowerTopIDFFile != null) && (botIDFFile != null))
                            {
                                float lowerTOPValue = lowerTopValues[rowIdx][colIdx];
                                if (!lowerTopValues.Equals(lowerTopIDFFile.NoDataValue) && !botValue.Equals(botIDFFile.NoDataValue))
                                {
                                    if (botValue < lowerTOPValue)
                                    {
                                        resultHandler.AddCheckResult(errorLayer, x, y, BottomBelowTopError);
                                    }
                                }
                            }
                        }
                    }

                    // Write errors
                    if (errorLayer.HasResults())
                    {
                        errorLayer.CompressLegend(CombinedResultLabel);
                        errorLayer.WriteResultFile(log);
                        resultHandler.AddExtraMapFile(topIDFFile);
                        resultHandler.AddExtraMapFile(botIDFFile);
                        if (lowerTopIDFFile != null)
                        {
                            resultHandler.AddExtraMapFile(lowerTopIDFFile);
                        }
                    }

                    // Write warnings
                    if (warningLayer.HasResults())
                    {
                        warningLayer.CompressLegend(CombinedResultLabel);
                        warningLayer.WriteResultFile(log);
                        resultHandler.AddExtraMapFile(topIDFFile);
                        resultHandler.AddExtraMapFile(botIDFFile);
                    }

                    warningLayer.ReleaseMemory(true);
                    long mem12 = GC.GetTotalMemory(true) / 1000000;
                    errorLayer.ReleaseMemory(true);
                    long mem13 = GC.GetTotalMemory(true) / 1000000;

                    topIDFFile.ReleaseMemory(true);
                    long mem14 = GC.GetTotalMemory(true) / 1000000;
                    botIDFFile.ReleaseMemory(true);
                    long mem15 = GC.GetTotalMemory(true) / 1000000;

                    minLevelSettingIDFFile.ReleaseMemory(true);
                    maxLevelSettingIDFFile.ReleaseMemory(true);
                    long mem4 = GC.GetTotalMemory(true) / 1000000;
                }
                else
                {
                    log.AddError(topPackage.Key, model.Runfilename, "TOP and/or BOT-file not found for layer " + (entryIdx + 1), 1);
                }

                topPackage.ReleaseMemory(true);
                botPackage.ReleaseMemory(true);
                GC.Collect();
                long mem5 = GC.GetTotalMemory(true) / 1000000;
            }
        }
    }
}
