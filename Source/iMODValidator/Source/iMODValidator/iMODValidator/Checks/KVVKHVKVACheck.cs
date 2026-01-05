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
    class KVVKHVKVACheckSettings : CheckSettings
    {
        public const float kValueErrorMargin = 0.001f;

        [Category("KHV-properties"), Description("The minimum valid KHV-value for this region"), PropertyOrder(10)]
        public string MinKHVValue { get; set; }

        [Category("KHV-properties"), Description("The maximum valid KHV-value for this region"), PropertyOrder(11)]
        public string MaxKHVValue { get; set; }

        [Category("KHV-properties"), Description("Comma-seperated list of extreme, valid integer KHV-values (e.g. for lakes) for this region. If grid value, higher than MaxKHVValue, equals one of these values (within errormargin), no range warning is given."), PropertyOrder(12)]
        public string ValidKHVValues { get; set; }

        [Category("KHV-properties"), Description("Specifies that outliers should be searched"), PropertyOrder(13)]
        public bool IsOutlierChecked { get; set; }

        [Category("KHV-properties"), Description("The method for identifying spatial outliers"), PropertyOrder(14)]
        public OutlierMethodEnum OutlierMethod { get; set; }

        [Category("KHV-properties"), Description("The valid base range for identifying outliers"), PropertyOrder(15)]
        public OutlierBaseRangeEnum KHVOutlierMethodBaseRange { get; set; }

        [Category("KHV-properties"), Description("The factor to multiply the base range"), PropertyOrder(16)]
        public float KHVOutlierMethodMultiplier { get; set; }

        [Category("KVV-properties"), Description("The minimum valid KVV-value for this region"), PropertyOrder(17)]
        public string MinKVVValue { get; set; }

        [Category("KVV-properties"), Description("The maximum valid KVV-value for this region"), PropertyOrder(18)]
        public string MaxKVVValue { get; set; }

        [Category("KVV-properties"), Description("Comma-seperated list of extreme, valid integer KVV-values (e.g. for lakes) for this region. If grid value, higher than MaxKVVValue, equals one of these values (within errormargin), no range warning is given."), PropertyOrder(19)]
        public string ValidKVVValues { get; set; }

        [Category("KVV-properties"), Description("The valid base range for identifying outliers"), PropertyOrder(20)]
        public OutlierBaseRangeEnum KVVOutlierMethodBaseRange { get; set; }

        [Category("KVV-properties"), Description("The factor to multiply the base range"), PropertyOrder(21)]
        public float KVVOutlierMethodMultiplier { get; set; }

        [Category("KVA-properties"), Description("The minimum valid KVA-value for this region"), PropertyOrder(22)]
        public string MinKVAValue { get; set; }

        [Category("KVA-properties"), Description("The maximum valid KVA-value for this region"), PropertyOrder(23)]
        public string MaxKVAValue { get; set; }

        [Category("KVA-properties"), Description("The maximum valid KVA-value in L1 for this region (or empty to ignore and use maxKVAValue)"), PropertyOrder(24)]
        public string MaxKVAL1Value { get; set; }

        [Category("KVA-properties"), Description("KVA-value that is allowed for dummy layers (with thickness zero) to prevent crash with PRJ/RUN-file"), PropertyOrder(24)]
        public string DummyKVAValue { get; set; }

        public KVVKHVKVACheckSettings(string checkName) : base(checkName)
        {
            OutlierMethod = OutlierMethodEnum.IQR;
            MinKHVValue = ((float)0.1f).ToString(englishCultureInfo);
            MaxKHVValue = "250";
            ValidKHVValues = string.Empty;
            IsOutlierChecked = false;
            KHVOutlierMethodBaseRange = OutlierBaseRangeEnum.Pct95_5;
            KHVOutlierMethodMultiplier = 3.95f;
            MinKVAValue = "0.0000001";
            MaxKVAValue = "1";
            MaxKVAL1Value = "5000";
            DummyKVAValue = "1";
            MinKVVValue = ((float)0.0000001f).ToString(englishCultureInfo); ;
            MaxKVVValue = ((float)1.0f).ToString(englishCultureInfo);
            ValidKVVValues = string.Empty;
            KVVOutlierMethodBaseRange = OutlierBaseRangeEnum.Pct95_5;
            KVVOutlierMethodMultiplier = 3.95f;
        }

        public override void LogSettings(Log log, int logIndentLevel = 0)
        {
            log.AddInfo("Minimum KHV-value: " + MinKHVValue + " m/d", logIndentLevel);
            log.AddInfo("Maximum KHV-value: " + MaxKHVValue + " m/d", logIndentLevel);
            log.AddInfo("Maximum KHV-value: " + MaxKHVValue + " m/d", logIndentLevel);
            if (!MaxKVAL1Value.Equals(string.Empty))
            {
                log.AddInfo("Extreme, valid KHV-values L1: " + MaxKVAL1Value, logIndentLevel);
            }
            if (!MaxKVAL1Value.Equals(string.Empty))
            {
                log.AddInfo("Maximum KVA-value L1: " + MaxKVAL1Value, logIndentLevel);
            }
            if (!DummyKVAValue.Equals(string.Empty))
            {
                log.AddInfo("KVA-value for dummy layers: " + DummyKVAValue, logIndentLevel);
            }
            log.AddInfo("Maximum KVA-value: " + MaxKVAValue, logIndentLevel);
            log.AddInfo("Minimum KVV-value: " + MinKVVValue + " m/d", logIndentLevel);
            log.AddInfo("Maximum KVV-value: " + MaxKVVValue + " m/d", logIndentLevel);
            if (IsOutlierChecked)
            {
                log.AddInfo("Outlier method: " + OutlierMethod.ToString(), logIndentLevel);
                log.AddInfo("KHV outlier base range method: " + KHVOutlierMethodBaseRange.ToString(), logIndentLevel);
                log.AddInfo("KHV outlier base range multiplier: " + KHVOutlierMethodMultiplier.ToString(), logIndentLevel);
                log.AddInfo("KVV outlier base range method: " + KVVOutlierMethodBaseRange.ToString(), logIndentLevel);
                log.AddInfo("KVV Outlier base range multiplier: " + KVVOutlierMethodMultiplier.ToString(), logIndentLevel);
            }
            else
            {
                log.AddInfo("Outliers are not checked", logIndentLevel);
            }
        }
    }

    class KVVKHVKVACheck : Check
    {
        // Define available errors and warnings
        protected CheckError KHVZeroValueError;
        protected CheckError KHVNegativeValueError;
        protected CheckError KHVInconsistentValuesError;
        protected CheckError KVVZeroValueError;
        protected CheckError KVVNegativeValueError;
        protected CheckError KVVInconsistentValuesError;
        protected CheckError KVANoDataValueError;
        protected CheckError KVAZeroValueError;
        protected CheckError KVANegativeValueError;
        protected CheckError KVAInconsistentValuesError;
        protected CheckWarning KHVNotZeroValueWarning;
        protected CheckWarning KVVNotZeroValueWarning;
        protected CheckWarning KVANonNoDataValueWarning;
        protected CheckWarning KHVRangeWarning;
        protected CheckWarning KVVRangeWarning;
        protected CheckWarning KVARangeWarning;
        protected CheckWarning KHVOutlierWarning;
        protected CheckWarning KVVOutlierWarning;
        //protected CheckWarning KVAOutlierWarning;

        // Define legends
        protected IDFLegend khvErrorLegend;
        protected IDFLegend kvvErrorLegend;
        protected IDFLegend kvaErrorLegend;
        protected IDFLegend khvWarningLegend;
        protected IDFLegend kvvWarningLegend;
        protected IDFLegend kvaWarningLegend;

        public override string Abbreviation
        {
            get { return "KHV-KVV-KVA"; }
        }

        public override string Description
        {
            get { return "Checks KHV-, KVV-, KVA- versus TOP- and BOT-values per layer"; }
        }

        private KVVKHVKVACheckSettings settings;
        public override CheckSettings Settings
        {
            get { return settings; }
            set
            {
                if (value is KVVKHVKVACheckSettings)
                {
                    settings = (KVVKHVKVACheckSettings)value;
                }
            }
        }

        public KVVKHVKVACheck()
        {
            settings = new KVVKHVKVACheckSettings(this.Name);
        }

        private void CreateCheckResultAndLegends()
        {
            // Define KHV Errors
            KHVZeroValueError = new CheckError(1, "Unexpected zero value", "The value is not, but should be, greater than zero (aquifer-thickness is greater than zero).");
            KHVNegativeValueError = new CheckError(2, "Unexpected negative value", "The value is not, but should be, greater than zero (aquifer-thickness is greater than zero).");
            KHVInconsistentValuesError = new CheckError(4, "Inconsistent values", "TOP, BOT and/or KHV is unexpectedly undefined");
            khvErrorLegend = new IDFLegend("Legend for KHV-file check");
            khvErrorLegend.AddClass(new ValueLegendClass(0, "No errors found", Color.White));
            khvErrorLegend.AddClass(KHVZeroValueError.CreateLegendValueClass(Color.Red, true));
            khvErrorLegend.AddClass(KHVNegativeValueError.CreateLegendValueClass(Color.Firebrick, true));
            khvErrorLegend.AddClass(KHVInconsistentValuesError.CreateLegendValueClass(Color.Orange, true));
            khvErrorLegend.AddUpperRangeClass(CombinedResultLabel, true);
            khvErrorLegend.AddInbetweenClasses(CombinedResultLabel, true);

            // Define KVA Errors
            KVANoDataValueError = new CheckError(1, "Unexpected NoData value", "The value is, but should not be, NoData (aquitard-thickness is greater than zero).");
            KVAZeroValueError = new CheckError(2, "Unexpected zero value", "The KVA-value should never be equal to zero (use NoData if thickness is zero).");
            KVANegativeValueError = new CheckError(4, "Unexpected value", "The value is not, but should be, greater than or equal to zero");
            KVAInconsistentValuesError = new CheckError(8, "Inconsistent values", "TOP, BOT and/or KHV is unexpectedly undefined");
            kvaErrorLegend = new IDFLegend("Legend for KVA-file check");
            kvaErrorLegend.AddClass(new ValueLegendClass(0, "No errors found", Color.White));
            kvaErrorLegend.AddClass(KVANoDataValueError.CreateLegendValueClass(Color.Orange, true));
            kvaErrorLegend.AddClass(KVAZeroValueError.CreateLegendValueClass(Color.Red, true));
            kvaErrorLegend.AddClass(KVANegativeValueError.CreateLegendValueClass(Color.Firebrick, true));
            kvaErrorLegend.AddClass(KVAInconsistentValuesError.CreateLegendValueClass(Color.Purple, true));
            kvaErrorLegend.AddUpperRangeClass(CombinedResultLabel, true);
            kvaErrorLegend.AddInbetweenClasses(CombinedResultLabel, true);

            // Define KVV Errors
            KVVZeroValueError = new CheckError(1, "Unexpected zero value", "The value is not, but should be, greater than zero (aquitard-thickness is greater than zero).");
            KVVNegativeValueError = new CheckError(2, "Unexpected negative value", "The value is not, but should be, greater than zero (aquitard-thickness is greater than zero).");
            KVVInconsistentValuesError = new CheckError(4, "Inconsistent values", "TOP, BOT and/or KVV is unexpectedly undefined");
            kvvErrorLegend = new IDFLegend("Legend for KVV-file check");
            kvvErrorLegend.AddClass(new ValueLegendClass(0, "No errors found", Color.White));
            kvvErrorLegend.AddClass(KVVZeroValueError.CreateLegendValueClass(Color.Red, true));
            kvvErrorLegend.AddClass(KVVNegativeValueError.CreateLegendValueClass(Color.Firebrick, true));
            kvvErrorLegend.AddClass(KVVInconsistentValuesError.CreateLegendValueClass(Color.Orange, true));
            kvvErrorLegend.AddUpperRangeClass(CombinedResultLabel, true);
            kvvErrorLegend.AddInbetweenClasses(CombinedResultLabel, true);

            // Define KHV warnings
            KHVNotZeroValueWarning = new CheckWarning(1, "Unexpected non-zero value", "The value is not, but should be, zero or NoData (aquifer-thickness is zero).");
            KHVRangeWarning = new CheckWarning(2, "Value outside expected range", "Value outside expected range ["
                + settings.MinKHVValue.ToString() + "," + settings.MaxKHVValue.ToString());
            khvWarningLegend = new IDFLegend("Legend for KHV-file check");
            khvWarningLegend.AddClass(new ValueLegendClass(0, "No warnings found", Color.White));
            khvWarningLegend.AddClass(KHVNotZeroValueWarning.CreateLegendValueClass(Color.Blue, true));
            khvWarningLegend.AddClass(KHVRangeWarning.CreateLegendValueClass(Color.Orange, true));
            if (settings.IsOutlierChecked)
            {
                KHVOutlierWarning = new CheckWarning(4, "Outlier");
                khvWarningLegend.AddClass(KHVOutlierWarning.CreateLegendValueClass(Color.Red, true));
                khvWarningLegend.AddUpperRangeClass(CombinedResultLabel, true);
                khvWarningLegend.AddInbetweenClasses(CombinedResultLabel, true);
            }

            // Define KVA warnings
            KVANonNoDataValueWarning = new CheckWarning(1, "Unexpected non-NoData value", "The value is not, but should be NoData (aquifer-thickness is zero).");
            KVARangeWarning = new CheckWarning(2, "Value outside expected range [0," + settings.MaxKVAValue.ToString());
            //KVAOutlierWarning = new CheckWarning(8, "Outlier");
            kvaWarningLegend = new IDFLegend("Legend for KVA-file check");
            kvaWarningLegend.AddClass(new ValueLegendClass(0, "No warnings found", Color.White));
            kvaWarningLegend.AddClass(KVANonNoDataValueWarning.CreateLegendValueClass(Color.Blue, true));
            kvaWarningLegend.AddClass(KVARangeWarning.CreateLegendValueClass(Color.Orange, true));
            //if (settings.IsOutlierChecked)
            //{
            //    kvaWarningLegend.AddClass(KVAOutlierWarning.CreateLegendValueClass(Color.Red, true));
            //}
            // kvaWarningLegend.AddDefaultUpperRangeClass(CombinedResultLabel, true);
            // kvaWarningLegend.AddInbetweenClasses(CombinedResultLabel, true);

            // Define KVV warnings
            KVVNotZeroValueWarning = new CheckWarning(1, "Unexpected non-zero value", "The value is not, but should be, zero or NoData (aquitard-thickness is zero).");
            KVVRangeWarning = new CheckWarning(2, "Value outside expected range ["
                + settings.MinKVVValue.ToString() + "," + settings.MaxKVVValue.ToString());
            kvvWarningLegend = new IDFLegend("Legend for KVV-file check");
            kvvWarningLegend.AddClass(new ValueLegendClass(0, "No warnings found", Color.White));
            kvvWarningLegend.AddClass(KVVNotZeroValueWarning.CreateLegendValueClass(Color.Blue, true));
            kvvWarningLegend.AddClass(KVVRangeWarning.CreateLegendValueClass(Color.Orange, true));
            if (settings.IsOutlierChecked)
            {
                KVVOutlierWarning = new CheckWarning(4, "Outlier");
                kvvWarningLegend.AddClass(KVVOutlierWarning.CreateLegendValueClass(Color.Red, true));
                kvvWarningLegend.AddUpperRangeClass(CombinedResultLabel, true);
                kvvWarningLegend.AddInbetweenClasses(CombinedResultLabel, true);
            }
        }

        public override void Run(Model model, CheckResultHandler resultHandler, Log log)
        {
            CreateCheckResultAndLegends();

            log.AddInfo("Checking KVV-, KHV, and KVA-packages ...");
            settings.LogSettings(log, 1);
            RunKvvKhvCheck1(model, resultHandler, log);
        }

        protected virtual void RunKvvKhvCheck1(Model model, CheckResultHandler resultHandler, Log log)
        {
            // Retrieve used packages
            IDFPackage topPackage = (IDFPackage)model.GetPackage(TOPPackage.DefaultKey);
            IDFPackage botPackage = (IDFPackage)model.GetPackage(BOTPackage.DefaultKey);
            IDFPackage khvPackage = (IDFPackage)model.GetPackage(KHVPackage.DefaultKey);
            IDFPackage kvvPackage = (IDFPackage)model.GetPackage(KVVPackage.DefaultKey);
            IDFPackage kvaPackage = (IDFPackage)model.GetPackage(KVAPackage.DefaultKey);

            // Check used packages
            if ((topPackage == null) || !topPackage.IsActive)
            {
                log.AddWarning(this.Name, model.RUNFilename, "TOP-package is not active. " + this.Name + " is skipped.", 1);
                return;
            }
            if ((botPackage == null) || !topPackage.IsActive)
            {
                log.AddWarning(this.Name, model.RUNFilename, "BOT-package is not active. " + this.Name + " is skipped.", 1);
                return;
            }
            if (((kvvPackage == null) || !kvvPackage.IsActive) && ((kvaPackage == null) || !kvaPackage.IsActive))
            {
                log.AddWarning(this.Name, model.RUNFilename, "KVV-package and/or KVA-package is not active. " + this.Name + " is skipped.", 1);
                return;
            }
            if ((khvPackage == null) || !khvPackage.IsActive)
            {
                log.AddWarning(this.Name, model.RUNFilename, "KHV-package is not active. " + this.Name + " is skipped.", 1);
                return;
            }

            // Retrieve IDF settingfiles
            IDFFile minKHVSettingIDFFile = settings.GetIDFFile(settings.MinKHVValue, log, 1);
            IDFFile maxKHVSettingIDFFile = settings.GetIDFFile(settings.MaxKHVValue, log, 1);
            IDFFile minKVASettingIDFFile = settings.GetIDFFile(settings.MinKVAValue, log, 1);
            IDFFile maxKVASettingIDFFile = settings.GetIDFFile(settings.MaxKVAValue, log, 1);
            IDFFile maxKVAL1SettingIDFFile = settings.GetIDFFile(settings.MaxKVAL1Value, log, 1);
            IDFFile dummyKVAValueIDFFile = settings.GetIDFFile(settings.DummyKVAValue, log, 1);
            IDFFile minKVVSettingIDFFile = settings.GetIDFFile(settings.MinKVVValue, log, 1);
            IDFFile maxKVVSettingIDFFile = settings.GetIDFFile(settings.MaxKVVValue, log, 1);

            // Retrieve other settings
            List<float> validKHVValues = settings.GetValues(settings.ValidKHVValues);
            List<float> validKVVValues = settings.GetValues(settings.ValidKVVValues);

            // Retrieve ErrorMargin
            float levelErrorMargin = resultHandler.LevelErrorMargin;

            // Process all entries
            for (int entryIdx = resultHandler.MinEntryNumber - 1; (entryIdx < topPackage.GetEntryCount()) && (entryIdx < resultHandler.MaxEntryNumber); entryIdx++)
            {
                log.AddInfo("Checking entry " + (entryIdx + 1) + " with " + Name + " ...", 1);
                int ilay = (khvPackage.GetPackageFile(entryIdx) != null) ? khvPackage.GetPackageFile(entryIdx).ILAY : -1;

                log.AddInfo("Checking ilay " + (ilay) + " ...", 1);

                // Retrieve IDF-files for current layer
                IDFFile topIDFFile = topPackage.GetIDFFile(entryIdx);
                IDFFile botIDFFile = botPackage.GetIDFFile(entryIdx);
                IDFFile khvIDFFile = khvPackage.GetIDFFile(entryIdx);
                IDFFile kvvIDFFile = ((kvvPackage != null) && (entryIdx < kvvPackage.GetEntryCount())) ? kvvPackage.GetIDFFile(entryIdx) : null; // Check for non-existing last kvv-layer
                IDFFile kvaIDFFile = (kvaPackage != null) ? kvaPackage.GetIDFFile(entryIdx) : null;
                IDFFile lowerTOPIDFFile = ((topPackage != null) && (entryIdx < topPackage.GetEntryCount() - 1)) ? topPackage.GetIDFFile(entryIdx + 1) : null; // If there is a layer below the current layer retrieve its TOP-file
                IDFFile lowerBOTIDFFile = ((botPackage != null) && (entryIdx < botPackage.GetEntryCount() - 1)) ? botPackage.GetIDFFile(entryIdx + 1) : null; // If there is a layer below the current layer retrieve its BOT-file
                
                // Check retrieved IDF-files
                if (topIDFFile == null)
                {
                    log.AddWarning(topPackage.Key, null, "TOP IDF-file missing for entry " + (entryIdx + 1) + ", check is canceled", 1);
                    return;
                }
                if (botIDFFile == null)
                {
                    log.AddWarning(botPackage.Key, null, "BOT IDF-file missing for entry " + (entryIdx + 1) + ", check is canceled", 1);
                    return;
                }
                if (khvIDFFile == null)
                {
                    log.AddWarning(khvPackage.Key, null, "KHV IDF-file missing for entry " + (entryIdx + 1) + ", check is canceled", 1);
                    return;
                }
                if ((kvvPackage != null) && (kvvIDFFile == null) && (entryIdx < kvvPackage.GetEntryCount()))
                {
                    log.AddWarning(kvvPackage.Key, null, "KVV IDF-file missing for entry " + (entryIdx + 1) + ", check is canceled", 1);
                    return;
                }
                if ((lowerTOPIDFFile == null) && (entryIdx < topPackage.GetEntryCount() - 1))
                {
                    log.AddWarning(topPackage.Key, null, "TOP IDF-file missing for entry " + (entryIdx + 2) + ", check is canceled", 1);
                    return;
                }
                if ((lowerBOTIDFFile == null) && (entryIdx < botPackage.GetEntryCount() - 1))
                {
                    log.AddWarning(botPackage.Key, null, "BOT IDF-file missing for entry " + (entryIdx + 2) + ", check is canceled", 1);
                    return;
                }
                if ((kvaPackage != null) && (kvaIDFFile == null))
                {
                    log.AddWarning(kvaPackage.Key, null, "KVA IDF-file not defined for entry " + (entryIdx + 1) + ", using default value 0", 1);
                    kvaIDFFile = new ConstantIDFFile(0);
                }

                bool isOutlierChecked = settings.IsOutlierChecked;
                double khvRangeUpperValue = double.NaN;
                double kvvRangeUpperValue = double.NaN;
                if (isOutlierChecked)
                {
                    // Retrieve statistics for outliers
                    log.AddInfo("Computing outlier statistics...", 1);
                    IDFStatistics khvStats = new IDFStatistics(khvIDFFile);
                    khvStats.AddSkippedValue(0.0f);
                    khvStats.ComputeOutlierStatistics(settings.OutlierMethod, settings.KHVOutlierMethodBaseRange, settings.KHVOutlierMethodMultiplier);
                    khvRangeUpperValue = khvStats.OutlierRangeUpperValue;
                    khvStats.ReleaseValuesMemory();
                    IDFStatistics kvvStats = null;
                    if (kvvIDFFile != null)
                    {
                        kvvStats = new IDFStatistics(kvvIDFFile);
                        kvvStats.AddSkippedValue(0.0f);
                        kvvStats.ComputeOutlierStatistics(settings.OutlierMethod, settings.KVVOutlierMethodBaseRange, settings.KVVOutlierMethodMultiplier);
                        kvvRangeUpperValue = kvvStats.OutlierRangeUpperValue;
                        kvvStats.ReleaseValuesMemory();
                    }
                }

                // Create cell iterator
                IDFCellIterator idfCellIterator = new IDFCellIterator(resultHandler.Extent);
                idfCellIterator.AddIDFFile(topIDFFile);
                idfCellIterator.AddIDFFile(botIDFFile);
                idfCellIterator.AddIDFFile(khvIDFFile);
                idfCellIterator.AddIDFFile(kvvIDFFile);
                idfCellIterator.AddIDFFile(kvaIDFFile);
                idfCellIterator.AddIDFFile(lowerTOPIDFFile);
                idfCellIterator.AddIDFFile(lowerBOTIDFFile);
                idfCellIterator.AddIDFFile(minKHVSettingIDFFile);
                idfCellIterator.AddIDFFile(maxKHVSettingIDFFile);
                idfCellIterator.AddIDFFile(maxKVASettingIDFFile);
                idfCellIterator.AddIDFFile(maxKVAL1SettingIDFFile);
                idfCellIterator.AddIDFFile(dummyKVAValueIDFFile);
                idfCellIterator.AddIDFFile(minKVVSettingIDFFile);
                idfCellIterator.AddIDFFile(maxKVVSettingIDFFile);
                idfCellIterator.CheckExtent(log, 2, LogLevel.Debug);

                // Create error IDFfiles for current layer
                CheckErrorLayer khvErrorLayer = CreateErrorLayer(resultHandler, khvPackage, null, StressPeriod.SteadyState, entryIdx + 1, idfCellIterator.XStepsize, khvErrorLegend);
                khvErrorLayer.AddSourceFile(khvIDFFile);

                CheckErrorLayer kvvErrorLayer = null;
                if (kvvIDFFile != null)
                {
                    kvvErrorLayer = CreateErrorLayer(resultHandler, kvvPackage, null, StressPeriod.SteadyState, entryIdx + 1, idfCellIterator.XStepsize, kvvErrorLegend);
                    kvvErrorLayer.AddSourceFile(kvvIDFFile);
                }

                CheckErrorLayer kvaErrorLayer = null;
                if (kvaIDFFile != null)
                {
                    kvaErrorLayer = CreateErrorLayer(resultHandler, kvaPackage, null, StressPeriod.SteadyState, entryIdx + 1, idfCellIterator.XStepsize, kvaErrorLegend);
                    kvaErrorLayer.AddSourceFile(kvaIDFFile);
                }

                // Create warning IDFfiles for current layer
                CheckWarningLayer khvWarningLayer = CreateWarningLayer(resultHandler, khvPackage, null, StressPeriod.SteadyState, entryIdx + 1, idfCellIterator.XStepsize, khvWarningLegend);
                khvWarningLayer.AddSourceFile(khvIDFFile);

                CheckWarningLayer kvvWarningLayer = null;
                if (kvvIDFFile != null)
                {
                    kvvWarningLayer = CreateWarningLayer(resultHandler, kvvPackage, null, StressPeriod.SteadyState, entryIdx + 1, idfCellIterator.XStepsize, kvvWarningLegend);
                    kvvWarningLayer.AddSourceFile(kvvIDFFile);
                }

                CheckWarningLayer kvaWarningLayer = CreateWarningLayer(resultHandler, kvaPackage, null, StressPeriod.SteadyState, entryIdx + 1, idfCellIterator.XStepsize, kvaWarningLegend);
                kvaWarningLayer.AddSourceFile(kvaIDFFile);

                // Iterate through cells
                idfCellIterator.Reset();
                float x = idfCellIterator.X;
                float y = idfCellIterator.Y;
                try
                {
                    while (idfCellIterator.IsInsideExtent())
                    {
                        // retrieve cell-values
                        float topValue = idfCellIterator.GetCellValue(topIDFFile);
                        float botValue = idfCellIterator.GetCellValue(botIDFFile);
                        float khvValue = idfCellIterator.GetCellValue(khvIDFFile);
                        float kvvValue = idfCellIterator.GetCellValue(kvvIDFFile);
                        float kvaValue = idfCellIterator.GetCellValue(kvaIDFFile);
                        float lowerTOPValue = idfCellIterator.GetCellValue(lowerTOPIDFFile);
                        float lowerBOTValue = idfCellIterator.GetCellValue(lowerBOTIDFFile);
                        float minKHVValue = idfCellIterator.GetCellValue(minKHVSettingIDFFile);
                        float maxKHVValue = idfCellIterator.GetCellValue(maxKHVSettingIDFFile);
                        float minKVAValue = idfCellIterator.GetCellValue(minKVASettingIDFFile);
                        float maxKVAValue = idfCellIterator.GetCellValue(maxKVASettingIDFFile);
                        float maxKVAL1Value = idfCellIterator.GetCellValue(maxKVAL1SettingIDFFile);
                        float dummyKVAValue = idfCellIterator.GetCellValue(dummyKVAValueIDFFile);
                        float minKVVValue = idfCellIterator.GetCellValue(minKVVSettingIDFFile);
                        float maxKVVValue = idfCellIterator.GetCellValue(maxKVVSettingIDFFile);

                        ///////////////////
                        // Check aquifer //
                        ///////////////////

                        float aquiferThickness = float.NaN;
                        float lowerAquiferThickness = float.NaN;
                        if (!topValue.Equals(topIDFFile.NoDataValue))
                        {
                            // aquifer TOP-value is defined
                            if (!botValue.Equals(botIDFFile.NoDataValue))
                            {
                                // aquifer TOP- and BOT-value are defined
                                if (topValue.Equals(botValue) || (topValue < botValue))
                                {
                                    // aquifer has no thickness, KHV should be zero or NoData
                                    if (!khvValue.Equals(0) && !khvValue.Equals(khvIDFFile.NoDataValue))
                                    {
                                        resultHandler.AddCheckResult(khvWarningLayer, x, y, KHVNotZeroValueWarning);

                                        // Also check for a range warning
                                        if (khvValue < 0)
                                        {
                                            resultHandler.AddCheckResult(khvErrorLayer, x, y, KHVNegativeValueError);
                                        }

                                        if (khvValue > maxKHVValue)
                                        {
                                            if (!ContainsValue(validKHVValues, khvValue, KVVKHVKVACheckSettings.kValueErrorMargin))
                                            {
                                                resultHandler.AddCheckResult(khvWarningLayer, x, y, KHVRangeWarning);
                                            }
                                        }
                                        else if (khvValue < minKHVValue)
                                        {
                                            // Only add KHVRangeWarning below minValue if there's also an aquitard defined below
                                            if ((botValue - lowerTOPValue) > levelErrorMargin)
                                            {
                                                resultHandler.AddCheckResult(khvWarningLayer, x, y, KHVRangeWarning);
                                            }
                                        }

                                        // Also check for an outlier-range warning
                                        if (isOutlierChecked && (khvValue > khvRangeUpperValue))
                                        {
                                            resultHandler.AddCheckResult(khvWarningLayer, x, y, KHVOutlierWarning);
                                        }
                                    }
                                    else
                                    {
                                        // KHV-value is zero or NoData, which is as expected
                                    }
                                }
                                else
                                {
                                    aquiferThickness = topValue - botValue;

                                    // aquifer has thickness, KHV should not be zero or NoData
                                    // Note: TOP- and BOT-values are not checked here, but are assumed to be valid

                                    if (khvValue.Equals(0) || khvValue.Equals(khvIDFFile.NoDataValue))
                                    {
                                        // Only report error if thickness is above level errormargin
                                        if ((topValue - botValue) > levelErrorMargin)
                                        {
                                            resultHandler.AddCheckResult(khvErrorLayer, x, y, KHVZeroValueError);
                                        }
                                    }
                                    else
                                    {
                                        // KHV-value is defined, check for negative non-NoData values
                                        if (khvValue < 0)
                                        {
                                            resultHandler.AddCheckResult(khvErrorLayer, x, y, KHVNegativeValueError);
                                        }

                                        // Also check for a range warning
                                        if (khvValue > maxKHVValue)
                                        {
                                            if (!ContainsValue(validKHVValues, khvValue, KVVKHVKVACheckSettings.kValueErrorMargin))
                                            {
                                                resultHandler.AddCheckResult(khvWarningLayer, x, y, KHVRangeWarning);
                                            }
                                        }
                                        else if (khvValue < minKHVValue)
                                        {
                                            // Only add KHVRangeWarning below minValue if there's also an aquitard defined below
                                            // This because the aquifer could be a complex
                                            if ((botValue - lowerTOPValue) > levelErrorMargin)
                                            {
                                                resultHandler.AddCheckResult(khvWarningLayer, x, y, KHVRangeWarning);
                                            }
                                        }

                                        // Also check for an outlier-range warning
                                        if (isOutlierChecked && (khvValue > khvRangeUpperValue))
                                        {
                                            resultHandler.AddCheckResult(khvWarningLayer, x, y, KHVOutlierWarning);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // aquifer TOP-value is defined, but BOT-value is NoData
                                if (!khvValue.Equals(khvIDFFile.NoDataValue))
                                {
                                    // aquifer TOP-value and KHV-value are defined, but BOT-value is NoData
                                    resultHandler.AddCheckResult(khvErrorLayer, x, y, KHVInconsistentValuesError);
                                }
                                else
                                {
                                    // TOP-value is defined, but BOT- and KHV-value not. Ignore possible error: TOP without BOT: TOP-value can be the bottom of the aquitard above (TODO: check?)
                                }
                            }
                        }
                        else
                        {
                            // TOP-value is not defined
                            if (!khvValue.Equals(0) && !khvValue.Equals(khvIDFFile.NoDataValue))
                            {
                                // KHV-value is defined, inconsistent aquifer definition
                                resultHandler.AddCheckResult(khvErrorLayer, x, y, KHVInconsistentValuesError);
                            }
                        }

                        if (!lowerTOPValue.Equals(float.NaN) && !lowerTOPValue.Equals(lowerTOPIDFFile.NoDataValue))
                        {
                            if (!lowerBOTValue.Equals(float.NaN) && !lowerBOTValue.Equals(lowerBOTIDFFile.NoDataValue))
                            {
                                lowerAquiferThickness = lowerTOPValue - lowerBOTValue;
                            }
                        }

                        // Check KVA-factor
                        if (aquiferThickness > 0)
                        {
                            // Aquifer has thickness, KVA should not be NoData
                            // Note: TOP- and BOT-values are not checked here, but are assumed to be valid
                            if (kvaValue.Equals(kvaIDFFile.NoDataValue))
                            {
                                if (aquiferThickness > levelErrorMargin)
                                {
                                    // Only report error if thickness is above level errormargin
                                    resultHandler.AddCheckResult(kvaErrorLayer, x, y, KVAInconsistentValuesError);
                                }
                            }
                            else if (kvaValue.Equals(0))
                            {
                                // Always report zero KVA-values but differentiate between inconsistency or zero KVA-value
                                if (aquiferThickness > levelErrorMargin)
                                {
                                    // Only report inconsistency error if thickness is above level errormargin
                                    resultHandler.AddCheckResult(kvaErrorLayer, x, y, KVAInconsistentValuesError);
                                }
                                else
                                {
                                    // For now report zero value with zero thickness as an error (even with zero thickness this leads an iMOD-error (division by zero))
                                    resultHandler.AddCheckResult(kvaErrorLayer, x, y, KVAZeroValueError);
                                }
                            }

                            // KVA-value is defined, check for negative (non-NoData) values
                            else if (kvaValue < 0)
                            {
                                resultHandler.AddCheckResult(kvaErrorLayer, x, y, KVANegativeValueError);
                            }
                            else
                            {
                                // Check for range warnings
                                if (kvaValue < minKVAValue)
                                {
                                    resultHandler.AddCheckResult(kvaWarningLayer, x, y, KVARangeWarning);
                                }

                                if (kvaValue > maxKVAValue)
                                {
                                    if ((ilay == 1) && !maxKVAL1Value.Equals(float.NaN))
                                    {
                                        if (kvaValue > maxKVAL1Value)
                                        {
                                            resultHandler.AddCheckResult(kvaWarningLayer, x, y, KVARangeWarning);
                                        }
                                    }
                                    else
                                    {
                                        resultHandler.AddCheckResult(kvaWarningLayer, x, y, KVARangeWarning);
                                    }
                                }
                            }
                        }
                        else
                        {
                            // aquifers have no thickness, KVA should be NoData (or a constant value larger than zero)
                            if (!kvaValue.Equals(kvaIDFFile.NoDataValue))
                            {
                                if (kvaValue.Equals(0))
                                {
                                    resultHandler.AddCheckResult(kvaErrorLayer, x, y, KVAZeroValueError);
                                }
                                else if (kvaValue < 0)
                                {
                                    resultHandler.AddCheckResult(kvaErrorLayer, x, y, KVANegativeValueError);
                                }
                                else if (!(kvaIDFFile is ConstantIDFFile) && !kvaValue.Equals(dummyKVAValue))
                                { 
                                    resultHandler.AddCheckResult(kvaWarningLayer, x, y, KVANonNoDataValueWarning);
                                }
                            }
                            else
                            {
                                // KVA-value is NoData (or constant), which is as expected
                            }
                        }

                        ////////////////////
                        // Check aquitard //
                        ////////////////////

                        if (!lowerTOPValue.Equals(float.NaN))
                        {
                            // A TOP-file is available for the modellayer below, an aquitard might be present
                            if (!botValue.Equals(botIDFFile.NoDataValue))
                            {
                                // aquitard topvalue (BOT-value of modellayer) is defined
                                if (!lowerTOPValue.Equals(lowerTOPIDFFile.NoDataValue))
                                {
                                    // aquitard top- and bottomvalue are defined
                                    if (botValue.Equals(lowerTOPValue) || (botValue < lowerTOPValue))
                                    {
                                        // aquitard has no thickness, KVV should be zero or NoData
                                        if ((kvvIDFFile != null) && !kvvValue.Equals(0) && !kvvValue.Equals(kvvIDFFile.NoDataValue))
                                        {
                                            resultHandler.AddCheckResult(kvvWarningLayer, x, y, KVVNotZeroValueWarning);

                                            // Also check for a range warning
                                            if ((kvvValue < minKVVValue) || (kvvValue > maxKVVValue))
                                            {
                                                if (!ContainsValue(validKVVValues, kvvValue, KVVKHVKVACheckSettings.kValueErrorMargin))
                                                {
                                                    resultHandler.AddCheckResult(kvvWarningLayer, x, y, KVVRangeWarning);
                                                }
                                            }

                                            // Also check for an IQR-range warning
                                            if (isOutlierChecked && (kvvValue > kvvRangeUpperValue))
                                            {
                                                resultHandler.AddCheckResult(kvvWarningLayer, x, y, KVVOutlierWarning);
                                            }
                                        }
                                        else
                                        {
                                            // KVV-value is zero or NoData, which is as expected
                                        }
                                    }
                                    else
                                    {
                                        // aquitard has thickness, KVV should not be zero or NoData
                                        // Note: TOP- and BOT-values are not checked here, but are assumed to be valid

                                        if (kvvValue.Equals(0) || kvvValue.Equals(kvvIDFFile.NoDataValue))
                                        {
                                            // Only report error if thickness is above level errormargin
                                            if ((botValue - lowerTOPValue) > levelErrorMargin)
                                            {
                                                resultHandler.AddCheckResult(kvvErrorLayer, x, y, KVVZeroValueError);
                                            }
                                        }
                                        else
                                        {
                                            // KVV-value is defined, check for negative non-NoData values
                                            if (kvvValue < 0)
                                            {
                                                resultHandler.AddCheckResult(kvvErrorLayer, x, y, KVVNegativeValueError);
                                            }

                                            // Also check for a range warning
                                            if ((kvvValue < minKVVValue) || (kvvValue > maxKVVValue))
                                            {
                                                if (!ContainsValue(validKVVValues, kvvValue, KVVKHVKVACheckSettings.kValueErrorMargin))
                                                {
                                                    resultHandler.AddCheckResult(kvvWarningLayer, x, y, KVVRangeWarning);
                                                }
                                            }

                                            // Also check for an IQR-range warning
                                            if (isOutlierChecked && (kvvValue > kvvRangeUpperValue))
                                            {
                                                resultHandler.AddCheckResult(kvvWarningLayer, x, y, KVVOutlierWarning);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    // aquitard topvalue is defined, but bottomvalue is NoData
                                    if (!kvvValue.Equals(kvvIDFFile.NoDataValue))
                                    {
                                        // aquitard topvalue and KVV-value are defined, but bottomvalue is NoData
                                        resultHandler.AddCheckResult(kvvErrorLayer, x, y, KVVInconsistentValuesError);

                                        // Also check for a range warning
                                        if ((kvvValue < minKVVValue) || (kvvValue > maxKVVValue))
                                        {
                                            if (!ContainsValue(validKVVValues, kvvValue, KVVKHVKVACheckSettings.kValueErrorMargin))
                                            {
                                                resultHandler.AddCheckResult(kvvWarningLayer, x, y, KVVRangeWarning);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // topvalue of aquitard is defined, but bottom- and KVV-value not. Ignore possible error: top without bottom: topvalue can be the bottom of the aquifer above (TODO: check?)
                                    }
                                }
                            }
                            else
                            {
                                // topvalue of aquitard is not defined
                                if ((kvvIDFFile != null) && !kvvValue.Equals(0) && !kvvValue.Equals(kvvIDFFile.NoDataValue))
                                {
                                    // KVV-value is defined, inconsistent aquitard definition
                                    resultHandler.AddCheckResult(kvvErrorLayer, x, y, KVVInconsistentValuesError);

                                    // Also check for a range warning
                                    if ((kvvValue < minKVVValue) || (kvvValue > maxKVVValue))
                                    {
                                        if (!ContainsValue(validKVVValues, kvvValue, KVVKHVKVACheckSettings.kValueErrorMargin))
                                        {
                                            resultHandler.AddCheckResult(kvvWarningLayer, x, y, KVVRangeWarning);
                                        }
                                    }
                                }
                            }
                        }

                        idfCellIterator.MoveNext();
                        x = idfCellIterator.X;
                        y = idfCellIterator.Y;
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error while checking KVV/KHV for cell (" + x + "," + y + ")", ex);
                }
                // Write KHV errors
                if (khvErrorLayer.HasResults())
                {
                    khvErrorLayer.CompressLegend(CombinedResultLabel);
                    khvErrorLayer.WriteResultFile(log);
                    resultHandler.AddExtraMapFile(topIDFFile);
                    resultHandler.AddExtraMapFile(botIDFFile);
                    resultHandler.AddExtraMapFile(khvIDFFile);
                }
                // Write KVA errors
                if ((kvaErrorLayer != null) && (kvaErrorLayer.HasResults()))
                {
                    kvaErrorLayer.CompressLegend(CombinedResultLabel);
                    kvaErrorLayer.WriteResultFile(log);
                    resultHandler.AddExtraMapFile(topIDFFile);
                    resultHandler.AddExtraMapFile(botIDFFile);
                    resultHandler.AddExtraMapFile(kvaIDFFile);
                }

                // Write KVV errors
                if ((kvvErrorLayer != null) && (kvvErrorLayer.HasResults()))
                {
                    kvvErrorLayer.CompressLegend(CombinedResultLabel);
                    kvvErrorLayer.WriteResultFile(log);
                    resultHandler.AddExtraMapFile(topIDFFile);
                    resultHandler.AddExtraMapFile(botIDFFile);
                    resultHandler.AddExtraMapFile(kvvIDFFile);
                    if (lowerTOPIDFFile != null)
                    {
                        resultHandler.AddExtraMapFile(lowerTOPIDFFile);
                    }
                }

                // Write KHV warnings
                if (khvWarningLayer.HasResults())
                {
                    khvWarningLayer.CompressLegend(CombinedResultLabel);
                    khvWarningLayer.WriteResultFile(log);
                    resultHandler.AddExtraMapFile(topIDFFile);
                    resultHandler.AddExtraMapFile(botIDFFile);
                    resultHandler.AddExtraMapFile(khvIDFFile);
                }
                // Write KVA warnings
                if ((kvaWarningLayer != null) && (kvaWarningLayer.HasResults()))
                {
                    kvaWarningLayer.CompressLegend(CombinedResultLabel);
                    kvaWarningLayer.WriteResultFile(log);
                    resultHandler.AddExtraMapFile(topIDFFile);
                    resultHandler.AddExtraMapFile(botIDFFile);
                    resultHandler.AddExtraMapFile(kvaIDFFile);
                }
                // Write KVV warnings
                if ((kvvWarningLayer != null) && (kvvWarningLayer.HasResults()))
                {
                    kvaWarningLayer.CompressLegend(CombinedResultLabel);
                    kvvWarningLayer.WriteResultFile(log);
                    resultHandler.AddExtraMapFile(topIDFFile);
                    resultHandler.AddExtraMapFile(botIDFFile);
                    resultHandler.AddExtraMapFile(kvvIDFFile);
                    if (lowerTOPIDFFile != null)
                    {
                        resultHandler.AddExtraMapFile(lowerTOPIDFFile);
                    }
                }

                if (khvErrorLayer != null)
                {
                    khvErrorLayer.ReleaseMemory(false);
                }
                if (khvWarningLayer != null)
                {
                    khvWarningLayer.ReleaseMemory(false);
                }
                if (kvaErrorLayer != null)
                {
                    kvaErrorLayer.ReleaseMemory(false);
                }
                if (kvaWarningLayer != null)
                {
                    kvaWarningLayer.ReleaseMemory(false);
                }
                if (kvvErrorLayer != null)
                {
                    kvvErrorLayer.ReleaseMemory(false);
                }
                if (kvvWarningLayer != null)
                {
                    kvvWarningLayer.ReleaseMemory(false);
                }

                khvIDFFile.ReleaseMemory(false);
                kvaIDFFile.ReleaseMemory(false);
                if (kvvIDFFile != null)
                {
                    kvvIDFFile.ReleaseMemory(false);
                }
                topIDFFile.ReleaseMemory(false);
                botIDFFile.ReleaseMemory(false);

                khvPackage.ReleaseMemory(false);
                if (kvvPackage != null)
                {
                    kvvPackage.ReleaseMemory(false);
                }
                if (kvaPackage != null)
                {
                    kvaPackage.ReleaseMemory(false);
                }
                GC.Collect();

                log.AddMessage(LogLevel.Debug, "Allocated memory " + (long)(GC.GetTotalMemory(true) / 1000000) + "Mb");

                //resultHandler.AddStatistics(khvIDFFile, khvStats);
                //if (kvvIDFFile != null)
                //{
                //    resultHandler.AddStatistics(kvvIDFFile, kvvStats);
                //}
            }

            minKHVSettingIDFFile.ReleaseMemory(true);
            maxKHVSettingIDFFile.ReleaseMemory(true);
            minKVVSettingIDFFile.ReleaseMemory(true);
            maxKVVSettingIDFFile.ReleaseMemory(true);
            maxKVASettingIDFFile.ReleaseMemory(true);
        }
        /// <summary>
        /// Checks if value equals one of the values in the specified list, within specified error margin
        /// </summary>
        /// <param name="values"></param>
        /// <param name="value"></param>
        /// <param name="errorMargin"></param>
        /// <returns></returns>
        protected bool ContainsValue(List<float> values, float value, float errorMargin = 0f)
        {
            if (values != null)
            {
                for (int idx = 0; idx < values.Count; idx++)
                {
                    float listValue = values[idx];
                    if (Math.Abs(listValue - value) < errorMargin)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
