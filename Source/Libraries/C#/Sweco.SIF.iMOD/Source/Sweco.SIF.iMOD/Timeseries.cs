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
    /// <summary>
    /// Class for storing and processing timeseries data. One column of timestamps can be stored together with one or more value columns.
    /// </summary>
    public class Timeseries
    {
        // Note: this implementation stores timestamps and values in different arrays to allow new value series/columns to be added easil
        
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
        /// NoData-values of each value column (series)
        /// </summary>
        public List<float> NoDataValues { get; set; }

        /// <summary>
        /// NoData-value of first value column (series)
        /// </summary>
        public float NoDataValue
        {
            get { return NoDataValues[0]; }
            set { NoDataValues[0] = value; }
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
        /// <param name="noDataValue"></param>
        public Timeseries(List<DateTime> timestamps, List<float> values, float noDataValue = float.NaN)
        {
            if (values.Count != timestamps.Count)
            {
                throw new ToolException("Number of values (" + NoDataValues.Count + ") does not equal number of timestamps (" + values.Count + ")");
            }
            this.Timestamps = timestamps;
            this.ValueColumns = new List<List<float>>();
            this.ValueColumns.Add(values);
            this.NoDataValues = new List<float>();
            this.NoDataValues.Add(noDataValue);
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
                    this.NoDataValues.Add(float.NaN);
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
        /// Selects values for specified column in specified period, add value before and after specified period if fromDate and toDate are not existing
        /// </summary>
        /// <param name="fromDate"></param>
        /// <param name="toDate"></param>
        /// <param name="valueColIdx">zero based column index, use -1 to retrieve all value columns</param>
        /// <returns></returns>
        public Timeseries Select(DateTime? fromDate = null, DateTime? toDate = null, int valueColIdx = -1)
        {
            return Select(fromDate, toDate, true, valueColIdx);
        }

        /// <summary>
        /// Selects dates/values for specified value column in specified period
        /// </summary>
        /// <param name="fromDate">first date of selection period</param>
        /// <param name="toDate">last date of selection period</param>
        /// <param name="isAddSurroundingValues">if true, add value before and after specified period if fromDate and toDate are not existing</param>
        /// <param name="valueColIdx">zero based column index, use -1 to retrieve all value columns</param>
        /// <returns></returns>
        public Timeseries Select(DateTime? fromDate, DateTime? toDate, bool isAddSurroundingValues, int valueColIdx = -1)
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
                    // if isAddSurroundingValues, use previous date if not equal, otherwise use next value
                    fromIdx = isAddSurroundingValues ? dateIdx - 1 : dateIdx;
                    if (fromIdx < 0)
                    {
                        fromIdx = 0;
                    }
                }
                dateIdx++;
            }
            if (fromIdx < 0)
            {
                List<DateTime> noDates = new List<DateTime>();
                List<float> noValues = new List<float>();
                Timeseries dummyTs = new Timeseries(noDates, noValues);
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
                    // if isAddSurroundingValues, use next date if not equal, otherwise use previous date
                    toIdx = isAddSurroundingValues ? dateIdx : (dateIdx - 1);
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
        //    // selectedTimeseries.filename = FileUtils.AddFilePostFix(filename, "_" + ((DateTime)fromDate).ToShortDateString() + "-" + ((DateTime)toDate).ToShortDateString());

        //    return selectedTimeseries;
        //}

        /// <summary>
        /// Select timestamp/value-pairs with value between specified min/max
        /// </summary>
        /// <param name="minValue">minValue, use float.NaN to ignore minValue</param>
        /// <param name="maxValue">maxValue, use float.NaN to ignore maxValue</param>
        /// <param name="valueColIdx">zero-based column index</param>
        /// <returns></returns>
        public Timeseries Select(float minValue, float maxValue, int valueColIdx = 0)
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

            for (int dateIdx = 0; dateIdx < Timestamps.Count(); dateIdx++)
            {
                float value = selectedColValues[dateIdx];
                if ((value >= minValue) && (value <= maxValue))
                {
                    selectedTimestamps.Add(Timestamps[dateIdx]);
                    selectedValues.Add(value);
                }
            }

            Timeseries result = new Timeseries(selectedTimestamps, selectedValues);
            return result;
        }

        /// <summary>
        /// Retrieves a derived timeseries with the change in values of specified column between available timestamps
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
            if (valueColIdx >= ValueColumns.Count)
            {
                throw new Exception("No timeseries values defined for column index " + valueColIdx);
            }
            List<float> selectedColValues = ValueColumns[valueColIdx];
            float noDataValue = NoDataValues[valueColIdx];

            if (ValueColumns != null)
            {
                for (int valueIdx = 0; valueIdx < ValueColumns.Count(); valueIdx++)
                {
                    if (!ValueColumns[valueIdx].Equals(noDataValue) && (selectedColValues[valueIdx] < 0))
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
        /// <returns></returns>
        public Timeseries Select(List<DateTime> timestamps2, int valueColIdx = -1)
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
                if (date1.Equals(date2))
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
        /// Avaiable Methods for resmpling timeseries
        /// </summary>
        public enum ResampleMethod
        {
            /// <summary>
            /// Use mean of values within sampled timespan
            /// </summary>
            Mean,
            /// <summary>
            /// Use minimum of values within sampled timespan
            /// </summary>
            Min,
            /// <summary>
            /// Use maximum of values within sampled timespan
            /// </summary>
            Max
        }

        public Timeseries Resample(TimeSpan frequency, ResampleMethod resampleMethod)
        {
            throw new NotSupportedException();
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
