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
using Sweco.SIF.Statistics;
using Sweco.SIF.iMOD.Utils;

namespace Sweco.SIF.iMODValidator.Checks
{
    [TypeConverter(typeof(PropertySorter))]
    class HEADCheckSettings : CheckSettings
    {
        private string minHead;
        [Category("HEAD-properties"), Description("The minimum valid head for this region"), PropertyOrder(10)]
        public string MinHead
        {
            get { return minHead; }
            set { minHead = value; }
        }

        private string maxHead;
        [Category("HEAD-properties"), Description("The maximum valid head for this region"), PropertyOrder(11)]
        public string MaxHead
        {
            get { return maxHead; }
            set { maxHead = value; }
        }

        private OutlierMethodEnum outlierMethod;
        [Category("Outlier-properties"), Description("The method for identifying spatial outliers"), PropertyOrder(12)]
        public OutlierMethodEnum OutlierMethod
        {
            get { return outlierMethod; }
            set { outlierMethod = value; }
        }

        private OutlierBaseRangeEnum outlierMethodBaseRange;
        [Category("Outlier-properties"), Description("The valid base range for identifying outliers"), PropertyOrder(13)]
        public OutlierBaseRangeEnum OutlierMethodBaseRange
        {
            get { return outlierMethodBaseRange; }
            set { outlierMethodBaseRange = value; }
        }

        private float outlierMethodMultiplier;
        [Category("Outlier-properties"), Description("The factor to multiply the base range with (2.0 corresponds with 3.5*SD, 3.95 with 6*SD)"), PropertyOrder(14)]
        public float OutlierMethodMultiplier
        {
            get { return outlierMethodMultiplier; }
            set
            {
                if (value > 0)
                {
                    outlierMethodMultiplier = value;
                }
            }
        }

        private int outlierBufferCellCount;
        [Category("Outlier-properties"), Description("The number of cells in buffer (left, right, above or below center cell, i.e. count 1 will give 9 cells) around each cell that is checked for outliers. Use 0 to use whole grid."), PropertyOrder(15)]
        public int OutlierBufferCellCount
        {
            get { return outlierBufferCellCount; }
            set
            {
                if (value >= 0)
                {
                    outlierBufferCellCount = value;
                }
            }
        }

        private bool checkOnlyLocalExtremes;
        [Category("Outlier-properties"), Description("Only check local extremes (cells that have a min or max value within surrounding 3x3-grid) for outliers"), PropertyOrder(16)]
        public bool CheckOnlyLocalExtremes
        {
            get { return checkOnlyLocalExtremes; }
            set { checkOnlyLocalExtremes = value; }
        }

        private float localExtremeTolerance;
        [Category("Outlier-properties"), Description("Minimum difference between minimum and maximum values in local 3x3-grid before marking a cell as an extreme"), PropertyOrder(16)]
        public float LocalExtremeTolerance
        {
            get { return localExtremeTolerance; }
            set
            {
                if (value >= 0)
                {
                    localExtremeTolerance = value;
                }
            }
        }

        private bool isSubsetSelected;
        [Category("Filter-properties"), Description("Specifies if a subset of all available resultfiles should be processed"), PropertyOrder(20)]
        public bool IsSubsetSelected
        {
            get { return isSubsetSelected; }
            set { isSubsetSelected = value; }
        }

        private string selectedYears;
        [Category("Filter-properties"), Description("The years for which resultfiles are are processed (comma seperated). If empty the middle and last year are used."), PropertyOrder(21)]
        public string SelectedYears
        {
            get { return selectedYears; }
            set
            {
                try
                {
                    List<int> intList = ParseIntArrayString(value);
                    if (intList != null)
                    {
                        selectedYears = value.Trim();
                    }
                }
                catch (Exception)
                {
                    // ignore
                }
            }
        }

        private string selectedMonths;
        [Category("Filter-properties"), Description("The months for which resultfiles are are processed (comma seperated). If empty the months 1, 3, 5, 7, 9 and 11 are used."), PropertyOrder(22)]
        public string SelectedMonths
        {
            get { return selectedMonths; }
            set
            {
                try
                {
                    List<int> intList = ParseIntArrayString(value);
                    if (intList != null)
                    {
                        selectedMonths = value.Trim();
                    }
                }
                catch (Exception)
                {
                    // ignore
                }
            }
        }

        private string selectedDays;
        [Category("Filter-properties"), Description("The days for which resultfiles are are processed (comma seperated). If empty the day 14 is used."), PropertyOrder(23)]
        public string SelectedDays
        {
            get { return selectedDays; }
            set
            {
                try
                {
                    List<int> intList = ParseIntArrayString(value);
                    if (intList != null)
                    {
                        selectedDays = value.Trim();
                    }
                }
                catch (Exception)
                {
                    // ignore
                }
            }
        }

        public HEADCheckSettings(string checkName) : base(checkName)
        {
            minHead = "-1000";
            maxHead = "500";
            outlierMethod = OutlierMethodEnum.HistogramGap;
            outlierMethodBaseRange = OutlierBaseRangeEnum.Pct90_10;
            outlierMethodMultiplier = 3.95f;
            outlierBufferCellCount = 5;
            checkOnlyLocalExtremes = true;
            localExtremeTolerance = 0.25f;
            isSubsetSelected = true;
            selectedYears = string.Empty;
            selectedMonths = string.Empty;
            SelectedDays = string.Empty;
        }

        public override void LogSettings(Log log, int logIndentLevel = 0)
        {
            log.AddInfo("Minimum head: " + minHead + "mNAP", logIndentLevel);
            log.AddInfo("Maximum head: " + maxHead + "mNAP", logIndentLevel);
            log.AddInfo("Outlier method: " + outlierMethod.ToString(), logIndentLevel);
            log.AddInfo("Outlier base range method: " + outlierMethodBaseRange.ToString(), logIndentLevel);
            log.AddInfo("Outlier base range multiplier: " + outlierMethodMultiplier.ToString(), logIndentLevel);
            log.AddInfo("Outlier buffer cells: " + outlierBufferCellCount.ToString(), logIndentLevel);
            log.AddInfo("Check only local extremes: " + checkOnlyLocalExtremes.ToString(), logIndentLevel);
            log.AddInfo("LocalExtremeTolerance: " + localExtremeTolerance.ToString(), logIndentLevel);
            log.AddInfo("Is subset selected: " + isSubsetSelected.ToString(), logIndentLevel);
            log.AddInfo("Selected years: " + selectedYears.ToString(), logIndentLevel);
            log.AddInfo("Selected months: " + selectedMonths.ToString(), logIndentLevel);
            log.AddInfo("Selected days: " + selectedDays.ToString(), logIndentLevel);
        }
    }

    class HEADCheck : Check
    {
        public override string Abbreviation
        {
            get { return "HEAD"; }
        }

        public override string Description
        {
            get { return "Checks HEAD modelresults"; }
        }

        private HEADCheckSettings settings;
        public override CheckSettings Settings
        {
            get { return settings; }
            set
            {
                if (value is HEADCheckSettings)
                {
                    settings = (HEADCheckSettings)value;
                }
            }
        }

        public HEADCheck()
        {
            settings = new HEADCheckSettings(this.Name);
        }

        public override void Run(Model model, CheckResultHandler resultHandler, Log log)
        {
            try
            {
                log.AddInfo("Checking HEAD-files ...");
                if (model.ModelresultsPath == null)
                {
                    log.AddWarning(BDGFLFPackage.DefaultKey, null, "Model RESULTS-path is not defined, check is skipped", 1);
                }
                else
                {
                    string headPath = Path.Combine(model.ModelresultsPath, "head");
                    if (!Directory.Exists(headPath))
                    {
                        log.AddWarning(HEADPackage.DefaultKey, null, "HEAD RESULTS-path not found: " + headPath, 1);
                        return;
                    }

                    settings.LogSettings(log, 1);
                    RunHEADCheck1(model, resultHandler, log);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error in " + this.Name, ex);
            }
        }

        /// <summary>
        /// Checks range and outliers for HEAD-files
        /// </summary>
        /// <param name="model"></param>
        /// <param name="resultHandler"></param>
        /// <param name="log"></param>
        protected virtual void RunHEADCheck1(Model model, CheckResultHandler resultHandler, Log log)
        {
            bool isSteadyState = false;

            string headPath = Path.Combine(model.ModelresultsPath, "head");
            string[] headFilenames = Directory.GetFiles(headPath, "head_????????_l*.IDF");
            if (headFilenames.Length == 0)
            {
                headFilenames = Directory.GetFiles(headPath, "head_steady-state_l*.IDF");
                if (headFilenames.Length == 0)
                {
                    log.AddWarning("No 'HEAD_????????_L*.IDF' or 'HEAD_steady-state_L*.IDF'-files found in " + headPath);
                    return;
                }
                else
                {
                    isSteadyState = true;
                }
            }
            CommonUtils.SortAlphanumericStrings(headFilenames);

            if (settings.IsSubsetSelected && !isSteadyState)
            {
                List<int> selectedYears = new List<int>(settings.ParseIntArrayString(settings.SelectedYears));
                List<int> selectedMonths = new List<int>(settings.ParseIntArrayString(settings.SelectedMonths));
                List<int> selectedDays = new List<int>(settings.ParseIntArrayString(settings.SelectedDays));
                headFilenames = SelectResultFileSubset(headFilenames, selectedYears, selectedMonths, selectedDays);
            }

            HEADPackage headPackage = (HEADPackage)PackageManager.Instance.CreatePackageInstance(HEADPackage.DefaultKey, model);

            ////////////////////////////////
            // Define legends and results //
            ////////////////////////////////

            // Define warnings
            CheckWarning HeadRangeWarning;
            CheckWarning OutlierWarning;
            HeadRangeWarning = CreateCheckWarning("Head outside the expected range ["
                + settings.MinHead.ToString() + "," + settings.MaxHead.ToString() + "]");
            OutlierWarning = CreateCheckWarning("Outlier");

            IDFLegend warningLegend = CreateIDFLegend();
            warningLegend.AddClass(HeadRangeWarning.CreateLegendValueClass(Color.Orange, true));
            warningLegend.AddClass(OutlierWarning.CreateLegendValueClass(Color.Red, true));
            warningLegend.AddUpperRangeClass(CombinedResultLabel, true);
            warningLegend.AddInbetweenClasses(CombinedResultLabel, true);

            ///////////////////////////
            // Retrieve settingfiles //
            ///////////////////////////
            IDFFile minHeadSettingIDFFile = settings.GetIDFFile(settings.MinHead, log, 1);
            IDFFile maxHeadSettingIDFFile = settings.GetIDFFile(settings.MaxHead, log, 1);

            // Process all HEAD-files
            log.AddInfo("Checking headfiles in folder: " + headPath + " ...", 1);
            bool checkOnlyLocalExtremes = settings.CheckOnlyLocalExtremes;
            float localExtremeTolerance = Math.Max(resultHandler.LevelErrorMargin, settings.LocalExtremeTolerance);
            for (int headFileIdx = 0; headFileIdx < headFilenames.Length; headFileIdx++)
            {
                string headFilename = headFilenames[headFileIdx];
                int ilay = IMODUtils.GetLayerNumber(headFilename);
                if ((ilay >= resultHandler.MinEntryNumber) && (ilay <= resultHandler.MaxEntryNumber))
                {
                    string sname = IMODUtils.GetStressPeriodString(headFilename);
                    StressPeriod stressPeriod = model.RetrieveStressPeriod(sname);
                    int kper = stressPeriod.KPER;

                    if ((kper >= resultHandler.MinKPER) && (kper <= resultHandler.MaxKPER))
                    {
                        log.AddInfo("Checking headfile " + Path.GetFileName(headFilename) + " ...", 1);
                        IDFFile headIDFFile = IDFFile.ReadFile(headFilename, false, log, 2);

                        IDFCellIterator idfCellIterator = new IDFCellIterator(resultHandler.Extent);
                        idfCellIterator.AddIDFFile(headIDFFile);

                        // Create warning IDFfile for head-file
                        CheckWarningLayer headWarningLayer = CreateWarningLayer(resultHandler, headPackage, null, stressPeriod, ilay, idfCellIterator.XStepsize, warningLegend);
                        headWarningLayer.AddSourceFile(headIDFFile);

                        float headOutlierRangeLowerValue = float.NaN;
                        float headOutlierRangeUpperValue = float.NaN;
                        if (settings.OutlierBufferCellCount == 0)
                        {
                            // Compute outlier statistics for whole grid if specified (buffercellcount == 0)
                            IDFStatistics headStats = new IDFStatistics(headIDFFile);
                            headStats.ComputeOutlierStatistics(settings.OutlierMethod, settings.OutlierMethodBaseRange, settings.OutlierMethodMultiplier, true, false);
                            headOutlierRangeLowerValue = headStats.OutlierRangeLowerValue;
                            headOutlierRangeUpperValue = headStats.OutlierRangeUpperValue;
                        }

                        // Iterate through cells
                        idfCellIterator.Reset();
                        //idfCellIterator.SetCurrentCell(161000f, 529000f);
                        while (idfCellIterator.IsInsideExtent())
                        {
                            float headValue = idfCellIterator.GetCellValue(headIDFFile);
                            float x = idfCellIterator.X;
                            float y = idfCellIterator.Y;

                            if (!headValue.Equals(headIDFFile.NoDataValue))
                            {
                                if (settings.OutlierBufferCellCount > 0)
                                {
                                    // Compute local outlier statistics if specified (buffercellcount > 0)
                                    IDFStatistics headStats = new IDFStatistics(headIDFFile, x, y, settings.OutlierBufferCellCount);
                                    headStats.ComputeOutlierStatistics(settings.OutlierMethod, settings.OutlierMethodBaseRange, settings.OutlierMethodMultiplier, true, false);
                                    headOutlierRangeLowerValue = headStats.OutlierRangeLowerValue;
                                    headOutlierRangeUpperValue = headStats.OutlierRangeUpperValue;
                                }

                                float minHead = minHeadSettingIDFFile.GetValue(x, y);
                                float maxHead = maxHeadSettingIDFFile.GetValue(x, y);

                                if ((headValue < minHead) || (headValue > maxHead))
                                {
                                    resultHandler.AddCheckResult(headWarningLayer, x, y, HeadRangeWarning);
                                }

                                if ((headValue < headOutlierRangeLowerValue) || (headValue > headOutlierRangeUpperValue))
                                {
                                    // Value is an outlier, but also check that it is a local min or max value in the surrounding 3x3-gid.
                                    if (!checkOnlyLocalExtremes || headIDFFile.IsMinMaxValue(x, y, 1, localExtremeTolerance))
                                    {
                                        resultHandler.AddCheckResult(headWarningLayer, x, y, OutlierWarning);
                                    }
                                }
                            }

                            idfCellIterator.MoveNext();
                        }

                        // Write warningfiles
                        if (headWarningLayer.HasResults())
                        {
                            headWarningLayer.CompressLegend(CombinedResultLabel);
                            headWarningLayer.WriteResultFile(log);
                            resultHandler.AddExtraMapFiles(headWarningLayer.SourceFiles);
                        }

                        headWarningLayer.ReleaseMemory(true);
                    }
                }
            }
            settings.ReleaseMemory(true);
        }
    }
}
