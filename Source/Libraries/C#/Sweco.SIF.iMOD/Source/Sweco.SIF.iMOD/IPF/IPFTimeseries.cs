// Sweco.SIF.iMOD is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of Sweco.SIF.iMOD.
// 
// Sweco.SIF.iMOD is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Sweco.SIF.iMOD is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Sweco.SIF.iMOD. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;

namespace Sweco.SIF.iMOD.IPF
{
    /// <summary>
    /// Class for reading, processing and writing TXT-files with timeseries of IPF-files 
    /// </summary>
    public class IPFTimeseries : Timeseries
    {
        /// <summary>
        /// Filename to read or write this timeseries from/to
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// Column names of value columns, excluding timestamp column
        /// </summary>
        public List<string> ColumnNames { get; set; }

        /// <summary>
        /// Default list seperators used for parsing IPF-timeseries textfiles
        /// </summary>
        public static string DefaultListSeperators { get; set; } = "	; ,";

        /// <summary>
        /// Specify if invalid dates in input file should be ignored, i.e. reading of file is continued without throwing an exception
        /// </summary>
        public static bool IsInvalidDateIgnored { get; set; }

        /// <summary>
        /// The timestamps in input IPF-timeseries that are have invalid chronologic order
        /// </summary>
        public List<DateTime> InvalidTimestamps { get; private set; }

        /// <summary>
        /// The values from the first column, for timestamps in input IPF-timeseries that have invalid chronologic order. These are set by while reading an IPF-timeseries file.
        /// </summary>
        public List<float> InvalidValues
        {
            get { return InvalidValueLists[0]; }
        }
        /// <summary>
        /// The valuelists of all columns, for timestamps in input IPF-timeseries that have invalid chronologic order. These are set by while reading an IPF-timeseries file.
        /// </summary>
        public List<List<float>> InvalidValueLists { get; private set; }

        /// <summary>
        /// Describes type of associated files (see iMOD-manual)
        /// </summary>
        public int ITYPE { get; protected set; }

        /// <summary>
        /// Create empty IPFTimeseries object
        /// </summary>
        protected IPFTimeseries() : base()
        {
            InvalidTimestamps = null;
            ColumnNames = new List<string>();
            InvalidValueLists = new List<List<float>>();
            ITYPE = 0;
        }

        /// <summary>
        /// Create IPF-timeseries consisting of one list with timestamps and one list with values
        /// </summary>
        /// <param name="timestamps"></param>
        /// <param name="values"></param>
        /// <param name="valueColumnName">a columnname for the value column, or 'values' when null was specified</param>
        /// <param name="noDataValue"></param>
        public IPFTimeseries(List<DateTime> timestamps, List<float> values, string valueColumnName = null, float noDataValue = float.NaN)
            : base(timestamps, values, noDataValue)
        {
            InvalidTimestamps = null;
            ColumnNames = new List<string>(new string[] { valueColumnName ?? "values" });
            InvalidValueLists = new List<List<float>>();
            ITYPE = 0;
        }

        /// <summary>
        /// Create IPF-timeseries consisting of one list with timestamps and one or more lists with values
        /// </summary>
        /// <param name="timestamps"></param>
        /// <param name="valueLists"></param>
        /// <param name="columnNames">columnnames for the value columns, or 'values' with index when null was specified</param>
        /// <param name="noDataValues"></param>
        public IPFTimeseries(List<DateTime> timestamps, List<List<float>> valueLists, List<string> columnNames = null, List<float> noDataValues = null)
            : base(timestamps, valueLists, noDataValues)
        {
            if (columnNames != null)
            {
                if (columnNames.Count == valueLists.Count)
                {
                    this.ColumnNames = columnNames;
                }
                else
                {
                    throw new Exception("Number of specified columnnames (" + columnNames.Count + ") does not match number of valueColumns (" + valueLists.Count + ")");
                }
            }
            else
            {
                this.ColumnNames = new List<string>();
                for (int colIdx = 0; colIdx < valueLists.Count; colIdx++)
                {
                    this.ColumnNames.Add("values" + (colIdx + 1));
                }
            }

            InvalidTimestamps = null;
            InvalidValueLists = new List<List<float>>();
            ITYPE = 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeseries"></param>
        /// <param name="columnNames"></param>
        public IPFTimeseries(Timeseries timeseries, List<string> columnNames) : base(timeseries.Timestamps, timeseries.ValueColumns, timeseries.NoDataValues)
        {
            InvalidTimestamps = null;
            InvalidValueLists = new List<List<float>>();
            ITYPE = 0;

            if (columnNames != null)
            {
                if ((columnNames == null) && (columnNames.Count != timeseries.ValueColumns.Count))
                {
                    throw new Exception("Number of column names (" + ((columnNames != null) ? columnNames.Count : 0) + ") does not match number of value columns (" + timeseries.ValueColumns.Count);
                }
                this.ColumnNames = new List<string>(columnNames);
            }
            else
            {
                throw new Exception("Column names cannot be null in IPFTimeseries() constructor");
            }
        }

        /// <summary>
        /// Read IPF-timeseries txt-file
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="isCommaCorrected">correct possible comma's (dutch decimal seperator, replace by english decimal seperator) in values when list seperator seems to be a space</param>
        /// <param name="listSeperators">string with possible list seperators, or leave null to use current default listseperators</param>
        /// <returns></returns>
        public static IPFTimeseries ReadFile(string filename, bool isCommaCorrected = true, string listSeperators = null)
        {
            IPFTimeseries ipfTimeseries = null;

            Stream stream = null;
            StreamReader sr = null;
            string line = null;

            try
            {
                if (!File.Exists(filename))
                {
                    throw new Exception("Timeseries-file doesn't exist: " + filename);
                }

                ipfTimeseries = new IPFTimeseries();
                ipfTimeseries.Filename = filename;
                stream = File.OpenRead(filename);
                sr = new StreamReader(stream);

                // Parse first line with number of points
                long timestampCount = 0;
                try
                {
                    line = sr.ReadLine();
                    timestampCount = long.Parse(line.Trim());
                }
                catch (Exception ex)
                {
                    throw new Exception("Could not read number of timestamps in line \"" + line + "\" for timeseries-file " + filename, ex);
                }

                // Parse second lines with number columns
                int columnCount = 0;
                try
                {
                    line = sr.ReadLine();
                    string[] lineValues = line.Trim().Split(new char[] { ',' });
                    if (lineValues.Length == 1)
                    {
                        columnCount = int.Parse(line.Trim());
                        ipfTimeseries.ITYPE = 1;
                    }
                    else
                    {
                        columnCount = int.Parse((lineValues[0].Trim()));
                        ipfTimeseries.ITYPE = int.Parse(lineValues[1].Trim());
                        if ((ipfTimeseries.ITYPE != 1) && (ipfTimeseries.ITYPE != 2))
                        {
                            throw new Exception("ITYPE " + ipfTimeseries.ITYPE + " is currently not supported");
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Could not read number of columns in line \"" + line + "\" for IPF-file " + filename, ex);
                }

                if (listSeperators == null)
                {
                    listSeperators = DefaultListSeperators;
                }

                char[] listSeperatorChars = listSeperators.ToCharArray();
                char[] listSeperatorExCommmaChars = listSeperators.Replace(",", string.Empty).ToCharArray();

                // Parse column and nodata definitions
                string noTimestampString = null;
                if (columnCount > 0)
                {
                    for (int i = 0; i < columnCount; i++)
                    {
                        line = sr.ReadLine().Trim();
                        string[] lineValues = null;
                        if (line.Contains("\""))
                        {
                            lineValues = CommonUtils.SplitQuoted(line, ',', '"', true, true);
                        }
                        else
                        {
                            lineValues = line.Split(listSeperatorChars, StringSplitOptions.RemoveEmptyEntries);
                        }
                        if (lineValues.Length != 2)
                        {
                            throw new Exception("Unexpected number of fields in line " + (2 + i) + " in timeseries-file: " + line);
                        }
                        if (i == 0)
                        {
                            noTimestampString = lineValues[1];
                        }
                        else
                        {
                            if (!float.TryParse(lineValues[1], NumberStyles.Float, englishCultureInfo, out float noValueValue))
                            {
                                throw new Exception("Invalid value used in noValue definition: " + lineValues[1]);
                            }

                            ipfTimeseries.ColumnNames.Add(lineValues[0].Trim());
                            ipfTimeseries.NoDataValues.Add(noValueValue);
                        }
                    }
                }

                List<DateTime> timestamps = new List<DateTime>();
                List<List<float>> valueColumns = new List<List<float>>();
                List<DateTime> invalidTimestamps = new List<DateTime>();
                List<List<float>> invalidValueLists = new List<List<float>>();
                for (int colIdx = 1; colIdx < columnCount; colIdx++)
                {
                    valueColumns.Add(new List<float>());
                    invalidValueLists.Add(new List<float>());
                }
                int timestampIdx = 0;
                while ((!sr.EndOfStream) && (timestampIdx < timestampCount))
                {
                    // Read next line and remove whitespace
                    line = sr.ReadLine().Trim();
                    while (line.Contains("  "))
                    {
                        line = line.Replace("  ", " ");
                    }

                    if (!line.Equals(string.Empty))
                    {
                        timestampIdx++;

                        // Split current line with specified listseperators
                        string[] columnValueStrings = line.Split(listSeperatorChars, StringSplitOptions.RemoveEmptyEntries);

                        if (columnValueStrings.Length != columnCount)
                        {
                            if (isCommaCorrected)
                            {
                                //  try again with default seperator's except comma, and correct comma later on
                                columnValueStrings = line.Split(listSeperatorExCommmaChars, StringSplitOptions.RemoveEmptyEntries);
                            }

                            if (columnValueStrings.Length != columnCount)
                            {
                                throw new Exception("Invalid number of columns in '" + line.Trim() + "', at timestamp-index " + timestampIdx + " in timeseries-file " + Path.GetFileName(filename));
                            }
                        }

                        string timestampString = columnValueStrings[0];
                        if (!timestampString.Equals(noTimestampString))
                        {
                            DateTime timestamp;
                            try
                            {
                                int year = int.Parse(timestampString.Substring(0, 4));
                                int month = int.Parse(timestampString.Substring(4, 2));
                                int day = int.Parse(timestampString.Substring(6, 2));
                                timestamp = new DateTime(year, month, day);
                            }
                            catch (Exception ex)
                            {
                                if (IsInvalidDateIgnored)
                                {
                                    System.Console.WriteLine("Could not parse timestamp '" + timestampString + "' in line " + line, ex);
                                    continue;
                                }
                                else
                                {
                                    throw new Exception("Could not parse timestamp '" + timestampString + "' in line " + line, ex);
                                }
                            }

                            List<float> columnValues = new List<float>();
                            for (int colIdx = 1; colIdx < columnCount; colIdx++)
                            {
                                string valueString = columnValueStrings[colIdx];

                                if (isCommaCorrected)
                                {
                                    valueString = valueString.Replace(",", ".");
                                }

                                float value = float.NaN;
                                try
                                {
                                    value = float.Parse(valueString, englishCultureInfo);
                                }
                                catch (Exception ex)
                                {
                                    throw new Exception("Could not parse value '" + valueString + "' in timestamp/value definition in line " + line, ex);
                                }

                                // Even if the value could be parsed, check for ','-symbols in the valuestring as they may not be read correctly
                                if (valueString.Contains(","))
                                {
                                    if (!valueString.Contains("."))
                                    {
                                        throw new Exception("Value in IPF-timeseries contains ','-symbol instead of english '.'-decimalseperator: " + line);
                                    }
                                }

                                columnValues.Add(value);
                            }

                            // Check for invalid timestamps, that are before previously processed timestamps
                            if ((timestamps.Count() > 0) && (timestamp <= timestamps[timestamps.Count() - 1]))
                            {
                                invalidTimestamps.Add(timestamp);
                                for (int colIdx = 1; colIdx < columnCount; colIdx++)
                                {
                                    invalidValueLists[colIdx - 1].Add(columnValues[colIdx - 1]);
                                }
                            }
                            else
                            {
                                timestamps.Add(timestamp);
                                for (int colIdx = 1; colIdx < columnCount; colIdx++)
                                {
                                    valueColumns[colIdx - 1].Add(columnValues[colIdx - 1]);
                                }
                            }
                        }
                    }
                }
                if (timestampIdx != timestampCount)
                {
                    throw new Exception("EOF found before last timestamp: " + timestampIdx + "/" + timestampCount + " timestamps processed");
                }

                ipfTimeseries.Timestamps = timestamps;
                ipfTimeseries.ValueColumns = valueColumns;
                ipfTimeseries.InvalidTimestamps = invalidTimestamps;
                ipfTimeseries.InvalidValueLists = invalidValueLists;
            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error while reading " + filename, ex);
            }
            finally
            {
                if (sr != null)
                {
                    sr.Close();
                }
            }

            return ipfTimeseries;
        }

        /// <summary>
        /// Check if invalid timestamps were found during reading IPF-timeseries
        /// </summary>
        /// <returns></returns>
        public bool HasInvalidTimestamps()
        {
            return ((InvalidTimestamps != null) && (InvalidTimestamps.Count() > 0));
        }

        /// <summary>
        /// Check if specified timestamp is in expected format for IPF-timeseries
        /// </summary>
        /// <param name="timestampString"></param>
        /// <returns></returns>
        public static bool IsIPFTimestampString(string timestampString)
        {
            if ((timestampString == null) || timestampString.Length != 8)
            {
                return false;
            }

            try
            {
                int year = int.Parse(timestampString.Substring(0, 4));
                int month = int.Parse(timestampString.Substring(4, 2));
                int day = int.Parse(timestampString.Substring(6, 2));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Parse string that is in IPF-timeseries format yyyymmdd
        /// </summary>
        /// <param name="timestampString"></param>
        /// <returns></returns>
        public static DateTime ParseIPFTimestampString(string timestampString)
        {
            try
            {
                int year = int.Parse(timestampString.Substring(0, 4));
                int month = int.Parse(timestampString.Substring(4, 2));
                int day = int.Parse(timestampString.Substring(6, 2));
                return new DateTime(year, month, day);
            }
            catch (Exception ex)
            {
                throw new Exception("Could not parse timestamp string '" + timestampString + ". Expected format: yyyymmdd", ex);
            }
        }

        /// <summary>
        /// Write timeseries, excluding invalid timestamps, to specifed file
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="decimalCount">Number of decimals for non-NoData-values when writing timeseries file. Use -1 to keep all decimals</param>
        public void WriteFile(string filename, int decimalCount = -1)
        {
            if ((Timestamps != null) && (Timestamps.Count() > 0))
            {
                // Check that timestamp and valuelists have equal number of items
                for (int valueListIdx = 0; valueListIdx < this.ValueColumns.Count; valueListIdx++)
                {
                    if ((ValueColumns[valueListIdx] == null) || (ValueColumns[valueListIdx].Count != Timestamps.Count))
                    {
                        throw new Exception("Number of timestamps " + Timestamps.Count + ") doesn't match number of values (" + ((ValueColumns[valueListIdx] == null) ? "null" : ValueColumns[valueListIdx].Count.ToString()) + ") in valuelist index " + valueListIdx + " for timeseriesfile: " + Path.GetFileName(filename));
                    }
                }
                // Check that other definitions for value columns have equal count
                if ((NoDataValues.Count != ValueColumns.Count))
                {
                    throw new Exception("Number of NoData-values (" + NoDataValues.Count + ") doesn't match number of valueLists (" + ValueColumns.Count + ") for timeseriesfile: " + Path.GetFileName(filename));
                }
                if ((ColumnNames.Count != ValueColumns.Count))
                {
                    throw new Exception("Number of column names (" + ColumnNames.Count + ") doesn't match number of valueLists (" + ValueColumns.Count + ") for timeseriesfile: " + Path.GetFileName(filename));
                }

                StreamWriter sw = null;
                try
                {
                    sw = new StreamWriter(filename, false);

                    // Write first line with number of timestamps
                    sw.WriteLine(Timestamps.Count());

                    // Write second lines with number columns (including timestamp column)
                    sw.WriteLine(ColumnNames.Count + 1);

                    // Write column definitions
                    sw.WriteLine("Date,-99999");
                    for (int colIdx = 0; colIdx < ColumnNames.Count; colIdx++)
                    {
                        sw.WriteLine(ColumnNames[colIdx] + "," + NoDataValues[colIdx].ToString(englishCultureInfo));
                    }

                    // Check if timestamps have hour/second resolution
                    bool hasHourResolution = false;
                    for (int timestampIdx = 0; timestampIdx < Timestamps.Count(); timestampIdx++)
                    {
                        DateTime timestamp = Timestamps[timestampIdx];
                        if ((timestamp.Hour != 0) || (timestamp.Minute != 0) || (timestamp.Second != 0))
                        {
                            hasHourResolution = true;
                            break;
                        }
                    }

                    // Write timestamps/values
                    for (int timestampIdx = 0; timestampIdx < Timestamps.Count(); timestampIdx++)
                    {
                        DateTime timestamp = Timestamps[timestampIdx];
                        string rowString = timestamp.Year.ToString() + timestamp.Month.ToString("D2") + timestamp.Day.ToString("D2");
                        if (hasHourResolution)
                        {
                            rowString += timestamp.Hour.ToString("D2") + timestamp.Second.ToString("D2") + timestamp.Minute.ToString("D2");
                        }
                        for (int colIdx = 0; colIdx < ValueColumns.Count; colIdx++)
                        {
                            float value = ValueColumns[colIdx][timestampIdx];
                            if (value.Equals(float.NaN) || value.Equals(NoDataValues[colIdx]))
                            {
                                rowString += "," + NoDataValues[colIdx].ToString(englishCultureInfo);
                            }
                            else
                            {
                                if (decimalCount >= 0)
                                {
                                    rowString += "," + Math.Round(value, decimalCount).ToString("F" + decimalCount, englishCultureInfo);
                                }
                                else
                                {
                                    rowString += "," + value.ToString(englishCultureInfo);
                                }
                            }
                        }
                        sw.WriteLine(rowString);
                    }
                    this.Filename = filename;
                }
                catch (IOException ex)
                {
                    if (ex.Message.ToLower().Contains("access") || ex.Message.ToLower().Contains("toegang"))
                    {
                        throw new ToolException("Timeseries file cannot be written, because it is being used by another process: " + Filename);
                    }
                    else
                    {
                        throw new Exception("Unexpected error while writing timeseries file: " + Filename, ex);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Unexpected error while writing timeseries file: " + Filename, ex);
                }
                finally
                {
                    if (sw != null)
                    {
                        sw.Close();
                    }
                }
            }
        }

        /// <summary>
        /// Selects values for specified column in specified period
        /// </summary>
        /// <param name="startMonth"></param>
        /// <param name="startDay"></param>
        /// <param name="endMonth"></param>
        /// <param name="endDay"></param>
        /// <param name="valueColIdx">zero based column index, use -1 to retrieve all value columns</param>
        /// <returns></returns>
        public new IPFTimeseries Select(int startMonth, int startDay, int endMonth, int endDay, int valueColIdx = 0)
        {
            List<string> selColumnNames = new List<string>();
            if (valueColIdx == -1)
            {
                for (int colIdx = 0; colIdx < ColumnNames.Count; colIdx++)
                {
                    selColumnNames.Add(ColumnNames[colIdx]);
                }
            }
            else
            {
                if (valueColIdx < ColumnNames.Count)
                {
                    selColumnNames.Add(ColumnNames[valueColIdx]);
                }
                else
                {
                    throw new Exception("Invalid value column index (larger or equal to value column count): " + valueColIdx);
                }
            }

            return new IPFTimeseries(base.Select(startMonth, startDay, endMonth, endDay, valueColIdx), selColumnNames);
        }

        /// <summary>
        /// Selects values for specified dates
        /// </summary>
        /// <param name="valueColIdx">zero based column index, use -1 to retrieve all value columns</param>
        /// <returns></returns>
        /// <param name="dates"></param>
        public new IPFTimeseries Select(List<DateTime> dates, int valueColIdx = 0)
        {
            List<string> selColumnNames = new List<string>();
            if (valueColIdx == -1)
            {
                for (int colIdx = 0; colIdx < ColumnNames.Count; colIdx++)
                {
                    selColumnNames.Add(ColumnNames[colIdx]);
                }
            }
            else
            {
                if (valueColIdx < ColumnNames.Count)
                {
                    selColumnNames.Add(ColumnNames[valueColIdx]);
                }
                else
                {
                    throw new Exception("Invalid value column index (larger or equal to value column count): " + valueColIdx);
                }
            }

            return new IPFTimeseries(base.Select(dates, valueColIdx), selColumnNames);
        }

        /// <summary>
        /// Select date/value-pairs with value between specified min/max
        /// </summary>
        /// <param name="minValue">minValue, use float.NaN to ignore minValue</param>
        /// <param name="maxValue">maxValue, use float.NaN to ignore maxValue</param>
        /// <param name="valueColIdx">zero based column index</param>
        /// <returns></returns>
        public new IPFTimeseries Select(float minValue, float maxValue, int valueColIdx = 0)
        {
            List<string> selColumnNames = new List<string>();
            if (valueColIdx < ColumnNames.Count)
            {
                selColumnNames.Add(ColumnNames[valueColIdx]);
            }
            else
            {
                throw new Exception("Invalid value column index (larger or equal to value column count): " + valueColIdx);
            }

            return new IPFTimeseries(base.Select(minValue, maxValue, valueColIdx), selColumnNames);
        }

        /// <summary>
        /// Selects values for specified column in specified period
        /// </summary>
        /// <param name="fromDate"></param>
        /// <param name="toDate"></param>
        /// <param name="valueColIdx">zero based column index, use -1 to retrieve all value columns</param>
        /// <returns></returns>
        public new IPFTimeseries Select(DateTime? fromDate = null, DateTime? toDate = null, int valueColIdx = -1)
        {
            List<string> selColumnNames = new List<string>();
            if (valueColIdx == -1)
            {
                for (int colIdx = 0; colIdx < ColumnNames.Count; colIdx++)
                {
                    selColumnNames.Add(ColumnNames[colIdx]);
                }
            }
            else
            {
                if (valueColIdx < ColumnNames.Count)
                {
                    selColumnNames.Add(ColumnNames[valueColIdx]);
                }
                else
                {
                    throw new Exception("Invalid value column index (larger or equal to value column count): " + valueColIdx);
                }
            }

            return new IPFTimeseries(base.Select(fromDate, toDate, valueColIdx), selColumnNames);
        }
    }
}
