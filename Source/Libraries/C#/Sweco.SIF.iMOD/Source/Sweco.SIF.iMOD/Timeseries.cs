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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;
using Sweco.SIF.iMOD.Utils;

namespace Sweco.SIF.iMOD
{
    public enum SequenceMethod
    {
        /// <summary>
        /// The length of a sequence is measured as the length of the period between first and last date in the sequence
        /// </summary>
        Period,
        /// <summary>
        /// The length of a sequence is measured as the number of dates/values in the sequence
        /// </summary>
        Count
    }

    public enum TimeStampResolution
    {
        /// <summary>
        /// Day resolution (i.e. no time component)
        /// </summary>
        Day
    }

    /// <summary>
    /// Avaiable methods for resampling timeseries (changing frequency)
    /// </summary>
    public enum ResampleMethod
    {
        /// <summary>
        /// Take first timestamp when multiple timestamps are present that are equal for the specified resolution
        /// </summary>
        First,
        /// <summary>
        /// Take first non-NoData timestamp when multiple timestamps are present that are equal for the specified resolution
        /// </summary>
        FirstNonNoData,
        ///// <summary>
        ///// Use mean of values within sampled timespan
        ///// </summary>
        //Mean,
        ///// <summary>
        ///// Use minimum of values within sampled timespan
        ///// </summary>
        //Min,
        ///// <summary>
        ///// Use maximum of values within sampled timespan
        ///// </summary>
        //Max
    }

    /// <summary>
    /// Class for storing and processing timeseries data. One column of timestamps can be stored together with one or more value columns.
    /// </summary>
    public class Timeseries
    {
        // Note: this implementation stores timestamps and values in different arrays to allow new value series/columns to be added easil

        /// <summary>
        /// Default value for NoData
        /// </summary>
        public const float DefaultNoDataValue = -99999f;

        /// <summary>
        /// Language definition for english culture as used in SIFToolSettings
        /// </summary>
        protected static CultureInfo englishCultureInfo = new CultureInfo("en-GB", false);

        /// <summary>
        /// List of all timestamps (dates/times) for this timeseries
        /// </summary>
        public List<DateTime> Timestamps { get; set; }

        /// <summary>
        /// List with defined value columns (series), with each column (serie) a list on itself. 
        /// Used for storing one or more columns (series) of values that correspond with the timestamps in the timeseries
        /// </summary>
        public List<List<float>> ValueColumns { get; set; }

        /// <summary>
        /// Retrieve values from first value series
        /// </summary>
        public List<float> Values
        {
            get { return ValueColumns[0]; }
        }

        /// <summary>
        /// NoData-values of each value column (series). Note: changing this value does NOT change the current NoData values.
        /// </summary>
        public List<float> NoDataValues { get; set; }

        /// <summary>
        /// NoData-value of first value column (series)
        /// </summary>
        public float NoDataValue
        {
            get { return NoDataValues[0]; }
            set
            {
                if (NoDataValues == null)
                {
                    NoDataValues = new List<float>();
                }

                if (NoDataValues.Count == 0)
                {
                    NoDataValues.Add(value);
                }
                else
                {
                    NoDataValues[0] = value;
                }
            }
        }

        /// <summary>
        /// Creates an empty Timeseries object
        /// </summary>
        protected Timeseries()
        {
            Timestamps = null;
            ValueColumns = new List<List<float>>();
            NoDataValues = new List<float>();
        }

        /// <summary>
        /// Create timeseries object consisting of a timestamp and single value series 
        /// </summary>
        /// <param name="timestamps"></param>
        /// <param name="values"></param>
        /// <param name="noDataValue">use float.NaN to take DefaultNoDataValue</param>
        public Timeseries(List<DateTime> timestamps, List<float> values, float noDataValue = float.NaN)
        {
            if (values.Count != timestamps.Count)
            {
                throw new ToolException("Number of values (" + values.Count + ") does not equal number of timestamps (" + timestamps.Count + ")");
            }
            this.Timestamps = timestamps;
            this.ValueColumns = new List<List<float>>();
            this.ValueColumns.Add(values);
            this.NoDataValues = new List<float>();
            this.NoDataValues.Add(!noDataValue.Equals(float.NaN) ? noDataValue : DefaultNoDataValue);
        }

        /// <summary>
        /// Create timeseries object consisting of a timestamp series and multiple value series 
        /// </summary>
        /// <param name="timestamps"></param>
        /// <param name="valueColumns"></param>
        /// <param name="noDataValues">a list of noDataValues, or null to use float.NaN for all valueList</param>
        public Timeseries(List<DateTime> timestamps, List<List<float>> valueColumns, List<float> noDataValues = null)
        {
            if ((valueColumns == null) || (valueColumns.Count == 0))
            {
                throw new Exception("At least one list of values should be specified");
            }
            for (int colIdx = 0; colIdx < valueColumns.Count; colIdx++)
            {
                if (valueColumns[colIdx].Count != timestamps.Count)
                {
                    throw new ToolException("Number of values in list at index " + colIdx + "(" + valueColumns[colIdx].Count + ") does not equal number of timestamps (" + timestamps.Count + ")");
                }
            }
            if ((noDataValues != null) && (noDataValues.Count != valueColumns.Count))
            {
                throw new ToolException("Number of noDataValues (" + noDataValues.Count + ") does not equal number of values (" + valueColumns.Count + ")");
            }

            this.Timestamps = timestamps;
            this.ValueColumns = valueColumns;
            if (noDataValues == null)
            {
                this.NoDataValues = new List<float>();
                for (int colIdx = 0; colIdx < valueColumns.Count; colIdx++)
                {
                    this.NoDataValues.Add(DefaultNoDataValue);
                }
            }
            else
            {
                this.NoDataValues = noDataValues;
            }
        }

        /// <summary>
        /// Calculates average values over all non-data values in specified column
        /// </summary>
        /// <param name="valueColIdx">zero-based column index</param>
        /// <returns></returns>
        public float CalculateAverage(int valueColIdx = 0)
        {
            if (valueColIdx >= ValueColumns.Count)
            {
                throw new Exception("No timeseries values defined for column index " + valueColIdx);
            }
            List<float> selectedColValues = ValueColumns[valueColIdx];
            float noDataValue = NoDataValues[valueColIdx];

            float valueSum = 0;
            long valueCount = 0;
            foreach (float value in selectedColValues)
            {
                if (!value.Equals(noDataValue))
                {
                    valueSum += value;
                    valueCount++;
                }
            }
            return valueSum / valueCount;
        }

        /// <summary>
        /// Retrieve value in specified column at specified date 
        /// </summary>
        /// <param name="date"></param>
        /// <param name="valueColIdx">zero-based column index</param>
        /// <returns></returns>
        public float GetValue(DateTime date, int valueColIdx = 0)
        {
            if (valueColIdx >= ValueColumns.Count)
            {
                throw new Exception("No timeseries values defined for column index " + valueColIdx);
            }
            List<float> selectedColValues = ValueColumns[valueColIdx];
            float noDataValue = NoDataValues[valueColIdx];

            int dateIdx = 0;
            while ((dateIdx < Timestamps.Count() - 1) && (Timestamps[dateIdx + 1] < date))
            {
                dateIdx++;
            }

            if ((dateIdx < (Timestamps.Count() - 1)) && Timestamps[dateIdx + 1].Equals(date))
            {
                return selectedColValues[dateIdx + 1];
            }
            else if (Timestamps[dateIdx] <= date)
            {
                return selectedColValues[dateIdx];
            }
            else
            {
                return float.NaN;
            }
        }

        /// <summary>
        /// Selects dates/values for specified value column in specified period
        /// </summary>
        /// <param name="fromDate">first date of selection period</param>
        /// <param name="toDate">last date of selection period</param>
        /// <param name="valueColIdx">zero based column index, use -1 to retrieve all value columns</param>
        /// <param name="excludeSurroundingValues">if false, add value before and after specified period if fromDate and toDate are not existing</param>
        /// <returns></returns>
        public Timeseries Select(DateTime? fromDate, DateTime? toDate, int valueColIdx, bool excludeSurroundingValues = true)
        {
            if (valueColIdx >= ValueColumns.Count)
            {
                throw new Exception("No timeseries values defined for column index " + valueColIdx);
            }

            if (fromDate == null)
            {
                fromDate = Timestamps[0];
            }
            if (toDate == null)
            {
                toDate = Timestamps[Timestamps.Count() - 1];
            }

            int fromIdx = -1;
            int dateIdx = 0;
            // find index for fromDate
            while ((dateIdx < Timestamps.Count()) && (fromIdx < 0))
            {
                if (Timestamps[dateIdx].Equals(fromDate))
                {
                    fromIdx = dateIdx;
                }
                else if (Timestamps[dateIdx] > fromDate)
                {
                    // if excludeSurroundingValues, use next value if not equal, otherwise use previous date 
                    fromIdx = excludeSurroundingValues ? dateIdx : dateIdx - 1;
                    if (fromIdx < 0)
                    {
                        fromIdx = 0;
                    }
                }
                dateIdx++;
            }
            if (fromIdx < 0)
            {
                // No dates found, return dummy, empty result
                List<DateTime> noDates = new List<DateTime>();
                Timeseries dummyTs = null;

                if (valueColIdx == -1)
                {
                    List<List<float>> noValueLists = new List<List<float>>();
                    List<float> noDataValueList = new List<float>();
                    for (int colIdx = 0; colIdx < this.ValueColumns.Count; colIdx++)
                    {
                        // Copy NoData-values per valuecolumn
                        noDataValueList.Add(NoDataValues[colIdx]);

                        // Add empty value list per value column
                        noValueLists.Add(new List<float>());
                    }

                    dummyTs = new Timeseries(noDates, noValueLists, noDataValueList);
                }
                else
                {
                    List<float> noValues = new List<float>();
                    dummyTs = new Timeseries(noDates, noValues);
                }
                return dummyTs;
            }

            // find index for toData
            int toIdx = -1;
            while ((dateIdx < Timestamps.Count()) && (toIdx < 0))
            {
                if (Timestamps[dateIdx].Equals(toDate))
                {
                    toIdx = dateIdx;
                }
                else if (Timestamps[dateIdx] > toDate)
                {
                    // if excludeSurroundingValues, use previous date if not equal, otherwise use next date
                    toIdx = excludeSurroundingValues ? (dateIdx - 1) : dateIdx;
                    if (toIdx < 0)
                    {
                        toIdx = 0;
                    }
                }
                dateIdx++;
            }
            if (toIdx < 0)
            {
                // specified toDate is not found, use last date available
                toIdx = Timestamps.Count() - 1;
            }

            List<int> selValueColIndices = new List<int>();
            if (valueColIdx == -1)
            {
                for (int colIdx = 0; colIdx < this.ValueColumns.Count; colIdx++)
                {
                    selValueColIndices.Add(colIdx);
                }
            }
            else
            {
                selValueColIndices.Add(valueColIdx);
            }

            // Select dates;
            List<DateTime> selectedDates = Timestamps.GetRange(fromIdx, toIdx - fromIdx + 1);

            // Select values for all specified value columns;
            List<List<float>> selectedValueColumns = new List<List<float>>();
            List<float> selectedNoDataValues = new List<float>();
            foreach (int selValueColIdx in selValueColIndices)
            {
                List<float> valueList = ValueColumns[selValueColIdx];
                float noDataValue = NoDataValues[selValueColIdx];

                List<float> selectedValueList = valueList.GetRange(fromIdx, toIdx - fromIdx + 1);
                selectedValueColumns.Add(selectedValueList);
                selectedNoDataValues.Add(noDataValue);
            }

            Timeseries selectedTimeseries = new Timeseries(selectedDates, selectedValueColumns, selectedNoDataValues);

            return selectedTimeseries;
        }

        ///// <summary>
        ///// Selects values for specified column in specified period. When specified timestamps are not found, existing timestamps just before and after are selected as well.
        ///// </summary>
        ///// <param name="startTimestamp"></param>
        ///// <param name="endTimestamp"></param>
        ///// <param name="valueColIdx">zero-based column index</param>
        ///// <returns></returns>
        //public Timeseries Select(DateTime? startTimestamp = null, DateTime? endTimestamp = null, int valueColIdx = 0)
        //{
        //    if (valueColIdx >= ValueColumns.Count)
        //    {
        //        throw new Exception("No timeseries values defined for column index " + valueColIdx);
        //    }

        //    if (startTimestamp == null)
        //    {
        //        startTimestamp = timestamps[0];
        //    }
        //    if (endTimestamp == null)
        //    {
        //        endTimestamp = timestamps[timestamps.Count() - 1];
        //    }

        //    int fromIdx = -1;
        //    int dateIdx = 0;
        //    // find index for fromDate
        //    while ((dateIdx < timestamps.Count()) && (fromIdx < 0))
        //    {
        //        if (timestamps[dateIdx].Equals(startTimestamp))
        //        {
        //            fromIdx = dateIdx;
        //        }
        //        else if (timestamps[dateIdx] > startTimestamp)
        //        {
        //            // Use previous date if not equal
        //            fromIdx = dateIdx - 1;
        //            if (fromIdx < 0)
        //            {
        //                fromIdx = 0;
        //            }
        //        }
        //        dateIdx++;
        //    }
        //    if (fromIdx < 0)
        //    {
        //        List<DateTime> noDates = new List<DateTime>();
        //        List<float> noValues = new List<float>();
        //        Timeseries dummyTs = new Timeseries(noDates, noValues);
        //        return dummyTs;
        //    }

        //    // find index for endTimestamp
        //    int toIdx = -1;
        //    while ((dateIdx < timestamps.Count()) && (toIdx < 0))
        //    {
        //        if (timestamps[dateIdx].Equals(endTimestamp))
        //        {
        //            toIdx = dateIdx;
        //        }
        //        else if (timestamps[dateIdx] > endTimestamp)
        //        {
        //            // Use previous date if not equal
        //            toIdx = dateIdx - 1;
        //            if (toIdx < 0)
        //            {
        //                toIdx = 0;
        //            }
        //        }
        //        dateIdx++;
        //    }
        //    if (toIdx < 0)
        //    {
        //        toIdx = timestamps.Count() - 1;
        //    }

        //    List<int> selValueColIndices = new List<int>();
        //    if (valueColIdx == -1)
        //    {
        //        for (int colIdx = 0; colIdx < this.ValueColumns.Count; colIdx++)
        //        {
        //            selValueColIndices.Add(colIdx);
        //        }
        //    }
        //    else
        //    {
        //        selValueColIndices.Add(valueColIdx);
        //    }

        //    // Select dates;
        //    List<DateTime> selectedDates = timestamps.GetRange(fromIdx, toIdx - fromIdx + 1);

        //    // Select values for all specified value columns;
        //    List<List<float>> selectedValueLists = new List<List<float>>();
        //    List<float> selectedNoDataValues = new List<float>();
        //    foreach (int selValueColIdx in selValueColIndices)
        //    {
        //        List<float> valueList = ValueColumns[selValueColIdx];
        //        float noDataValue = NoDataValues[selValueColIdx];

        //        List<float> selectedValueList = valueList.GetRange(fromIdx, toIdx - fromIdx + 1);
        //        selectedValueLists.Add(selectedValueList);
        //        selectedNoDataValues.Add(noDataValue);
        //    }

        //    Timeseries selectedTimeseries = new Timeseries(selectedDates, selectedValueLists, selectedNoDataValues);

        //    return selectedTimeseries;
        //}

        /// <summary>
        /// Select timestamp/value-pairs with value between specified min/max
        /// </summary>
        /// <param name="minValue">minValue, use float.NaN to ignore minValue</param>
        /// <param name="maxValue">maxValue, use float.NaN to ignore maxValue</param>
        /// <param name="valueColIdx">zero-based column index</param>
        /// <param name="isInverted">if true, timestamps outside specified range are selected</param>
        /// <param name="includeNoData">if true, NoData-values are returned in the result</param>
        /// <returns></returns>
        public Timeseries Select(float minValue, float maxValue, int valueColIdx = 0, bool isInverted = false, bool includeNoData = true)
        {
            if (valueColIdx >= ValueColumns.Count)
            {
                throw new Exception("No timeseries values defined for column index " + valueColIdx);
            }
            List<float> selectedColValues = ValueColumns[valueColIdx];
            float noDataValue = NoDataValues[valueColIdx];

            List<DateTime> selectedTimestamps = new List<DateTime>();
            List<float> selectedValues = new List<float>();

            if (minValue.Equals(float.NaN))
            {
                minValue = float.MinValue;
            }
            if (maxValue.Equals(float.NaN))
            {
                maxValue = float.MaxValue;
            }

            if (isInverted)
            {
                // Select timestamps outside specified range (note: source code of loop is copied for faster computation)
                for (int dateIdx = 0; dateIdx < Timestamps.Count(); dateIdx++)
                {
                    float value = selectedColValues[dateIdx];

                    if (value.Equals(noDataValue))
                    {
                        if (includeNoData)
                        {
                            selectedTimestamps.Add(Timestamps[dateIdx]);
                            selectedValues.Add(value);
                        }
                    }
                    else if ((value < minValue) || (value > maxValue))
                    {
                        selectedTimestamps.Add(Timestamps[dateIdx]);
                        selectedValues.Add(value);
                    }
                }
            }
            else
            {
                for (int dateIdx = 0; dateIdx < Timestamps.Count(); dateIdx++)
                {
                    float value = selectedColValues[dateIdx];
                    if (value.Equals(noDataValue))
                    {
                        if (includeNoData)
                        {
                            selectedTimestamps.Add(Timestamps[dateIdx]);
                            selectedValues.Add(value);
                        }
                    }
                    else if ((value >= minValue) && (value <= maxValue))
                    {
                        selectedTimestamps.Add(Timestamps[dateIdx]);
                        selectedValues.Add(value);
                    }
                }
            }

            Timeseries result = new Timeseries(selectedTimestamps, selectedValues, noDataValue);
            return result;
        }


        /// <summary>
        /// Select dates/values from this timeseries that are different from the other timeseries. 
        /// Only select sequences with specified minimum length without (differing) intermediate dates/values from timeseries 2. Equal dates or dates unique to TS2 are skipped.
        /// When <paramref name="minSeqLength"/>=0, the check for sequences is skipped and also equal dates will be selected when values are different.
        /// When <paramref name="minSeqLength"/>=1, only unique TS1-dates will be selected.
        /// </summary>
        /// <param name="ts2"></param>
        /// <param name="valueColIdx1">zero based column index of this TS for value column to process</param>
        /// <param name="valueColIdx2">zero based column index of TS2 for value column to process</param>
        /// <param name="valueTolerance">acceptable difference for equal values</param>
        /// <param name="minSeqLength">minimum length of TS1-sequence without intermediate TS2-dates; use 0 to ignore; use -1 to calculate exclusive difference</param>
        /// <param name="seqMethod">method for calculating length of sequence</param>
        /// <returns></returns>
        public Timeseries RetrieveDifference(Timeseries ts2, int valueColIdx1, int valueColIdx2, float valueTolerance, int minSeqLength, SequenceMethod seqMethod)
        {
            Timeseries selectedTimeseries = null;
            if (ts2 == null)
            {
                selectedTimeseries = new Timeseries(Timestamps, ValueColumns[valueColIdx1], NoDataValues[valueColIdx1]);
                return selectedTimeseries;
            }

            if ((valueColIdx1 < 0) || valueColIdx1 >= ValueColumns.Count)
            {
                throw new Exception("No timeseries values defined for column index 1" + valueColIdx1);
            }
            if ((valueColIdx2 < 0) || valueColIdx2 >= ts2.ValueColumns.Count)
            {
                throw new Exception("No timeseries values defined for column index 2" + valueColIdx2);
            }

            List<DateTime> timestamps2 = ts2.Timestamps;
            List<float> valueColumns1 = ValueColumns[valueColIdx1];
            List<float> valueColumns2 = ts2.ValueColumns[valueColIdx2];

            List<DateTime> selectedTimestamps = new List<DateTime>();
            List<float> selectedValues = new List<float>();
            List<DateTime> currDateSequence = new List<DateTime>();
            List<float> currValueSequence = new List<float>();
            DateTime? minSeqDate = null;
            DateTime? maxSeqDate = null;
            int seqCount = 0;
            float selectedNoDataValue = this.NoDataValues[valueColIdx1];

            int dateIdx1 = 0;
            int dateIdx2 = 0;
            if (minSeqLength >= 0)
            {
                while (dateIdx1 < Timestamps.Count)
                {
                    DateTime date1 = Timestamps[dateIdx1];
                    float value1 = valueColumns1[dateIdx1];
                    DateTime? date2 = null;
                    if (dateIdx2 < timestamps2.Count)
                    {
                        date2 = timestamps2[dateIdx2];
                    }


                    // Store all TS1-dates/values before TS2-date
                    while ((dateIdx1 < Timestamps.Count) && ((date2 == null) || (date1 < date2)))
                    {
                        currDateSequence.Add(date1);
                        currValueSequence.Add(value1);
                        if (minSeqDate == null)
                        {
                            minSeqDate = date1;
                        }
                        maxSeqDate = date1;
                        seqCount++;
                        dateIdx1++;
                        if (dateIdx1 < Timestamps.Count)
                        {
                            date1 = Timestamps[dateIdx1];
                            value1 = valueColumns1[dateIdx1];
                        }
                    }
                    if ((date2 != null) && date1.Equals(date2))
                    {
                        // date2 is equal to date1, compare values
                        float value2 = valueColumns2[dateIdx2];
                        if ((minSeqLength == 0) && (Math.Abs(value2 - value1) > valueTolerance))
                        {
                            // There is a difference between both series (larger than tolerance), keep marking TS1-dates as part of the sequence
                            currDateSequence.Add(date1);
                            currValueSequence.Add(value1);
                            maxSeqDate = date1;
                            seqCount++;
                        }
                        else
                        {
                            // An (intermediate) equal date from series 2 was found, so this is not a unique date for TS1, check sequence length before storing them
                            if (HasMinSeqLength(seqMethod, minSeqLength, seqCount, minSeqDate, maxSeqDate))
                            {
                                selectedTimestamps.AddRange(currDateSequence);
                                selectedValues.AddRange(currValueSequence);
                            }
                            currDateSequence.Clear();
                            currValueSequence.Clear();
                            minSeqDate = null;
                            maxSeqDate = null;
                            seqCount = 0;
                        }
                        dateIdx1++;
                        dateIdx2++;
                    }
                    else
                    {
                        // date2 is before date1, read dates from series 2 until equal to or after date1
                        while (((date2 != null) && (date2 < date1)) && (dateIdx2 < timestamps2.Count))
                        {
                            dateIdx2++;
                            if (dateIdx2 < timestamps2.Count)
                            {
                                date2 = timestamps2[dateIdx2];
                            }
                            // skip date2/value2 in result
                        }

                        // One or more (intermediate) dates from series 2 were found, check sequence length before storing them
                        if (HasMinSeqLength(seqMethod, minSeqLength, seqCount, minSeqDate, maxSeqDate))
                        {
                            selectedTimestamps.AddRange(currDateSequence);
                            selectedValues.AddRange(currValueSequence);
                        }
                        currDateSequence.Clear();
                        currValueSequence.Clear();
                        minSeqDate = null;
                        maxSeqDate = null;
                        seqCount = 0;
                    }
                }
                // Add remaining sequence if large enough
                if (HasMinSeqLength(seqMethod, minSeqLength, seqCount, minSeqDate, maxSeqDate))
                {
                    selectedTimestamps.AddRange(currDateSequence);
                    selectedValues.AddRange(currValueSequence);
                }
            }
            else
            {
                // Exclusive difference is requested: select dates from series 1 that are outside complete period of series 2
                DateTime firstDate2 = timestamps2[0];
                DateTime lastDate2 = timestamps2[timestamps2.Count - 1];

                while (dateIdx1 < Timestamps.Count)
                {
                    DateTime date1 = Timestamps[dateIdx1];
                    if ((date1 < firstDate2) || (date1 > lastDate2))
                    {
                        selectedTimestamps.Add(date1);
                        selectedValues.Add(valueColumns1[dateIdx1]);
                    }

                    dateIdx1++;
                }
            }

            selectedTimeseries = new Timeseries(selectedTimestamps, selectedValues, selectedNoDataValue);

            return selectedTimeseries;
        }

        private bool HasMinSeqLength(SequenceMethod seqMethod, int minSeqLength, int seqCount, DateTime? minSeqDate, DateTime? maxSeqDate)
        {
            return ((seqMethod == SequenceMethod.Count) && (seqCount >= minSeqLength))
                || ((seqMethod == SequenceMethod.Period) && (maxSeqDate != null) && ((((DateTime)maxSeqDate).Subtract((DateTime)minSeqDate).Days + 1) >= minSeqLength));
        }

        /// <summary>
        /// Select dates/values from this timeseries that are different from the other timeseries. Equal dates or dates unique to TS2 are skipped.
        /// </summary>
        /// <param name="ts2"></param>
        /// <param name="valueColIdx1">zero based column index of this TS for value column to process</param>
        /// <param name="valueColIdx2">zero based column index of TS2 for value column to process</param>
        /// <param name="valueTolerance">acceptable difference for equal values</param>
        /// <returns></returns>
        public Timeseries RetrieveDifference(Timeseries ts2, int valueColIdx1 = 0, int valueColIdx2 = 0, float valueTolerance = 0)
        {
            Timeseries selectedTimeseries = null;
            if (ts2 == null)
            {
                selectedTimeseries = new Timeseries(Timestamps, ValueColumns[valueColIdx1], NoDataValues[valueColIdx1]);
                return selectedTimeseries;
            }

            if ((valueColIdx1 < 0) || valueColIdx1 >= ValueColumns.Count)
            {
                throw new Exception("No timeseries values defined for column index 1" + valueColIdx1);
            }
            if ((valueColIdx2 < 0) || valueColIdx2 >= ts2.ValueColumns.Count)
            {
                throw new Exception("No timeseries values defined for column index 2" + valueColIdx2);
            }

            List<DateTime> dateList2 = ts2.Timestamps;
            List<float> valueList1 = ValueColumns[valueColIdx1];
            List<float> valueList2 = ts2.ValueColumns[valueColIdx2];

            List<DateTime> selectedDates = new List<DateTime>();
            List<float> selectedValues = new List<float>();
            float selectedNoDataValue = this.NoDataValues[valueColIdx1];

            int dateIdx1 = 0;
            int dateIdx2 = 0;
            while (dateIdx1 < Timestamps.Count)
            {
                DateTime date1 = Timestamps[dateIdx1];
                DateTime? date2 = null;
                if (dateIdx2 < dateList2.Count)
                {
                    date2 = dateList2[dateIdx2];
                }
                float value1 = valueList1[dateIdx1];
                while (((date2 == null) || (date1 < date2)) && (dateIdx1 < Timestamps.Count))
                {
                    selectedDates.Add(date1);
                    selectedValues.Add(value1);
                    dateIdx1++;
                    if (dateIdx1 < Timestamps.Count)
                    {
                        date1 = Timestamps[dateIdx1];
                        value1 = valueList1[dateIdx1];
                    }
                }
                while (((date2 != null) && (date2 < date1)) && (dateIdx2 < dateList2.Count))
                {
                    dateIdx2++;
                    if (dateIdx2 < dateList2.Count)
                    {
                        date2 = dateList2[dateIdx2];
                    }
                    // skip date2/value2 in result
                }
                if ((date2 != null) && date1.Equals(date2))
                {
                    float value2 = valueList2[dateIdx2];
                    if (Math.Abs(value2 - value1) > valueTolerance)
                    {
                        // Value at same date, but difference is larger than tolerance
                        selectedDates.Add(date1);
                        selectedValues.Add(value1);
                    }
                    dateIdx1++;
                    dateIdx2++;
                }
            }

            selectedTimeseries = new Timeseries(selectedDates, selectedValues, selectedNoDataValue);

            return selectedTimeseries;
        }

        /// <summary>
        /// Select dates/values from this timeseries that are equal to the other timeseries.
        /// </summary>
        /// <param name="ts2"></param>
        /// <param name="valueColIdx1">zero based column index of this TS for value column to process</param>
        /// <param name="valueColIdx2">zero based column index of TS2 for value column to process</param>
        /// <param name="valueTolerance">acceptable difference for equal values</param>
        /// <returns></returns>
        public Timeseries RetrieveOverlap(Timeseries ts2, int valueColIdx1 = 0, int valueColIdx2 = 0, float valueTolerance = 0)
        {
            if (ts2 == null)
            {
                return null;
            }

            if ((valueColIdx1 < 0) || valueColIdx1 >= ValueColumns.Count)
            {
                throw new Exception("No timeseries values defined for column index 1" + valueColIdx1);
            }
            if ((valueColIdx2 < 0) || valueColIdx2 >= ts2.ValueColumns.Count)
            {
                throw new Exception("No timeseries values defined for column index 2" + valueColIdx2);
            }

            List<DateTime> dateList2 = ts2.Timestamps;
            List<float> valueList1 = ValueColumns[valueColIdx1];
            List<float> valueList2 = ts2.ValueColumns[valueColIdx2];

            List<DateTime> selectedDates = new List<DateTime>();
            List<float> selectedValues = new List<float>();
            float selectedNoDataValue = this.NoDataValues[valueColIdx1];

            int dateIdx1 = 0;
            int dateIdx2 = 0;
            while (dateIdx1 < Timestamps.Count)
            {
                DateTime date1 = Timestamps[dateIdx1];
                DateTime? date2 = null;
                if (dateIdx2 < dateList2.Count)
                {
                    date2 = dateList2[dateIdx2];
                }
                float value1 = valueList1[dateIdx1];
                while (((date2 == null) || (date1 < date2)) && (dateIdx1 < Timestamps.Count))
                {
                    dateIdx1++;
                    if (dateIdx1 < Timestamps.Count)
                    {
                        date1 = Timestamps[dateIdx1];
                        value1 = valueList1[dateIdx1];
                    }
                }
                while (((date2 != null) && (date2 < date1)) && (dateIdx2 < dateList2.Count))
                {
                    dateIdx2++;
                    if (dateIdx2 < dateList2.Count)
                    {
                        date2 = dateList2[dateIdx2];
                    }
                }
                if ((date2 != null) && date1.Equals(date2))
                {
                    float value2 = valueList2[dateIdx2];
                    if (Math.Abs(value2 - value1) <= valueTolerance)
                    {
                        // Value at same date, but difference is smaller than tolerance
                        selectedDates.Add(date1);
                        selectedValues.Add(value1);
                    }
                    dateIdx1++;
                    dateIdx2++;
                }
            }

            Timeseries selectedTimeseries = new Timeseries(selectedDates, selectedValues, selectedNoDataValue);

            return selectedTimeseries;
        }

        /// <summary>
        /// Retrieves a derived timeseries with the change in values of specified column between available timestamps. 
        /// Each date/value in the resulting timeseries represents the differences between the value of the next timestamp and current timestamp .
        /// </summary>
        /// <param name="fromDate"></param>
        /// <param name="toDate"></param>
        /// <param name="valueColIdx">zero-based column index</param>
        /// <returns></returns>
        public Timeseries RetrieveDifferences(DateTime? fromDate = null, DateTime? toDate = null, int valueColIdx = 0)
        {
            if (valueColIdx >= ValueColumns.Count)
            {
                throw new Exception("No timeseries values defined for column index " + valueColIdx);
            }
            List<float> selectedColValues = ValueColumns[valueColIdx];
            float noDataValue = NoDataValues[valueColIdx];

            List<DateTime> changeDates = new List<DateTime>();
            List<float> changeValues = new List<float>();

            if (fromDate == null)
            {
                fromDate = Timestamps[0];
            }
            if (toDate == null)
            {
                toDate = Timestamps[Timestamps.Count() - 1];
            }

            if (Timestamps != null)
            {
                for (int dateIdx = 1; dateIdx < Timestamps.Count(); dateIdx++)
                {
                    changeDates.Add(Timestamps[dateIdx]);
                    if (!selectedColValues[dateIdx].Equals(noDataValue) && !selectedColValues[dateIdx - 1].Equals(noDataValue))
                    {
                        changeValues.Add(selectedColValues[dateIdx] - selectedColValues[dateIdx - 1]);
                    }
                    else
                    {
                        changeValues.Add(noDataValue);
                    }
                }
            }

            Timeseries changeTS = new Timeseries(changeDates, changeValues);
            return changeTS;
        }

        /// <summary>
        /// Remove all timeseries entries (timestamp and values) that have specified value in the specified value column (series)
        /// </summary>
        /// <param name="value"></param>
        /// <param name="valueColIdx">zero-based value column index</param>
        public void Remove(float value, int valueColIdx = 0)
        {
            if (ValueColumns != null)
            {
                if (valueColIdx >= ValueColumns.Count)
                {
                    throw new Exception("No timeseries values defined for column index " + valueColIdx);
                }
                List<float> selectedColValues = ValueColumns[valueColIdx];

                int valueIdx = 0;
                while (valueIdx < selectedColValues.Count())
                {
                    if (selectedColValues[valueIdx].Equals(value))
                    {
                        Timestamps.RemoveAt(valueIdx);
                        for (int colIdx = 0; colIdx < ValueColumns.Count; colIdx++)
                        {
                            ValueColumns[colIdx].RemoveAt(valueIdx);
                        }
                    }
                    else
                    {
                        valueIdx++;
                    }
                }
            }
        }

        /// <summary>
        /// Retrieve number of NoData-values in specified column
        /// </summary>
        /// <param name="valueColIdx">zero based column index</param>
        public int RetrieveNoDataCount(int valueColIdx = 0)
        {
            if (ValueColumns != null)
            {
                if (valueColIdx >= ValueColumns.Count)
                {
                    throw new Exception("No timeseries values defined for column index " + valueColIdx);
                }

                List<float> selectedColValues = ValueColumns[valueColIdx];
                float noDataValue = NoDataValues[valueColIdx];
                int noDataCount = 0;
                for (int valueIdx = 0; valueIdx < selectedColValues.Count; valueIdx++)
                {
                    if (selectedColValues[valueIdx].Equals(noDataValue))
                    {
                        noDataCount++;
                    }
                }

                return noDataCount;
            }

            return 0;
        }
        
        /// <summary>
        /// Change all (non-noData) values in specified column to their absolute values
        /// </summary>
        /// <param name="valueColIdx">zero-based column index</param>
        public void Abs(int valueColIdx = 0)
        {
            if (ValueColumns != null)
            {
                if (valueColIdx >= ValueColumns.Count)
                {
                    throw new Exception("No timeseries values defined for column index " + valueColIdx);
                }

                List<float> selectedColValues = ValueColumns[valueColIdx];
                float noDataValue = NoDataValues[valueColIdx];

                for (int valueIdx = 0; valueIdx < selectedColValues.Count; valueIdx++)
                {
                    if (!selectedColValues[valueIdx].Equals(noDataValue) && (selectedColValues[valueIdx] < 0))
                    {
                        selectedColValues[valueIdx] = -selectedColValues[valueIdx];
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves maximum change between available dates
        /// </summary>
        /// <param name="valueColIdx">zero-based column index</param>
        /// <returns></returns>
        public int GetMaxFrequency(int valueColIdx = 0)
        {
            if (valueColIdx >= ValueColumns.Count)
            {
                throw new Exception("No timeseries values defined for column index " + valueColIdx);
            }
            List<float> selectedColValues = ValueColumns[valueColIdx];
            float noDataValue = NoDataValues[valueColIdx];

            int maxFrequency = int.MaxValue;
            if (Timestamps != null)
            {
                maxFrequency = 0;
                for (int dateIdx = 1; dateIdx < Timestamps.Count(); dateIdx++)
                {
                    int diff = Timestamps[dateIdx].Subtract(Timestamps[dateIdx - 1]).Days;
                    if (diff > maxFrequency)
                    {
                        maxFrequency = diff;
                    }
                }
            }
            return maxFrequency;
        }

        /// <summary>
        /// Select entries of this timeseries between specfied period witin a year, which can continue in the next year: start date may lie after end date, e.g. 01-10 until 31-03
        /// </summary>
        /// <param name="startMonth"></param>
        /// <param name="startDay"></param>
        /// <param name="endMonth"></param>
        /// <param name="endDay"></param>
        /// <param name="valueColIdx"></param>
        /// <returns></returns>
        public Timeseries Select(int startMonth, int startDay, int endMonth, int endDay, int valueColIdx = 0)
        {
            if (valueColIdx >= ValueColumns.Count)
            {
                throw new Exception("No timeseries values defined for column index " + valueColIdx);
            }
            List<float> selectedColValues = ValueColumns[valueColIdx];

            List<DateTime> selectedDates = new List<DateTime>();
            List<float> selectedValues = new List<float>();

            if (startMonth > endMonth)
            {
                for (int dateIdx = 0; dateIdx < Timestamps.Count(); dateIdx++)
                {
                    DateTime date = Timestamps[dateIdx];
                    if (((date.Month >= startMonth) && (date.Day >= startDay))
                        || ((date.Month <= endMonth) && (date.Day <= endDay)))
                    {
                        selectedDates.Add(date);
                        selectedValues.Add(selectedColValues[dateIdx]);
                    }
                }
            }
            else
            {
                for (int dateIdx = 0; dateIdx < Timestamps.Count(); dateIdx++)
                {
                    DateTime date = Timestamps[dateIdx];
                    if (((date.Month >= startMonth) && (date.Day >= startDay))
                        && ((date.Month <= endMonth) && (date.Day <= endDay)))
                    {
                        selectedDates.Add(date);
                        selectedValues.Add(selectedColValues[dateIdx]);
                    }
                }
            }
            Timeseries ts = new Timeseries(selectedDates, selectedValues);
            return ts;
        }

        /// <summary>
        /// Selects values for specified timestamps
        /// </summary>
        /// <param name="timestamps2"></param>
        /// <param name="valueColIdx">zero based column index, use -1 to retrieve all value columns</param>
        /// <param name="isInverted">if true, timestamps that are unequal to specified dates are selected</param>
        /// <returns></returns>
        public Timeseries Select(List<DateTime> timestamps2, int valueColIdx = -1, bool isInverted = false)
        {
            if (valueColIdx >= ValueColumns.Count)
            {
                throw new Exception("No timeseries values defined for column index " + valueColIdx);
            }

            List<DateTime> selectedDates = new List<DateTime>();
            List<List<float>> selectedValueColumns = new List<List<float>>();
            List<float> selectedNoDataValues = new List<float>();
            if (valueColIdx != -1)
            {
                selectedValueColumns.Add(new List<float>());
                selectedNoDataValues.Add(this.NoDataValues[valueColIdx]);
            }
            else
            {
                for (int colIdx = 0; colIdx < this.ValueColumns.Count; colIdx++)
                {
                    selectedValueColumns.Add(new List<float>());
                    selectedNoDataValues.Add(this.NoDataValues[colIdx]);
                }
            }

            int dateIdx1 = 0;
            int dateIdx2 = 0;
            while ((dateIdx1 < Timestamps.Count) && (dateIdx2 < timestamps2.Count))
            {
                DateTime date1 = Timestamps[dateIdx1];
                DateTime date2 = timestamps2[dateIdx2];
                while ((date1 < date2) && (dateIdx1 < Timestamps.Count))
                {
                    if (isInverted)
                    {
                        selectedDates.Add(date1);
                        if (valueColIdx != -1)
                        {
                            selectedValueColumns[0].Add(ValueColumns[valueColIdx][dateIdx1]);
                        }
                        else
                        {
                            for (int colIdx = 0; colIdx < this.ValueColumns.Count; colIdx++)
                            {
                                selectedValueColumns[valueColIdx].Add(ValueColumns[valueColIdx][dateIdx1]);
                            }
                        }
                    }
                    dateIdx1++;
                    if (dateIdx1 < Timestamps.Count)
                    {
                        date1 = Timestamps[dateIdx1];
                    }
                }
                while ((date2 < date1) && (dateIdx2 < timestamps2.Count))
                {
                    dateIdx2++;
                    if (dateIdx2 < timestamps2.Count)
                    {
                        date2 = timestamps2[dateIdx2];
                    }
                }
                if ((!isInverted && date1.Equals(date2)) || (isInverted && !date1.Equals(date2)))
                {
                    selectedDates.Add(date1);
                    if (valueColIdx != -1)
                    {
                        selectedValueColumns[0].Add(ValueColumns[valueColIdx][dateIdx1]);
                    }
                    else
                    {
                        for (int colIdx = 0; colIdx < this.ValueColumns.Count; colIdx++)
                        {
                            selectedValueColumns[valueColIdx].Add(ValueColumns[valueColIdx][dateIdx1]);
                        }
                    }
                    dateIdx1++;
                    dateIdx2++;
                }
                else if (date1.Equals(date2))
                {
                    dateIdx1++;
                    dateIdx2++;
                }
            }
            // Add last part of this timeseries for inverted selection
            if (isInverted)
            {
                while (dateIdx1 < Timestamps.Count)
                {
                    DateTime date1 = Timestamps[dateIdx1];
                    selectedDates.Add(date1);
                    if (valueColIdx != -1)
                    {
                        selectedValueColumns[0].Add(ValueColumns[valueColIdx][dateIdx1]);
                    }
                    else
                    {
                        for (int colIdx = 0; colIdx < this.ValueColumns.Count; colIdx++)
                        {
                            selectedValueColumns[valueColIdx].Add(ValueColumns[valueColIdx][dateIdx1]);
                        }
                    }
                    dateIdx1++;
                    if (dateIdx1 < Timestamps.Count)
                    {
                        date1 = Timestamps[dateIdx1];
                    }
                }
            }

            Timeseries selectedTimeseries = new Timeseries(selectedDates, selectedValueColumns, selectedNoDataValues);

            return selectedTimeseries;
        }


        /// <summary>
        /// Selects dates/values for specified value column when daynumber matches specified daynrs
        /// </summary>
        /// <param name="dayNrs"></param>
        /// <param name="valueColIdx">zero based column index, use -1 to retrieve all value columns</param>
        /// <returns></returns>
        public Timeseries Select(List<int> dayNrs, int valueColIdx = -1)
        {
            if (valueColIdx >= ValueColumns.Count)
            {
                throw new Exception("No timeseries values defined for column index " + valueColIdx);
            }

            if ((dayNrs == null) || (dayNrs.Count == 0))
            {
                return null;
            }

            List<int> selValueColIndices = new List<int>();
            if (valueColIdx == -1)
            {
                for (int colIdx = 0; colIdx < this.ValueColumns.Count; colIdx++)
                {
                    selValueColIndices.Add(colIdx);
                }
            }
            else
            {
                selValueColIndices.Add(valueColIdx);
            }

            List<DateTime> selectedDates = new List<DateTime>();
            List<List<float>> selectedValueColumns = new List<List<float>>();
            List<float> selectedNoDataValues = new List<float>();
            foreach (int selValueColIdx in selValueColIndices)
            {
                selectedValueColumns.Add(new List<float>());
                selectedNoDataValues.Add(NoDataValues[selValueColIdx]);
            }
            for (int dateIdx = 0; dateIdx < Timestamps.Count; dateIdx++)
            {
                DateTime date = Timestamps[dateIdx];
                if (dayNrs.Contains(date.Day))
                {
                    selectedDates.Add(date);
                    foreach (int selValueColIdx in selValueColIndices)
                    {
                        selectedValueColumns[selValueColIdx].Add(ValueColumns[selValueColIdx][dateIdx]);
                    }
                }
            }

            Timeseries selectedTimeseries = new Timeseries(selectedDates, selectedValueColumns, selectedNoDataValues);

            return selectedTimeseries;
        }

        /// <summary>
        /// Merge dates/values from this timeseries with dates/values from the other timeseries. In case of equal dates, the date of series 1 is always preserved.
        /// Only select sequences with specified minimum length without intermediate dates from timeseries 2.
        /// </summary>
        /// <param name="ts2"></param>
        /// <param name="valueColIdx1">zero based column index of this TS for value column to process</param>
        /// <param name="valueColIdx2">zero based column index of TS2 for value column to process</param>
        /// <param name="minSeqLength">minimum length of TS1-sequences without intermediate TS2-dates; use 0 or 1 to ignore; use -1 to calculate exclusive difference</param>
        /// <param name="seqMethod">method for calculating length of sequence (default: Period)</param>
        /// <returns></returns>
        public Timeseries Merge(Timeseries ts2, int valueColIdx1 = 0, int valueColIdx2 = 0, int minSeqLength = 1, SequenceMethod seqMethod = SequenceMethod.Period)
        {
            Timeseries mergedTimeseries = null;
            if ((ts2 == null) || (ts2.Timestamps.Count == 0))
            {
                mergedTimeseries = new Timeseries(Timestamps, ValueColumns[valueColIdx1], NoDataValues[valueColIdx1]);
                return mergedTimeseries;
            }

            if ((valueColIdx1 < 0) || valueColIdx1 >= ValueColumns.Count)
            {
                throw new Exception("No timeseries values defined for column index 1" + valueColIdx1);
            }
            if ((valueColIdx2 < 0) || valueColIdx2 >= ts2.ValueColumns.Count)
            {
                throw new Exception("No timeseries values defined for column index 2" + valueColIdx2);
            }

            List<DateTime> timestamps2 = ts2.Timestamps;
            List<float> valueColumns1 = ValueColumns[valueColIdx1];
            List<float> valueColumns2 = ts2.ValueColumns[valueColIdx2];

            List<DateTime> mergedDates = new List<DateTime>();
            List<float> mergedValues = new List<float>();
            List<DateTime> currDateSequence = new List<DateTime>();
            List<float> currValueSequence = new List<float>();
            DateTime? minSeqDate = null;
            DateTime? maxSeqDate = null;
            int seqCount = 0;
            float mergedNoDataValue = this.NoDataValues[valueColIdx1];

            int dateIdx1 = 0;
            int dateIdx2 = 0;
            if (minSeqLength >= 0)
            {
                while (dateIdx1 < Timestamps.Count)
                {
                    DateTime date1 = Timestamps[dateIdx1];
                    float value1 = valueColumns1[dateIdx1];
                    DateTime? date2 = null;
                    if (dateIdx2 < timestamps2.Count)
                    {
                        date2 = timestamps2[dateIdx2];
                    }

                    // Add dates from TS1 as long as date1 < date2
                    while (((date2 == null) || (date1 < date2)) && (dateIdx1 < Timestamps.Count))
                    {
                        // Dates from TS1 are always added 
                        mergedDates.Add(date1);
                        mergedValues.Add(value1);
                        dateIdx1++;
                        if (dateIdx1 < Timestamps.Count)
                        {
                            date1 = Timestamps[dateIdx1];
                            value1 = valueColumns1[dateIdx1];
                        }
                    }

                    // Check TS1-dates for minimum sequence length
                    bool hasDate2 = false;
                    while (((date2 != null) && (date2 < date1)) && (dateIdx2 < timestamps2.Count))
                    {
                        hasDate2 = true;

                        // Dates from TS2 are only added when sequence count is large enough
                        if (minSeqDate == null)
                        {
                            minSeqDate = date2;
                        }
                        maxSeqDate = date2;
                        currDateSequence.Add((DateTime)date2);
                        currValueSequence.Add(valueColumns2[dateIdx2]);
                        seqCount++;

                        dateIdx2++;
                        if (dateIdx2 < timestamps2.Count)
                        {
                            date2 = timestamps2[dateIdx2];
                        }
                    }

                    if (hasDate2)
                    {
                        // An (intermediate) date from series 1 was found, check sequence length from series 2 were found before adding them
                        if (HasMinSeqLength(seqMethod, minSeqLength, seqCount, minSeqDate, maxSeqDate))
                        {
                            mergedDates.AddRange(currDateSequence);
                            mergedValues.AddRange(currValueSequence);
                        }
                        currDateSequence.Clear();
                        currValueSequence.Clear();
                        minSeqDate = null;
                        maxSeqDate = null;
                        seqCount = 0;
                    }
                    if ((date2 != null) && date1.Equals(date2))
                    {
                        // Dates from TS1 and TS2 are equal, use date/value from TS1
                        mergedDates.Add(date1);
                        mergedValues.Add(value1);

                        dateIdx1++;
                        dateIdx2++;
                    }
                }
                // Check for remaining sequence
                if (seqCount > 0)
                {
                    throw new Exception("this should not happen!");
                }
            }
            else
            {
                // Add only dates from series 2 outside period with dates for series 1
                DateTime firstDate1 = Timestamps[0];
                DateTime lastDate1 = Timestamps[Timestamps.Count - 1];
                DateTime firstDate2 = timestamps2[0];
                DateTime lastDate2 = timestamps2[timestamps2.Count - 1];

                // Add dates from series 2 that come before series 1
                DateTime date2;
                if (firstDate2 < firstDate1)
                {
                    while ((dateIdx2 < timestamps2.Count) && ((date2 = timestamps2[dateIdx2]) < firstDate1))
                    {
                        mergedDates.Add(date2);
                        mergedValues.Add(valueColumns2[dateIdx2]);
                        dateIdx2++;
                        date2 = timestamps2[dateIdx2];
                    }
                }

                // Add dates from series 1
                while (dateIdx1 < Timestamps.Count)
                {
                    mergedDates.Add(Timestamps[dateIdx1]);
                    mergedValues.Add(valueColumns1[dateIdx1]);
                    dateIdx1++;
                }

                // Add dates from series 2 that come after series 1
                if (lastDate2 > lastDate1)
                {
                    while ((dateIdx2 < timestamps2.Count) && ((date2 = timestamps2[dateIdx2]) <= lastDate1))
                    {
                        dateIdx2++;
                        date2 = timestamps2[dateIdx2];
                    }
                    while ((dateIdx2 < timestamps2.Count))
                    {
                        mergedDates.Add(timestamps2[dateIdx2]);
                        mergedValues.Add(valueColumns2[dateIdx2]);
                        dateIdx2++;
                    }
                }
            }

            mergedTimeseries = new Timeseries(mergedDates, mergedValues, mergedNoDataValue);

            return mergedTimeseries;
        }

        /// <summary>
        /// Resample timeseries
        /// </summary>
        /// <param name="resolution"></param>
        /// <param name="resampleMethod"></param>
        /// <returns></returns>
        public Timeseries Resample(TimeStampResolution resolution, ResampleMethod resampleMethod)
        {
            List<DateTime> newDateList = new List<DateTime>();
            List<List<float>> newValueLists = new List<List<float>>();
            List<float> newNoDataValues = new List<float>();
            for (int colIdx = 0; colIdx < this.ValueColumns.Count; colIdx++)
            {
                newValueLists.Add(new List<float>());
                newNoDataValues.Add(this.NoDataValues[colIdx]);
            }


            int timeStampIdx = 0;
            int newTimeStampIdx = 0;
            DateTime prevTimeStamp = DateTime.MinValue;
            while (timeStampIdx < Timestamps.Count)
            {
                DateTime currentTimeStamp = Timestamps[timeStampIdx];
                if (IsEqualTimeStamp(currentTimeStamp, prevTimeStamp, resolution))
                {
                    switch (resampleMethod)
                    {
                        case ResampleMethod.First:
                            // ignore other dates
                            break;
                        case ResampleMethod.FirstNonNoData:
                            bool hasNonNoDataValues = false;
                            for (int colIdx = 0; colIdx < this.ValueColumns.Count; colIdx++)
                            {
                                if (!newValueLists[colIdx][newTimeStampIdx - 1].Equals(newNoDataValues[colIdx]))
                                {
                                    hasNonNoDataValues = true;
                                }
                            }
                            if (!hasNonNoDataValues)
                            {
                                // Copy values of this timestamp over values of previous timestamp
                                newDateList[newTimeStampIdx - 1] = currentTimeStamp.Date;
                                for (int colIdx = 0; colIdx < this.ValueColumns.Count; colIdx++)
                                {
                                    newValueLists[colIdx][newTimeStampIdx - 1] = ValueColumns[colIdx][timeStampIdx];
                                }
                            }
                            break;
                        default:
                            throw new Exception("Unknown resample method: " + resampleMethod);
                    }
                }
                else
                {
                    // Add new value
                    newDateList.Add(currentTimeStamp.Date);
                    for (int colIdx = 0; colIdx < this.ValueColumns.Count; colIdx++)
                    {
                        newValueLists[colIdx].Add(ValueColumns[colIdx][timeStampIdx]);
                    }
                    newTimeStampIdx++;
                }

                prevTimeStamp = currentTimeStamp;
                timeStampIdx++;
            }

            Timeseries newTimeseries = new Timeseries(newDateList, newValueLists, newNoDataValues);
            return newTimeseries;
        }

        /// <summary>
        /// Retrieve (linearly) interpolated values based on date distance between values, use defined period of timeseries
        /// </summary>
        /// <param name="fromDate"></param>
        /// <param name="toDate"></param>
        /// <param name="dayStep">timestep in days, between interpolated values</param>
        /// <returns></returns>
        public Timeseries InterpolateTimeseries(DateTime? fromDate = null, DateTime? toDate = null, int dayStep = 0)
        {
            List<DateTime> interpolatedTimeStamps = new List<DateTime>();
            List<float> interpolatedValues = new List<float>();

            if (fromDate == null)
            {
                fromDate = Timestamps[0];
            }
            if (toDate == null)
            {
                toDate = Timestamps[Timestamps.Count() - 1];
            }

            int dateIdx = 0;
            if (dayStep == 0)
            {
                // When no timestep is specified, use minimum timestep between defined dates
                dayStep = int.MaxValue;
                for (dateIdx = 0; dateIdx < Timestamps.Count() - 1; dateIdx++)
                {
                    int diff = Timestamps[dateIdx + 1].Subtract(Timestamps[dateIdx]).Days;
                    if ((diff < dayStep) && (diff > 0))
                    {
                        dayStep = diff;
                    }
                }
            }
            if (dayStep > 365)
            {
                dayStep = 365;
            }

            // Find first date larger than fromDate
            dateIdx = 0;
            DateTime date1;
            float value1 = float.NaN;
            while ((dateIdx < Timestamps.Count()) && (Timestamps[dateIdx] <= fromDate))
            {
                dateIdx++;
            }
            date1 = (DateTime)fromDate;
            value1 = (dateIdx > 0) ? Values[dateIdx - 1] : 0;
            interpolatedTimeStamps.Add((DateTime)fromDate);
            interpolatedValues.Add(value1);

            // Now process date until first date after toDate
            while ((dateIdx < Timestamps.Count()) && (Timestamps[dateIdx] <= toDate))
            {
                DateTime date2 = Timestamps[dateIdx];
                float value2 = Values[dateIdx];
                int distance = date2.Subtract(date1).Days;
                float margin = distance * 0.1f; // for avoiding getting too close or equal to date2
                for (int ts = dayStep; ts < (distance - margin); ts += dayStep)
                {
                    interpolatedTimeStamps.Add(date1.AddDays(ts));
                    interpolatedValues.Add(value1);
                }
                interpolatedTimeStamps.Add(date2);
                interpolatedValues.Add(value2);
                date1 = date2;
                value1 = value2;
                dateIdx++;
            }
            if (date1 < toDate)
            {
                // selected period is not yet complete, check this
                int distance = ((DateTime)toDate).Subtract(date1).Days;
                if (distance > dayStep)
                {
                    // selected period is not complete, fill it with last value
                    float margin = distance * 0.1f; // for avoiding getting too close or equal to date2
                    for (int ts = dayStep; ts < (distance - margin); ts += dayStep)
                    {
                        interpolatedTimeStamps.Add(date1.AddDays(ts));
                        interpolatedValues.Add(value1);
                    }
                    interpolatedTimeStamps.Add((DateTime)toDate);
                    interpolatedValues.Add(value1);
                }
            }

            Timeseries interpolatedTimeseries = new Timeseries(interpolatedTimeStamps, interpolatedValues);

            return interpolatedTimeseries;
        }

        /// <summary>
        /// Retrieve all date/value-pairs with NoData-values for specified column
        /// </summary>
        /// <param name="valueColIdx">zero based column index</param>
        public Timeseries RetrieveNoDataSeries(int valueColIdx = 0)
        {
            if (valueColIdx >= ValueColumns.Count)
            {
                throw new Exception("No timeseries values defined for column index " + valueColIdx);
            }
            List<float> selectedColValues = ValueColumns[valueColIdx];
            float noDataValue = NoDataValues[valueColIdx];

            List<DateTime> selectedTimestamps = new List<DateTime>();
            List<float> selectedValues = new List<float>();

            for (int timestampIdx = 0; timestampIdx < Timestamps.Count(); timestampIdx++)
            {
                float value = selectedColValues[timestampIdx];
                if (value.Equals(noDataValue))
                {
                    selectedTimestamps.Add(Timestamps[timestampIdx]);
                    selectedValues.Add(value);
                }
            }

            Timeseries result = new Timeseries(selectedTimestamps, selectedValues, noDataValue);
            return result;
        }

        /// <summary>
        /// Check if specified timestamps are equal at specified resolution (e.g. time is ignored when Day is specified)
        /// </summary>
        /// <param name="timestamp1"></param>
        /// <param name="timestamp2"></param>
        /// <param name="resolution"></param>
        /// <returns></returns>
        protected bool IsEqualTimeStamp(DateTime timestamp1, DateTime timestamp2, TimeStampResolution resolution)
        {
            switch (resolution)
            {
                case TimeStampResolution.Day:
                    return timestamp1.Date.Equals(timestamp2.Date);
                default:
                    throw new Exception("Unknown TimeStampResolution: " + resolution);
            }
        }

        /// <summary>
        /// Replace all occurances of some value in specified column by specified new value
        /// </summary>
        /// <param name="replacedValue"></param>
        /// <param name="newValue"></param>
        /// <param name="valueColIdx">zero based column index</param>
        public void Replace(float replacedValue, float newValue, int valueColIdx = 0)
        {
            if (ValueColumns!= null)
            {
                if (valueColIdx >= ValueColumns.Count)
                {
                    throw new Exception("No timeseries values defined for column index " + valueColIdx);
                }

                List<float> selectedColValues = ValueColumns[valueColIdx];

                for (int valueIdx = 0; valueIdx < selectedColValues.Count(); valueIdx++)
                {
                    if (selectedColValues[valueIdx].Equals(replacedValue))
                    {
                        selectedColValues[valueIdx] = newValue;
                    }
                }
            }
        }

        /// <summary>
        /// Creates string with header and lines for each date/values combination of timeseries
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string result = string.Empty;
            for (int dateIdx = 0; dateIdx < Timestamps.Count; dateIdx++)
            {
                result += Timestamps[dateIdx].ToString("dd-MM-yyyy hh:mm:ss") + ": ";
                for (int valueListIdx = 0; valueListIdx < ValueColumns.Count; valueListIdx++)
                {
                    result += ValueColumns[valueListIdx][dateIdx].ToString(englishCultureInfo) + ((valueListIdx < (ValueColumns.Count - 1)) ? ", " : string.Empty);
                }
                result += "; \n";
            }
            return result;
        }
    }
}
