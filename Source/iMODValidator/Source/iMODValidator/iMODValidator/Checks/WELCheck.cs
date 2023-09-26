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
using System.ComponentModel;
using System.Drawing;
using System.IO;
using OrderedPropertyGrid;
using Sweco.SIF.iMOD.IDF;
using Sweco.SIF.Common;
using Sweco.SIF.iMODValidator.Models.Packages;
using Sweco.SIF.iMODValidator.Models;
using Sweco.SIF.iMODValidator.Checks.CheckResults;
using Sweco.SIF.Statistics;
using Sweco.SIF.iMOD;
using Sweco.SIF.iMOD.IPF;
using System.Globalization;
using Sweco.SIF.GIS;
using Sweco.SIF.iMODValidator.Models.Packages.Files;

namespace Sweco.SIF.iMODValidator.Checks
{
    /// <summary>
    /// Define settings for WELL-check
    /// </summary>
    [TypeConverter(typeof(PropertySorter))]
    class WELCheckSettings : CheckSettings
    {
        internal bool IsCheckLevelWarningDisabled = false;

        /// <summary>
        /// Available CheckLevels that define a range of predefined settings that influence the number of results
        /// </summary>
        public enum CheckLevelEnum
        {
            Custom      = 0,
            FewResults  = 1,
            Medium      = 2,
            ManyResults = 3,
        }

        private string ipfDischargeCol;
        private string ipfZ1Col;
        private string ipfZ2Col;
        private float resultCellSize;
        private float tsMinMaxPercentage;
        private string minFilterLength;
        private string maxFilterLength;
        private string minLayerFilterFraction;
        private string minAquiferFilterFraction;
        private string minLayerFraction;
        private string luseCityCodes;
        private string luseCityBlockSize;
        private string luseCityBufferSize;
        private string luseCityBufferAccuracy;
        private string cityCheckMaxAvgDischarge;
        private string minAvgDischargeToCheckWell;
        private string maxAvgDischargeToCheckWell;
        private float outlierMethodMultiplier;
        private float changeOutlierMethodMultiplier;
        private string clusterIDColumnIndex;
        private int minClusterSize;
        private float maxClusterDistance;
        private CheckLevelEnum checkLevel;

        [Category("CheckLevel-properties"), Description("Specifies if only the modelperiod (as defined in the runfile) should be checked or the complete timeseries"), PropertyOrder(41)]
        public bool UseModelperiodForChecks { get; set; }

        [Category("CheckLevel-properties"), Description("Specify that a just warning is given for a column mismatch with the available list seperators and an exception is not thrown"), PropertyOrder(42)]
        public bool UseWarningForColumnMismatch { get; set; }

        [Category("CheckLevel-properties"), Description("Minimum average discharge (m3/d) to check wells. Wells with lower discharge are not checked. Note: pumped water has a negative discharge!"), PropertyOrder(43)]
        public string MinAvgDischargeToCheckWell
        {
            get { return minAvgDischargeToCheckWell; }
            set
            {
                try
                {
                    // Allow a float or an empty string
                    if (value.Trim().Equals(string.Empty))
                    {
                        minAvgDischargeToCheckWell = string.Empty;
                    }
                    else
                    {
                        float discharge = float.Parse(value, englishCultureInfo);
                        minAvgDischargeToCheckWell = discharge.ToString(englishCultureInfo);
                    }
                }
                catch (Exception)
                {
                    // ignore
                }
            }
        }

        [Category("CheckLevel-properties"), Description("Maximum average discharge (m3/d) to check wells. Wells with higher discharge are not checked. Note: pumped water has a negative discharge!"), PropertyOrder(44)]
        public string MaxAvgDischargeToCheckWell
        {
            get { return maxAvgDischargeToCheckWell; }
            set
            {
                // Allow a float or an empty string
                if (value.Trim().Equals(string.Empty))
                {
                    maxAvgDischargeToCheckWell = string.Empty;
                }
                else
                {
                    float discharge = float.Parse(value, englishCultureInfo);
                    maxAvgDischargeToCheckWell = discharge.ToString(englishCultureInfo);
                }
            }
        }

        [Category("CheckLevel-properties"), Description("Specifies if kD (of other layer) is first checked for FilterNotInLayer error"), PropertyOrder(45)]
        public bool IsFilterNotInLayerKDChecked { get; set; }

        [Category("CheckLevel-properties"), Description("Specifies if should be checked for filters in aquitard or above surfacelevel"), PropertyOrder(46)]
        public bool IsFilterInAquitardChecked { get; set; }

        [Category("WEL-properties"), Description("Discharge column: as a (zero-based) index or as a semicolon-seperated list of strings with the columnnames"), PropertyOrder(10)]
        public string IPFDischargeCol
        {
            get { return ipfDischargeCol; }
            set
            {
                if (int.TryParse(value, out int intValue))
                {
                    if (intValue > 0)
                    {
                        ipfDischargeCol = value;
                    }
                }
                else
                {
                    ipfDischargeCol = value;
                }
            }
        }

        [Category("WEL-properties"), Description("Z1 (filter top level): as a (zero-based) index or as a semicolon-seperated list of strings with the columnnames"), PropertyOrder(11)]
        public string IPFZ1Col
        {
            get { return ipfZ1Col; }
            set
            {
                if (int.TryParse(value, out int intValue))
                {
                    if (intValue > 0)
                    {
                        ipfZ1Col = value;
                    }
                }
                else
                {
                    ipfZ1Col = value;
                }
            }
        }

        [Category("WEL-properties"), Description("z2 (filter bottom level): as a (zero-based) index or as a semicolon-seperated list of strings with the columnnames"), PropertyOrder(12)]
        public string IPFZ2Col
        {
            get { return ipfZ2Col; }
            set
            {
                if (int.TryParse(value, out int intValue))
                {
                    if (intValue > 0)
                    {
                        ipfZ2Col = value;
                    }
                }
                else
                {
                    ipfZ2Col = value;
                }
            }
        }

        [Category("WEL-properties"), Description("Cellsize (m) for the resultgrids"), PropertyOrder(13)]
        public float ResultCellSize
        {
            get { return resultCellSize; }
            set
            {
                if (value > 0)
                {
                    resultCellSize = value;
                }
            }
        }

        [Category("WEL-properties"), Description("The value that is recognized as NoData in top- or bottom values in the IPF-files"), PropertyOrder(14)]
        public float NoDataFilterLevelValue { get; set; }

        [Category("WEL-properties"), Description("Specified that comma's in values of WEL-timeseries should be processed as a decimal seperator, otherwise they will generate an error"), PropertyOrder(15)]
        public bool AllowTimeseriesComma { get; set; }

        [Category("WEL-properties"), Description("The minimum valid average discharge (m3/d). Note: pumped water has a negative discharge!"), PropertyOrder(20)]
        public string MinAvgDischarge { get; set; }

        [Category("WEL-properties"), Description("The maximum valid average discharge (m3/d). Note: pumped water has a negative discharge!"), PropertyOrder(21)]
        public string MaxAvgDischarge { get; set; }

        [Category("WEL-properties"), Description("Percentage of min/max discharge in timeseries allowed on top of defined min/max average discharge"), PropertyOrder(22)]
        public float TSMinMaxPercentage
        {
            get { return tsMinMaxPercentage; }
            set
            {
                try
                {
                    if (value >= 1 && value <= 100)
                    {
                        tsMinMaxPercentage = value;
                    }
                }
                catch (Exception)
                {
                    // ignore
                }
            }
        }

        [Category("WEL-properties"), Description("The minimum kD-value for a well-aquifer"), PropertyOrder(23)]
        public string MinKDValue { get; set; }

        [Category("WEL-properties"), Description("The minimum c-value above and below aquifer to do a small aquifer check"), PropertyOrder(24)]
        public string MinCValue { get; set; }

        [Category("WEL-properties"), Description("The minimum kH-value for a well-aquifer"), PropertyOrder(25)]
        public string MinKHValue { get; set; }

        [Category("WEL-properties"), Description("The minimum kDq-ratio (kD / -avgdischarge) for a well-aquifer. Currently only layers enclosed by aquitards are checked."), PropertyOrder(26)]
        public string MinKDQRatio { get; set; }

        [Category("WEL-properties"), Description("The minimum layer thickness fraction [0 - 1.0] within the modellayer to be assigned to it"), PropertyOrder(27)]
        public string MinLayerFraction
        {
            get { return minLayerFraction; }
            set
            {
                try
                {
                    float fraction = float.Parse(value, englishCultureInfo);
                    if ((fraction >= 0) && (fraction <= 1.0))
                    {
                        minLayerFraction = fraction.ToString(englishCultureInfo);
                    }
                }
                catch (Exception)
                {
                    // ignore
                }
            }
        }

        [Category("WEL-properties"), Description("The minimum filter length fraction [0 - 1.0] inside the modellayer to be assigned to it"), PropertyOrder(28)]
        public string MinLayerFilterFraction
        {
            get { return minLayerFilterFraction; }
            set
            {
                try
                {
                    float fraction = float.Parse(value, englishCultureInfo);
                    if ((fraction >= 0) && (fraction <= 1.0))
                    {
                        minLayerFilterFraction = fraction.ToString(englishCultureInfo);
                    }
                }
                catch (Exception)
                {
                    // ignore
                }
            }
        }

        [Category("WEL-properties"), Description("The minimum filter length fraction [0 - 1.0] inside the aquifer to be assigned to this layer"), PropertyOrder(29)]
        public string MinAquiferFilterFraction
        {
            get { return minAquiferFilterFraction; }
            set
            {
                try
                {
                    float fraction = float.Parse(value, englishCultureInfo);
                    if ((fraction >= 0) && (fraction <= 1.0))
                    {
                        minAquiferFilterFraction = fraction.ToString(englishCultureInfo);
                    }
                }
                catch (Exception)
                {
                    // ignore
                }
            }
        }

        [Category("WEL-properties"), Description("The minimum total filter length (m)"), PropertyOrder(30)]
        public string MinFilterLength
        {
            get { return minFilterLength; }
            set
            {
                try
                {
                    float length = float.Parse(value, englishCultureInfo);
                    if (length >= 0)
                    {
                        minFilterLength = length.ToString(englishCultureInfo);
                    }
                }
                catch (Exception)
                {
                    // ignore
                }
            }
        }

        [Category("WEL-properties"), Description("The maximum total filter length (m)"), PropertyOrder(30)]
        public string MaxFilterLength
        {
            get { return maxFilterLength; }
            set
            {
                try
                {
                    float length = float.Parse(value, englishCultureInfo);
                    if (length >= 0)
                    {
                        maxFilterLength = length.ToString(englishCultureInfo);
                    }
                }
                catch (Exception)
                {
                    // ignore
                }
            }
        }

        [Category("WEL-properties"), Description("The name or index of the well fraction-column in WEL IPF-files (or empty string to ignore)"), PropertyOrder(31)]
        public string WELFractionColumNameOrIdx { get; set; }

        [Category("Citycheck-properties"), Description("Specifies if the check for wells in cities (LUSE) should be done"), PropertyOrder(61)]
        public bool IsCityChecked { get; set; }

        [Category("Citycheck-properties"), Description("The LUSE-codes that can occur in cities (comma-seperated)"), PropertyOrder(62)]
        public string LUSECityCodes
        {
            get { return luseCityCodes; }
            set
            {
                try
                {
                    List<int> codeList = ParseIntArrayString(value);
                    if (codeList != null)
                    {
                        luseCityCodes = value.Trim();
                    }
                }
                catch (Exception)
                {
                    // ignore
                }
            }
        }

        [Category("Citycheck-properties"), Description("The block size (m) in which the most occuring LUSE-code determines the location of a city"), PropertyOrder(63)]
        public string LUSECityBlockSize
        {
            get { return luseCityBlockSize; }
            set
            {
                try
                {
                    float blocksize = float.Parse(value, englishCultureInfo);
                    if (blocksize > 0)
                    {
                        luseCityBlockSize = blocksize.ToString(englishCultureInfo);
                    }
                }
                catch (Exception)
                {
                    // ignore
                }
            }
        }

        [Category("Citycheck-properties"), Description("Buffersize (m) around cityblocks within which is checked for wells"), PropertyOrder(64)]
        public string LUSECityBufferSize
        {
            get { return luseCityBufferSize; }
            set
            {
                try
                {
                    float buffersize = float.Parse(value, englishCultureInfo);
                    if (buffersize > 0)
                    {
                        luseCityBufferSize = buffersize.ToString(englishCultureInfo);
                    }
                }
                catch (Exception)
                {
                    // ignore
                }
            }
        }

        [Category("Citycheck-properties"), Description("Accurary (%) of buffergrid: cellsize = 1 / accurary * buffersize"), PropertyOrder(65)]
        public string LUSECityBufferAccuracy
        {
            get { return luseCityBufferAccuracy; }
            set
            {
                try
                {
                    float percentage = float.Parse(value, englishCultureInfo);
                    if (percentage >= 1 && percentage <= 100)
                    {
                        luseCityBufferAccuracy = percentage.ToString(englishCultureInfo);
                    }
                }
                catch (Exception)
                {
                    // ignore
                }
            }
        }

        [Category("Citycheck-properties"), Description("Maximum average discharge (m3/d) for checking wells in cities. Note: pumped water has a negative discharge!"), PropertyOrder(66)]
        public string CityCheckMaxAvgDischarge
        {
            get { return cityCheckMaxAvgDischarge; }
            set
            {
                try
                {
                    float dischargeValue = float.Parse(value, englishCultureInfo);
                    cityCheckMaxAvgDischarge = dischargeValue.ToString(englishCultureInfo);
                }
                catch (Exception)
                {
                    // ignore
                }
            }
        }

        [Category("Outlier-properties"), Description("Specifies that outliers should be searched"), PropertyOrder(44)]
        public bool IsOutlierChecked { get; set; }

        [Category("Outlier-properties"), Description("The method for identifying spatial outliers"), PropertyOrder(45)]
        public OutlierMethodEnum OutlierMethod { get; set; }

        [Category("Outlier-properties"), Description("The valid base range for identifying outliers"), PropertyOrder(46)]
        public OutlierBaseRangeEnum OutlierMethodBaseRange { get; set; }

        [Category("Outlier-properties"), Description("Outlier multiplier for discharge in timeseries: the factor to multiply the base range with (For IQR-method, 1.5 corresponds with 2*SD, 3.95 with 6*SD if underlying distribution is Gaussian)"), PropertyOrder(47)]
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

        [Category("Outlier-properties"), Description("Outlier multiplier for discharge changes in timeseries: the factor to multiply the base range with (For IQR-method, 1.5 corresponds with 2*SD, 3.95 with 6*SD if underlying distribution is Gaussian)"), PropertyOrder(48)]
        public float ChangeOutlierMethodMultiplier
        {
            get { return changeOutlierMethodMultiplier; }
            set
            {
                if (value > 0)
                {
                    changeOutlierMethodMultiplier = value;
                }
            }
        }

        [Category("Cluster-properties"), Description("Specifies if the check for clusters should be done"), PropertyOrder(52)]
        public bool IsClusterChecked { get; set; }

        [Category("Cluster-properties"), Description("Minumum number of wells in a cluster (at least 3)"), PropertyOrder(53)]
        public int MinClusterSize
        {
            get { return minClusterSize; }
            set
            {
                if (value > 2)
                {
                    minClusterSize = value;
                }
            }
        }

        [Category("Cluster-properties"), Description("Maximum distance (m) between wells in a cluster"), PropertyOrder(54)]
        public float MaxClusterDistance
        {
            get { return maxClusterDistance; }
            set
            {
                try
                {
                    if (value >= 0)
                    {
                        maxClusterDistance = value;
                    }
                }
                catch (Exception)
                {
                    // ignore
                }
            }
        }

        [Category("Cluster-properties"), Description("Maximum average discharge (m3/d) for cluster wells. Note: pumped water has a negative discharge!"), PropertyOrder(55)]
        public float MaxClusterDischarge { get; set; }

        [Category("Cluster-properties"), Description("Optional (zero-based) column index in WEL IPF-file for cluster-ID"), PropertyOrder(56)]
        public string ClusterIDColumnIndex
        {
            get { return clusterIDColumnIndex; }
            set
            {
                try
                {
                    int idx = int.Parse(value);
                    clusterIDColumnIndex = value;
                }
                catch (Exception)
                {
                    // ignore
                }
            }
        }

        [Category("CheckLevel-properties"), Description("Profiles for preselected settings to restrict results. Note: CheckLevel is set to Custom after choosing a preselected CheckLevel."), PropertyOrder(40)]
        public CheckLevelEnum CheckLevel
        {
            get { return (CheckLevelEnum) checkLevel; }
            set
            {
                if ((value != CheckLevelEnum.Custom) && !IsCheckLevelWarningDisabled)
                {
                    bool isYes = Forms.MessageForm.ShowYesNoMessage("Changing CheckLevel will reset all WELCheck-settings and settingsfile will be overwritten. Do you want to continue?", "WELCheck settings");
                    if (!isYes)
                    {
                        return;
                    }
                }

                switch (value)
                {
                    // Define hardcoded default settings for WEL-check for each profile
                    case CheckLevelEnum.FewResults:
                        MinAvgDischarge = string.Empty;
                        MaxAvgDischarge = "0";
                        tsMinMaxPercentage = 20f;
                        MinKDValue = "10";
                        MinCValue = "50";
                        MinKHValue = "5";
                        MinKDQRatio = (0.5).ToString(SIFTool.EnglishCultureInfo);
                        minLayerFraction = (0.1f).ToString(SIFTool.EnglishCultureInfo);
                        minLayerFilterFraction = (0.01f).ToString(SIFTool.EnglishCultureInfo);
                        minAquiferFilterFraction = (0f).ToString(SIFTool.EnglishCultureInfo);
                        minFilterLength = "0";
                        maxFilterLength = "500";
                        IsCityChecked = true;
                        luseCityCodes = "18";
                        luseCityBlockSize = "1000";
                        luseCityBufferSize = "500";
                        luseCityBufferAccuracy = "10";
                        cityCheckMaxAvgDischarge = "-1000";
                        UseModelperiodForChecks = true;
                        UseWarningForColumnMismatch = true;
                        minAvgDischargeToCheckWell = string.Empty;
                        maxAvgDischargeToCheckWell = (-50f).ToString();
                        IsFilterNotInLayerKDChecked = true;
                        IsFilterInAquitardChecked = false;
                        IsOutlierChecked = false;
                        OutlierMethod = OutlierMethodEnum.IQR;
                        OutlierMethodBaseRange = OutlierBaseRangeEnum.Pct95_5;
                        outlierMethodMultiplier = 3.95f;
                        changeOutlierMethodMultiplier = 3.95f;
                        IsClusterChecked = false;
                        minClusterSize = 5;
                        maxClusterDistance = 300;
                        MaxClusterDischarge = -10;
                        clusterIDColumnIndex = string.Empty;
                        break;
                    case CheckLevelEnum.Medium:
                        MinAvgDischarge = string.Empty;
                        MaxAvgDischarge = "0";
                        tsMinMaxPercentage = 10f;
                        MinKDValue = "10";
                        MinCValue = "50";
                        MinKHValue = "10";
                        MinKDQRatio = "1";
                        minLayerFraction = (0.1f).ToString(SIFTool.EnglishCultureInfo);
                        minLayerFilterFraction = (0.05f).ToString(SIFTool.EnglishCultureInfo);
                        minAquiferFilterFraction = (0.01f).ToString(SIFTool.EnglishCultureInfo);
                        minFilterLength = "0";
                        maxFilterLength = "250";
                        IsCityChecked = true;
                        luseCityCodes = "18";
                        luseCityBlockSize = "500";
                        luseCityBufferSize = "500";
                        luseCityBufferAccuracy = "10";
                        cityCheckMaxAvgDischarge = "-750";
                        UseModelperiodForChecks = true;
                        UseWarningForColumnMismatch = true;
                        minAvgDischargeToCheckWell = string.Empty;
                        maxAvgDischargeToCheckWell = (-10f).ToString(SIFTool.EnglishCultureInfo);
                        IsFilterNotInLayerKDChecked = false;
                        IsFilterInAquitardChecked = false;
                        IsOutlierChecked = true;
                        OutlierMethod = OutlierMethodEnum.IQR;
                        OutlierMethodBaseRange = OutlierBaseRangeEnum.Pct75_25;
                        outlierMethodMultiplier = 3.95f;
                        changeOutlierMethodMultiplier = 3.95f;
                        IsClusterChecked = true;
                        minClusterSize = 5;
                        maxClusterDistance = 300;
                        MaxClusterDischarge = -10;
                        clusterIDColumnIndex = string.Empty;
                        break;
                    case CheckLevelEnum.ManyResults:
                        MinAvgDischarge = string.Empty;
                        MaxAvgDischarge = "0";
                        tsMinMaxPercentage = 10f;
                        MinKDValue = "10";
                        MinKHValue = "15";
                        MinKDQRatio = "2";
                        minLayerFraction = (0.1f).ToString(SIFTool.EnglishCultureInfo);
                        minLayerFilterFraction = (0.05f).ToString(SIFTool.EnglishCultureInfo);
                        minAquiferFilterFraction = (0.05f).ToString(SIFTool.EnglishCultureInfo);
                        minFilterLength = (0.1f).ToString(SIFTool.EnglishCultureInfo);
                        maxFilterLength = "150";
                        IsCityChecked = true;
                        luseCityCodes = "18";
                        luseCityBlockSize = "500";
                        luseCityBufferSize = "500";
                        luseCityBufferAccuracy = "10";
                        cityCheckMaxAvgDischarge = "-500";
                        UseModelperiodForChecks = true;
                        UseWarningForColumnMismatch = true;
                        minAvgDischargeToCheckWell = string.Empty;
                        maxAvgDischargeToCheckWell = string.Empty;
                        IsFilterNotInLayerKDChecked = false;
                        IsFilterInAquitardChecked = true;
                        IsOutlierChecked = true;
                        OutlierMethod = OutlierMethodEnum.IQR;
                        OutlierMethodBaseRange = OutlierBaseRangeEnum.Pct75_25;
                        outlierMethodMultiplier = 1.5f;
                        changeOutlierMethodMultiplier = 1.5f;
                        IsClusterChecked = true;
                        minClusterSize = 5;
                        maxClusterDistance = 300;
                        MaxClusterDischarge = -10;
                        clusterIDColumnIndex = string.Empty;
                        break;
                    case CheckLevelEnum.Custom:
                        // leave as it is (just to make clear that settings are specified by user
                        break;
                    default:
                        throw new Exception("Undefined checklevel: " + checkLevel);
                }

                // Always set CheckLevel to Custom after applying specified CheckLevel
                checkLevel = CheckLevelEnum.Custom;
            }
        }

        public WELCheckSettings(string checkName) : base(checkName)
        {
            IPFDischargeCol = "2";
            IPFZ1Col = "3";
            IPFZ2Col = "4";
            resultCellSize = 100f;
            NoDataFilterLevelValue = -999999.0f;
            AllowTimeseriesComma = true;
            WELFractionColumNameOrIdx = "frac";
            IsCheckLevelWarningDisabled = true;
            CheckLevel = CheckLevelEnum.FewResults;
            IsCheckLevelWarningDisabled = false;
            clusterIDColumnIndex = string.Empty;
            minLayerFilterFraction = (0.01f).ToString(SIFTool.EnglishCultureInfo);
        }

        public override void LogSettings(Log log, int logIndentLevel = 0)
        {
            log.AddInfo("IPFDischargeCol: " + ipfDischargeCol, logIndentLevel);

            log.AddInfo("IPFZ1Col: " + ipfZ1Col, logIndentLevel);
            log.AddInfo("IPFZ2Col: " + ipfZ2Col, logIndentLevel);
            log.AddInfo("CheckLevel: " + checkLevel.ToString(), logIndentLevel);
            log.AddInfo("Only check within defined modelperiod: " + UseModelperiodForChecks, logIndentLevel);
            log.AddInfo("Give warning for mismatch: " + UseWarningForColumnMismatch, logIndentLevel);
            log.AddInfo("Minimum discharge for a well to be checked: " + minAvgDischargeToCheckWell, logIndentLevel);
            log.AddInfo("Maximum discharge for a well to be checked: " + maxAvgDischargeToCheckWell, logIndentLevel);
            log.AddInfo("Is kD checked for FilterNotInLayer-issue: " + IsFilterNotInLayerKDChecked, logIndentLevel);
            log.AddInfo("Is checked for filters in aquitards: " + IsFilterInAquitardChecked, logIndentLevel);
            log.AddInfo("result cellsize: " + resultCellSize, logIndentLevel);
            log.AddInfo("NoData TOPBOT-value: " + NoDataFilterLevelValue, logIndentLevel);
            log.AddInfo("Correct comma in timeseries: " + AllowTimeseriesComma, logIndentLevel);
            log.AddInfo("Minimum discharge: " + MinAvgDischarge, logIndentLevel);
            log.AddInfo("Maximum discharge: " + MaxAvgDischarge, logIndentLevel);
            log.AddInfo("Minimum kD-value: " + MinKDValue, logIndentLevel);
            log.AddInfo("Minimum C-value: " + MinCValue, logIndentLevel);
            log.AddInfo("Minimum kH-value: " + MinKHValue, logIndentLevel);
            log.AddInfo("Percentage of min/max-range in timeserieson top of min/max average: " + tsMinMaxPercentage, logIndentLevel);
            log.AddInfo("Minimum kDq-ratio: " + MinKDQRatio, logIndentLevel);
            log.AddInfo("Minimum layer thickness fraction: " + minLayerFraction, logIndentLevel);
            log.AddInfo("Minimum layer filter length fraction: " + minLayerFilterFraction, logIndentLevel);
            log.AddInfo("Minimum aquifer filter length fraction: " + minAquiferFilterFraction, logIndentLevel);
            log.AddInfo("Minimum filter length: " + minFilterLength, logIndentLevel);
            log.AddInfo("Maximum filter length: " + maxFilterLength, logIndentLevel);
            log.AddInfo("Is city checked: " + IsCityChecked, logIndentLevel);
            log.AddInfo("LUSE-citycodes: " + luseCityCodes, logIndentLevel);
            log.AddInfo("LUSE-cityblocksize: " + luseCityBlockSize, logIndentLevel);
            log.AddInfo("LUSE-citybuffersize: " + luseCityBufferSize, logIndentLevel);
            log.AddInfo("LUSE-citybuffer accurary: " + luseCityBufferAccuracy + "%", logIndentLevel);
            log.AddInfo("city-check minimum discharge: " + cityCheckMaxAvgDischarge, logIndentLevel);
            //            log.AddInfo("WEL-fraction columnname: " + welFractionColumName, logIndentLevel);
            log.AddInfo("Outlier method: " + OutlierMethod.ToString(), logIndentLevel);
            log.AddInfo("Outlier base range method: " + OutlierMethodBaseRange.ToString(), logIndentLevel);
            log.AddInfo("Outlier base range multiplier: " + outlierMethodMultiplier.ToString(), logIndentLevel);
            log.AddInfo("Change Outlier base range multiplier: " + changeOutlierMethodMultiplier.ToString(), logIndentLevel);
            log.AddInfo("Is cluster checked: " + IsClusterChecked, logIndentLevel);
            log.AddInfo("Minimum clustersize: " + minClusterSize.ToString(), logIndentLevel);
            log.AddInfo("Maximum clusterdischarge: " + MaxClusterDischarge.ToString(), logIndentLevel);
            log.AddInfo("Maximum clusterdistance: " + maxClusterDistance.ToString(), logIndentLevel);
            log.AddInfo("Column index of cluster-ID: " + clusterIDColumnIndex.ToString(), logIndentLevel);
        }
    }

    class WELCheck : Check
    {
        public override string Abbreviation
        {
            get { return "WEL"; }
        }

        public override string Description
        {
            get { return "Checks well locations per model layer/timestep and checks some timeseries-characteristics"; }
        }

        private WELCheckSettings settings;
        public override CheckSettings Settings
        {
            get { return settings; }
            set
            {
                if (value is WELCheckSettings)
                {
                    settings = (WELCheckSettings)value;
                }
            }
        }

        public WELCheck()
        {
            settings = new WELCheckSettings(this.Name);
        }

        public override void Run(Model model, CheckResultHandler resultHandler, Log log)
        {
            try
            {
                log.AddInfo("Checking WEL-package ...");

                Package welPackage = model.GetPackage(WELPackage.DefaultKey);
                if ((welPackage == null) || !welPackage.IsActive)
                {
                    log.AddWarning(this.Name, model.Runfilename, "WEL-package is not active. " + this.Name + " is skipped.", 1);
                    return;
                }

                settings.LogSettings(log, 1);
                RunWELCheck1(model, resultHandler, log);
            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error in " + this.Name, ex);
            }
        }

        protected virtual void RunWELCheck1(Model model, CheckResultHandler resultHandler, Log log)
        {
            CultureInfo englishCultureInfo = new CultureInfo("en-GB", false);

            ///////////////////////
            // Retrieve Packages //
            ///////////////////////

            // Retrieve WEL-package, note: WEL-files will be IPF-files
            Package welPackage = model.GetPackage(WELPackage.DefaultKey);

            // Retrieve CAP-package (MetaSWAP) for LUSE-file 
            bool isCityChecked = settings.IsCityChecked;
            bool isClusterChecked = settings.IsClusterChecked;
            IDFFile luseIDFFile = null;
            IDFFile cityIDFFile = null;
            if (isCityChecked)
            {
                CAPPackage capPackage = (CAPPackage)model.GetPackage(CAPPackage.DefaultKey);
                if (capPackage == null)
                {
                    log.AddInfo("CAP-package is not defined. LUSE-data cannot be retrieved. Citycheck is skipped.", 1);
                }
                if (capPackage != null)
                {
                    luseIDFFile = (IDFFile)capPackage.GetIMODFile(CAPPackage.GetEntryIdx(CAPEntryCode.LUSE));
                }
                if (luseIDFFile == null)
                {
                    log.AddInfo("LUSE-data cannot be retrieved. Citycheck is skipped.", 1);
                }
                else
                {
                    cityIDFFile = CreateCityIDFFile(luseIDFFile, settings, model, log, 1);
                }
            }

            // Retrieve TOP-/BOT- and KDW- or kHV-package(s)
            IDFPackage topPackage = (IDFPackage)model.GetPackage(TOPPackage.DefaultKey);
            IDFPackage botPackage = (IDFPackage)model.GetPackage(BOTPackage.DefaultKey);
            IDFPackage kdwPackage = (IDFPackage)model.GetPackage(KDWPackage.DefaultKey);
            IDFPackage khvPackage = (IDFPackage)model.GetPackage(KHVPackage.DefaultKey);
            IDFPackage vcwPackage = (IDFPackage)model.GetPackage(VCWPackage.DefaultKey);
            IDFPackage kvvPackage = (IDFPackage)model.GetPackage(KVVPackage.DefaultKey);
            bool hasTOPPackage = model.HasActivePackage(TOPPackage.DefaultKey);
            bool hasBOTPackage = model.HasActivePackage(BOTPackage.DefaultKey);
            bool hasKDWPackage = model.HasActivePackage(KDWPackage.DefaultKey);
            bool hasKHVPackage = model.HasActivePackage(KHVPackage.DefaultKey);
            bool hasVCWPackage = model.HasActivePackage(VCWPackage.DefaultKey);
            bool hasKVVPackage = model.HasActivePackage(KVVPackage.DefaultKey);
            if ((hasKDWPackage && hasKHVPackage) || (!hasKDWPackage && !hasKHVPackage))
            {
                log.AddInfo("WEL-package is active, but KDW or KHV-package are not active or are both active...", 1);
            }
            if ((hasVCWPackage && hasKVVPackage) || (!hasVCWPackage && !hasKVVPackage))
            {
                log.AddInfo("WEL-package is active, but VCW or KVV-package are not active or are both active...", 1);
            }
            if ((!hasTOPPackage || !hasBOTPackage))
            {
                log.AddInfo("WEL-package is active, but TOP and BOT-packages are not active...", 1);
            }

            // Retrieve settings 
            IPFFile.IsWarnedForColumnMismatch = settings.UseWarningForColumnMismatch;
            float noDataFilterLevelValue = settings.NoDataFilterLevelValue;
            float tsMinMaxPercentage = settings.TSMinMaxPercentage;
            float minFilterLength = settings.GetValue(settings.MinFilterLength);
            float maxFilterLength = settings.GetValue(settings.MaxFilterLength);
            float minLayerFilterLengthFraction = settings.GetValue(settings.MinLayerFilterFraction);
            float minAquiferFilterLengthFraction = settings.GetValue(settings.MinAquiferFilterFraction);
            int minLayerFilterLengthPercentage = (int)((minLayerFilterLengthFraction) * 100);
            int minAquiferFilterLengthPercentage = (int)((minAquiferFilterLengthFraction) * 100);
            float minLayerThicknessFraction = settings.GetValue(settings.MinLayerFraction);
            int minLayerThicknessPercentage = (int)((minLayerThicknessFraction) * 100);
            float minDischargeWellChecked = settings.GetValue(settings.MinAvgDischargeToCheckWell);
            float maxDischargeWellChecked = settings.GetValue(settings.MaxAvgDischargeToCheckWell);
            bool isFilterNotInLayerKDChecked = settings.IsFilterNotInLayerKDChecked;
            bool isFilterInAquitardChecked = settings.IsFilterInAquitardChecked;

            ////////////////////////////////
            // Define legends and results //
            ////////////////////////////////

            // Define errors
            CheckError InvalidFilterLevelError = CreateCheckError("Invalid filter level(s)", "Filter level is NoData");
            CheckError NegativeFilterLengthError = CreateCheckError("Filterlength is negative", "Filter top-level is below bottom-level");
            CheckError FilterNotInLayerError = CreateCheckError("Filter not in modellayer", "Filter is with less than " + minLayerFilterLengthPercentage + "% in this modellayer, less than " + minAquiferFilterLengthFraction + "% in this aquitard or takes less than " + minLayerThicknessPercentage + "% of this modellayer");
            CheckError MissingTimeseriesError = CreateCheckError("Timeseries file could not be found");
            CheckError InvalidTimeseriesDateError = CreateCheckError("Invalid date(s) in timeseries");
            CheckError InvalidTimeseriesFileError = CreateCheckError("Invalid timeseries file");

            IPFLegend errorLegend = CreateIPFLegend();
            errorLegend.AddClass(InvalidFilterLevelError.CreateLegendValueClass(Color.Yellow, true));
            errorLegend.AddClass(NegativeFilterLengthError.CreateLegendValueClass(Color.Purple, true));
            errorLegend.AddClass(FilterNotInLayerError.CreateLegendValueClass(Color.Red, true));
            errorLegend.AddClass(MissingTimeseriesError.CreateLegendValueClass(Color.Silver, true));
            errorLegend.AddClass(InvalidTimeseriesDateError.CreateLegendValueClass(Color.Olive, true));
            errorLegend.AddClass(InvalidTimeseriesFileError.CreateLegendValueClass(Color.Pink, true));
            errorLegend.AddUpperRangeClass(CombinedResultLabel, true);
            errorLegend.AddInbetweenClasses(CombinedResultLabel, true);
            errorLegend.IsLabelShown = true;

            // Define warnings
            CheckWarning FilterLevelsEqualWarning = CreateCheckWarning("Filter top equals bottom");
            CheckWarning FilterLengthRangeWarning = CreateCheckWarning("Filterlength outside defined range", "Filterlength outside range [" + minFilterLength + "," + maxFilterLength + "]");
            CheckWarning SmallAquiferWarning = CreateCheckWarning("Filter in low kD aquifer", "Filter is in a small aquifer (kD less than specified minimum)");
            CheckWarning FilterAboveSurfaceLevelWarning = CreateCheckWarning("Filter above surfacelevel", "Filter is (partly) above surfacelevel");
            CheckWarning FilterInAquitardWarning = CreateCheckWarning("Filter in aquitard", "Filter is for more than " + minLayerFilterLengthPercentage + "% in an aquitard");
            CheckWarning ZeroDischargeWarning = CreateCheckWarning("Average discharge is zero", "Average discharge is zero (and modellayer-filterpart is not middle part of complete filterlength)");
            CheckWarning FilterInCityWarning = CreateCheckWarning("Filter near city", "Filter is within " + settings.LUSECityBufferSize + "m of city");
            CheckWarning DischargeOutlierWarning = CreateCheckWarning("Outlier in timeseries", "Discharge outlier in timeseries");
            CheckWarning DischargeChangeWarning = CreateCheckWarning("Unexpected change in timeseries");
            CheckWarning XYEqualWarning = CreateCheckWarning("XY-coordinates are equal");
            CheckWarning DischargeRangeWarning = CreateCheckWarning("Dischargevalue(s) out of range");

            IPFLegend warningLegend = CreateIPFLegend();
            warningLegend.AddClass(FilterLevelsEqualWarning.CreateLegendValueClass(Color.RosyBrown, true));
            warningLegend.AddClass(FilterLengthRangeWarning.CreateLegendValueClass(Color.Purple, true));
            warningLegend.AddClass(SmallAquiferWarning.CreateLegendValueClass(Color.Olive, true));
            warningLegend.AddClass(FilterAboveSurfaceLevelWarning.CreateLegendValueClass(Color.Orange, true));
            warningLegend.AddClass(FilterInAquitardWarning.CreateLegendValueClass(Color.Brown, true));
            warningLegend.AddClass(ZeroDischargeWarning.CreateLegendValueClass(Color.Yellow, true));
            warningLegend.AddClass(FilterInCityWarning.CreateLegendValueClass(Color.Blue, true));
            warningLegend.AddClass(XYEqualWarning.CreateLegendValueClass(Color.Red, true));
            warningLegend.AddClass(DischargeRangeWarning.CreateLegendValueClass(Color.DarkRed, true));
            if (settings.IsOutlierChecked)
            {
                warningLegend.AddClass(DischargeOutlierWarning.CreateLegendValueClass(Color.LightBlue, true));
                warningLegend.AddClass(DischargeChangeWarning.CreateLegendValueClass(Color.DarkBlue, true));
            }
            warningLegend.AddUpperRangeClass(CombinedResultLabel, true);
            errorLegend.AddInbetweenClasses(CombinedResultLabel, true);
            warningLegend.IsLabelShown = true;

            ///////////////////////////
            // Retrieve settingfiles //
            ///////////////////////////
            IDFFile minDischargeSettingIDFFile = settings.GetIDFFile(settings.MinAvgDischarge, log, 1);
            IDFFile maxDischargeSettingIDFFile = settings.GetIDFFile(settings.MaxAvgDischarge, log, 1);
            IDFFile minKDSettingIDFFile = settings.GetIDFFile(settings.MinKDValue, log, 1);
            IDFFile minCSettingIDFFile = settings.GetIDFFile(settings.MinCValue, log, 1);
            IDFFile minKHSettingIDFFile = settings.GetIDFFile(settings.MinKHValue, log, 1);
            IDFFile minKDQRatioSettingIDFFile = settings.GetIDFFile(settings.MinKDQRatio, log, 1);
            IDFFile cityMaxDischargeIDFFile = settings.GetIDFFile(settings.CityCheckMaxAvgDischarge, log, 1);

            // Process all periods
            DateTime? modelStartDate = null;
            DateTime? modelEndDate = null;
            if ((model.StartDate != null) && settings.UseModelperiodForChecks)
            {
                modelStartDate = model.StartDate.Value;
                modelEndDate = model.StartDate.Value.AddDays(model.NPER);
            }
            Dictionary<string, int> checkedWELFiles = new Dictionary<string, int>();
            bool isWarnedForZeroLayer = false;
            for (int kper = resultHandler.MinKPER; (kper <= model.NPER) && (kper <= resultHandler.MaxKPER); kper++)
            {
                if (welPackage.GetEntryCount(kper) > 0)
                {
                    if (model.NPER > 1)
                    {
                        log.AddInfo("Checking stressperiod " + kper + " " + Model.GetStressPeriodString(model.StartDate, kper) + " ...", 1);
                    }
                    else
                    {
                        log.AddInfo("Checking stressperiod " + kper + " " + Model.GetStressPeriodString(model.StartDate, kper) + " ...", 1);
                    }

                    if (!isWarnedForZeroLayer)
                    {
                        List<IMODFile> zeroLayerPackageFiles = welPackage.GetIMODFiles(0, 0, kper);
                        if ((zeroLayerPackageFiles != null) && (zeroLayerPackageFiles.Count() > 0))
                        {
                            log.AddWarning(welPackage.Key, model.Runfilename, "Currently layer 0 is not fully supported and some checks are skipped", 1);
                            isWarnedForZeroLayer = true;
                        }
                    }

                    // Process all specified systems within the current period
                    for (int entryIdx = resultHandler.MinEntryNumber - 1; (entryIdx < welPackage.GetEntryCount(kper)) && (entryIdx < resultHandler.MaxEntryNumber); entryIdx++)
                    {
                        IPFPackageFile welPackageFile = (IPFPackageFile) welPackage.GetPackageFile(entryIdx, 0, kper);
                        int ilay = welPackageFile.ilay;

                        CheckManager.Instance.CheckForAbort();
                        if ((welPackageFile != null) && !checkedWELFiles.ContainsKey(welPackageFile.FName))
                        {
                            log.AddInfo("Checking entry " + (entryIdx + 1) + " for layer " + (ilay) + ": " + Path.GetFileName(welPackageFile.FName) + " ...", 1);

                            IDFFile upperkDIDFFile = (hasKDWPackage && (ilay > 1)) ? kdwPackage.GetIDFFile(ilay - 2) : null;
                            IDFFile upperKHIDFFile = (hasKHVPackage && (ilay > 1)) ? khvPackage.GetIDFFile(ilay - 2) : null;
                            IDFFile kdIDFFile = (hasKDWPackage && (ilay > 0)) ? kdwPackage.GetIDFFile(ilay - 1) : null;
                            IDFFile khIDFFile = (hasKHVPackage && (ilay > 0)) ? khvPackage.GetIDFFile(ilay - 1) : null;
                            IDFFile lowerKDIDFFile = (hasKDWPackage && (ilay < (kdwPackage.GetEntryCount() - 1)) && (ilay > 0)) ? kdwPackage.GetIDFFile(ilay) : null;
                            IDFFile lowerKHIDFFile = (hasKHVPackage && (ilay < (khvPackage.GetEntryCount() - 1)) && (ilay > 0)) ? khvPackage.GetIDFFile(ilay) : null;
                            IDFFile upperTopIDFFile = ((topPackage != null) && (ilay > 1)) ? topPackage.GetIDFFile(ilay - 2) : null;
                            IDFFile upperBotIDFFile = ((botPackage != null) && (ilay > 1)) ? botPackage.GetIDFFile(ilay - 2) : null;
                            IDFFile topIDFFile = (topPackage != null) ? topIDFFile = topPackage.GetIDFFile(ilay - 1) : null;
                            IDFFile botIDFFile = (botPackage != null) ? botIDFFile = botPackage.GetIDFFile(ilay - 1) : null;
                            IDFFile lowerTopIDFFile = ((topPackage != null) && (ilay < topPackage.GetEntryCount() - 1)) ? topPackage.GetIDFFile(ilay) : null; // If there is a layer below the current layer retrieve its TOP-file
                            IDFFile lowerBotIDFFile = ((botPackage != null) && (ilay < botPackage.GetEntryCount() - 1)) ? botPackage.GetIDFFile(ilay) : null; // If there is a layer below the current layer retrieve its TOP-file
                            IDFFile upperCIDFFile = (hasVCWPackage && (ilay > 1)) ? vcwPackage.GetIDFFile(ilay - 2) : null;
                            IDFFile upperKVIDFFile = (hasKVVPackage && (ilay > 1)) ? kvvPackage.GetIDFFile(ilay - 2) : null;
                            IDFFile cIDFFile = (hasVCWPackage && (ilay > 0)) ? vcwPackage.GetIDFFile(ilay - 1) : null;
                            IDFFile kvIDFFile = (hasKVVPackage && (ilay > 0)) ? kvvPackage.GetIDFFile(ilay - 1) : null;
                            IDFFile calcKDIDFFile = null;
                            IDFFile calcCIDFFile = null;
                            IDFFile topL1IDFFile = ((topIDFFile == null) && (topPackage != null) && (ilay == 0)) ? topPackage.GetIDFFile(0) : null;
                            IDFFile botLNIDFFile = ((botIDFFile == null) && (botPackage != null) && (ilay == 0)) ? topPackage.GetIDFFile(model.NLAY - 1) : null;

                            List<IMODFile> sourceFiles = new List<IMODFile>();
                            sourceFiles.Add(welPackageFile.IMODFile);
                            sourceFiles.AddRange(new List<IMODFile> { upperBotIDFFile, topIDFFile, botIDFFile, lowerTopIDFFile, cityIDFFile, luseIDFFile, upperkDIDFFile, kdIDFFile, lowerKDIDFFile, upperKHIDFFile, khIDFFile, lowerKHIDFFile });

                            // Create error IPFfiles for current layer
                            // CheckErrorLayer welErrorLayer = CreateErrorLayer(resultHandler, welPackage, kper, ilay, errorLegend);
                            CheckErrorLayer welErrorLayer = CreateErrorLayer(resultHandler, welPackage, "SYS" + (entryIdx + 1), kper, ilay, errorLegend);
                            welErrorLayer.AddSourceFiles(sourceFiles);

                            // Create warning IDFfiles for current layer
                            CheckWarningLayer welWarningLayer = CreateWarningLayer(resultHandler, welPackage, "SYS" + (entryIdx + 1), kper, ilay, warningLegend);
                            welWarningLayer.AddSourceFiles(sourceFiles);

                            // Create IPF-File to store details
                            CheckDetailLayer welDetailLayer = CreateDetailLayer(resultHandler, welPackage, "SYS" + (entryIdx + 1), kper, ilay);

                            // Process all WEL-files in the current modellayer for the current period
                            long prevErrorCount = 0;
                            long prevWarningCount = 0;
                            IPFFile welIPFFile = (IPFFile)welPackageFile.IMODFile;
                            welIPFFile.Log = log;
                            welIPFFile.LogIndentLevel = 2;

                            // Retrieve and log column indices
                            if (checkedWELFiles.ContainsKey(welIPFFile.Filename))
                            {
                                int checkedILay = checkedWELFiles[welIPFFile.Filename];
                                if (checkedILay.Equals(ilay))
                                {
                                    // File has been checked before with this ilay, don't check again
                                }
                                else
                                {
                                    // File has been checked before with another ilay
                                    log.AddError(welPackage.Key, welIPFFile.Filename, "WEL-file for ilay " + ilay + " has been assigned before to another ilay (" + checkedILay + "), skipped: " + welIPFFile.Filename, 2);
                                }
                            }
                            else
                            {
                                checkedWELFiles.Add(welIPFFile.Filename, ilay);

                                int ipfDischargeColIdx = FindIPFColIdx(settings.IPFDischargeCol, welIPFFile);
                                if (ipfDischargeColIdx >= 0)
                                {
                                    log.AddInfo("Discharge in IPF has column index " + ipfDischargeColIdx + " and column name " + welIPFFile.ColumnNames[ipfDischargeColIdx], 2);
                                }
                                else
                                {
                                    log.AddWarning(welPackage.Key, welIPFFile.Filename, "Discharge column in IPF not found for specified column names: " + settings.IPFDischargeCol, 2);
                                }
                                int ipfZ1ColIdx = FindIPFColIdx(settings.IPFZ1Col, welIPFFile);
                                int ipfZ2ColIdx = FindIPFColIdx(settings.IPFZ2Col, welIPFFile);
                                if (((ipfZ1ColIdx >= 0) && (ipfZ1ColIdx < welIPFFile.ColumnCount)) && ((ipfZ2ColIdx >= 0) && (ipfZ2ColIdx < welIPFFile.ColumnCount)))
                                {
                                    log.AddInfo("IPF columnindex for z1/z2: " + ipfZ1ColIdx + ", " + ipfZ2ColIdx
                                        + "; column names: '" + welIPFFile.ColumnNames[ipfZ1ColIdx] + "', '" + welIPFFile.ColumnNames[ipfZ2ColIdx] + "'", 2);
                                }
                                else if ((ipfZ1ColIdx >= 0) && (ipfZ1ColIdx < welIPFFile.ColumnCount))
                                {
                                    log.AddInfo("IPF columnindex z1 (top): " + ipfZ1ColIdx
                                        + "; column name: '" + welIPFFile.ColumnNames[ipfZ1ColIdx] + "'", 2);
                                    log.AddWarning(welPackage.Key, welIPFFile.Filename, "IPF column not found for filter level z2 (bot) with specified column name: " + settings.IPFZ2Col, 2);
                                }
                                else if ((ipfZ2ColIdx >= 0) && (ipfZ2ColIdx < welIPFFile.ColumnCount))
                                {
                                    log.AddInfo("IPF columnindex for z2 (bot): " + ipfZ2ColIdx
                                        + "; column name: '" + welIPFFile.ColumnNames[ipfZ2ColIdx] + "'", 2);
                                    log.AddWarning(welPackage.Key, welIPFFile.Filename, "IPF column not found for filter level z1 (top) with specified column name: " + settings.IPFZ1Col, 2);
                                }
                                else
                                {
                                    log.AddWarning(welPackage.Key, welIPFFile.Filename, "IPF columns not found for filter levels z1 (top) and z2 (bot) with specified column names: " + settings.IPFZ1Col + ", " + settings.IPFZ2Col, 2);
                                }
                                int valueColIdx = ipfDischargeColIdx;
                                welIPFFile.IsCommaCorrectedInTimeseries = settings.AllowTimeseriesComma;
                                int ipfFractionColIdx = welIPFFile.FindColumnIndex(settings.WELFractionColumNameOrIdx);
                                //                                Statistics kDqRatioStats = new Statistics();

                                // Check textfileColIdx if it is defined
                                if ((welIPFFile.AssociatedFileColIdx >= 0) && (welIPFFile.AssociatedFileColIdx != ipfDischargeColIdx) && (ipfDischargeColIdx > 1))
                                {
                                    log.AddWarning(welPackage.Key, welIPFFile.Filename, "Unexpected TextFileColumnNumber (" + (welIPFFile.AssociatedFileColIdx + 1) + ") instead of " + (ipfDischargeColIdx + 1) + ", as defined in validator settings) for: " + welIPFFile.Filename, 2);
                                    log.AddInfo("Using TextFileColumnIndex " + ipfDischargeColIdx + ", as defined in validator settings", 3);
                                    // Force points to be loaded now, otherwise columnindex will be reset again
                                    List<IPFPoint> pointList = welIPFFile.Points;
                                    welIPFFile.AssociatedFileColIdx = ipfDischargeColIdx;
                                }

                                // Retrieve all timeseries of current IPF-file
                                log.AddInfo("Reading timeseries for WEL-file: " + Path.GetFileName(welIPFFile.Filename) + " ...", 2);
                                foreach (IPFPoint ipfPoint in welIPFFile.Points)
                                {
                                    if (ipfPoint.IsContainedBy(resultHandler.Extent) && ipfPoint.HasTimeseries())
                                    {
                                        try
                                        {
                                            if (ipfPoint.HasTimeseries())
                                            {
                                                ipfPoint.LoadTimeseries();
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            if (resultHandler.Extent.Contains(ipfPoint.X, ipfPoint.Y))
                                            {
                                                resultHandler.AddCheckResult(welErrorLayer, (float)ipfPoint.X, (float)ipfPoint.Y, InvalidTimeseriesFileError);
                                                resultHandler.AddCheckDetail(welDetailLayer, (float)ipfPoint.X, (float)ipfPoint.Y, new CheckDetail(InvalidTimeseriesFileError, welIPFFile,
                                                    "Error in timeseries", ex.GetBaseException().Message, (ipfDischargeColIdx > 0) ? ipfPoint.ColumnValues[ipfDischargeColIdx] : float.NaN.ToString()));
                                            }
                                        }
                                    }
                                }

                                Dictionary<IPFPoint, IPFCluster> clusterDictionary = new Dictionary<IPFPoint, IPFCluster>();
                                if (isClusterChecked)
                                {
                                    // Find clusters of wells
                                    log.AddInfo("Checking for clusters in WEL-file: " + Path.GetFileName(welIPFFile.Filename) + " ...", 2);
                                    List<IPFPoint> selectedPoints = new List<IPFPoint>();
                                    foreach (IPFPoint point in welIPFFile.Points)
                                    {
                                        if (resultHandler.Extent.Contains(point.X, point.Y))
                                        {
                                            selectedPoints.Add(point.CopyIPFPoint());
                                        }
                                    }
                                    List<IPFCluster> clusters = FindClustersByDistance(welIPFFile, selectedPoints, settings.MaxClusterDistance, settings.MaxClusterDischarge, settings.MinClusterSize, 2000f, ipfDischargeColIdx);
                                    if (!settings.ClusterIDColumnIndex.Equals(string.Empty))
                                    {
                                        int clusterIdColIdx = int.Parse(settings.ClusterIDColumnIndex);
                                        List<IPFPoint> remainingClusterPoints = CopyExcluding(welIPFFile.Points, IPFCluster.ClustersToPoints(clusters));
                                        clusters.AddRange(FindClustersById(welIPFFile, remainingClusterPoints, clusterIdColIdx, settings.MaxClusterDischarge, 5, ipfDischargeColIdx));
                                    }

                                    // Store clusters in dictionary
                                    if ((clusters != null) && (clusters.Count() > 0))
                                    {
                                        log.AddInfo("WEL-clusters: " + clusters.Count(), 3);

                                        // Calculate total discharge per cluster
                                        for (int clusterIdx = 0; clusterIdx < clusters.Count(); clusterIdx++)
                                        {
                                            IPFCluster cluster = clusters[clusterIdx];
                                            cluster.ID = Path.GetFileNameWithoutExtension(welIPFFile.Filename) + "_cluster" + (clusterIdx + 1);
                                            if (cluster.HasTimeseries())
                                            {
                                                cluster.CalculateTimeseries(ipfDischargeColIdx);
                                            }
                                            else
                                            {
                                                cluster.CalculateAverage(ipfDischargeColIdx);
                                            }

                                            foreach (IPFPoint point in cluster.Points)
                                            {
                                                if (clusterDictionary.ContainsKey(point))
                                                {
                                                    log.AddWarning(welPackage.Key, cluster.IPFFile.Filename, "Point in more than one cluster is skipped: " + point.ToString() + ". May be caused by multiple wellpoints at same xy-coordinates.", 3);
                                                    // throw new Exception("Point in more than one cluster: " + point.ToString());
                                                }
                                                else
                                                {
                                                    clusterDictionary.Add(point, cluster);
                                                }
                                            }
                                        }

                                        // Write clusterfile, including total timeseries to tool output folder
                                        string clusterFilename = FileUtils.AddFilePostFix(Path.Combine(FileUtils.EnsureFolderExists(GetIMODFilesPath(model), Name), Path.GetFileName(welIPFFile.Filename)), "_clusters");
                                        IPFFile ipfClusterFile = welIPFFile.CopyIPF(clusterFilename);
                                        ipfClusterFile.ResetValues();
                                        ipfClusterFile.AddPoints(IPFCluster.ClustersToPoints(clusters));
                                        if (ipfClusterFile.PointCount > 0)
                                        {
                                            ipfClusterFile.Legend = ipfClusterFile.CreateLegend("WEL-clusters L" + ilay, Color.DarkBlue);
                                            ipfClusterFile.WriteFile(false);
                                            resultHandler.AddExtraMapFile(ipfClusterFile);
                                        }

                                        // Add IPFfile to all clusters
                                        foreach (IPFCluster cluster in clusters)
                                        {
                                            cluster.IPFFile = ipfClusterFile;
                                        }
                                    }
                                }

                                // Process all wells for the current WEL-file
                                log.AddInfo("Checking " + welIPFFile.PointCount + " locations in WEL-file: " + Path.GetFileName(welIPFFile.Filename) + " ...", 2);
                                List<IPFPoint> clusterDischargeWellSelection = new List<IPFPoint>();
                                int checkedWellCount = 0;
                                int extentWellCount = 0;
                                foreach (IPFPoint ipfPoint in welIPFFile.Points)
                                {
                                    try
                                    {
                                        float x = (float)ipfPoint.X;
                                        float y = (float)ipfPoint.Y;

                                        if (x.Equals(y))
                                        {
                                            // Add log warning as well because ipfpoint is likely outside range and can easily be missed
                                            log.AddWarning(welPackage.Key, welIPFFile.Filename, "XY-coordinates are equal for x/y: " + x + "," + y);
                                            resultHandler.AddCheckResult(welWarningLayer, x, y, XYEqualWarning);
                                            resultHandler.AddCheckDetail(welDetailLayer, x, y, new CheckDetail(XYEqualWarning, welIPFFile,
                                                "XY-coordinates equal: " + x.ToString("F0", EnglishCultureInfo) + " = " + y.ToString("F0", EnglishCultureInfo)));
                                        }

                                        // Check if location inside modelling area
                                        if (ipfPoint.IsContainedBy(resultHandler.Extent))
                                        {
                                            extentWellCount++;

                                            ///////////////////////////////////
                                            // Retrieve well-characteristics //
                                            ///////////////////////////////////

                                            // Retrieve well-discharge data
                                            Sweco.SIF.iMOD.Timeseries wellTimeseries = null;            // data specific for individual well
                                            float wellAvgDischargeValue = float.NaN;
                                            float wellMinDischargeValue = float.NaN;
                                            float wellMaxDischargeValue = float.NaN;

                                            IPFCluster cluster = null;                                  // cluster data 
                                            float clusterX = x;
                                            float clusterY = y;

                                            Sweco.SIF.iMOD.Timeseries timeseries = null;                // data specific for cluster or (if not present) for individual well
                                            Sweco.SIF.iMOD.Timeseries modelperiodTimeseries = null;
                                            int avgDischargePeriod = -1;                                // number of days of period within which some discharge is actually present
                                            float avgDischargeValue = float.NaN;
                                            float minDischargeValue = float.NaN;
                                            float maxDischargeValue = float.NaN;

                                            // Retrieve individual well-discharge data
                                            if (!ipfPoint.HasAssociatedFile())
                                            {
                                                if (ipfPoint.HasFloatValue(ipfDischargeColIdx))
                                                {
                                                    // Use floating point value if present
                                                    wellAvgDischargeValue = ipfPoint.GetFloatValue(ipfDischargeColIdx);
                                                    avgDischargeValue = wellAvgDischargeValue;
                                                }
                                                else
                                                {
                                                    // Timeseriesfile does not exist
                                                    resultHandler.AddCheckResult(welErrorLayer, x, y, MissingTimeseriesError);
                                                    resultHandler.AddCheckDetail(welDetailLayer, x, y, new CheckDetail(MissingTimeseriesError, welIPFFile,
                                                        ipfPoint.ColumnValues[ipfPoint.IPFFile.AssociatedFileColIdx]));
                                                }
                                            }
                                            else
                                            {
                                                try
                                                {
                                                    wellTimeseries = ipfPoint.Timeseries;
                                                    timeseries = wellTimeseries;
                                                    modelperiodTimeseries = wellTimeseries.InterpolateTimeseries(modelStartDate, modelEndDate);

                                                    // Calculate average over modelperiod (including zero values) with interpolated values to ensure each value has proper weight 
                                                    Sweco.SIF.iMOD.Timeseries wellDischargeperiodTimeseries = modelperiodTimeseries.Select(float.NaN, 0.001f);

                                                    // Get number of days of period within which some discharge is actually present
                                                    avgDischargePeriod = (wellDischargeperiodTimeseries.Timestamps.Count() > 0) ? wellDischargeperiodTimeseries.Timestamps[wellDischargeperiodTimeseries.Timestamps.Count() - 1].Subtract(wellDischargeperiodTimeseries.Timestamps[0]).Days : 0;

                                                    Statistics.Statistics dischargeStats = new Statistics.Statistics(modelperiodTimeseries.Values);
                                                    dischargeStats.ComputeBasicStatistics(true, true, false);
                                                    wellAvgDischargeValue = dischargeStats.Mean;
                                                    wellMinDischargeValue = dischargeStats.Min;
                                                    wellMaxDischargeValue = dischargeStats.Max;
                                                    if (wellAvgDischargeValue.Equals(float.NaN))
                                                    {
                                                        wellAvgDischargeValue = 0;
                                                        wellMinDischargeValue = 0;
                                                        wellMaxDischargeValue = 0;
                                                    }
                                                    avgDischargeValue = wellAvgDischargeValue;
                                                    minDischargeValue = wellMinDischargeValue;
                                                    maxDischargeValue = wellMaxDischargeValue;

                                                }
                                                catch (Exception)
                                                {
                                                    // timeseries file could not be loaded
                                                    resultHandler.AddCheckResult(welErrorLayer, x, y, InvalidTimeseriesFileError);
                                                    resultHandler.AddCheckDetail(welDetailLayer, x, y, new CheckDetail(InvalidTimeseriesFileError, welIPFFile,
                                                        ipfPoint.ColumnValues[ipfPoint.IPFFile.AssociatedFileColIdx]));
                                                }
                                            }

                                            // Check if point is part of cluster
                                            if (isClusterChecked && clusterDictionary.TryGetValue(ipfPoint, out cluster))
                                            {
                                                try
                                                {
                                                    IPFPoint clusterPoint = cluster.ToPoint(ipfPoint.IPFFile, ipfPoint, ipfDischargeColIdx);
                                                    clusterX = (float)clusterPoint.X;
                                                    clusterY = (float)clusterPoint.Y;

                                                    if (clusterPoint.HasAssociatedFile())
                                                    {
                                                        timeseries = clusterPoint.Timeseries;

                                                        // In this case actually calculate the real clusterstatistics that can be different from the individual well statistics
                                                        modelperiodTimeseries = timeseries.InterpolateTimeseries(modelStartDate, modelEndDate);
                                                        Sweco.SIF.iMOD.Timeseries clusterDischargeperiodTimeseries = modelperiodTimeseries.Select(float.NaN, 0.001f);
                                                        avgDischargePeriod = (clusterDischargeperiodTimeseries.Timestamps.Count() > 0) ? clusterDischargeperiodTimeseries.Timestamps[clusterDischargeperiodTimeseries.Timestamps.Count() - 1].Subtract(clusterDischargeperiodTimeseries.Timestamps[0]).Days : 0;
                                                        Statistics.Statistics dischargeStats = new Statistics.Statistics(modelperiodTimeseries.Values);
                                                        dischargeStats.ComputeBasicStatistics(true, true, false);
                                                        avgDischargeValue = dischargeStats.Mean;
                                                        minDischargeValue = dischargeStats.Min;
                                                        maxDischargeValue = dischargeStats.Max;
                                                        if (avgDischargeValue.Equals(float.NaN))
                                                        {
                                                            avgDischargeValue = 0;
                                                            minDischargeValue = 0;
                                                            maxDischargeValue = 0;
                                                        }

                                                        // Only check cluster timeseries for first point of cluster (to avoid double results)
                                                        if (!cluster.Points[0].Equals(ipfPoint))
                                                        {
                                                            timeseries = null;
                                                        }
                                                    }
                                                }
                                                catch (Exception)
                                                {
                                                    // Timeseriesfiles cannot be read correctly
                                                    resultHandler.AddCheckResult(welErrorLayer, x, y, MissingTimeseriesError);
                                                    resultHandler.AddCheckDetail(welDetailLayer, x, y, new CheckDetail(MissingTimeseriesError, welIPFFile,
                                                        ipfPoint.ColumnValues[ipfPoint.IPFFile.AssociatedFileColIdx]));
                                                }
                                            }

                                            //////////////////////////////////////////////////////////////////////
                                            // Check if average discharge is within range to check well/cluster //
                                            //////////////////////////////////////////////////////////////////////
                                            if (!(avgDischargeValue < minDischargeWellChecked) && !(avgDischargeValue > maxDischargeWellChecked))
                                            {
                                                checkedWellCount++;

                                                // Retrieve check settings for current location
                                                float minDischarge = (minDischargeSettingIDFFile != null) ? minDischargeSettingIDFFile.GetNaNBasedValue(x, y) : float.NaN;
                                                float maxDischarge = (maxDischargeSettingIDFFile != null) ? maxDischargeSettingIDFFile.GetNaNBasedValue(x, y) : float.NaN;
                                                float minKDValue = (minKDSettingIDFFile != null) ? minKDSettingIDFFile.GetNaNBasedValue(x, y) : float.NaN;
                                                float minCValue = (minCSettingIDFFile != null) ? minCSettingIDFFile.GetNaNBasedValue(x, y) : float.NaN;
                                                float minKHValue = (minKHSettingIDFFile != null) ? minKHSettingIDFFile.GetNaNBasedValue(x, y) : float.NaN;
                                                float minKDQRatio = (minKDQRatioSettingIDFFile != null) ? minKDQRatioSettingIDFFile.GetNaNBasedValue(x, y) : float.NaN;
                                                float cityMaxDischarge = (cityMaxDischargeIDFFile != null) ? cityMaxDischargeIDFFile.GetNaNBasedValue(x, y) : float.NaN;

                                                // Retrieve kD/kH/Top/Bot-values for current location
                                                float upperTopValue = (upperTopIDFFile != null) ? upperTopIDFFile.GetNaNBasedValue(x, y) : float.NaN;
                                                float upperBotValue = (upperBotIDFFile != null) ? upperBotIDFFile.GetNaNBasedValue(x, y) : float.NaN;
                                                float topValue = (topIDFFile != null) ? topIDFFile.GetNaNBasedValue(x, y) : float.NaN;
                                                float botValue = (botIDFFile != null) ? botIDFFile.GetNaNBasedValue(x, y) : float.NaN;
                                                float lowerTopValue = (lowerTopIDFFile != null) ? lowerTopIDFFile.GetNaNBasedValue(x, y) : float.NaN;
                                                float lowerBotValue = (lowerBotIDFFile != null) ? lowerBotIDFFile.GetNaNBasedValue(x, y) : float.NaN;
                                                float upperKDValue = (upperkDIDFFile != null) ? upperkDIDFFile.GetNaNBasedValue(x, y) : float.NaN;
                                                float upperKHValue = (upperKHIDFFile != null) ? upperKHIDFFile.GetNaNBasedValue(x, y) : float.NaN;
                                                float kdValue = (kdIDFFile != null) ? kdIDFFile.GetNaNBasedValue(x, y) : float.NaN;
                                                float khValue = (khIDFFile != null) ? khIDFFile.GetNaNBasedValue(x, y) : float.NaN;
                                                float lowerKDValue = (lowerKDIDFFile != null) ? lowerKDIDFFile.GetNaNBasedValue(x, y) : float.NaN;
                                                float lowerKHValue = (lowerKHIDFFile != null) ? lowerKHIDFFile.GetNaNBasedValue(x, y) : float.NaN;
                                                float upperCValue = (upperCIDFFile != null) ? upperCIDFFile.GetNaNBasedValue(x, y) : 0;
                                                float upperKVValue = (upperKVIDFFile != null) ? upperKVIDFFile.GetNaNBasedValue(x, y) : 0;
                                                float cValue = (cIDFFile != null) ? cIDFFile.GetNaNBasedValue(x, y) : float.NaN;
                                                float kvValue = (kvIDFFile != null) ? kvIDFFile.GetNaNBasedValue(x, y) : float.NaN;
                                                float topL1Value = (topL1IDFFile != null) ? topL1IDFFile.GetNaNBasedValue(x, y) : float.NaN;
                                                float botLNValue = (botLNIDFFile != null) ? botLNIDFFile.GetNaNBasedValue(x, y) : float.NaN;
                                                // float totalKDValue = RetrieveTotalKDValue(x, y, ilay, kdwPackage, khvPackage, vcwPackage, topPackage, botPackage);

                                                // If no kD-values are defined, calculate kD-values from kh-value. And vice versa.
                                                if (upperKDValue.Equals(float.NaN) && !upperKHValue.Equals(float.NaN))
                                                {
                                                    upperKDValue = (!(upperTopValue.Equals(float.NaN) || upperBotValue.Equals(float.NaN))) ? upperKHValue * (upperTopValue - upperBotValue) : 0;
                                                }
                                                if (kdValue.Equals(float.NaN) && !khValue.Equals(float.NaN))
                                                {
                                                    kdValue = (!(topValue.Equals(float.NaN) || botValue.Equals(float.NaN))) ? khValue * (topValue - botValue) : 0;
                                                    if (calcKDIDFFile == null)
                                                    {
                                                        calcKDIDFFile = khIDFFile.CopyIDF(Path.Combine(Path.Combine(GetIMODFilesPath(model), this.Name), "KDcalc_L" + ilay.ToString() + ".IDF"));
                                                        calcKDIDFFile.ResetValues();
                                                    }
                                                    calcKDIDFFile.SetValue(x, y, kdValue);
                                                }
                                                else if (khValue.Equals(float.NaN) && !kdValue.Equals(float.NaN))
                                                {
                                                    khValue = ((!(topValue.Equals(float.NaN) || botValue.Equals(float.NaN))) && ((topValue - botValue) > 0)) ? (kdValue / (topValue - botValue)) : 0;
                                                }
                                                if (lowerKDValue.Equals(float.NaN) && !lowerKHValue.Equals(float.NaN))
                                                {
                                                    lowerKDValue = (!(lowerTopValue.Equals(float.NaN) || lowerBotValue.Equals(float.NaN))) ? lowerKHValue * (lowerTopValue - lowerBotValue) : 0;
                                                }

                                                // If no C-values are defined, calculate C-values from kv-value. And vice versa.
                                                if (upperCValue.Equals(float.NaN) && !upperKVValue.Equals(float.NaN))
                                                {
                                                    upperCValue = ((!(upperBotValue.Equals(float.NaN) || topValue.Equals(float.NaN))) && (upperKVValue > 0)) ? (upperBotValue - topValue) / upperKVValue : 0;
                                                }
                                                if (cValue.Equals(float.NaN) && !kvValue.Equals(float.NaN))
                                                {
                                                    cValue = ((!(botValue.Equals(float.NaN) || lowerTopValue.Equals(float.NaN))) && (kvValue > 0)) ? (botValue - lowerTopValue) / kvValue : 0;
                                                    if (calcCIDFFile == null)
                                                    {
                                                        calcCIDFFile = kvIDFFile.CopyIDF(Path.Combine(Path.Combine(GetIMODFilesPath(model), this.Name), "Ccalc_L" + ilay.ToString() + ".IDF"));
                                                        calcCIDFFile.ResetValues();
                                                    }
                                                    calcCIDFFile.SetValue(x, y, cValue);
                                                }

                                                // Retrieve filter-characteristics
                                                bool isFilterLengthDefined = false;
                                                float filterlength = float.NaN;
                                                float welIPFFraction = ((ipfFractionColIdx >= 0) && (ipfPoint.HasFloatValue(ipfFractionColIdx))) ? ipfPoint.GetFloatValue(ipfFractionColIdx) : float.NaN;
                                                float z1 = (ipfPoint.HasFloatValue(ipfZ1ColIdx)) ? ipfPoint.GetFloatValue(ipfZ1ColIdx) : float.NaN;   // (optional) top of well filter
                                                float z2 = (ipfPoint.HasFloatValue(ipfZ2ColIdx)) ? ipfPoint.GetFloatValue(ipfZ2ColIdx) : float.NaN;   // (optional) bottom of well filter
                                                if (z1.Equals(noDataFilterLevelValue))
                                                {
                                                    z1 = z2;
                                                }
                                                if (z2.Equals(noDataFilterLevelValue))
                                                {
                                                    z2 = z1;
                                                    if (z1.Equals(noDataFilterLevelValue))
                                                    {
                                                        z1 = float.NaN;
                                                        z2 = float.NaN;
                                                    }
                                                }

                                                //////////////////////
                                                // Do actual checks //
                                                //////////////////////

                                                ///////////////////////////////////////////////////////
                                                // 2. Check filter position in aquifer               //
                                                ///////////////////////////////////////////////////////

                                                // Check that either z1, z2 or both are defined
                                                if (z1.Equals(float.NaN) && z1.Equals(float.NaN))
                                                {
                                                    resultHandler.AddCheckResult(welErrorLayer, x, y, InvalidFilterLevelError);
                                                    resultHandler.AddCheckDetail(welDetailLayer, x, y, new CheckDetail(InvalidFilterLevelError, welIPFFile,
                                                        "Invalid filter top-levels z1 (" + z1.ToString(EnglishCultureInfo) + ") and z2 (" +  z2.ToString(englishCultureInfo) + ")"));
                                                }
                                                else if (z1.Equals(float.NaN))
                                                {
                                                    resultHandler.AddCheckResult(welErrorLayer, x, y, InvalidFilterLevelError);
                                                    resultHandler.AddCheckDetail(welDetailLayer, x, y, new CheckDetail(InvalidFilterLevelError, welIPFFile,
                                                        "Invalid filter top-level z1:" + z1.ToString(EnglishCultureInfo)));
                                                }
                                                else if (z2.Equals(float.NaN))
                                                {
                                                    resultHandler.AddCheckResult(welErrorLayer, x, y, InvalidFilterLevelError);
                                                    resultHandler.AddCheckDetail(welDetailLayer, x, y, new CheckDetail(InvalidFilterLevelError, welIPFFile,
                                                        "Invalid filter bottom-level z2:" + z2.ToString(EnglishCultureInfo)));
                                                }
                                                else if (z1 < z2) // filter top-level is below bot-level 
                                                {
                                                    resultHandler.AddCheckResult(welErrorLayer, x, y, NegativeFilterLengthError);
                                                    resultHandler.AddCheckDetail(welDetailLayer, x, y, new CheckDetail(NegativeFilterLengthError, welIPFFile,
                                                        "Filter bottom (z2) is above top (z1): " + z2.ToString(englishCultureInfo) + ">" + z1.ToString(englishCultureInfo)));
                                                }
                                                else if (z1.Equals(z2))
                                                {
                                                    resultHandler.AddCheckResult(welWarningLayer, x, y, FilterLevelsEqualWarning);
                                                    resultHandler.AddCheckDetail(welDetailLayer, x, y, new CheckDetail(FilterLevelsEqualWarning, welIPFFile,
                                                        "Filter bottom (z2) equal to top (z1): " + z2.ToString(englishCultureInfo) + "=" + z1.ToString(englishCultureInfo)));
                                                }
                                                else
                                                {
                                                    // Filterlevels are defined (z1 and z2 columns are present in IPF-file) and filter length is positive (> 0)
                                                    filterlength = z1 - z2;
                                                    isFilterLengthDefined = (filterlength > 0.1f);
                                                    if ((filterlength < minFilterLength) || (filterlength > maxFilterLength))
                                                    {
                                                        resultHandler.AddCheckResult(welWarningLayer, x, y, FilterLengthRangeWarning);
                                                        resultHandler.AddCheckDetail(welDetailLayer, x, y, new CheckDetail(FilterLengthRangeWarning, welIPFFile,
                                                            "Filter length (" + filterlength.ToString(englishCultureInfo) + ") outside defined range [" + minFilterLength + "," + maxFilterLength + "]"));
                                                    }

                                                    // Check that top and bottom values of aquifer are defined, if ilay=0, top/bot value are NaN and checks are skipped
                                                    if (!topValue.Equals(float.NaN) && !botValue.Equals(float.NaN))
                                                    {
                                                        // Determine filter position relative to modellayer
                                                        if ((z1 > upperBotValue) && (z2 < lowerTopValue))
                                                        {
                                                            // Filter is above AND below this modellayer, so filter position is correct
                                                        }
                                                        else
                                                        {
                                                            // filter does not fully occupy modellayer

                                                            if (isFilterInAquitardChecked && (z1 <= botValue) && (z2 >= lowerTopValue))
                                                            {
                                                                // Filter is completely in aquitard of this modellayer
                                                                resultHandler.AddCheckResult(welWarningLayer, x, y, FilterInAquitardWarning);
                                                                CheckDetail checkDetail = new CheckDetail(FilterInAquitardWarning, welIPFFile,
                                                                    "(z1 <= BOT) and (z2 >= lowerTOP)",
                                                                    "z1=" + z1.ToString("F2", SIFTool.EnglishCultureInfo) + ", z2=" + z2.ToString("F2", SIFTool.EnglishCultureInfo) + ", BOT=" + botValue.ToString("F2", SIFTool.EnglishCultureInfo) + ", lowerTOP=" + lowerTopValue.ToString("F2", SIFTool.EnglishCultureInfo));
                                                                checkDetail.AddFilename(GetAssociatedFilename(ipfPoint));
                                                                resultHandler.AddCheckDetail(welDetailLayer, x, y, checkDetail);
                                                            }
                                                            else if (z2 > topValue)
                                                            {
                                                                // Filter is completely above this modellayer
                                                                ReportFilterNotInLayerError(resultHandler, welErrorLayer, welDetailLayer, welIPFFile, ipfPoint, FilterNotInLayerError,
                                                                    z1, z2, topValue, botValue, upperBotValue, lowerTopValue, isFilterInAquitardChecked, isFilterNotInLayerKDChecked,
                                                                    upperKDValue, kdValue, lowerKDValue, minKDValue,
                                                                    "z2 > TOP", "z2=" + z2.ToString("F2", SIFTool.EnglishCultureInfo) + ", TOP=" + topValue.ToString("F2", SIFTool.EnglishCultureInfo));
                                                            }
                                                            else if (z1 < lowerTopValue)
                                                            {
                                                                // Filter is completely below lower aquitard
                                                                ReportFilterNotInLayerError(resultHandler, welErrorLayer, welDetailLayer, welIPFFile, ipfPoint, FilterNotInLayerError,
                                                                    z1, z2, topValue, botValue, upperBotValue, lowerTopValue, isFilterInAquitardChecked, isFilterNotInLayerKDChecked,
                                                                    upperKDValue, kdValue, lowerKDValue, minKDValue,
                                                                    "z1 < lowerTOP", "z1=" + z1.ToString("F2", SIFTool.EnglishCultureInfo) + ", lowerTOP=" + lowerTopValue.ToString("F2", SIFTool.EnglishCultureInfo));
                                                            }
                                                            else if (isFilterLengthDefined)
                                                            {
                                                                // Determine fraction of length (include part in aquitard below aquifer in same modellayer)
                                                                float aquiferFilterTop = (z1 > topValue) ? topValue : z1;
                                                                float aquiferFilterBot = (z2 < botValue) ? botValue : z2;           // Only include part in aquifer in same modellayer
                                                                float layerFilterBot = (z2 < lowerTopValue) ? lowerTopValue : z2;   // Also include part in aquitard below aquifer in same modellayer
                                                                float aquiferFilterLength = (aquiferFilterTop - aquiferFilterBot) > 0 ? (aquiferFilterTop - aquiferFilterBot) : 0;
                                                                float layerFilterLength = (aquiferFilterTop - layerFilterBot) > 0 ? (aquiferFilterTop - layerFilterBot) : 0;
                                                                float aquiferFilterLengthFraction = (filterlength.Equals(0f)) ? 0 : aquiferFilterLength / filterlength;
                                                                float layerFilterLengthFraction = (filterlength.Equals(0f)) ? 0 : layerFilterLength / filterlength;
                                                                float layerThickness = topValue - lowerTopValue;
                                                                float layerThicknessFraction = (layerThickness > 0) ? (layerFilterLength / layerThickness) : float.NaN;

                                                                //if ((layerThicknessFraction < minLayerThicknessFraction) && (aquiferFilterLengthFraction < 0.5f))
                                                                //{
                                                                //    // Fraction of filter in modellayer is less than specified minimum modellayer thickness fraction (aquifer plus aquitard)
                                                                //    // Only report this error if there is less than half of the filter in the aquifer of this modellayer
                                                                //    // Note: these checks come from an older iMOD-version and the way that WEL-filter were assigned to modellayers in MIPWA
                                                                //    ReportFilterNotInLayerError(resultHandler, welErrorLayer, welDetailLayer, welIPFFile, x, y, FilterNotInLayerError,
                                                                //        z1, z2, topValue, botValue, upperBotValue, lowerTopValue, isFilterInAquitardChecked, isFilterNotInLayerKDChecked,
                                                                //        upperKDValue, kdValue, lowerKDValue, minKDValue,
                                                                //        "(layThFr < minLayThFr) and (aqFiltLenFr < 0.5)",
                                                                //        "layThFr=" + layerThicknessFraction.ToString("F2", SIFTool.EnglishCultureInfo) + ", minLayThFr=" + minLayerThicknessFraction.ToString("F2", SIFTool.EnglishCultureInfo) + ", aqFiltLenFr=" + aquiferFilterLengthFraction.ToString("F2", SIFTool.EnglishCultureInfo));
                                                                //}
                                                                //else if (welIPFFraction > 0.5)
                                                                //{
                                                                //    // For now only report if a large fraction of the filter is assigned to this layer
                                                                //    if ((layerFilterLengthFraction < minLayerFilterLengthFraction) && (layerFilterLength < 0.5f))
                                                                //    {
                                                                //        // Fraction of filterlength in this modellayer (aquifer plus aquitard) is too small 
                                                                //        ReportFilterNotInLayerError(resultHandler, welErrorLayer, welDetailLayer, welIPFFile, x, y, FilterNotInLayerError,
                                                                //            z1, z2, topValue, botValue, upperBotValue, lowerTopValue, isFilterInAquitardChecked, isFilterNotInLayerKDChecked,
                                                                //            upperKDValue, kdValue, lowerKDValue, minKDValue,
                                                                //            "(layFltLenFr < minLayFltLenFr) and (layFltLenFr < 0.5)",
                                                                //            "layFltLenFr=" + layerFilterLengthFraction.ToString("F2", SIFTool.EnglishCultureInfo) + ", minLayFltLenFr=" + minLayerFilterLengthFraction.ToString("F2", SIFTool.EnglishCultureInfo) + ", layFltLenFr=" + layerFilterLengthFraction.ToString("F2", SIFTool.EnglishCultureInfo));
                                                                //    }
                                                                //    else if ((aquiferFilterLengthFraction < minAquiferFilterLengthFraction) && (aquiferFilterLength < 0.5f))
                                                                //    {
                                                                //        // Fraction of filterlength in this aquifer is small/none, but a relatively large part is in the aquitard above or below. 

                                                                //        // Check that the remaining filterpart in next aquifer (above or below) is not even smaller
                                                                //        if (z1 > upperBotValue)
                                                                //        {
                                                                //            // remaining part of filter is in aquifer above or higher, check if it's more than in this aquifer
                                                                //            float remainingPartFraction = (z1 - upperBotValue) / filterlength;
                                                                //            if ((remainingPartFraction > aquiferFilterLengthFraction) && (kdValue < upperKDValue))
                                                                //            {
                                                                //                // Avoid reporting an error if there is no aquitard above with thickness > 0
                                                                //                //                                                                                if (!((upperBotValue - topValue) <= 0))
                                                                //                {
                                                                //                    resultHandler.AddCheckResult(welErrorLayer, x, y, FilterNotInLayerError);
                                                                //                    resultHandler.AddCheckDetail(welDetailLayer, x, y, new CheckDetail(FilterNotInLayerError, welIPFFile,
                                                                //                        "(partFr > aqFiltLenFr) and (KD < upperKD)",
                                                                //                        "partFr=" + remainingPartFraction.ToString("F2", SIFTool.EnglishCultureInfo) + ", aqFiltLenFr=" + aquiferFilterLengthFraction.ToString("F2", SIFTool.EnglishCultureInfo) + ", KD=" + kdValue.ToString("F2", SIFTool.EnglishCultureInfo) + ", upperKD=" + upperKDValue.ToString("F2", SIFTool.EnglishCultureInfo)));
                                                                //                }
                                                                //            }
                                                                //        }
                                                                //        else if (z2 < lowerTopValue)
                                                                //        {
                                                                //            // remaining part of filter is in aquifer below or lower, check if it's more than in this aquifer
                                                                //            float remainingPartFraction = (lowerTopValue - z2) / filterlength;
                                                                //            if ((remainingPartFraction > aquiferFilterLengthFraction) && (kdValue < lowerKDValue))
                                                                //            {
                                                                //                // Avoid reporting an error if there is no aquitard below with thickness > 0
                                                                //                //   if (!((botValue - lowerTopValue) <= 0))
                                                                //                {
                                                                //                    resultHandler.AddCheckResult(welErrorLayer, x, y, FilterNotInLayerError);
                                                                //                    resultHandler.AddCheckDetail(welDetailLayer, (float)ipfPoint.X, (float)ipfPoint.Y, new CheckDetail(FilterNotInLayerError, welIPFFile,
                                                                //                        "RemainingFiltFr>aqFiltLenFr AND kDi<KDi+1", remainingPartFraction.ToString("F3", SIFTool.EnglishCultureInfo) + "<" + aquiferFilterLengthFraction.ToString("F3", SIFTool.EnglishCultureInfo) + "kDi=" + kdValue.ToString("F1", SIFTool.EnglishCultureInfo) + ", kDi+1=" + lowerKDValue.ToString("F1", SIFTool.EnglishCultureInfo)));
                                                                //                }
                                                                //            }
                                                                //        }
                                                                //    }
                                                                    //// If filter is (partly) above surfacelevel give warning
                                                                    //else if (upperBotValue.Equals(float.NaN) && ((z1 - topValue) > (0.5 * filterlength)))
                                                                    //{
                                                                    //    resultHandler.AddCheckResult(welWarningLayer, x, y, FilterAboveSurfaceLevelWarning);
                                                                    //    resultHandler.AddCheckDetail(welDetailLayer, (float)ipfPoint.X, (float)ipfPoint.Y, new CheckDetail(FilterAboveSurfaceLevelWarning, welIPFFile,
                                                                    //        "(z1-TOP)>0.5*filterlength", "z1=" + z1.ToString("F2", SIFTool.EnglishCultureInfo) + ", TOP=" + topValue.ToString("F2", SIFTool.EnglishCultureInfo) + ", filterlength=" + filterlength.ToString("F2", SIFTool.EnglishCultureInfo)));
                                                                    //}
                                                                    //else if (!welIPFFraction.Equals(float.NaN))
                                                                    //{
                                                                    //    // check defined fraction
                                                                    //    if (Math.Abs(filterLengthFraction - welIPFFraction) > 0.3)
                                                                    //    {
                                                                    //        resultHandler.AddCheckResult(welWarningLayer, x, y, FilterFractionWarning);
                                                                    //    }
                                                                    //}
                                                                //}
                                                                // When filter is more than half of the filter length above surfacelevel give warning
                                                                if (upperBotValue.Equals(float.NaN) && ((z1 - topValue) > (0.5 * filterlength)))
                                                                {
                                                                    resultHandler.AddCheckResult(welWarningLayer, x, y, FilterAboveSurfaceLevelWarning);
                                                                    resultHandler.AddCheckDetail(welDetailLayer, (float)ipfPoint.X, (float)ipfPoint.Y, new CheckDetail(FilterAboveSurfaceLevelWarning, welIPFFile,
                                                                        "(z1-TOP)>0.5*filterlength", "z1=" + z1.ToString("F2", SIFTool.EnglishCultureInfo) + ", TOP=" + topValue.ToString("F2", SIFTool.EnglishCultureInfo) + ", filterlength=" + filterlength.ToString("F2", SIFTool.EnglishCultureInfo)));
                                                                }
                                                            }
                                                        }
                                                    }
                                                    else if (!topL1Value.Equals(float.NaN) && (z2 > topL1Value))
                                                    {
                                                        // Filter is completely above model highest TOP-layer
                                                        ReportFilterNotInLayerError(resultHandler, welErrorLayer, welDetailLayer, welIPFFile, ipfPoint, FilterNotInLayerError,
                                                            z1, z2, topValue, botValue, upperBotValue, lowerTopValue, isFilterInAquitardChecked, isFilterNotInLayerKDChecked,
                                                            upperKDValue, kdValue, lowerKDValue, minKDValue,
                                                            "z2 > TOP_L1", "z2=" + z2.ToString("F2", SIFTool.EnglishCultureInfo) + ", TOP_L1=" + topL1Value.ToString("F2", SIFTool.EnglishCultureInfo));
                                                    }
                                                    else if (!botLNValue.Equals(float.NaN) && (z1 < botLNValue) && settings.IsFilterInAquitardChecked)
                                                    {
                                                        // Filter is completely below model lowest BOT-layer
                                                        ReportFilterNotInLayerError(resultHandler, welErrorLayer, welDetailLayer, welIPFFile, ipfPoint, FilterNotInLayerError,
                                                            z1, z2, topValue, botValue, upperBotValue, lowerTopValue, isFilterInAquitardChecked, isFilterNotInLayerKDChecked,
                                                            upperKDValue, kdValue, lowerKDValue, minKDValue,
                                                            "z1 < BOT_" + model.NLAY, "z1=" + z1.ToString("F2", SIFTool.EnglishCultureInfo) + ", BOT_L" + model.NLAY + "=" + botLNValue.ToString("F2", SIFTool.EnglishCultureInfo));
                                                    }
                                                }

                                                ///////////////////////////////////////////////////////
                                                // 3. check if filter is in too small aquifer        //
                                                ///////////////////////////////////////////////////////
                                                if (!kdValue.Equals(float.NaN))
                                                {
                                                    // Check if aquifer is enclosed by aquitards that cannot be ignored (C > 50)
                                                    if (!((upperCValue < minCValue) || (cValue < minCValue)))
                                                    {
                                                        if (!wellAvgDischargeValue.Equals(0f))
                                                        {
                                                            float kDqRatio = -1.0f * (kdValue / wellAvgDischargeValue);
                                                            //                                                        kDqRatioStats.AddValue(kDqRatio);

                                                            if (!minKDQRatio.Equals(float.NaN) && (kDqRatio < minKDQRatio))
                                                            {

                                                                resultHandler.AddCheckResult(welWarningLayer, x, y, SmallAquiferWarning);
                                                                resultHandler.AddCheckDetail(welDetailLayer, (float)ipfPoint.X, (float)ipfPoint.Y, new CheckDetail(SmallAquiferWarning, welIPFFile,
                                                                    "kDq-ratio " + kDqRatio.ToString("F2", SIFTool.EnglishCultureInfo) + " < " + minKDQRatio, "kD=" + kdValue.ToString("F1", SIFTool.EnglishCultureInfo) + ", q_avg=" + wellAvgDischargeValue));
                                                            }
                                                            else if (!minKHValue.Equals(float.NaN) && (khValue < minKHValue))
                                                            {
                                                                resultHandler.AddCheckResult(welWarningLayer, x, y, SmallAquiferWarning);
                                                                welWarningLayer.AddSourceFile(khIDFFile);
                                                                resultHandler.AddCheckDetail(welDetailLayer, (float)ipfPoint.X, (float)ipfPoint.Y, new CheckDetail(SmallAquiferWarning, welIPFFile,
                                                                    "kh " + khValue.ToString("F5") + " < " + minKHValue, null));
                                                            }
                                                            else if (!minKDValue.Equals(float.NaN) && (kdValue < minKDValue))
                                                            {
                                                                resultHandler.AddCheckResult(welWarningLayer, x, y, SmallAquiferWarning);
                                                                resultHandler.AddCheckDetail(welDetailLayer, (float)ipfPoint.X, (float)ipfPoint.Y, new CheckDetail(SmallAquiferWarning, welIPFFile,
                                                                    "kD " + kdValue.ToString("F2", SIFTool.EnglishCultureInfo) + " < " + minKDValue, null));
                                                            }
                                                        }
                                                    }
                                                }

                                                //////////////////////////////////////
                                                // 4. Check Absolute extreme values //
                                                //////////////////////////////////////
                                                if ((wellAvgDischargeValue < minDischarge) || (wellAvgDischargeValue > maxDischarge))
                                                {
                                                    resultHandler.AddCheckResult(welWarningLayer, x, y, DischargeRangeWarning);
                                                    resultHandler.AddCheckDetail(welDetailLayer, (float)ipfPoint.X, (float)ipfPoint.Y, new CheckDetail(DischargeRangeWarning, welIPFFile,
                                                        "q_avg<q_minavg OR q_avg>q_maxavg", "q_avg=" + wellAvgDischargeValue.ToString("F2", SIFTool.EnglishCultureInfo) + ", q_min" + minDischarge.ToString("F2", SIFTool.EnglishCultureInfo) + ", q_max" + maxDischarge.ToString("F2", SIFTool.EnglishCultureInfo)));
                                                }

                                                /////////////////////////////////////////////////////
                                                // 5. Check Individual timeseries: invalid dates   //
                                                /////////////////////////////////////////////////////
                                                if (wellTimeseries != null)
                                                {
                                                    if (wellTimeseries.RetrieveNoDataCount() > 0)
                                                    {
                                                        resultHandler.AddCheckResult(welErrorLayer, x, y, InvalidTimeseriesDateError);
                                                        resultHandler.AddCheckDetail(welDetailLayer, (float)ipfPoint.X, (float)ipfPoint.Y, new CheckDetail(InvalidTimeseriesFileError, welIPFFile,
                                                            wellTimeseries.NoDataValues.Count() + " invalid dates in timeseriesfile", "see " + wellTimeseries.NoDataValues[0],
                                                            ipfPoint.ColumnValues[welIPFFile.AssociatedFileColIdx]));
                                                    }
                                                    wellTimeseries = null;
                                                }

                                                /////////////////////////////////////////////////////////////
                                                // 6. Check Individual/Cluster timeseries: Outliers        //
                                                /////////////////////////////////////////////////////////////

                                                // Do checks that apply to the clustertimeseries if the well is part of a cluster, 
                                                // otherwise the clustertimeseries==ipftimeseries and the individual well is checked
                                                if (timeseries != null)
                                                {
                                                    if (settings.IsOutlierChecked)
                                                    {
                                                        // Compute outlier statistics for total IPF-timeseries (which might be from a cluster), excluding zeroes
                                                        Statistics.Statistics clusterTimeseriesDischargeStats = new Statistics.Statistics(timeseries.Values);
                                                        clusterTimeseriesDischargeStats.AddSkippedValue(0);
                                                        clusterTimeseriesDischargeStats.ComputeOutlierStatistics(settings.OutlierMethod, settings.OutlierMethodBaseRange, settings.OutlierMethodMultiplier, true, false);
                                                        float outlierRangeLowerValue = clusterTimeseriesDischargeStats.OutlierRangeLowerValue;
                                                        float outlierRangeUpperValue = clusterTimeseriesDischargeStats.OutlierRangeUpperValue;

                                                        // Apply correction for wells (in case of a small IQR around relative extreme Q1/Q3 values)
                                                        outlierRangeLowerValue = Math.Min(outlierRangeLowerValue, clusterTimeseriesDischargeStats.Q1 * ((clusterTimeseriesDischargeStats.Q1 >= 0) ? 0.5f : 2f));
                                                        outlierRangeUpperValue = Math.Max(outlierRangeUpperValue, clusterTimeseriesDischargeStats.Q3 * ((clusterTimeseriesDischargeStats.Q3 >= 0) ? 2.0f : 0.5f));

                                                        // Check for outliers and/or invalid values, but avoid outliers close to zero since these can be caused by turning off pumps slowly
                                                        if ((avgDischargeValue < 0) && (minDischargeValue < outlierRangeLowerValue))
                                                        {
                                                            if (resultHandler.Extent.Contains(clusterX, clusterY))
                                                            {
                                                                resultHandler.AddCheckResult(welWarningLayer, clusterX, clusterY, DischargeOutlierWarning);
                                                            }
                                                            string tsFilename = (cluster != null) ? FileUtils.AddFilePostFix(ipfPoint.ColumnValues[welIPFFile.AssociatedFileColIdx], "_cluster", 50) : ipfPoint.ColumnValues[welIPFFile.AssociatedFileColIdx];
                                                            resultHandler.AddCheckDetail(welDetailLayer, clusterX, clusterY, new CheckDetail(DischargeOutlierWarning, welIPFFile,
                                                                "Min discharge (" + minDischargeValue.ToString("F1", englishCultureInfo) + ") < outlierrange (" + outlierRangeLowerValue.ToString("F1", englishCultureInfo) + "); Assoc.File: " + Path.GetFileName(ipfPoint.ColumnValues[ipfPoint.IPFFile.AssociatedFileColIdx]) + "." + ipfPoint.IPFFile.AssociatedFileExtension, null,
                                                                tsFilename, timeseries));
                                                        }
                                                        else if ((avgDischargeValue > 0) && (maxDischargeValue > outlierRangeUpperValue))
                                                        {
                                                            if (resultHandler.Extent.Contains(clusterX, clusterY))
                                                            {
                                                                resultHandler.AddCheckResult(welWarningLayer, clusterX, clusterY, DischargeOutlierWarning);
                                                            }
                                                            string tsFilename = (cluster != null) ? FileUtils.AddFilePostFix(ipfPoint.ColumnValues[welIPFFile.AssociatedFileColIdx], "_cluster", 50) : ipfPoint.ColumnValues[welIPFFile.AssociatedFileColIdx];
                                                            resultHandler.AddCheckDetail(welDetailLayer, clusterX, clusterY, new CheckDetail(DischargeOutlierWarning, welIPFFile,
                                                                "Max discharge (" + maxDischargeValue.ToString("F1", englishCultureInfo) + ") > outlierrange (" + outlierRangeUpperValue.ToString("F1", englishCultureInfo) + ")", null,
                                                                tsFilename, timeseries));
                                                        }
                                                    }

                                                    // Check min/max discharge in modelperiod for exceeding defined min/max average discharge.
                                                    float minmaxAbsMax = Math.Max(Math.Abs(minDischargeValue), Math.Abs(maxDischargeValue));
                                                    float minmaxMargin = minmaxAbsMax * (tsMinMaxPercentage / 100.0f);
                                                    if (minDischargeValue < (minDischarge - minmaxMargin))
                                                    {
                                                        if (resultHandler.Extent.Contains(clusterX, clusterY))
                                                        {
                                                            resultHandler.AddCheckResult(welWarningLayer, clusterX, clusterY, DischargeRangeWarning);
                                                        }
                                                        string tsFilename = (cluster != null) ? FileUtils.AddFilePostFix(ipfPoint.ColumnValues[welIPFFile.AssociatedFileColIdx], "_cluster", 50) : ipfPoint.ColumnValues[welIPFFile.AssociatedFileColIdx];
                                                        resultHandler.AddCheckDetail(welDetailLayer, clusterX, clusterY, new CheckDetail(DischargeRangeWarning, welIPFFile,
                                                            "Extreme min. discharge in timeseries (" + minDischargeValue.ToString("F1", englishCultureInfo) + ")", null,
                                                            tsFilename, timeseries));
                                                    }
                                                    else if (maxDischargeValue > (maxDischarge + minmaxMargin))
                                                    {
                                                        if (resultHandler.Extent.Contains(clusterX, clusterY))
                                                        {
                                                            resultHandler.AddCheckResult(welWarningLayer, clusterX, clusterY, DischargeRangeWarning);
                                                        }
                                                        resultHandler.AddCheckDetail(welDetailLayer, clusterX, clusterY, new CheckDetail(DischargeRangeWarning, welIPFFile,
                                                            "Extreme max. discharge in timeseries (" + maxDischargeValue.ToString("F1", englishCultureInfo) + ")", null,
                                                            FileUtils.AddFilePostFix(ipfPoint.ColumnValues[welIPFFile.AssociatedFileColIdx], "_details", 50), timeseries));
                                                    }

                                                    if (settings.IsOutlierChecked)
                                                    {
                                                        // Only check for outliers in change of discharge if the measurement frequency timeseries is high enough (here <= montly)
                                                        if (modelperiodTimeseries.GetMaxFrequency() <= 31)
                                                        {
                                                            //check for outliers in change of discharge 
                                                            modelperiodTimeseries.Remove(0f);

                                                            Sweco.SIF.iMOD.Timeseries changeTimeseries = modelperiodTimeseries.Select(modelStartDate, modelEndDate, -1);
                                                            changeTimeseries.Remove(0f);
                                                            changeTimeseries.Abs();
                                                            Statistics.Statistics changeStats = new Statistics.Statistics(changeTimeseries.Values);
                                                            changeStats.ComputeOutlierStatistics(settings.OutlierMethod, settings.OutlierMethodBaseRange, settings.ChangeOutlierMethodMultiplier, true, false);
                                                            if (changeStats.Max > changeStats.OutlierRangeUpperValue)
                                                            {
                                                                if (resultHandler.Extent.Contains(clusterX, clusterY))
                                                                {
                                                                    resultHandler.AddCheckResult(welWarningLayer, clusterX, clusterY, DischargeChangeWarning);
                                                                }
                                                                string tsFilename = (cluster != null) ? FileUtils.AddFilePostFix(ipfPoint.ColumnValues[welIPFFile.AssociatedFileColIdx], "_cluster", 50) : ipfPoint.ColumnValues[welIPFFile.AssociatedFileColIdx];

                                                                resultHandler.AddCheckDetail(welDetailLayer, clusterX, clusterY,
                                                                    new CheckDetail(DischargeChangeWarning, welIPFFile,
                                                                    "Change in discharge (" + changeStats.Max.ToString() + ") above outlierrange (" + changeStats.OutlierRangeUpperValue + ")",
                                                                    null, tsFilename, timeseries));
                                                            }

                                                        }
                                                    }

                                                    modelperiodTimeseries = null;
                                                }

                                                ///////////////////////////////////////////////////////
                                                // 7. Check other things: city, zero discharge       //
                                                ///////////////////////////////////////////////////////

                                                // Check if discharge is zero (if not already checked for a maxdischarge < 0)
                                                if (!(maxDischarge < 0) && wellAvgDischargeValue.Equals(0f))
                                                {
                                                    resultHandler.AddCheckResult(welWarningLayer, x, y, ZeroDischargeWarning);
                                                }

                                                // Check if a large well or well cluster is not near a city (check for minimum discharge period (or -1 for constant discharge values)
                                                if ((avgDischargeValue <= cityMaxDischarge) && ((avgDischargePeriod > 275) || (avgDischargePeriod.Equals(-1))))
                                                {
                                                    if (cityIDFFile != null)
                                                    {
                                                        // Check xy-coordinates of each individual well, not the cluster
                                                        if (!cityIDFFile.GetValue(x, y).Equals(cityIDFFile.NoDataValue))
                                                        {
                                                            resultHandler.AddCheckResult(welWarningLayer, x, y, FilterInCityWarning);
                                                            resultHandler.AddCheckDetail(welDetailLayer, clusterX, clusterY, new CheckDetail(FilterInCityWarning, welIPFFile,
                                                                "Large well (" + avgDischargeValue.ToString("F1", englishCultureInfo) + ") in citybuffer", null, null, timeseries));
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        throw new Exception("Could not process well " + ipfPoint.ToString() + " at zero-based index " + welIPFFile.IndexOf(ipfPoint) + " in IPF-file: " + welIPFFile.Filename, ex);
                                    }
                                }

                                if (isClusterChecked)
                                {
                                    // Now add clusterlocations and timeseries to detail IPF
                                    foreach (IPFCluster cluster in clusterDictionary.Values)
                                    {
                                        // Add cluster to detailfile if this or another messagepoint not yet present at the same location
                                        IPFPoint clusterPoint = cluster.ToPoint(((IPFFile)welDetailLayer.ResultFile), cluster.Points[0], ipfDischargeColIdx);
                                        if (!(((IPFFile)welDetailLayer.ResultFile).IndexOf(clusterPoint) >= 0))
                                        {
                                            if (clusterPoint.HasTimeseries())
                                            {
                                                string tsFilename = cluster.ID;
                                                resultHandler.AddCheckDetail(welDetailLayer, (float)clusterPoint.X, (float)clusterPoint.Y, new CheckDetail("Cluster: " + cluster.ID, cluster.IPFFile,
                                                    "Centerpoint of cluster, summed timeseries", null, tsFilename, clusterPoint.Timeseries));
                                            }
                                            else
                                            {
                                                resultHandler.AddCheckDetail(welDetailLayer, (float)clusterPoint.X, (float)clusterPoint.Y, new CheckDetail("Cluster: " + cluster.ID, cluster.IPFFile,
                                                    "Centerpoint of cluster, summed average: " + GetAverageValue(clusterPoint, valueColIdx).ToString("F3", englishCultureInfo), null, GetAverageValue(clusterPoint, valueColIdx)));
                                            }
                                        }
                                    }
                                }

                                //                                // Report kDqRatio-statistics
                                //                                kDqRatioStats.ComputeStatistics(OutlierMethodEnum.IQR, OutlierBaseRangeEnum.Pct75_25, 3.95f);
                                //                                log.AddInfo("kDqRatio-statistics for this entry: N: " + kDqRatioStats.Count + ", Median: " + kDqRatioStats.Median + ", IQR: " + kDqRatioStats.IQR + ", Q1: " + kDqRatioStats.Q1 + ", Q3: " + kDqRatioStats.Q3 + ", min: " + kDqRatioStats.Min + ", max: " + kDqRatioStats.Max + ", mean: " + kDqRatioStats.Mean + ", SD: " + kDqRatioStats.SD, 2);
                                long welErrorCount = welErrorLayer.ResultCount - prevErrorCount;
                                long welWarningCount = welWarningLayer.ResultCount - prevWarningCount;
                                if (welErrorCount > 0)
                                {
                                    log.AddInfo("Wells in extent: " + extentWellCount + ", wells checked: " + checkedWellCount + ", errors: " + welErrorCount + ", warnings: " + welWarningCount, 3);
                                }
                                else if (welWarningCount > 0)
                                {
                                    log.AddInfo("Wells in extent: " + extentWellCount + ", wells checked: " + checkedWellCount + ", warnings: " + welWarningCount, 3);
                                }
                                else
                                {
                                    log.AddInfo("Wells in extent: " + extentWellCount + ", no issues found", 3);
                                }
                                prevErrorCount = welErrorLayer.ResultCount;
                                prevWarningCount = welWarningLayer.ResultCount;
                            }

                            // If created, write calculated kD- and C-file
                            if (calcKDIDFFile != null)
                            {
                                calcKDIDFFile.WriteFile(model.CreateMetadata("kD, calculated by iMODValidator: kD = (top-bot)*kh", ((topIDFFile != null) ? Path.GetFileName(topIDFFile.Filename) + ";" : "") + ((botIDFFile != null) ? Path.GetFileName(botIDFFile.Filename) + ";" : "") + Path.GetFileName(khIDFFile.Filename)));
                            }
                            if (calcCIDFFile != null)
                            {
                                calcCIDFFile.WriteFile(model.CreateMetadata("C, calculated by iMODValidator: c = (top-bot)/kv (currently kva is ignored for " + this.Name, ((topIDFFile != null) ? Path.GetFileName(topIDFFile.Filename) + ";" : "") + ((botIDFFile != null) ? Path.GetFileName(botIDFFile.Filename) + ";" : "") + Path.GetFileName(kvIDFFile.Filename)));
                            }

                            // Write errorfiles and add files to error handler
                            if (welErrorLayer.HasResults())
                            {
                                welErrorLayer.CompressLegend(CombinedResultLabel);
                                try
                                {
                                    welErrorLayer.WriteResultFile(log);
                                }
                                catch (Exception ex)
                                {
                                    log.AddWarning("Could not write warning layer: " + ex.InnerException.Message + "\r\n" + ex.StackTrace);
                                }
                                resultHandler.AddExtraMapFiles(welErrorLayer.SourceFiles);
                                if (calcKDIDFFile != null)
                                {
                                    resultHandler.AddExtraMapFile(calcKDIDFFile);
                                }
                                if (calcCIDFFile != null)
                                {
                                    resultHandler.AddExtraMapFile(calcCIDFFile);
                                }
                            }

                            // Write warningfiles
                            if (welWarningLayer.HasResults())
                            {
                                welWarningLayer.CompressLegend(CombinedResultLabel);
                                try
                                {
                                    welWarningLayer.WriteResultFile(log);
                                }
                                catch (Exception ex)
                                {
                                    log.AddWarning("Could not write warning layer: " + ex.InnerException.Message + "\r\n" + ex.StackTrace);
                                }
                                resultHandler.AddExtraMapFiles(welWarningLayer.SourceFiles);
                                if (calcKDIDFFile != null)
                                {
                                    resultHandler.AddExtraMapFile(calcKDIDFFile);
                                }
                                if (calcCIDFFile != null)
                                {
                                    resultHandler.AddExtraMapFile(calcCIDFFile);
                                }
                            }

                            // Write IPF-details for results
                            if (welDetailLayer.HasResults())
                            {
                                try
                                {
                                    welDetailLayer.WriteResultFile(log);
                                }
                                catch (Exception ex)
                                {
                                    log.AddWarning("Could not write detail layer: " + ex.Message + "\r\n" + ex.StackTrace);
                                }
                            }

                            welErrorLayer.ReleaseMemory(false);
                            welWarningLayer.ReleaseMemory(true);
                        }
                    }
                }
            }

            settings.ReleaseMemory(false);
            welPackage.ReleaseMemory(true);
        }

        private string GetAssociatedFilename(IPFPoint ipfPoint)
        {
            return ipfPoint.HasAssociatedFile() ? Path.GetFileName(ipfPoint.ColumnValues[ipfPoint.IPFFile.AssociatedFileColIdx]) + "." + ipfPoint.IPFFile.AssociatedFileExtension : null;
        }

        public List<IPFCluster> FindClustersById(IPFFile ipfFile, List<IPFPoint> points, int idColIdx, float maxDischarge, int minClusterSize, int dischargeColIdx)
        {
            Dictionary<string, IPFCluster> clusterDictionary = new Dictionary<string, IPFCluster>();

            // First group points by id
            foreach (IPFPoint point in points)
            {
                float ipfAvgDischargeValue = GetAverageValue(point, dischargeColIdx);
                if (ipfAvgDischargeValue < maxDischarge)
                {
                    string id = point.ColumnValues[idColIdx];
                    if (!clusterDictionary.ContainsKey(id))
                    {
                        clusterDictionary.Add(id, new IPFCluster(ipfFile, id));
                    }
                    clusterDictionary[id].AddPoint(point);
                }
            }

            // Now select groups with more than minClustersize points
            List<IPFCluster> clusters = new List<IPFCluster>();
            foreach (IPFCluster cluster in clusterDictionary.Values)
            {
                if (cluster.Points.Count() >= minClusterSize)
                {
                    clusters.Add(cluster);
                }
            }

            return clusters;
        }

        /// <summary>
        /// Extends given clusterPointList from startPoint within given points (which will be modified in this method)
        /// </summary>
        /// <param name="clusterPointList"></param>
        /// <param name="startPoint"></param>
        /// <param name="points"></param>
        /// <param name="maxDistance">maximum distance between points in cluster</param>
        /// <param name="maxValue">maximum (average) value for potential points for cluster</param>
        /// <param name="ipfDischargeColIdx">zero-based index of IPF-column with discharge</param>
        private void GrowClusterList(List<IPFPoint> clusterPointList, IPFPoint startPoint, List<IPFPoint> points, float maxDistance, float maxValue, int ipfDischargeColIdx)
        {
            if (points.Count() > 0)
            {
                // First check all connections for this point
                List<IPFPoint> growPointList = new List<IPFPoint>();
                List<IPFPoint> remainingPointsList = new List<IPFPoint>();
                while (points.Count() > 0)
                {
                    IPFPoint point = points[points.Count() - 1];
                    float distance = (float)startPoint.GetDistance(point);
                    if (distance < maxDistance)
                    {
                        float average = GetAverageValue(point, ipfDischargeColIdx);
                        if (average < maxValue)
                        {
                            // Add to list of points for extending cluster
                            growPointList.Add(point);

                            // Add to cluster
                            if (!clusterPointList.Contains(point))
                            {
                                clusterPointList.Add(point);
                            }
                        }
                        else
                        {
                            remainingPointsList.Add(point);
                        }
                    }
                    else
                    {
                        remainingPointsList.Add(point);
                    }
                    points.RemoveAt(points.Count() - 1);
                }

                // Now recursively search onwards from the new points in the rest of the cluster
                foreach (IPFPoint growPoint in growPointList)
                {
                    GrowClusterList(clusterPointList, growPoint, remainingPointsList, maxDistance, maxValue, ipfDischargeColIdx);
                }

                // Add leftover points again to specified pointlist for possible extension from other startpoints
                points.AddRange(remainingPointsList);
                int pointCount = points.Count;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="points"></param>
        /// <param name="excludedPoints"></param>
        /// <param name="maxDistance"></param>
        /// <param name="minClusterSize"></param>
        /// <returns></returns>
        public IPFCluster FindClusterByDistance(IPFFile ipfFile, IPFPoint startPoint, List<IPFPoint> points, float maxDistance, float maxDischarge, int minClusterSize, int ipfDischargeColIdx)
        {
            List<IPFPoint> clusterPointList = new List<IPFPoint>();
            List<IPFPoint> remainingPoints = new List<IPFPoint>(points);

            clusterPointList.Add(startPoint);
            remainingPoints.Remove(startPoint);

            GrowClusterList(clusterPointList, startPoint, remainingPoints, maxDistance, maxDischarge, ipfDischargeColIdx);

            if (clusterPointList.Count() >= minClusterSize)
            {
                IPFCluster cluster = new IPFCluster(ipfFile);
                cluster.AddPoints(clusterPointList);
                return cluster;
            }
            else
            {
                return null;
            }
        }


        public List<IPFCluster> FindClustersByDistance(IPFFile ipfFile, List<IPFPoint> points, float maxDistance, float maxDischarge, int minClusterSize, float buffersize, int ipfDischargeColIdx)
        {
            List<IPFPoint> remainingPoints = new List<IPFPoint>(points);
            List<IPFCluster> clusters = new List<IPFCluster>();

            float ipfAvgDischargeValue = float.NaN;
            while (remainingPoints.Count() > 0)
            {
                IPFPoint point1 = remainingPoints[remainingPoints.Count() - 1];
                try
                {
                    ipfAvgDischargeValue = GetAverageValue(point1, ipfDischargeColIdx);
                    if (ipfAvgDischargeValue < maxDischarge)
                    {
                        Extent selectionExtent = new Extent((float)point1.X - buffersize, (float)point1.Y - buffersize, (float)point1.X + buffersize, (float)point1.Y + buffersize);
                        List<IPFPoint> remainingPointSelection = IPFUtils.SelectPoints(remainingPoints, selectionExtent);
                        IPFCluster cluster = FindClusterByDistance(ipfFile, point1, remainingPointSelection, maxDistance, maxDischarge, minClusterSize, ipfDischargeColIdx);

                        if (cluster != null)
                        {
                            clusters.Add(cluster);
                            foreach (IPFPoint point in cluster.Points)
                            {
                                remainingPoints.Remove(point);
                            }
                        }
                        else
                        {
                            remainingPoints.RemoveAt(remainingPoints.Count() - 1);
                        }
                    }
                    else
                    {
                        remainingPoints.RemoveAt(remainingPoints.Count() - 1);
                    }
                }
                catch (Exception)
                {
                    // The point could not be read, probably the timeseriesfile is corrupt
                    // ignore error here, it should be checked elsewhere
                    remainingPoints.RemoveAt(remainingPoints.Count() - 1);
                }
            }

            return clusters;
        }

        /// <summary>
        /// Parses a string with either a (zero-based) index or as a (list of) string(s) with the columnname(s) for the given IPF-file
        /// </summary>
        /// <param name="colString"></param>
        /// <returns></returns>
        private int FindIPFColIdx(string colString, IPFFile ipfFile)
        {
            if (int.TryParse(colString, out int colIdx))
            {
                return colIdx;
            }
            else
            {
                if (ipfFile != null)
                {
                    string[] colNames = colString.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string colName in colNames)
                    {
                        for (int ipfColIdx = 0; ipfColIdx < ipfFile.ColumnCount; ipfColIdx++)
                        {
                            string ipfColName = ipfFile.ColumnNames[ipfColIdx];
                            if (ipfColName.ToLower().Equals(colName.ToLower()))
                            {
                                return ipfColIdx;
                            }
                        }
                    }
                }
            }
            return -1;
        }

        private List<IPFPoint> CopyExcluding(List<IPFPoint> points, List<IPFPoint> excludedPoints)
        {
            List<IPFPoint> copiedPoints = new List<IPFPoint>();
            foreach (IPFPoint point in points)
            {
                if (!copiedPoints.Contains(point) && !excludedPoints.Contains(point))
                {
                    copiedPoints.Add(point);
                }
            }
            return copiedPoints;
        }

        public bool ContainsAll(Dictionary<string, int> checkedFiles, List<IMODFile> imodFiles, int ilay, Log log, int logIndentLevel = 0)
        {
            if (imodFiles == null)
            {
                return true;
            }
            else
            {
                if (checkedFiles != null)
                {
                    foreach (IMODFile imodFile in imodFiles)
                    {
                        string filename = imodFile.Filename;
                        if (!checkedFiles.ContainsKey(filename))
                        {
                            return false;
                        }
                        else
                        {
                            int checkedILay = checkedFiles[filename];
                            if (checkedILay.Equals(ilay))
                            {
                                // File has been checked before with this ilay, which is ok
                            }
                            else
                            {
                                // File has been checked before with another ilay
                                log.AddError(WELPackage.DefaultKey, filename, "File for ilay " + ilay + " has been assigned before to another ilay (" + checkedILay + "): " + imodFile.Filename, logIndentLevel);
                            }
                        }
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Report a FilterNotInLayer error if the layer above or below actually is a better alternative
        /// </summary>
        /// <param name="welErrorLayer"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="FilterNotInLayerError"></param>
        /// <param name="z1"></param>
        /// <param name="z2"></param>
        /// <param name="topValue"></param>
        /// <param name="botValue"></param>
        /// <param name="upperBotValue"></param>
        /// <param name="lowerTopValue"></param>
        /// <param name="isFilterInAquitardChecked"></param>
        /// <param name="isFilterNotInLayerKDChecked"></param>
        /// <param name="minKDValue"></param>
        private void ReportFilterNotInLayerError(CheckResultHandler resultHandler, CheckErrorLayer welErrorLayer, CheckDetailLayer welDetailLayer, IPFFile welIPFFile, IPFPoint ipfPoint, CheckError FilterNotInLayerError, float z1, float z2, float topValue, float botValue, float upperBotValue, float lowerTopValue, bool isFilterInAquitardChecked, bool isFilterNotInLayerKDChecked, float upperKDValue, float kDValue, float lowerKDValue, float minKDValue, string detail, string moreDetail)
        {
            // Check if the remaining part of the filter is above or below this filter.
            if (z1 > topValue)
            {
                // Don't report error if filter is inside upper aquitard and the kD in this layer is larger than the upper kD
                if (!((z1 <= upperBotValue) && (upperKDValue < kDValue)))
                {
                    //                    if (!upperBotValue.Equals(float.NaN))
                    // Avoid reporting an error if the kD above is (also) too small
                    if (!isFilterNotInLayerKDChecked || !(upperKDValue <= minKDValue))
                    {
                        // Avoid reporting an error if there is no aquitard above with thickness > 0
                        //                            if (!((upperBotValue - topValue) <= 0))
                        {
                            resultHandler.AddCheckResult(welErrorLayer, (float) ipfPoint.X, (float) ipfPoint.Y, FilterNotInLayerError);
                            CheckDetail checkDetail = new CheckDetail(FilterNotInLayerError, welIPFFile, detail, moreDetail);
                            checkDetail.AddFilename(GetAssociatedFilename(ipfPoint));
                            resultHandler.AddCheckDetail(welDetailLayer, (float) ipfPoint.X, (float) ipfPoint.Y, checkDetail);
                        }
                    }
                }
            }
            else
            {
                // Don't report error if filter is inside lower aquitard and the kD in this layer is larger than the lower kD
                if (!((z2 >= lowerTopValue) && (lowerKDValue < kDValue)))
                {
                    // Avoid reporting filters below lowest modellayer if specified so
                    //                    if (isFilterInAquitardChecked || !lowerTopValue.Equals(float.NaN))
                    {
                        // Avoid reporting an error if the kD below is (also) too small
                        if (!isFilterNotInLayerKDChecked || !(lowerKDValue <= minKDValue))
                        {
                            // Avoid reporting an error if there is no aquitard below with thickness > 0
                            //                            if (!((botValue - lowerTopValue) <= 0))
                            {
                                resultHandler.AddCheckResult(welErrorLayer, (float) ipfPoint.X, (float) ipfPoint.Y, FilterNotInLayerError);
                                CheckDetail checkDetail = new CheckDetail(FilterNotInLayerError, welIPFFile, detail, moreDetail);
                                checkDetail.AddFilename(GetAssociatedFilename(ipfPoint));
                                resultHandler.AddCheckDetail(welDetailLayer, (float) ipfPoint.X, (float) ipfPoint.Y, checkDetail);
                            }
                        }
                    }
                }
            }
        }

        private IDFFile CreateCityIDFFile(IDFFile luseIDFFile, WELCheckSettings settings, Model model, Log log, int indentLevel = 0, bool isFileSaved = true)
        {
            log.AddInfo("Creating city IDF-file from LUSE-file...", indentLevel);

            if (luseIDFFile == null)
            {
                throw new Exception("CreateCityIDFFile: LUSE IDF-file is null");
            }

            List<int> luseCityCodes = settings.ParseIntArrayString(settings.LUSECityCodes);
            int firstLUSECityCode = luseCityCodes[0];
            float blocksize = settings.GetValue(settings.LUSECityBlockSize);
            float buffersize = settings.GetValue(settings.LUSECityBufferSize);
            float bufferAccuracy = settings.GetValue(settings.LUSECityBufferAccuracy.ToString());

            // Create a copy of the land use with the first LUSE-citycode value for each cell with an LUSE-citycode, leave other codes
            log.AddInfo("Creating LUSE-cities IDF-file ...", indentLevel + 1);
            IDFFile luseCityIDFFile = luseIDFFile.CopyIDF("LUSECities.idf");
            for (int luseCityCodeIdx = 1; luseCityCodeIdx < luseCityCodes.Count(); luseCityCodeIdx++)
            {
                int luseCityCode = luseCityCodes[luseCityCodeIdx];
                luseCityIDFFile.ReplaceValues(luseCityCode, firstLUSECityCode);
            }
            luseCityIDFFile.Filename = Path.Combine(FileUtils.EnsureFolderExists(GetIMODFilesPath(model), Name), "LUSECityFile.IDF");
            luseCityIDFFile.WriteFile();

            // Now scale file to LUSE-blocksize
            log.AddInfo("Creating LUSE-cities block scale " + blocksize + " IDF-file ...", indentLevel + 1);
            IDFFile luseCityBlockIDFFile = luseCityIDFFile.ScaleUp(blocksize, UpscaleMethodEnum.MostOccurring);
            luseCityBlockIDFFile.Filename = Path.Combine(FileUtils.EnsureFolderExists(GetIMODFilesPath(model), Name), "LUSECityBlockFile_scale" + blocksize + ".IDF");
            luseCityBlockIDFFile.WriteFile();

            // Select city cells from LUSE-blockfile
            log.AddInfo("Select city cells from LUSE-city blockfile with scale " + blocksize + " IDF-file ...", indentLevel + 1);
            IDFFile cityBlockIDFFile = luseCityBlockIDFFile.CopyIDF(Path.Combine(FileUtils.EnsureFolderExists(GetIMODFilesPath(model), Name), "CityBlockFile_scale" + blocksize + ".IDF"));
            cityBlockIDFFile.ResetValues();
            cityBlockIDFFile.ReplaceValues(luseCityBlockIDFFile, firstLUSECityCode, 1f);
            luseCityBlockIDFFile.ReleaseMemory();
            cityBlockIDFFile.WriteFile();

            // Calculate fine cityblock cellsize
            float fineCityBlockCellsize = (float)(buffersize * (bufferAccuracy / 100.0f));
            if (fineCityBlockCellsize < luseIDFFile.XCellsize)
            {
                fineCityBlockCellsize = luseIDFFile.XCellsize;
            }
            float[] allowedCellsizes = new float[] { 5, 10, 25, 50, 100, 200, 250, 500, 1000, 2000 };
            int idx = 0;
            while ((allowedCellsizes[idx] < fineCityBlockCellsize) && (idx < allowedCellsizes.Length))
            {
                idx = idx + 1;
            }
            fineCityBlockCellsize = allowedCellsizes[idx];

            // Downscale blockfile again to finer cellsize
            log.AddInfo("Downscale cities block IDF-file ...", indentLevel + 1);
            IDFFile fineCityBlockIDFFile = cityBlockIDFFile.ScaleDown(fineCityBlockCellsize, DownscaleMethodEnum.Block);
            luseCityBlockIDFFile.ReleaseMemory();
            fineCityBlockIDFFile.Filename = Path.Combine(FileUtils.EnsureFolderExists(GetIMODFilesPath(model), Name), "CityBlockFile_scale" + (int)(fineCityBlockCellsize) + ".IDF");
            fineCityBlockIDFFile.WriteFile();

            // Now select LUSE city cells from blockfile
            log.AddInfo("Select LUSE-cities within downscaled block IDF-file ...", indentLevel + 1);
            IDFFile luseCityFilteredIDFFile = fineCityBlockIDFFile.CopyIDF(Path.Combine(FileUtils.EnsureFolderExists(GetIMODFilesPath(model), Name), "LUSECityFilteredFile_scale" + (int)(fineCityBlockIDFFile.XCellsize) + ".IDF"));
            luseCityFilteredIDFFile.ResetValues();
            luseCityFilteredIDFFile.ReplaceValues(luseCityIDFFile, firstLUSECityCode, fineCityBlockIDFFile);
            luseCityIDFFile.ReleaseMemory(false);
            fineCityBlockIDFFile.ReleaseMemory(false);
            luseCityFilteredIDFFile.WriteFile();

            // Now buffer around city blocks
            log.AddInfo("Creating LUSE-cities buffered block IDF-file ...", indentLevel + 1);
            IDFFile cityBufferIDFFile = luseCityFilteredIDFFile.Buffer(1f, buffersize);
            luseCityFilteredIDFFile.ReleaseMemory();

            cityBufferIDFFile.Filename = Path.Combine(FileUtils.EnsureFolderExists(GetIMODFilesPath(model), Name), "CityBuffer.IDF");

            if (isFileSaved)
            {
                Metadata metadata = new Metadata();
                metadata.Version = "1.0";
                metadata.Modelversion = model.Runfilename;
                metadata.Source = luseIDFFile.Filename;
                metadata.Producer = "iMODValidator";
                metadata.ProcessDescription = "Generated by iMODValidator: select LUSE-citycodes (" + settings.LUSECityCodes + ", upscale (most occurring, " + blocksize + "), downscale (block), buffer";
                metadata.Scale = "-";
                metadata.Unit = "-";
                metadata.Resolution = luseIDFFile.XCellsize.ToString();
                metadata.Organisation = "Sweco Nederland B.V.";
                metadata.Website = "http://www.sweco.nl/ons-aanbod/water/?service=Waterbeheer";
                metadata.Contact = "-";
                metadata.Emailaddress = "info@sweco.nl";

                cityBufferIDFFile.WriteFile(metadata);
                log.AddInfo("Saved city IDF-file to " + cityBufferIDFFile.Filename, indentLevel);
            }

            return cityBufferIDFFile;
        }

        private float GetAverageValue(IPFPoint ipfPoint, int valueColIdx)
        {
            if (ipfPoint.HasTimeseries())
            {
                return ipfPoint.Timeseries.CalculateAverage();
            }
            else
            {
                if ((valueColIdx > 0) && ipfPoint.HasFloatValue(valueColIdx))
                {
                    return ipfPoint.GetFloatValue(valueColIdx);
                }
                else
                {
                    throw new Exception("Cannot calculate average: invalid value column index (" + valueColIdx + ") for IPF-point: " + this.ToString());
                }
            }
        }
    }
}
