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
using Sweco.SIF.iMOD.Legends;
using Sweco.SIF.iMODPlus.IDF;
using Sweco.SIF.iMODValidator.Checks.CheckResults;
using Sweco.SIF.iMODValidator.Models;
using Sweco.SIF.iMODValidator.Models.Packages;
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
    class KDCCheckSettings : CheckSettings
    {
        private string dummyCValue;
        private string dummyKDValue;
        private bool isKValueChecked;
        private bool isUnknownKValueWarningShown;
        private string minKHValue;
        private string maxKHValue;
        private string minKVValue;
        private string maxKVValue;
        private bool isKDCSummed;
        private string maxKHTot1ErrorValue;
        private string minKVTotErrorValue;

        [Category("KDC-properties"), Description("Defines a dummy/minimum kD-value for which a thickness may not be present"), PropertyOrder(10)]
        public string DummyKDValue
        {
            get { return dummyKDValue; }
            set { dummyKDValue = value; }
        }
        [Category("KDC-properties"), Description("Defines a dummy/minimum C-value for which a thickbess may not be present"), PropertyOrder(11)]
        public string DummyCValue
        {
            get { return dummyCValue; }
            set { dummyCValue = value; }
        }
        [Category("KDC-properties"), Description("Defines that the k-values are calculated and checked"), PropertyOrder(15)]
        public bool IsKValueChecked
        {
            get { return isKValueChecked; }
            set { isKValueChecked = value; }
        }
        [Category("KDC-properties"), Description("Defines that a warning is reported for undefined/unknown k-values"), PropertyOrder(15)]
        public bool IsUnknownKValueWarningShown
        {
            get { return isUnknownKValueWarningShown; }
            set { isUnknownKValueWarningShown = value; }
        }
        [Category("KDC-properties"), Description("The minimum valid kh-value for this region"), PropertyOrder(20)]
        public string MinKHValue
        {
            get { return minKHValue; }
            set { minKHValue = value; }
        }
        [Category("KDC-properties"), Description("The maximum valid kh-value for this region"), PropertyOrder(21)]
        public string MaxKHValue
        {
            get { return maxKHValue; }
            set { maxKHValue = value; }
        }
        [Category("KDC-properties"), Description("The minimum valid kv-value for this region"), PropertyOrder(30)]
        public string MinKVValue
        {
            get { return minKVValue; }
            set { minKVValue = value; }
        }
        [Category("KDC-properties"), Description("The maximum valid kv-value for this region"), PropertyOrder(31)]
        public string MaxKVValue
        {
            get { return maxKVValue; }
            set { maxKVValue = value; }
        }
        [Category("KDC-properties"), Description("The maximum kh-value of layer 1, without aquifer, so based on aquitard, before reporting non-zero kD error, for this region"), PropertyOrder(40)]
        public string MaxKHTot1ErrorValue
        {
            get { return maxKHTot1ErrorValue; }
            set { maxKHTot1ErrorValue = value; }
        }
        [Category("KDC-properties"), Description("The minimum kv-value of a layer, without aquitard, so based on aquifer, before reporting non-zero C error, for this region"), PropertyOrder(41)]
        public string MinKVTotErrorValue
        {
            get { return minKVTotErrorValue; }
            set { minKVTotErrorValue = value; }
        }
        [Category("KDC-properties"), Description("The kD- and C-values are summed over all layers and written to a file"), PropertyOrder(50)]
        public bool IsKDCSummed
        {
            get { return isKDCSummed; }
            set { isKDCSummed = value; }
        }

        public KDCCheckSettings(string checkName) : base(checkName)
        {
            DummyKDValue = "1";
            DummyCValue = "1";
            IsKValueChecked = true;
            IsUnknownKValueWarningShown = true;
            MinKHValue = ((float)0.1f).ToString(englishCultureInfo);
            MaxKHValue = "200";
            MinKVValue = ((float)0.0000001f).ToString(englishCultureInfo);
            MaxKVValue = ((float)0.1f).ToString(englishCultureInfo);
            IsKDCSummed = false;
            maxKHTot1ErrorValue = "0";
            minKVTotErrorValue = "0";
        }

        public override void LogSettings(Log log, int logIndentLevel = 0)
        {
            log.AddInfo("DummyKDValue: " + dummyKDValue, logIndentLevel);
            log.AddInfo("DummyCValue: " + dummyCValue, logIndentLevel);
            log.AddInfo("minKHTot1ErrorValue: " + maxKHTot1ErrorValue, logIndentLevel);
            log.AddInfo("minKVTotErrorValue: " + minKVTotErrorValue, logIndentLevel);
            if (isKValueChecked)
            {
                log.AddInfo("k-values are checked for cells with consistent kD-, C- and TOP-, BOT-values", logIndentLevel);
                log.AddInfo("Defined kh_min: " + MinKHValue, logIndentLevel);
                log.AddInfo("Defined kh_max: " + MaxKHValue, logIndentLevel);
                log.AddInfo("Defined kv_min: " + MinKVValue, logIndentLevel);
                log.AddInfo("Defined kv_max: " + MaxKVValue, logIndentLevel);
                log.AddInfo("Inconsistent cells are not checked", logIndentLevel);
                if (IsUnknownKValueWarningShown)
                {
                    log.AddInfo("A warning is given for inconsistent cells", logIndentLevel);
                }
                else
                {
                    log.AddInfo("No warning is given for inconsistent cells", logIndentLevel);
                }
            }
            else
            {
                log.AddInfo("k-values are NOT checked", logIndentLevel);
            }
            if (isKDCSummed)
            {
                log.AddInfo("kD- and C-values are summed", logIndentLevel);
            }
            else
            {
                log.AddInfo("kD- and C-values are not summed", logIndentLevel);
            }
        }
    }

    class KDCCheck : Check
    {
        public override string Abbreviation
        {
            get { return "kD-C"; }
        }

        public override string Description
        {
            get { return "Checks k-, kD-, C-, TOP- and BOT- values per layer"; }
        }

        private KDCCheckSettings settings;
        public override CheckSettings Settings
        {
            get { return settings; }
            set
            {
                if (value is KDCCheckSettings)
                {
                    settings = (KDCCheckSettings)value;
                }
            }
        }

        private CheckError kDNotZeroValueError;
        private CheckError kDZeroValueError;
        private CheckError kDNegativeValueError;
        private CheckError kDInconsistentValuesError;
        private CheckError CNotZeroValueError;
        private CheckError CZeroValueError;
        private CheckError CNegativeValueError;
        private CheckError CInconsistentValuesError;
        private CheckWarning KHRangeWarning;
        private CheckWarning KVRangeWarning;
        private CheckWarning UnknownKValueWarning;

        private IDFLegend kdErrorLegend;
        private IDFLegend cErrorLegend;
        private IDFLegend khWarningLegend;
        private IDFLegend kvWarningLegend;

        public KDCCheck()
        {
            settings = new KDCCheckSettings(this.Name);

            // Define kD-errors
            kDNotZeroValueError = new CheckError(1, "Unexpected non-zero value", "The value is not, but should be, zero (aquifer-thickness is zero).");
            kDZeroValueError = new CheckError(2, "Unexpected zero value", "The value is not, but should be, greater than zero (aquifer-thickness is greater than zero).");
            kDNegativeValueError = new CheckError(4, "Unexpected negative value", "The value is not, but should be, greater than zero (aquifer-thickness is greater than zero).");
            kDInconsistentValuesError = new CheckError(8, "Inconsistent values", "TOP, BOT and/or kD is unexpectedly undefined");

            kdErrorLegend = new IDFLegend("Legend for kD-file check");
            kdErrorLegend.AddClass(new ValueLegendClass(0, "No errors found", Color.White));
            kdErrorLegend.AddClass(kDNotZeroValueError.CreateLegendValueClass(Color.Orange, true));
            kdErrorLegend.AddClass(kDZeroValueError.CreateLegendValueClass(Color.Red, true));
            kdErrorLegend.AddClass(kDNegativeValueError.CreateLegendValueClass(Color.Firebrick, true));
            kdErrorLegend.AddClass(kDInconsistentValuesError.CreateLegendValueClass(Color.DarkRed, true));
            kdErrorLegend.AddClass(new ValueLegendClass(kDNotZeroValueError.ResultValue + kDNegativeValueError.ResultValue, (kDNotZeroValueError.ResultValue + kDNegativeValueError.ResultValue) + " - Unexpected non-zero (and negative)", Color.Indigo, "The value is not, but should be, zero (aquifer-thickness is zero)."));
            kdErrorLegend.AddInbetweenClasses(CombinedResultLabel, true);

            // Define C-errors
            CNotZeroValueError = new CheckError(1, "Unexpected non-zero value", "The value is not, but should be, zero (aquitard-thickness is zero).");
            CZeroValueError = new CheckError(2, "Unexpected zero value", "The value is not, but should be, greater than zero (aquitard-thickness is greater than zero).");
            CNegativeValueError = new CheckError(4, "Unexpected negative value", "The value is not, but should be, greater than zero (aquitard-thickness is greater than zero).");
            CInconsistentValuesError = new CheckError(8, "Inconsistent values", "TOP, BOT and/or C is unexpectedly undefined");

            cErrorLegend = new IDFLegend("Legend for C-file check");
            cErrorLegend.AddClass(new ValueLegendClass(0, "No errors found", Color.White));
            cErrorLegend.AddClass(CNotZeroValueError.CreateLegendValueClass(Color.Orange, true));
            cErrorLegend.AddClass(CZeroValueError.CreateLegendValueClass(Color.Red, true));
            cErrorLegend.AddClass(CNegativeValueError.CreateLegendValueClass(Color.Firebrick, true));
            cErrorLegend.AddClass(CInconsistentValuesError.CreateLegendValueClass(Color.DarkRed, true));
            cErrorLegend.AddClass(new ValueLegendClass(CNotZeroValueError.ResultValue + CNegativeValueError.ResultValue, (CNotZeroValueError.ResultValue + CNegativeValueError.ResultValue) + " - Unexpected non-zero (and negative)", Color.Indigo, "The value is not, but should be, zero (aquitard-thickness is zero)."));
            cErrorLegend.AddInbetweenClasses(CombinedResultLabel, true);

            UnknownKValueWarning = new CheckWarning(1, "k-value unknown", "k-value cannot be calculated");

            // Define kh-warnings
            KHRangeWarning = new CheckWarning(2, "kh-value outside range ["
                + settings.MinKHValue.ToString() + "," + settings.MaxKHValue.ToString() + "]");

            khWarningLegend = new IDFLegend("Legend for kh-check");
            khWarningLegend.AddClass(new ValueLegendClass(0, "No errors found", Color.White));
            khWarningLegend.AddClass(UnknownKValueWarning.CreateLegendValueClass(Color.Orange, true));
            khWarningLegend.AddClass(KHRangeWarning.CreateLegendValueClass(Color.Red, true));
            khWarningLegend.AddInbetweenClasses(CombinedResultLabel, true);

            // Define kv-warnings
            KVRangeWarning = new CheckWarning(2, "kv-value outside range ["
                + settings.MinKVValue.ToString() + "," + settings.MaxKVValue.ToString() + "]");

            kvWarningLegend = new IDFLegend("Legend for kv-check");
            kvWarningLegend.AddClass(new ValueLegendClass(0, "No errors found", Color.White));
            kvWarningLegend.AddClass(UnknownKValueWarning.CreateLegendValueClass(Color.Orange, true));
            kvWarningLegend.AddClass(KVRangeWarning.CreateLegendValueClass(Color.Red, true));
            kvWarningLegend.AddInbetweenClasses(CombinedResultLabel, true);
        }

        public override void Run(Model model, CheckResultHandler resultHandler, Log log)
        {
            IDFPackage kdwPackage = (IDFPackage)model.GetPackage(KDWPackage.DefaultKey);
            IDFPackage vcwPackage = (IDFPackage)model.GetPackage(VCWPackage.DefaultKey);
            if ((vcwPackage == null) && (kdwPackage == null))
            {
                // When both VCW- and KDW-package are not present in RUN-file, skip whole check silently
                return;
            }

            log.AddInfo("Checking VCW- and KDW-packages ...");

            if (((vcwPackage != null) && !vcwPackage.IsActive) || ((kdwPackage != null) && !kdwPackage.IsActive))
            {
                log.AddWarning(this.Name, model.RUNFilename, "VCW- and/or KDW-packages are not active. " + this.Name + " is skipped.", 1);
                return;
            }

            settings.LogSettings(log, 1);
            RunKvvKhvCheck1(model, resultHandler, log);
        }

        protected virtual void RunKvvKhvCheck1(Model model, CheckResultHandler resultHandler, Log log)
        {
            // Retrieve used packages
            IDFPackage topPackage = (IDFPackage)model.GetPackage(TOPPackage.DefaultKey);
            IDFPackage botPackage = (IDFPackage)model.GetPackage(BOTPackage.DefaultKey);
            IDFPackage kdwPackage = (IDFPackage)model.GetPackage(KDWPackage.DefaultKey);
            IDFPackage vcwPackage = (IDFPackage)model.GetPackage(VCWPackage.DefaultKey);

            if ((topPackage == null) || !topPackage.IsActive || (botPackage == null) || !botPackage.IsActive)
            {
                log.AddWarning(this.Name, model.RUNFilename, "TOP- and/or BOT-package is not active. TOP-BOT part of check is skipped.", 1);
                topPackage = null;
                botPackage = null;
                if (settings.IsKValueChecked)
                {
                    log.AddWarning(this.Name, model.RUNFilename, "TOP/BOT-package is not active. Check for k-values is skipped.", 1);
                    settings.IsKValueChecked = false;
                }
            }

            float levelErrorMargin = resultHandler.LevelErrorMargin;
            float dummyCValue = settings.GetValue(settings.DummyCValue, log, 1);
            float dummyKDValue = settings.GetValue(settings.DummyKDValue, log, 1);
            log.AddInfo("Using levelErrorMargin = " + levelErrorMargin, 1);
            log.AddInfo("Using dummyCValue = " + dummyCValue, 1);
            log.AddInfo("Using dummyKDValue= " + dummyKDValue, 1);
            IDFFile minKHSettingIDFFile = settings.GetIDFFile(settings.MinKHValue, log, 1);
            IDFFile maxKHSettingIDFFile = settings.GetIDFFile(settings.MaxKHValue, log, 1);
            IDFFile minKVSettingIDFFile = settings.GetIDFFile(settings.MinKVValue, log, 1);
            IDFFile maxKVSettingIDFFile = settings.GetIDFFile(settings.MaxKVValue, log, 1);
            IDFFile maxKHTot1ErrorValueIDFFile = settings.GetIDFFile(settings.MaxKHTot1ErrorValue, log, 1);
            IDFFile minKVTotErrorValueIDFFile = settings.GetIDFFile(settings.MinKVTotErrorValue, log, 1);
            IDFFile kdSumIDFFile = null;
            IDFFile cSumIDFFile = null;
            // Check if the kD- and C-sums over all layers have to be calculated as well
            if (settings.IsKDCSummed)
            {
                if (kdwPackage.GetEntryCount() > 0)
                {
                    string kdSumFilename = Path.Combine(FileUtils.EnsureFolderExists(GetIMODFilesPath(model), this.Name), "kDSum.IDF");
                    IDFFile kD1IDFFile = kdwPackage.GetIDFFile(0);
                    if (kD1IDFFile != null)
                    {
                        kdSumIDFFile = kD1IDFFile.CopyIDF(kdSumFilename);
                        kdSumIDFFile.ResetValues();
                    }
                }
                if (vcwPackage.GetEntryCount() > 0)
                {
                    string cSumFilename = Path.Combine(FileUtils.EnsureFolderExists(GetIMODFilesPath(model), this.Name), "cSum.IDF");
                    IDFFile c1IDFFile = vcwPackage.GetIDFFile(0);
                    if (c1IDFFile != null)
                    {
                        cSumIDFFile = c1IDFFile.CopyIDF(cSumFilename);
                        cSumIDFFile.ResetValues();
                    }
                }
            }

            // Process all layers
            for (int entryIdx = resultHandler.MinEntryNumber - 1; (entryIdx < kdwPackage.GetEntryCount()) && (entryIdx < resultHandler.MaxEntryNumber); entryIdx++)
            {
                int ilay = entryIdx + 1;
                log.AddInfo("Checking entry " + (entryIdx + 1) + " with " + Name + " ...", 1);

                // Retrieve IDF files for current layer
                IDFFile topIDFFile = (topPackage != null) ? topPackage.GetIDFFile(entryIdx) : null;
                IDFFile botIDFFile = (botPackage != null) ? botPackage.GetIDFFile(entryIdx) : null;
                IDFFile kdIDFFile = kdwPackage.GetIDFFile(entryIdx);
                IDFFile cIDFFile = (entryIdx < vcwPackage.GetEntryCount()) ? vcwPackage.GetIDFFile(entryIdx) : null; // Check for non-existing last kvv-layer
                IDFFile lowerTopIDFFile = ((topPackage != null) && (entryIdx < topPackage.GetEntryCount() - 1)) ? topPackage.GetIDFFile(entryIdx + 1) : null; // If there is a layer below the current layer retrieve its TOP-file

                if (kdIDFFile == null)
                {
                    log.AddWarning(kdwPackage.Key, kdIDFFile.Filename, "kD IDF-file missing for entry " + entryIdx + 1 + ", check is canceled", 1);
                    return;
                }
                if ((cIDFFile == null) && (entryIdx < vcwPackage.GetEntryCount()))
                {
                    log.AddWarning(vcwPackage.Key, cIDFFile.Filename, "C IDF-file missing for entry " + entryIdx + 1 + ", check is canceled", 1);
                    return;
                }

                IDFFile.ReplaceNoDataWithNaN(topIDFFile);
                IDFFile.ReplaceNoDataWithNaN(botIDFFile);
                IDFFile.ReplaceNoDataWithNaN(kdIDFFile);
                IDFFile.ReplaceNoDataWithNaN(cIDFFile);
                IDFFile.ReplaceNoDataWithNaN(lowerTopIDFFile);

                if (settings.IsKDCSummed)
                {
                    if ((kdSumIDFFile != null) && (kdIDFFile != null))
                    {
                        kdSumIDFFile = kdSumIDFFile + kdIDFFile;
                    }
                    if ((cSumIDFFile != null) && (cIDFFile != null))
                    {
                        cSumIDFFile = cSumIDFFile + cIDFFile;
                    }
                }

                IDFCellIterator idfCellIterator = new IDFCellIterator(resultHandler.Extent);
                idfCellIterator.AddIDFFile(topIDFFile);
                idfCellIterator.AddIDFFile(botIDFFile);
                idfCellIterator.AddIDFFile(kdIDFFile);
                idfCellIterator.AddIDFFile(cIDFFile);
                idfCellIterator.AddIDFFile(lowerTopIDFFile);
                idfCellIterator.CheckExtent(log, 2, LogLevel.Debug);

                idfCellIterator.AddIDFFile(minKHSettingIDFFile);
                idfCellIterator.AddIDFFile(maxKHSettingIDFFile);
                idfCellIterator.AddIDFFile(minKVSettingIDFFile);
                idfCellIterator.AddIDFFile(maxKVSettingIDFFile);

                idfCellIterator.AddIDFFile(maxKHTot1ErrorValueIDFFile);
                idfCellIterator.AddIDFFile(minKVTotErrorValueIDFFile);

                // Create error IDFfiles for current layer
                CheckErrorLayer kdErrorLayer = CreateErrorLayer(resultHandler, kdwPackage, null, StressPeriod.SteadyState, entryIdx + 1, idfCellIterator.XStepsize, kdErrorLegend);
                kdErrorLayer.AddSourceFiles(idfCellIterator.GetIDFFiles());

                CheckErrorLayer cErrorLayer = null;
                if (cIDFFile != null)
                {
                    cErrorLayer = CreateErrorLayer(resultHandler, vcwPackage, null, StressPeriod.SteadyState, entryIdx + 1, idfCellIterator.XStepsize, cErrorLegend);
                    cErrorLayer.AddSourceFiles(idfCellIterator.GetIDFFiles());
                }

                // Create warning IDFfiles for current layer
                CheckWarningLayer kdWarningLayer = CreateWarningLayer(resultHandler, kdwPackage, null, StressPeriod.SteadyState, entryIdx + 1, idfCellIterator.XStepsize, khWarningLegend);
                kdWarningLayer.AddSourceFiles(idfCellIterator.GetIDFFiles());

                CheckWarningLayer cWarningLayer = null;
                if (cIDFFile != null)
                {
                    cWarningLayer = CreateWarningLayer(resultHandler, vcwPackage, null, StressPeriod.SteadyState, entryIdx + 1, idfCellIterator.XStepsize, kvWarningLegend);
                    cErrorLayer.AddSourceFiles(idfCellIterator.GetIDFFiles());
                }

                // Create IDFfiles for calculated kh- and kv-values
                IDFFile khIDFFile = null;
                if (settings.IsKValueChecked)
                {
                    khIDFFile = CreateSparseKIDFFile("kh", "kD", kdIDFFile, topIDFFile, botIDFFile, model, ilay);
                }
                IDFFile kvIDFFile = null;
                if (settings.IsKValueChecked && cIDFFile != null)
                {
                    kvIDFFile = CreateSparseKIDFFile("kv", "C", cIDFFile, topIDFFile, botIDFFile, model, ilay);
                }
                IDFFile khTot1IDFFile = null;
                if (ilay == 1)
                {
                    khTot1IDFFile = CreateSparseKIDFFile("khTOT", "kD", kdIDFFile, topIDFFile, lowerTopIDFFile, model, ilay);
                }
                IDFFile kvTotIDFFile = null;
                if (cIDFFile != null)
                {
                    kvTotIDFFile = CreateSparseKIDFFile("kvTOT", "c", cIDFFile, topIDFFile, lowerTopIDFFile, model, ilay);
                }

                // Iterate through cells
                idfCellIterator.IsNaNUsedForNoData = true;
                idfCellIterator.Reset();
                float x = idfCellIterator.X;
                float y = idfCellIterator.Y;
                try
                {
                    while (idfCellIterator.IsInsideExtent())
                    {
                        float topValue = idfCellIterator.GetCellValue(topIDFFile);
                        float botValue = idfCellIterator.GetCellValue(botIDFFile);
                        float kdValue = idfCellIterator.GetCellValue(kdIDFFile);
                        float cValue = idfCellIterator.GetCellValue(cIDFFile);
                        float lowerTopValue = idfCellIterator.GetCellValue(lowerTopIDFFile);
                        float minKHValue = idfCellIterator.GetCellValue(minKHSettingIDFFile);
                        float maxKHValue = idfCellIterator.GetCellValue(maxKHSettingIDFFile);
                        float minKVValue = idfCellIterator.GetCellValue(minKVSettingIDFFile);
                        float maxKVValue = idfCellIterator.GetCellValue(maxKVSettingIDFFile);
                        float maxKHTot1ErrorValue = idfCellIterator.GetCellValue(maxKHTot1ErrorValueIDFFile);
                        float minKVTotErrorValue = idfCellIterator.GetCellValue(minKVTotErrorValueIDFFile);

                        ///////////////////
                        // Check aquifer //
                        ///////////////////

                        if (topPackage != null)
                        {
                            if (!topValue.Equals(float.NaN))
                            {
                                // aquifer TOP-value is defined
                                if (!botValue.Equals(float.NaN))
                                {
                                    // aquifer TOP- and BOT-value are defined
                                    if (topValue.Equals(botValue) || (topValue < botValue))
                                    {
                                        // aquifer has no thickness, kD should be zero or NoData
                                        if (!kdValue.Equals(0) && !kdValue.Equals(float.NaN) && !kdValue.Equals(dummyKDValue))
                                        {
                                            if (ilay == 1)
                                            {
                                                // For layer 1, first check total kh based on aquitard thickness, since there's no aquifer above and the kD may be based on the aquitard thickness
                                                if (!lowerTopValue.Equals(float.NaN) && ((topValue - lowerTopValue) > levelErrorMargin))
                                                {
                                                    float khTot = kdValue / (topValue - lowerTopValue);
                                                    if (khTot > maxKHTot1ErrorValue)
                                                    {
                                                        resultHandler.AddCheckResult(kdErrorLayer, x, y, kDNotZeroValueError);
                                                        if (khTot1IDFFile != null)
                                                        {
                                                            khTot1IDFFile.SetValue(x, y, khTot);
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    resultHandler.AddCheckResult(kdErrorLayer, x, y, kDNotZeroValueError);
                                                }
                                            }
                                            else
                                            {
                                                resultHandler.AddCheckResult(kdErrorLayer, x, y, kDNotZeroValueError);
                                            }

                                            // Check if k-value is to be calculated and checked
                                            if (khIDFFile != null)
                                            {
                                                if (settings.IsUnknownKValueWarningShown)
                                                {
                                                    // kh cannot be calculated or checked, report warning
                                                    resultHandler.AddCheckResult(kdWarningLayer, x, y, UnknownKValueWarning);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            // kD-value is zero or NoData, which is as expected
                                            // leave kh-value NoData, kD is valid, but without a known kh-value
                                        }
                                    }
                                    else
                                    {
                                        // aquifer has thickness, kD should not be zero or NoData
                                        // Note: TOP- and BOT-values are not checked here, but are assumed to be valid

                                        if (kdValue.Equals(0) || kdValue.Equals(float.NaN))
                                        {
                                            // Only report error if thickness is above level errormargin
                                            if ((topValue - botValue) > levelErrorMargin)
                                            {
                                                resultHandler.AddCheckResult(kdErrorLayer, x, y, kDZeroValueError);
                                            }

                                            if (khIDFFile != null)
                                            {
                                                if (kdValue.Equals(0))
                                                {
                                                    khIDFFile.SetValue(x, y, 0);
                                                }
                                                else if (settings.IsUnknownKValueWarningShown)
                                                {
                                                    // kh cannot be calculated or checked, report warning
                                                    resultHandler.AddCheckResult(kdWarningLayer, x, y, UnknownKValueWarning);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            // kD-value is defined, check for negative non-NoData values
                                            if (kdValue < 0)
                                            {
                                                resultHandler.AddCheckResult(kdErrorLayer, x, y, kDNegativeValueError);
                                            }

                                            // Also check for a range warning
                                            if (khIDFFile != null)
                                            {
                                                float khValue = kdValue / (topValue - botValue);
                                                khIDFFile.SetValue(x, y, khValue);
                                                if ((khValue < minKHValue) || (khValue > maxKHValue))
                                                {
                                                    // For layer one and very small aquifers (below defined error margin), first check total kh, including aquitard in case there's an aquitard with thickness
                                                    if ((ilay == 1) && ((topValue - botValue) < levelErrorMargin))
                                                    {
                                                        if (!lowerTopValue.Equals(float.NaN) && ((topValue - lowerTopValue) > levelErrorMargin))
                                                        {
                                                            float khTot = kdValue / (topValue - lowerTopValue);
                                                            if (khTot1IDFFile != null)
                                                            {
                                                                khTot1IDFFile.SetValue(x, y, khTot);
                                                            }
                                                            if (khTot > maxKHTot1ErrorValue)
                                                            {
                                                                resultHandler.AddCheckResult(kdWarningLayer, x, y, KHRangeWarning);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            resultHandler.AddCheckResult(kdWarningLayer, x, y, KHRangeWarning);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        resultHandler.AddCheckResult(kdWarningLayer, x, y, KHRangeWarning);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    // aquifer TOP-value is defined, but BOT-value is NoData or outside IDF
                                    if (!kdValue.Equals(float.NaN))
                                    {
                                        // aquifer TOP-value and kD-value are defined, but BOT-value is NoData or outside IDF
                                        resultHandler.AddCheckResult(kdErrorLayer, x, y, kDInconsistentValuesError);

                                        if (khIDFFile != null)
                                        {
                                            if (settings.IsUnknownKValueWarningShown)
                                            {
                                                // kh cannot be calculated or checked, report warning
                                                resultHandler.AddCheckResult(kdWarningLayer, x, y, UnknownKValueWarning);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // TOP-value is defined, but BOT- and kD-value not. Ignore possible error: TOP without BOT: TOP-value can be the bottom of the aquitard above (TODO: check?)
                                    }
                                }
                            }
                            else
                            {
                                // TOP-value is not defined
                                if (!kdValue.Equals(0) && !kdValue.Equals(float.NaN) && !kdValue.Equals(dummyKDValue))
                                {
                                    // kD-value is defined, inconsistent aquifer definition
                                    resultHandler.AddCheckResult(kdErrorLayer, x, y, kDInconsistentValuesError);

                                    if (khIDFFile != null)
                                    {
                                        if (settings.IsUnknownKValueWarningShown)
                                        {
                                            // kh cannot be calculated or checked, report warning
                                            resultHandler.AddCheckResult(kdWarningLayer, x, y, UnknownKValueWarning);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            // TOP/BOT not available, just check for invalid kD-value
                            if (!kdValue.Equals(float.NaN))
                            {
                                // kD-value is defined, check for negative non-NoData values
                                if (kdValue < 0)
                                {
                                    resultHandler.AddCheckResult(kdErrorLayer, x, y, kDNegativeValueError);
                                }

                                if (khIDFFile != null)
                                {
                                    if (settings.IsUnknownKValueWarningShown)
                                    {
                                        // kh cannot be calculated or checked, report warning
                                        resultHandler.AddCheckResult(kdWarningLayer, x, y, UnknownKValueWarning);
                                    }
                                }
                            }
                        }

                        ////////////////////
                        // Check aquitard //
                        ////////////////////

                        if (botPackage != null)
                        {
                            if (!lowerTopValue.Equals(float.NaN))
                            {
                                // A TOP-file is available for the modellayer below, an aquitard might be present
                                if (!botValue.Equals(float.NaN))
                                {
                                    // aquitard topvalue (BOT-value of modellayer) is defined
                                    if (!lowerTopValue.Equals(float.NaN))
                                    {
                                        // aquitard top- and bottomvalue are defined
                                        if (botValue.Equals(lowerTopValue) || (botValue < lowerTopValue))
                                        {
                                            // aquitard has no thickness, C should be zero or NoData, unless there's an aquifer above for which the c-value has been calculated
                                            if (!cValue.Equals(0) && !cValue.Equals(float.NaN) && !cValue.Equals(dummyCValue))
                                            {
                                                // If aquifer thickess is present, always report an error since there's no aquifer for which the c-value could be calculated 
                                                if (!topValue.Equals(botValue))
                                                {
                                                    // There is an aquifer above
                                                    float kvTot = (topValue - lowerTopValue) / cValue;
                                                    kvTotIDFFile.SetValue(x, y, kvTot);
                                                    if (kvTot < minKVTotErrorValue)
                                                    {
                                                        resultHandler.AddCheckResult(cErrorLayer, x, y, CNotZeroValueError);
                                                    }
                                                }
                                                else
                                                {
                                                    resultHandler.AddCheckResult(cErrorLayer, x, y, CNotZeroValueError);
                                                }
                                                // Check if k-value is to be calculated and checked
                                                if (kvIDFFile != null)
                                                {
                                                    if (settings.IsUnknownKValueWarningShown)
                                                    {
                                                        // kv cannot be calculated or checked, report warning
                                                        resultHandler.AddCheckResult(cWarningLayer, x, y, UnknownKValueWarning);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                // C-value is zero or NoData, which is as expected
                                                // leave kv-value NoData, C is valid, but without a known kv-value
                                            }
                                        }
                                        else
                                        {
                                            // aquitard has thickness, C should not be zero or NoData
                                            // Note: TOP- and BOT-values are not checked here, but are assumed to be valid

                                            if (cValue.Equals(0) || cValue.Equals(float.NaN))
                                            {
                                                // Only report error if thickness is above level errormargin
                                                if ((botValue - lowerTopValue) > levelErrorMargin)
                                                {
                                                    resultHandler.AddCheckResult(cErrorLayer, x, y, CZeroValueError);
                                                }

                                                if (kvIDFFile != null)
                                                {
                                                    if (settings.IsUnknownKValueWarningShown)
                                                    {
                                                        // kv cannot be calculated or checked, report warning
                                                        resultHandler.AddCheckResult(cWarningLayer, x, y, UnknownKValueWarning);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                // C-value is defined, check for negative non-NoData values
                                                if (cValue < 0)
                                                {
                                                    resultHandler.AddCheckResult(cErrorLayer, x, y, CNegativeValueError);
                                                }

                                                // Also check for a range warning
                                                if (kvIDFFile != null)
                                                {
                                                    float kvValue = (botValue - lowerTopValue) / cValue;
                                                    kvIDFFile.SetValue(x, y, kvValue);
                                                    if ((kvValue < minKVValue) || (kvValue > maxKVValue))
                                                    {
                                                        resultHandler.AddCheckResult(cWarningLayer, x, y, KVRangeWarning);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // aquitard topvalue is defined, but bottomvalue is NoData
                                        if (!cValue.Equals(float.NaN))
                                        {
                                            // aquitard topvalue and C-value are defined, but bottomvalue is NoData
                                            resultHandler.AddCheckResult(cErrorLayer, x, y, CInconsistentValuesError);

                                            if (kvIDFFile != null)
                                            {
                                                if (settings.IsUnknownKValueWarningShown)
                                                {
                                                    // kv cannot be calculated or checked, report warning
                                                    resultHandler.AddCheckResult(cWarningLayer, x, y, UnknownKValueWarning);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            // topvalue of aquitard is defined, but bottom- and C-value not. Ignore possible error: top without bottom: topvalue can be the bottom of the aquifer above (TODO: check?)
                                        }
                                    }
                                }
                                else
                                {
                                    // topvalue of aquitard is not defined
                                    if (!cValue.Equals(float.NaN))
                                    {
                                        if (!cValue.Equals(0) && !cValue.Equals(float.NaN) && !cValue.Equals(1.0f))
                                        {
                                            // C-value is defined, inconsistent aquitard definition
                                            resultHandler.AddCheckResult(cErrorLayer, x, y, CInconsistentValuesError);

                                            if (kvIDFFile != null)
                                            {
                                                if (settings.IsUnknownKValueWarningShown)
                                                {
                                                    // kv cannot be calculated or checked, report warning
                                                    resultHandler.AddCheckResult(cWarningLayer, x, y, UnknownKValueWarning);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // There's no aquitard bottom-file defined for this modellayer, probably lower modelbound, ignore
                            }
                        }
                        else
                        {
                            // TOP/BOT not available, just check for invalid c-value
                            if (!cValue.Equals(float.NaN))
                            {
                                // c-value is defined, check for negative non-NoData values
                                if (cValue < 0)
                                {
                                    resultHandler.AddCheckResult(cErrorLayer, x, y, CNegativeValueError);
                                }

                                if (kvIDFFile != null)
                                {
                                    if (settings.IsUnknownKValueWarningShown)
                                    {
                                        // kv cannot be calculated or checked, report warning
                                        resultHandler.AddCheckResult(cWarningLayer, x, y, UnknownKValueWarning);
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
                    throw new Exception("Error while checking kDC for cell (" + x + "," + y + ")", ex);
                }

                if (khIDFFile != null)
                {
                    IDFFile.ReplaceNaNWithNoData(khIDFFile);
                    khIDFFile.WriteFile();
                }
                if (kvIDFFile != null)
                {
                    IDFFile.ReplaceNaNWithNoData(kvIDFFile);
                    kvIDFFile.WriteFile();
                }
                if (khTot1IDFFile != null)
                {
                    IDFFile.ReplaceNaNWithNoData(khTot1IDFFile);
                    khTot1IDFFile.WriteFile();
                }
                if (kvTotIDFFile != null)
                {
                    IDFFile.ReplaceNaNWithNoData(kvTotIDFFile);
                    kvTotIDFFile.WriteFile();
                }

                // Write kD errors
                if (kdErrorLayer.HasResults())
                {
                    kdErrorLayer.CompressLegend(CombinedResultLabel);
                    kdErrorLayer.WriteResultFile(log);
                    resultHandler.AddExtraMapFile(topIDFFile);
                    resultHandler.AddExtraMapFile(botIDFFile);
                    resultHandler.AddExtraMapFile(kdIDFFile);
                    resultHandler.AddExtraMapFile(khIDFFile);
                    resultHandler.AddExtraMapFile(khTot1IDFFile);
                    resultHandler.AddExtraMapFile(lowerTopIDFFile);
                }

                // Write C errors
                if ((cErrorLayer != null) && (cErrorLayer.HasResults()))
                {
                    cErrorLayer.CompressLegend(CombinedResultLabel);
                    cErrorLayer.WriteResultFile(log);
                    resultHandler.AddExtraMapFile(topIDFFile);
                    resultHandler.AddExtraMapFile(botIDFFile);
                    resultHandler.AddExtraMapFile(cIDFFile);
                    resultHandler.AddExtraMapFile(kvTotIDFFile);
                    resultHandler.AddExtraMapFile(lowerTopIDFFile);
                }

                // Write kD warnings
                if (kdWarningLayer.HasResults())
                {
                    kdWarningLayer.CompressLegend(CombinedResultLabel);
                    kdWarningLayer.WriteResultFile(log);
                    resultHandler.AddExtraMapFile(kdIDFFile);
                    resultHandler.AddExtraMapFile(khIDFFile);
                }
                // Write C warnings
                if ((cWarningLayer != null) && (cWarningLayer.HasResults()))
                {
                    cWarningLayer.CompressLegend(CombinedResultLabel);
                    cWarningLayer.WriteResultFile(log);
                    resultHandler.AddExtraMapFile(cIDFFile);
                    resultHandler.AddExtraMapFile(kvIDFFile);
                    resultHandler.AddExtraMapFile(lowerTopIDFFile);
                }

                if (cErrorLayer != null)
                {
                    cErrorLayer.ReleaseMemory(true);
                }
                if (kdErrorLayer != null)
                {
                    kdErrorLayer.ReleaseMemory(true);
                }
                if (cWarningLayer != null)
                {
                    cWarningLayer.ReleaseMemory(true);
                }
                if (kdWarningLayer != null)
                {
                    kdWarningLayer.ReleaseMemory(true);
                }
                if (khIDFFile != null)
                {
                    khIDFFile.ReleaseMemory(true);
                }
                if (kvIDFFile != null)
                {
                    kvIDFFile.ReleaseMemory(true);
                }

                // Collect freed memory
                kdwPackage.ReleaseMemory(true);
                vcwPackage.ReleaseMemory(true);
                if (topPackage != null)
                {
                    topPackage.ReleaseMemory(true);
                }
                if (botPackage != null)
                {
                    botPackage.ReleaseMemory(true);
                }
                GC.Collect();
            }

            if (kdSumIDFFile != null)
            {
                string kdSumFilename = Path.Combine(FileUtils.EnsureFolderExists(GetIMODFilesPath(model), this.Name), "kDSum.IDF");
                Metadata metadata = new Metadata("kD-sum of layers " + resultHandler.MinEntryNumber + "-" + resultHandler.MaxEntryNumber);
                kdSumIDFFile.WriteFile(kdSumFilename, metadata);
                resultHandler.AddExtraMapFile(kdSumIDFFile);
                kdSumIDFFile.ReleaseMemory(true);
            }
            if (cSumIDFFile != null)
            {
                string cSumFilename = Path.Combine(FileUtils.EnsureFolderExists(GetIMODFilesPath(model), this.Name), "cSum.IDF");
                Metadata metadata = new Metadata("c-sum of layers " + resultHandler.MinEntryNumber + "-" + resultHandler.MaxEntryNumber);
                cSumIDFFile.WriteFile(cSumFilename, metadata);
                resultHandler.AddExtraMapFile(cSumIDFFile);
                cSumIDFFile.ReleaseMemory(true);
            }
        }

        private IDFFile CreateSparseKIDFFile(string kPrefix, string kdcPrefix, IDFFile kdcIDFFile, IDFFile topIDFFile, IDFFile botIDFFile, Model model, int ilay)
        {
            IDFFile kIDFFile = new SparseIDFFile(Path.Combine(FileUtils.EnsureFolderExists(GetIMODFilesPath(model), this.Name), kPrefix.ToUpper() + "_L" + ilay + ".IDF"),
                kdcIDFFile.Extent, kdcIDFFile.XCellsize, kdcIDFFile.NoDataValue);
            kIDFFile.ResetValues();
            kIDFFile.Metadata = model.CreateMetadata();
            kIDFFile.Metadata.Description = "Calculated " + kPrefix.ToLower() + "-values from " + kdcPrefix + "-, TOP- and BOT-values; NoData for undefined/unknown k-value";
            kIDFFile.Metadata.Source = kdcIDFFile.Filename;
            if (topIDFFile != null)
            {
                kIDFFile.Metadata.Source += ";" + topIDFFile.Filename;
            }
            if (botIDFFile != null)
            {
                kIDFFile.Metadata.Source += ";" + botIDFFile.Filename;
            }

            return kIDFFile;
        }

        private IDFFile CreateKIDFFile(string kPrefix, string kdcPrefix, IDFFile kdcIDFFile, IDFFile topIDFFile, IDFFile botIDFFile, Model model, int ilay)
        {
            IDFFile kIDFFile = kdcIDFFile.CopyIDF(Path.Combine(FileUtils.EnsureFolderExists(GetIMODFilesPath(model), this.Name), kPrefix.ToUpper() + "_L" + ilay + ".IDF"));
            kIDFFile.ResetValues();
            kIDFFile.Metadata = model.CreateMetadata();
            kIDFFile.Metadata.Description = "Calculated " + kPrefix.ToLower() + "-values from " + kdcPrefix + "-, TOP- and BOT-values; NoData for undefined/unknown k-value";
            kIDFFile.Metadata.Source = kdcIDFFile.Filename;
            if (topIDFFile != null)
            {
                kIDFFile.Metadata.Source += ";" + topIDFFile.Filename;
            }
            if (botIDFFile != null)
            {
                kIDFFile.Metadata.Source += ";" + botIDFFile.Filename;
            }

            return kIDFFile;
        }
    }
}
