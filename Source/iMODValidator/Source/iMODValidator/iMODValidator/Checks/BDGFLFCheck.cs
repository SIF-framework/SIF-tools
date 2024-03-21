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
using Sweco.SIF.iMOD;
using Sweco.SIF.iMOD.IDF;
using Sweco.SIF.iMODValidator.Actions;
using Sweco.SIF.iMODValidator.Checks.CheckResults;
using Sweco.SIF.iMODValidator.Models;
using Sweco.SIF.iMODValidator.Models.Packages;
using Sweco.SIF.Statistics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;

namespace Sweco.SIF.iMODValidator.Checks
{
    [TypeConverter(typeof(PropertySorter))]
    class BDGFLFCheckSettings : CheckSettings
    {
        private string minFLF;
        [Category("BDGFLF-properties"), Description("The minimum valid flf-value (mm/d) for this region"), PropertyOrder(10)]
        public string MinFLF
        {
            get { return minFLF; }
            set { minFLF = value; }
        }

        private string maxFLF;
        [Category("BDGFLF-properties"), Description("The maximum valid flf-value (mm/d) for this region"), PropertyOrder(11)]
        public string MaxFLF
        {
            get { return maxFLF; }
            set { maxFLF = value; }
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
        [Category("Outlier-properties"), Description("The number of cells in the buffer around each cell checked for outliers. Use 0 to use whole grid."), PropertyOrder(15)]
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
        [Category("Outlier-properties"), Description("Check only local extremes (min/max) value within surrounding cellsof cells (local 3x3-grid)"), PropertyOrder(16)]
        public bool CheckOnlyLocalExtremes
        {
            get { return checkOnlyLocalExtremes; }
            set { checkOnlyLocalExtremes = value; }
        }

        private float localExtremeTolerance;
        [Category("Outlier-properties"), Description("Maximum difference between minimum and maximum values in local 3x3-grid before marking a cell as an extreme"), PropertyOrder(17)]
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

        public BDGFLFCheckSettings(string checkName) : base(checkName)
        {
            minFLF = "-25";
            maxFLF = "25";
            outlierMethod = OutlierMethodEnum.HistogramGap;
            outlierMethodBaseRange = OutlierBaseRangeEnum.Pct95_5;
            outlierMethodMultiplier = 10f;                          // Note: 2*IQR corresponds to 3.5*sigma, 3.95*IQR corresponds with 6*Sigma (with sigma the standard deviation in a normal distribution)
            outlierBufferCellCount = 5;
            checkOnlyLocalExtremes = true;
            localExtremeTolerance = 0;
            isSubsetSelected = true;
            selectedYears = string.Empty;
            selectedMonths = string.Empty;
            SelectedDays = string.Empty;
        }

        public override void LogSettings(Log log, int logIndentLevel = 0)
        {
            log.AddInfo("Minimum flf: " + minFLF + "mNAP", logIndentLevel);
            log.AddInfo("Maximum flf: " + maxFLF + "mNAP", logIndentLevel);
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

    class BDGFLFCheck : Check
    {
        public override string Abbreviation
        {
            get { return "BDGFLF"; }
        }

        public override string Description
        {
            get { return "Checks BDGFLF modelresults"; }
        }

        private BDGFLFCheckSettings settings;
        public override CheckSettings Settings
        {
            get { return settings; }
            set
            {
                if (value is BDGFLFCheckSettings)
                {
                    settings = (BDGFLFCheckSettings)value;
                }
            }
        }

        public BDGFLFCheck()
        {
            settings = new BDGFLFCheckSettings(this.Name);
        }

        public override void Run(Model model, CheckResultHandler resultHandler, Log log)
        {
            try
            {
                log.AddInfo("Checking BDGFLF-files ...");

                if (model.ModelresultsPath == null)
                {
                    log.AddWarning(BDGFLFPackage.DefaultKey, null, "Model RESULTS-path not defined, check is skipped", 1);
                }
                else
                {
                    string bdgflfPath = Path.Combine(model.ModelresultsPath, "bdgflf");
                    if (!Directory.Exists(bdgflfPath))
                    {
                        log.AddWarning(BDGFLFPackage.DefaultKey, null, "BDGFLF RESULTS-path not found, check is skipped: " + bdgflfPath, 1);
                        return;
                    }

                    settings.LogSettings(log, 1);
                    RunFLFCheck1(model, resultHandler, log);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error in " + this.Name, ex);
            }
        }

        /// <summary>
        /// Checks range and outliers for FLF-files
        /// </summary>
        /// <param name="model"></param>
        /// <param name="resultHandler"></param>
        /// <param name="log"></param>
        protected virtual void RunFLFCheck1(Model model, CheckResultHandler resultHandler, Log log)
        {
            bool isSteadyState = false;
            string bdgflfPath = Path.Combine(model.ModelresultsPath, "bdgflf");

            // Try retrieving transient result files as bdgflf_yyyymmdd_l*.idf
            string[] bdgflfFilenames = Directory.GetFiles(bdgflfPath, "bdgflf_????????_l*.IDF");
            if (bdgflfFilenames.Length == 0)
            {
                bdgflfFilenames = Directory.GetFiles(bdgflfPath, "bdgflf_steady-state_l*.IDF");
                if (bdgflfFilenames.Length == 0)
                {
                    log.AddWarning(BDGFLFPackage.DefaultKey, null, "No 'BDGFLF_????????_L*.IDF' or 'BDGFLF_steady-state_L*.IDF'-files found in " + bdgflfPath);
                    return;
                }
                else
                {
                    isSteadyState = true;
                }
            }

            if (settings.IsSubsetSelected && !isSteadyState)
            {
                List<int> selectedYears = new List<int>(settings.ParseIntArrayString(settings.SelectedYears));
                List<int> selectedMonths = new List<int>(settings.ParseIntArrayString(settings.SelectedMonths));
                List<int> selectedDays = new List<int>(settings.ParseIntArrayString(settings.SelectedDays));
                bdgflfFilenames = SelectResultFileSubset(bdgflfFilenames, selectedYears, selectedMonths, selectedDays);
            }
            BDGFLFPackage bdgflfPackage = (BDGFLFPackage)PackageManager.Instance.CreatePackageInstance(BDGFLFPackage.DefaultKey, model);

            ////////////////////////////////
            // Define legends and results //
            ////////////////////////////////

            // Define warnings
            CheckWarning FLFRangeWarning;
            CheckWarning OutlierWarning;
            FLFRangeWarning = CreateCheckWarning("FLF outside the expected range ["
                + settings.MinFLF.ToString() + "," + settings.MaxFLF.ToString() + "]");
            OutlierWarning = CreateCheckWarning("Outlier");

            IDFLegend warningLegend = CreateIDFLegend();
            warningLegend.AddClass(FLFRangeWarning.CreateLegendValueClass(Color.Orange, true));
            warningLegend.AddClass(OutlierWarning.CreateLegendValueClass(Color.Red, true));
            warningLegend.AddUpperRangeClass(CombinedResultLabel, true);
            warningLegend.AddInbetweenClasses(CombinedResultLabel, true);

            ///////////////////////////
            // Retrieve settingfiles //
            ///////////////////////////
            IDFFile minFLFSettingIDFFile = settings.GetIDFFile(settings.MinFLF, log, 1);
            IDFFile maxFLFSettingIDFFile = settings.GetIDFFile(settings.MaxFLF, log, 1);

            // Process all BDGFLF-files
            log.AddInfo("Checking BDGFLF-files in folder: " + bdgflfPath + " ...", 1);
            bool checkOnlyLocalExtremes = settings.CheckOnlyLocalExtremes;
            float localExtremeTolerance = Math.Max(resultHandler.LevelErrorMargin, settings.LocalExtremeTolerance);
            for (int bdgflfFileIdx = 0; bdgflfFileIdx < bdgflfFilenames.Length; bdgflfFileIdx++)
            {
                string bdgflfFilename = bdgflfFilenames[bdgflfFileIdx];
                int ilay = GetLayerNumber(bdgflfFilename);
                if ((ilay >= resultHandler.MinEntryNumber) && (ilay <= resultHandler.MaxEntryNumber))
                {
                    string sname = GetStressPeriodString(bdgflfFilename);
                    //if ((model.StartDate == null) && !isSteadyState)
                    //{
                    //    model.SDATE = long.Parse(stressPeriodString);
                    //}
                    int kper = model.RetrieveKPER(sname);

                    if ((kper >= resultHandler.MinKPER) && (kper <= resultHandler.MaxKPER))
                    {
                        log.AddInfo("Checking BDGFLF-file " + Path.GetFileName(bdgflfFilename) + " ...", 1);
                        IDFFile bdgflfIDFFile = IDFFile.ReadFile(bdgflfFilename, false, log, 2);

                        IDFCellIterator idfCellIterator = new IDFCellIterator(resultHandler.Extent);
                        idfCellIterator.AddIDFFile(bdgflfIDFFile);

                        // Create warning IDFfile for bdgflf-file
                        CheckWarningLayer bdgflfWarningLayer = CreateWarningLayer(resultHandler, bdgflfPackage, null, StressPeriod.SteadyState, ilay, idfCellIterator.XStepsize, warningLegend);
                        bdgflfWarningLayer.AddSourceFile(bdgflfIDFFile);

                        float bdgflfOutlierRangeLowerValue = float.NaN;
                        float bdgflfOutlierRangeUpperValue = float.NaN;
                        if (settings.OutlierBufferCellCount == 0)
                        {
                            // Compute outlier statistics for whole grid if specified (buffercellcount == 0)
                            IDFStatistics bdgflfStats = new IDFStatistics(bdgflfIDFFile);
                            bdgflfStats.ComputeOutlierStatistics(settings.OutlierMethod, settings.OutlierMethodBaseRange, settings.OutlierMethodMultiplier, true, false);
                            bdgflfOutlierRangeLowerValue = bdgflfStats.OutlierRangeLowerValue;
                            bdgflfOutlierRangeUpperValue = bdgflfStats.OutlierRangeUpperValue;
                        }

                        float bdgflfConversionFactor = (bdgflfIDFFile.XCellsize * bdgflfIDFFile.YCellsize) / 1000f;

                        // Iterate through cells
                        idfCellIterator.Reset();
                        while (idfCellIterator.IsInsideExtent())
                        {
                            float bdgflfValue = idfCellIterator.GetCellValue(bdgflfIDFFile);
                            float x = idfCellIterator.X;
                            float y = idfCellIterator.Y;

                            if (!bdgflfValue.Equals(bdgflfIDFFile.NoDataValue))
                            {
                                if (settings.OutlierBufferCellCount > 0)
                                {
                                    // Compute local outlier statistics if specified (buffercellcount > 0)
                                    IDFStatistics bdgflfStats = new IDFStatistics(bdgflfIDFFile, x, y, settings.OutlierBufferCellCount);
                                    bdgflfStats.ComputeOutlierStatistics(settings.OutlierMethod, settings.OutlierMethodBaseRange, settings.OutlierMethodMultiplier, true, false);
                                    bdgflfOutlierRangeLowerValue = bdgflfStats.OutlierRangeLowerValue;
                                    bdgflfOutlierRangeUpperValue = bdgflfStats.OutlierRangeUpperValue;
                                }

                                float minBDGFLF = minFLFSettingIDFFile.GetValue(x, y) * bdgflfConversionFactor;
                                float maxBDGFLF = maxFLFSettingIDFFile.GetValue(x, y) * bdgflfConversionFactor;

                                if ((bdgflfValue < minBDGFLF) || (bdgflfValue > maxBDGFLF))
                                {
                                    resultHandler.AddCheckResult(bdgflfWarningLayer, x, y, FLFRangeWarning);
                                }

                                if ((bdgflfValue < bdgflfOutlierRangeLowerValue) || (bdgflfValue > bdgflfOutlierRangeUpperValue))
                                {
                                    // Value is an outlier, but also check that it is a local min or max value in the surrounding 3x3-gid.
                                    if (!checkOnlyLocalExtremes || bdgflfIDFFile.IsMinMaxValue(x, y, 1, localExtremeTolerance))
                                    {
                                        resultHandler.AddCheckResult(bdgflfWarningLayer, x, y, OutlierWarning);
                                    }
                                }
                            }

                            idfCellIterator.MoveNext();
                        }

                        // Write warningfiles
                        if (bdgflfWarningLayer.HasResults())
                        {
                            bdgflfWarningLayer.CompressLegend(CombinedResultLabel);
                            bdgflfWarningLayer.WriteResultFile(log);
                            resultHandler.AddExtraMapFiles(bdgflfWarningLayer.SourceFiles);

                            IDFFile flfIDFFile = bdgflfIDFFile.CopyIDF(Path.Combine(FileUtils.EnsureFolderExists(GetIMODFilesPath(model), Name), Path.GetFileNameWithoutExtension(bdgflfIDFFile.Filename) + "_mm" + ".IDF"));
                            flfIDFFile.Multiply(1000f / (bdgflfIDFFile.XCellsize * bdgflfIDFFile.YCellsize));
                            flfIDFFile.WriteFile(new Metadata("BDGFLF-file converted to mm"));
                            resultHandler.AddExtraMapFile(flfIDFFile);
                        }

                        bdgflfWarningLayer.ReleaseMemory(true);
                    }
                }
            }
            settings.ReleaseMemory(true);
        }

        public static string GetStressPeriodString(string resultFilename)
        {
            string fname = Path.GetFileNameWithoutExtension(resultFilename);
            int underScoreIdx1 = fname.IndexOf("_");
            if ((underScoreIdx1 > 0) && (underScoreIdx1 < fname.Length - 1))
            {
                int underScoreIdx2 = fname.IndexOf("_", underScoreIdx1 + 1);
                if (underScoreIdx2 > 0)
                {
                    string stressPeriodString = fname.Substring(underScoreIdx1 + 1, (underScoreIdx2 - underScoreIdx1 - 1));
                    if (!IsStressPeriodString(stressPeriodString))
                    {
                        stressPeriodString = string.Empty;
                    }
                    return stressPeriodString;
                }
            }

            return string.Empty;
        }

        public static bool IsStressPeriodString(string stressPeriodString)
        {
            if (!int.TryParse(stressPeriodString, out int stressPeriodInt))
            {
                return stressPeriodString.ToUpper().Equals(StressPeriod.SteadyStateSNAME);
            }

            try
            {
                if (stressPeriodString.Length != 8)
                {
                    return false;
                }
                int year = int.Parse(stressPeriodString.Substring(0, 4));
                int month = int.Parse(stressPeriodString.Substring(4, 2));
                int day = int.Parse(stressPeriodString.Substring(6, 2));
                DateTime date = new DateTime(year, month, day);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static int GetLayerNumber(string headFilename)
        {
            string fname = Path.GetFileNameWithoutExtension(headFilename);
            int underScoreIdx1 = fname.IndexOf("_");
            if (underScoreIdx1 > 0)
            {
                int underScoreIdx2 = fname.IndexOf("_", underScoreIdx1 + 1);
                if (underScoreIdx2 > 0)
                {
                    return int.Parse(fname.Substring(underScoreIdx2 + 2));
                }
                else
                {
                    return int.Parse(fname.Substring(underScoreIdx1 + 2));
                }
            }

            throw new Exception("Could not parse layernumber for HEAD-file: " + Path.GetFileName(headFilename));
        }
    }
}
