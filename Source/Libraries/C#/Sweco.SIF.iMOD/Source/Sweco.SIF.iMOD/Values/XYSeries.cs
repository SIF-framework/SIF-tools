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
using Sweco.SIF.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMOD.Values
{
    /// <summary>
    /// Class for storing and processing timeseries data. One column of timestamps can be stored together with one or more value columns.
    /// </summary>
    public class XYSeries
    {
        // Note: this implementation stores xvalues and (y)values in different arrays to allow new value series/columns to be added easily

        /// <summary>
        /// Default value for NoData
        /// </summary>
        public const float DefaultNoDataValue = -99999f;

        /// <summary>
        /// Language definition for english culture as used in SIFToolSettings
        /// </summary>
        protected static CultureInfo englishCultureInfo = new CultureInfo("en-GB", false);

        /// <summary>
        /// List with all XValues (double) for this XY-series
        /// </summary>
        public virtual  List<double> XValues { get; set; }

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
        public XYSeries()
        {
            XValues = null;
            ValueColumns = new List<List<float>>();
            NoDataValues = new List<float>();
        }

        /// <summary>
        /// Create timeseries object consisting of a timestamp and single value series 
        /// </summary>
        /// <param name="xvalues"></param>
        /// <param name="values"></param>
        /// <param name="noDataValue">use float.NaN to take DefaultNoDataValue</param>
        public XYSeries(List<double> xvalues, List<float> values, float noDataValue = float.NaN)
        {
            if (values.Count != xvalues.Count)
            {
                throw new ToolException("Number of values (" + values.Count + ") does not equal number of X-values (" + XValues.Count + ")");
            }
            this.XValues = xvalues;
            this.ValueColumns = new List<List<float>>();
            this.ValueColumns.Add(values);
            this.NoDataValues = new List<float>();
            this.NoDataValues.Add(!noDataValue.Equals(float.NaN) ? noDataValue : DefaultNoDataValue);
        }

        /// <summary>
        /// Create timeseries object consisting of a timestamp series and multiple value series 
        /// </summary>
        /// <param name="xvalues"></param>
        /// <param name="valueColumns"></param>
        /// <param name="noDataValues">a list of noDataValues, or null to use float.NaN for all value columns</param>
        public XYSeries(List<double> xvalues, List<List<float>> valueColumns, List<float> noDataValues = null)
        {
            if ((valueColumns == null) || (valueColumns.Count == 0))
            {
                throw new Exception("At least one list of values should be specified");
            }
            for (int colIdx = 0; colIdx < valueColumns.Count; colIdx++)
            {
                if (valueColumns[colIdx].Count != xvalues.Count)
                {
                    throw new ToolException("Number of values in list at index " + colIdx + "(" + valueColumns[colIdx].Count + ") does not equal number of xvalues (" + xvalues.Count + ")");
                }
            }
            if ((noDataValues != null) && (noDataValues.Count != valueColumns.Count))
            {
                throw new ToolException("Number of noDataValues (" + noDataValues.Count + ") does not equal number of values (" + valueColumns.Count + ")");
            }

            this.XValues = xvalues;
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
        /// Define XValues
        /// </summary>
        /// <param name="xvalues"></param>
        public void SetXValues(List<double> xvalues)
        {
            XValues = xvalues;
        }

        /// <summary>
        /// Add list of Y-values and its NoData-value
        /// </summary>
        /// <param name="values"></param>
        /// <param name="noDataValue"></param>
        public void AddValueColumn(List<float> values, float noDataValue)
        {
            ValueColumns.Add(values);
            NoDataValues.Add(noDataValue);
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
        /// Remove all timeseries entries (timestamp and values) that have specified value in the specified value column (series)
        /// </summary>
        /// <param name="value"></param>
        /// <param name="valueColIdx">zero-based value column index</param>
        public virtual void Remove(float value, int valueColIdx = 0)
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
                        XValues.RemoveAt(valueIdx);
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

    }
}
