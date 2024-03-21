// iMODstats is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of iMODstats.
// 
// iMODstats is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// iMODstats is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with iMODstats. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD.IPF;
using Sweco.SIF.Statistics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODstats.Zones
{
    /// <summary>
    /// Class to store statistics for a specific zone of a specific IPF-file: number of points, Extent coverage (%), min value, max value, avegage value, standard deviation, sum, percentiles
    /// </summary>
    public class IPFZoneStatistics : ZoneStatistics
    {
        public IPFFile IPFFile { get; set; }
        public IPFFile ResultIPFFile { get; set; }

        protected Statistics.Statistics ResidualZoneStats { get; set; }
        protected Statistics.Statistics AbsResidualZoneStats { get; set; }

        public IPFZoneStatistics(IPFFile ipfFile, IPFZoneSettings settings, string zoneID = null) : base(ipfFile.Filename, settings, zoneID)
        {
            this.IPFFile = ipfFile;
            this.ResultIPFFile = null;

            ResidualZoneStats = new Statistics.Statistics();
            AbsResidualZoneStats = new Statistics.Statistics();
        }

        /// <summary>
        /// Retrieve predefined columnnames for each statistics column in result table
        /// </summary>
        /// <param name="hasZones"></param>
        /// <returns></returns>
        protected override List<string> GetTableColumnNames(bool hasZones = false)
        {
            IPFZoneSettings settings = (IPFZoneSettings)Settings;

            List<string> columnNames = new List<string>();
            columnNames.AddRange(new List<string>(new string[] { "N", "Coverage", "Avg(val)", "SD(val)", "Min(val)", "Max(val)" }));
            columnNames.AddRange(GetPercentileColumnNames());
            if (settings.IPFTSValueColIdx1 >= 0)
            {
                columnNames.AddRange(new List<string>() { "MinFirstDate", "MaxFirstDate", "MinLastDate", "MaxLastDate", "AvgNdates", "SDNdates" });
            }

            columnNames.AddRange(GetTableResidualColumnNames());

            return columnNames;
        }

        /// <summary>
        /// Retrieves predefined Excel number format string for each statistics column in result table
        /// </summary>
        /// <param name="decimalCount"></param>
        /// <param name="hasZones"></param>
        /// <returns></returns>
        protected override List<string> GetTableNumberFormats(int decimalCount, bool hasZones = false)
        {
            IPFZoneSettings settings = (IPFZoneSettings)Settings;

            RetrieveFormatStrings(out string decimalFormatString, out string percentageFormatString);
            string dateFormatString = "dd-MM-yyyy";

            List<string> numberFormats = new List<string>();
            numberFormats.AddRange(new List<string>(new string[] { "0", percentageFormatString, decimalFormatString, decimalFormatString, decimalFormatString, decimalFormatString }));
            numberFormats.AddRange(GetPercentileNumberFormats(decimalCount));
            if (settings.IPFTSValueColIdx1 >= 0)
            {
                numberFormats.AddRange(new List<string>() { dateFormatString, dateFormatString, dateFormatString, dateFormatString, decimalFormatString, decimalFormatString });
            }

            numberFormats.AddRange(GetTableResidualNumberFormats());

            return numberFormats;
        }

        /// <summary>
        /// Retrieves predefined cell comments for Excel formatting for each statistics column header in result table
        /// </summary>
        /// <param name="hasZones"></param>
        /// <returns></returns>
        protected override List<string> GetTableColumnComments(bool hasZones)
        {
            IPFZoneSettings settings = (IPFZoneSettings)Settings;

            List<string> comments = new List<string>();
            comments.AddRange(new List<string>(new string[] {
                "Number of IPF-points used for statistic",
                (settings.IPFValueColRef != null) ? "Fraction of IPF-points with valid values" : "Fraction of IPF-points with timestamps within specified period",
                (settings.IPFValueColRef != null) ? "Average value over IPF-points" : "Average of averaged TS-values per IPF-point",
                (settings.IPFValueColRef != null) ? "Standard deviation (for population) over IPF-points" : "Standard deviation (for population) of averaged TS-values per IPF-point",
                (settings.IPFValueColRef != null) ? "Minimum value of IPF-points" : "Minimum of averaged TS-values per IPF-point",
                (settings.IPFValueColRef != null) ? "Maximum value of IPF-points" : "Maximum of averaged values per IPF-point",
            }));
            comments.AddRange(GetPercentileColumnComments());
            if (settings.IPFTSValueColIdx1 >= 0)
            {
                comments.AddRange(new List<string>() {
                    "Minimum startdate of timeseries of all IPF-points",
                    "Maximum startdate of timeseries of all IPF-points",
                    "Minimum enddate of timeseries of all IPF-points",
                    "Maximum enddate of timeseries of all IPF-points",
                    "Average number of dates of all IPF-points",
                    "Standard deviation (for population) of number of dates per IPF-point"
                });
            }

            comments.AddRange(GetTableResidualComments());

            return comments;
        }

        /// <summary>
        /// Retrieve predefined optional columnnames for residual columns in result table
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerable<string> GetTableResidualColumnNames()
        {
            List<string> columnNames = new List<string>();

            IPFZoneSettings settings = (IPFZoneSettings)Settings;
            if ((settings.IPFTSValueColIdx2 >= 0) && (settings.IPFResidualMethod != ResidualMethod.None))
            {
                columnNames.AddRange(new List<string>() { "Avg(res)", "SD(res)", "AvgAbs(res)", "SDAbs(res)" });
            }

            return columnNames;

        }

        /// <summary>
        /// Retrieve predefined optional number formats for residual columns in result table
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerable<string> GetTableResidualNumberFormats()
        {
            IPFZoneSettings settings = (IPFZoneSettings)Settings;
            RetrieveFormatStrings(out string decimalFormatString, out string percentageFormatString);

            List<string> numberFormats = new List<string>();
            if ((settings.IPFTSValueColIdx2 >= 0) && (settings.IPFResidualMethod != ResidualMethod.None))
            {
                numberFormats.AddRange(new List<string>() { decimalFormatString, decimalFormatString, decimalFormatString, decimalFormatString });
            }
            return numberFormats;
        }

        /// <summary>
        /// Retrieve predefined optional comments for residual columns in result table
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerable<string> GetTableResidualComments()
        {
            IPFZoneSettings settings = (IPFZoneSettings)Settings;

            List<string> comments = new List<string>();
            if ((settings.IPFTSValueColIdx2 >= 0) && (settings.IPFResidualMethod != ResidualMethod.None))
            {
                comments.AddRange(new List<string>() { null, null, null, null });
            }
            return comments;
        }

        /// <summary>
        /// Retrieve statistics for IPF-points with timeseries in specified IPF-file as defined by specified ZoneSettings.
        /// </summary>
        public override void Calculate()
        {
            IPFZoneSettings settings = (IPFZoneSettings)Settings;

            if (settings.IPFValueColRef != null)
            {
                int ipfValueColIdx = IPFFile.FindColumnIndex(settings.IPFValueColRef);
                if (ipfValueColIdx >= IPFFile.ColumnCount)
                {
                    throw new ToolException("Specified IPF value column number (" + (settings.IPFValueColRef) + ") cannot be larger than number of colums in IPF-file (" + IPFFile.ColumnCount + ")");
                }

                CalculateValStats(ipfValueColIdx);
            }
            else
            {
                int tsValueColIdx1 = settings.IPFTSValueColIdx1;

                CalculateTSStats(tsValueColIdx1);
            }
        }

        /// <summary>
        /// Calculate statistics per IPF-point for timeseries and add to class datasets
        /// </summary>
        /// <param name="tsValueColIdx1"></param>
        public virtual void CalculateTSStats(int tsValueColIdx1)
        {
            IPFZoneSettings settings = (IPFZoneSettings)Settings;

            CultureInfo englishCultureInfo = SIFTool.EnglishCultureInfo;
            string floatFormatString = "F" + (settings.DecimalCount.Equals(int.MaxValue) ? string.Empty : settings.DecimalCount.ToString());

            DateTime? MinFirstDate = DateTime.MaxValue;
            DateTime? MaxFirstDate = DateTime.MinValue;
            DateTime? MinLastDate = DateTime.MaxValue;
            DateTime? MaxLasttDate = DateTime.MinValue;

            // Create statistics objects for the mean value and for date counts (within specified period)
            Statistics.Statistics ipfFileMeanStats = new Statistics.Statistics();
            Statistics.Statistics ipfFileDateCountStats = new Statistics.Statistics();

            // Create an IPF-file to store statistics about individual IPF-points
            IPFFile resultIPFFile = new IPFFile();
            resultIPFFile.AddXYColumns();

            // Add optional ID-column and selected columns from source IPF-file
            if (settings.IPFIDColRef != null)
            {
                int ipfIDColIdx = IPFFile.FindColumnIndex(settings.IPFIDColRef);
                if (ipfIDColIdx >= IPFFile.ColumnCount)
                {
                    throw new ToolException("Specified ID column number (" + settings.IPFIDColRef + ") cannot be larger than number of colums in IPF-file (" + IPFFile.ColumnCount + ")");
                }
                resultIPFFile.AddColumn(IPFFile.ColumnNames[ipfIDColIdx]);
            }
            foreach (string selColRef in settings.IPFSelColRefs)
            {
                int selColIdx = IPFFile.FindColumnIndex(selColRef);
                if (selColIdx >= IPFFile.ColumnCount)
                {
                    throw new ToolException("Specified selected column number (" + selColRef + ") cannot be larger than number of colums in IPF-file (" + IPFFile.ColumnCount + ")");
                }
                resultIPFFile.AddColumn(IPFFile.ColumnNames[selColIdx]);
            }

            // Add default statistics columns
            resultIPFFile.AddColumns(new List<string>() { "NVAL", "AVGVAL", "SDVAL", "MINVAL", "MAXVAL" });
            resultIPFFile.AddColumns(GetPercentileColumnNames("VAL"));

            // Add optional residual statistic objects and IPF-columns
            AddIPFResidualColumns(resultIPFFile);

            // Add column for reference to source IPF-file
            resultIPFFile.AddColumn("SourceFile");

            // Now loop through all IPF-points and create statistics
            foreach (IPFPoint ipfPoint in IPFFile.Points)
            {
                if (ipfPoint.HasTimeseries())
                {
                    IPFTimeseries ipfTimeseries = ipfPoint.Timeseries;
                    ipfTimeseries = ipfTimeseries.Select(settings.IPFTSPeriodStartDate, settings.IPFTSPeriodEndDate);

                    if (tsValueColIdx1 >= ipfTimeseries.ValueColumns.Count)
                    {
                        throw new ToolException("Specified value column number v1 for timeseries (" + (tsValueColIdx1 + 1) + ") cannot be larger than number of value columns (" + ipfTimeseries.ValueColumns.Count + ") in associated file: " + ipfTimeseries.Filename);
                    }

                    // Retrieve statistics about timeseries values
                    IPFTSStatistics ipfTSStats = new IPFTSStatistics(ipfTimeseries, tsValueColIdx1);
                    ipfTSStats.ComputeBasicStatistics(false, false);
                    ipfTSStats.ComputePercentiles();

                    ipfFileMeanStats.AddValue(ipfTSStats.Mean);
                    ipfFileDateCountStats.AddValue(ipfTSStats.Count);

                    DateTime firstdate = ipfTimeseries.Timestamps[0];
                    DateTime lastdate = ipfTimeseries.Timestamps[ipfTimeseries.Timestamps.Count - 1];
                    if (firstdate < MinFirstDate)
                    {
                        MinFirstDate = firstdate;
                    }
                    if (firstdate > MaxFirstDate)
                    {
                        MaxFirstDate = firstdate;
                    }
                    if (lastdate < MinLastDate)
                    {
                        MinLastDate = lastdate;
                    }
                    if (lastdate > MaxLasttDate)
                    {
                        MaxLasttDate = lastdate;
                    }

                    List<string> columnValues = new List<string>();
                    if (settings.IPFIDColRef != null)
                    {
                        int idColIdx = IPFFile.FindColumnIndex(settings.IPFIDColRef);
                        columnValues.Add(ipfPoint.ColumnValues[idColIdx]);
                    }
                    foreach (string selColRef in settings.IPFSelColRefs)
                    {
                        int selColIdx = IPFFile.FindColumnIndex(selColRef);
                        columnValues.Add(ipfPoint.ColumnValues[selColIdx]);
                    }

                    columnValues.AddRange(new List<string>() {
                        ipfTSStats.Count.ToString(),
                        ipfTSStats.Mean.ToString(floatFormatString, englishCultureInfo),
                        ipfTSStats.SD.ToString(floatFormatString, englishCultureInfo),
                        ipfTSStats.Min.ToString(floatFormatString, englishCultureInfo),
                        ipfTSStats.Max.ToString(floatFormatString, englishCultureInfo),
                        });

                    List<object> percentileValues = RetrievePercentileValues(ipfTSStats);
                    foreach (object percentileValue in percentileValues)
                    {
                        columnValues.Add(((float)percentileValue).ToString(floatFormatString, englishCultureInfo));
                    }

                    // Optionally add residual statistics
                    AddPointResidualValues(ipfTimeseries, ipfTSStats, resultIPFFile, columnValues);

                    // Add reference to source filename
                    columnValues.Add(FileUtils.GetRelativePath(IPFFile.Filename, settings.InputPath));

                    IPFPoint resultIPFPoint = new IPFPoint(resultIPFFile, new DoublePoint(ipfPoint.X, ipfPoint.Y), columnValues);
                    resultIPFFile.AddPoint(resultIPFPoint);
                }
                else
                {
                    // skip points without timeseries
                }
            }

            ResultIPFFile = resultIPFFile;

            ipfFileDateCountStats.ComputeBasicStatistics();
            ipfFileMeanStats.ComputeBasicStatistics(false, false);
            ipfFileMeanStats.ComputePercentiles();

            // Store statistics over all points in the IPF-file; first add basic statistics
            StatValues = new List<object>();
            float notSkippedFraction = (IPFFile.PointCount > 0) ? (((float)ipfFileMeanStats.Count) / (float)IPFFile.PointCount) : float.NaN;
            StatValues.AddRange(RetrieveCommonStats(ipfFileMeanStats, notSkippedFraction));

            // Add statistics that are specific for IPF-files
            StatValues.AddRange(new List<object>() {
                MinFirstDate.Equals(DateTime.MaxValue) ? null : MinFirstDate,
                MaxFirstDate.Equals(DateTime.MinValue) ? null : MaxFirstDate,
                MinLastDate.Equals(DateTime.MaxValue) ? null : MinLastDate,
                MaxLasttDate.Equals(DateTime.MinValue) ? null : MaxLasttDate, ipfFileDateCountStats.Mean, ipfFileDateCountStats.SD });

            AddZoneResidualValues(StatValues);
        }

        /// <summary>
        /// Calculate statistics for specified IPF-column to class datasets
        /// </summary>
        /// <param name="ipfValueColIdx"></param>
        public virtual void CalculateValStats(int ipfValueColIdx)
        {
            IPFZoneSettings settings = (IPFZoneSettings)Settings;

            CultureInfo englishCultureInfo = SIFTool.EnglishCultureInfo;
            string floatFormatString = "F" + (settings.DecimalCount.Equals(int.MaxValue) ? string.Empty : settings.DecimalCount.ToString());

            // Create statistics objects for the mean value
            Statistics.Statistics ipfFileMeanStats = new Statistics.Statistics();

            // Loop through all IPF-points and create statistics
            foreach (IPFPoint ipfPoint in IPFFile.Points)
            {
                string valueString = CorrectStringValue(ipfPoint.ColumnValues[ipfValueColIdx]);
                if (float.TryParse(valueString, NumberStyles.Float, englishCultureInfo, out float value))
                { 
                    ipfFileMeanStats.AddValue(value);
                }
                else
                {
                    // skip points without timeseries
                }
            }

            ipfFileMeanStats.ComputeBasicStatistics(false, false);
            ipfFileMeanStats.ComputePercentiles();
        }

        /// <summary>
        /// Correct specific string values (e.g. infinity) for parsing 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected string CorrectStringValue(string value)
        {
            if (value.Equals("Infinity"))
            {
                // For infinity, try Inf which is string that is recognized as a floating point value
                value = double.PositiveInfinity.ToString(SIFTool.EnglishCultureInfo);
            }
            else if (value.Equals("-Infinity"))
            {
                // For infinity, try Inf which is string that is recognized as a floating point value
                value = double.NegativeInfinity.ToString(SIFTool.EnglishCultureInfo);
            }

            return value;
        }

        /// <summary>
        /// Add residual columns to result IPF-file
        /// </summary>
        /// <param name="resultIPFFile"></param>
        protected virtual void AddIPFResidualColumns(IPFFile resultIPFFile)
        {
            IPFZoneSettings settings = (IPFZoneSettings)Settings;

            if ((settings.IPFTSValueColIdx2 >= 0) && (settings.IPFResidualMethod != ResidualMethod.None))
            {
                switch (settings.IPFResidualMethod)
                {
                    case ResidualMethod.TSValueResidual:
                        resultIPFFile.AddColumns(new List<string>() { "NRES", "AVGRES", "SDRES", "MINRES", "MAXRES" });
                        resultIPFFile.AddColumns(GetPercentileColumnNames("RES"));
                        resultIPFFile.AddColumns(new List<string>() { "AVGABSRES", "SDABSRES", "MINABSRES", "MAXABSRES" });
                        resultIPFFile.AddColumns(GetPercentileColumnNames("ABSRES"));
                        break;
                    case ResidualMethod.TSAverageResidual:
                        resultIPFFile.AddColumns(new List<string>() { "NVAL2", "AVGVAL2", "SDVAL2", "MINVAL2", "MAXVAL2", "dAVGVAL" });
                        break;
                    default:
                        throw new Exception("Unknown ResidualMethod: " + settings.IPFResidualMethod);
                }
            }
        }

        /// <summary>
        /// Calculate and add optional residual values and statistics for specified IPF-point
        /// </summary>
        /// <param name="ipfTimeseries"></param>
        /// <param name="ipfTSStats"></param>
        /// <param name="resultIPFFile"></param>
        /// <param name="columnValues"></param>
        /// <param name="ipfFileResidualStats"></param>
        /// <param name="ipfFileAbsResidualStats"></param>
        protected virtual void AddPointResidualValues(IPFTimeseries ipfTimeseries, IPFTSStatistics ipfTSStats, IPFFile resultIPFFile, List<string> columnValues)
        {
            IPFZoneSettings settings = (IPFZoneSettings)Settings;

            int tsValueColIdx1 = settings.IPFTSValueColIdx1;
            int tsValueColIdx2 = settings.IPFTSValueColIdx2;

            if ((tsValueColIdx2 >= 0) && (settings.IPFResidualMethod != ResidualMethod.None))
            {
                if (tsValueColIdx2 >= ipfTimeseries.ValueColumns.Count)
                {
                    throw new ToolException("Specified value column number v2 for timeseries (" + (tsValueColIdx2 + 1) + ") cannot be larger than number of value columns (" + ipfTimeseries.ValueColumns.Count + ") in associated file: " + ipfTimeseries.Filename);
                }

                CultureInfo englishCultureInfo = SIFTool.EnglishCultureInfo;
                string floatFormatString = "F" + (settings.DecimalCount.Equals(int.MaxValue) ? string.Empty : settings.DecimalCount.ToString());
                float tsNoDataVal1 = ipfTimeseries.NoDataValues[tsValueColIdx1];
                float tsNoDataVal2 = ipfTimeseries.NoDataValues[tsValueColIdx2];

                switch (settings.IPFResidualMethod)
                {
                    case ResidualMethod.TSValueResidual:
                        // Calculate residual for each timestamp of the current timeseries
                        List<float> valueCol1 = ipfTimeseries.ValueColumns[tsValueColIdx1];
                        List<float> valueCol2 = ipfTimeseries.ValueColumns[tsValueColIdx2];
                        Statistics.Statistics ipfResStats = new Statistics.Statistics();
                        Statistics.Statistics ipfAbsResStats = new Statistics.Statistics();
                        List<DateTime> timestamps = ipfTimeseries.Timestamps;
                        for (int timestampIdx = 0; timestampIdx < timestamps.Count; timestampIdx++)
                        {
                            DateTime timestamp = timestamps[timestampIdx];
                            float value1 = valueCol1[timestampIdx];
                            float value2 = valueCol2[timestampIdx];
                            if (!value1.Equals(tsNoDataVal1) && !value2.Equals(tsNoDataVal2))
                            {
                                float valResidual = value2 - value1;
                                ipfResStats.AddValue(valResidual);
                                ipfAbsResStats.AddValue((float)Math.Abs(valResidual));
                            }
                        }
                        ipfResStats.ComputeBasicStatistics();
                        ipfResStats.ComputePercentiles();
                        ipfAbsResStats.ComputeBasicStatistics(false, false);
                        ipfAbsResStats.ComputePercentiles();

                        // Add residual statistics to IPF-point for resulting IPF-file
                        columnValues.Add(ipfResStats.Count.ToString());
                        columnValues.Add(ipfResStats.Mean.ToString(floatFormatString, englishCultureInfo));
                        columnValues.Add(ipfResStats.SD.ToString(floatFormatString, englishCultureInfo));
                        columnValues.Add(ipfResStats.Min.ToString(floatFormatString, englishCultureInfo));
                        columnValues.Add(ipfResStats.Max.ToString(floatFormatString, englishCultureInfo));
                        List<object> resPercentileValues = RetrievePercentileValues(ipfResStats);
                        foreach (object resPercentileValue in resPercentileValues)
                        {
                            columnValues.Add(((float)resPercentileValue).ToString(floatFormatString, englishCultureInfo));
                        }

                        columnValues.Add(ipfAbsResStats.Mean.ToString(floatFormatString, englishCultureInfo));
                        columnValues.Add(ipfAbsResStats.SD.ToString(floatFormatString, englishCultureInfo));
                        columnValues.Add(ipfAbsResStats.Min.ToString(floatFormatString, englishCultureInfo));
                        columnValues.Add(ipfAbsResStats.Max.ToString(floatFormatString, englishCultureInfo));
                        List<object> absResPercentileValues = RetrievePercentileValues(ipfAbsResStats);
                        foreach (object absResPercentileValue in absResPercentileValues)
                        {
                            columnValues.Add(((float)absResPercentileValue).ToString(floatFormatString, englishCultureInfo));
                        }

                        // Add average residual to IPF file statistics
                        ResidualZoneStats.AddValue(ipfResStats.Mean);
                        AbsResidualZoneStats.AddValue((float)Math.Abs(ipfAbsResStats.Mean));

                        break;

                    case ResidualMethod.TSAverageResidual:
                        IPFTSStatistics ipfTSStats2 = new IPFTSStatistics(ipfTimeseries, tsValueColIdx2);
                        ipfTSStats2.ComputeBasicStatistics(true);
                        float diffVal = ipfTSStats2.Mean - ipfTSStats.Mean;

                        // resultIPFFile.AddColumns(new List<string>() { "AVGVAL2", "SDVAL2", "MINVAL2", "MAXVAL2", "dAVGVAL" });
                        columnValues.Add(ipfTSStats2.Count.ToString());
                        columnValues.Add(ipfTSStats2.Mean.ToString(floatFormatString, englishCultureInfo));
                        columnValues.Add(ipfTSStats2.SD.ToString(floatFormatString, englishCultureInfo));
                        columnValues.Add(ipfTSStats2.Min.ToString(floatFormatString, englishCultureInfo));
                        columnValues.Add(ipfTSStats2.Max.ToString(floatFormatString, englishCultureInfo));
                        columnValues.Add(diffVal.ToString(floatFormatString, englishCultureInfo));

                        // Add average residual to IPF file statistics
                        ResidualZoneStats.AddValue(diffVal);
                        AbsResidualZoneStats.AddValue((float)Math.Abs(diffVal));

                        break;

                    default:
                        throw new Exception("Unknown ResidualMethod: ");
                }
            }
        }

        /// <summary>
        /// Add optional residual statistics for this zone
        /// </summary>
        /// <param name="residualZoneStats"></param>
        /// <param name="absResidualZoneStats"></param>
        /// <param name="statValues"></param>
        protected virtual void AddZoneResidualValues(List<object> statValues)
        {
            IPFZoneSettings settings = (IPFZoneSettings)Settings;

            if ((settings.IPFTSValueColIdx2 >= 0) && (settings.IPFResidualMethod != ResidualMethod.None))
            {
                ResidualZoneStats.ComputeBasicStatistics();
                AbsResidualZoneStats.ComputeBasicStatistics();

                StatValues.AddRange(new List<object>() {
                    ResidualZoneStats.Mean,
                    ResidualZoneStats.SD,
                    AbsResidualZoneStats.Mean,
                    AbsResidualZoneStats.SD
                });
            }
        }
    }

    /// <summary>
    /// Class to store settings for calculation of IPFZoneStatistics
    /// </summary>
    public class IPFZoneSettings : ZoneSettings
    {
        public string InputPath { get; set; }
        public string IPFXColRef { get; set; }
        public string IPFYColRef { get; set; }
        public string IPFIDColRef { get; set; }
        public string IPFValueColRef { get; set; }
        public List<string> IPFSelColRefs { get; set; }
        public int IPFTSValueColIdx1 { get; set; }
        public int IPFTSValueColIdx2 { get; set; }
        public ResidualMethod IPFResidualMethod { get; set; }
        public DateTime? IPFTSPeriodStartDate { get; set; }
        public DateTime? IPFTSPeriodEndDate { get; set; }

        public IPFZoneSettings()
        {
            InputPath = null;
            IPFXColRef = "1";
            IPFXColRef = "2";
            IPFIDColRef = null;
            IPFValueColRef = null;
            IPFSelColRefs = new List<string>();
            IPFTSValueColIdx1 = 0;
            IPFTSValueColIdx2 = -1;
            IPFResidualMethod = ResidualMethod.None;
            IPFTSPeriodStartDate = null;
            IPFTSPeriodEndDate = null;
        }
    }
}
