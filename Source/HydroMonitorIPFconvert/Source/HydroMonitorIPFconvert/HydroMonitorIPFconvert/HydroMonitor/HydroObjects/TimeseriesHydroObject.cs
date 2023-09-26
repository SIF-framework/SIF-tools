// HydroMonitorIPFconvert is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of HydroMonitorIPFconvert.
// 
// HydroMonitorIPFconvert is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// HydroMonitorIPFconvert is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with HydroMonitorIPFconvert. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.iMOD;

namespace Sweco.SIF.HydroMonitorIPFconvert
{
    /// <summary>
    /// Subclass of HydroObject for storing timerseries
    /// </summary>
    public class TimeseriesHydroObject : HydroObject
    {
        public TimeseriesHydroObject(HydroMonitorFile hydroMonitorFile, string id) : base(hydroMonitorFile, id)
        {
        }

        public Timeseries RetrieveTimeseries()
        {
            if ((DataValues == null) || (DataValues.Count == 0))
            {
                return null;
            }

            // Find date column index
            int dateColIdx = GetDateColumnIndex();
            if (dateColIdx == -1)
            {
                throw new Exception("No date column found in HydroData, could not retrieve timeseries for HydroObject " + Id);
            }

            List<DateTime> timestamps = new List<DateTime>();
            List<List<float>> valueColumns = new List<List<float>>();
            int valueColumnCount = HydroMonitorFile.DataColumnNames.Count - dateColIdx - 1;
            for (int valColIdx = 0; valColIdx < valueColumnCount; valColIdx++)
            {
                valueColumns.Add(new List<float>());
            }

            for (int dateIdx = 0; dateIdx < DataValues.Count; dateIdx++)
            {
                List<object> values = DataValues[dateIdx];
                DateTime dateTime = GetDateTime(values, dateColIdx);
                timestamps.Add(dateTime);
                for (int valColIdx = 0; valColIdx < valueColumnCount; valColIdx++)
                {
                    object value = values[dateColIdx + valColIdx + 1];
                    float floatValue = ParseFloatValue(value);
                    valueColumns[valColIdx].Add(floatValue);
                }
            }

            return new Timeseries(timestamps, valueColumns);
        }

        public List<string> RetrieveDataValueColumnNames()
        {
            // Find date column index
            int dateColIdx = GetDateColumnIndex();
            if (dateColIdx == -1)
            {
                throw new Exception("No date column found in HydroData, could not retrieve value columnnames for HydroObject " + Id);
            }

            List<string> valueColumnNames = new List<string>();
            for (int valColIdx = dateColIdx + 1; valColIdx < HydroMonitorFile.DataColumnNames.Count; valColIdx++)
            {
                valueColumnNames.Add(HydroMonitorFile.DataColumnNames[valColIdx]);
            }

            return valueColumnNames;
        }

        public int GetDateColumnIndex()
        {
            int dateColIdx = -1;
            // HydroMonitorFile.DataColumnNames;
            for (int colIdx = 0; colIdx < HydroMonitorFile.DataColumnNames.Count; colIdx++)
            {
                if (HydroMonitorFile.IsDateTimeDataColumn(colIdx))
                {
                    dateColIdx = colIdx;
                }
            }
            return dateColIdx;
        }

        private float ParseFloatValue(object value)
        {
            float fltValue = float.NaN;
            if (value is int)
            {
                int intValue = (int)value;
                fltValue = intValue;
            }
            else if (value is float)
            {
                fltValue = (float)value;
            }
            else if (value is double)
            {
                double dblValue = (double)value;
                fltValue = (float) dblValue;
            }
            else if (value is string)
            {
                // First try one of the predefined keywords
                if (HydroMonitorSettings.TSValueStrings.ContainsKey((string)value))
                {
                    fltValue = HydroMonitorSettings.TSValueStrings[(string)value];
                }
                else
                {
                    float.TryParse((string)value, System.Globalization.NumberStyles.Float, SIFTool.EnglishCultureInfo, out fltValue);
                }
            }
            return fltValue;
        }

        private DateTime GetDateTime(List<object> values, int dateIdx)
        {
            DateTime dateTime;
            object dateValue = values[dateIdx];
            if (dateValue is DateTime)
            {
                dateTime = (DateTime)dateValue;
            }
            else if (dateValue is double)
            {
                dateTime = DateTime.FromOADate((double)dateValue);
            }
            else
            {
                throw new Exception("Could not parse DateTime from value for HydroObject '" + Id + "' in data-row " + (dateIdx + 1) + ": " + dateValue.ToString());
            }

            return dateTime;
        }

        public int RetrieveColNameIdx(string selColumnName)
        {
            int valueColumnCount = HydroMonitorFile.DataColumnNames.Count;
            for (int colIdx = 0; colIdx < HydroMonitorFile.DataColumnNames.Count; colIdx++)
            {
                string colName = HydroMonitorFile.DataColumnNames[colIdx];
                if (colName.Equals(selColumnName))
                {
                    return colIdx;
                }
            }
            return -1;
        }
    }
}
