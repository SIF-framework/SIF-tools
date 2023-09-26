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
using Sweco.SIF.Common;
using Sweco.SIF.iMOD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.HydroMonitorIPFconvert
{
    public enum Unit
    {
        m3d,
    }

    /// <summary>
    /// Class with general utility methods for processing HydroMonitor format
    /// </summary>
    public static class HydroUtils
    {
        /// <summary>
        /// Create concatenated ID-string from list of ID-strings. Individual ID's are seperated by undescores.
        /// </summary>
        /// <param name="idStrings"></param>
        /// <returns></returns>
        public static string GetId(List<string> idStrings)
        {
            return CommonUtils.ToString(idStrings, "_");
        }

        /// <summary>
        /// Convert volume value in timeseries to volume flow rates with specified unit (e.g. m3/d)
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="selValColIdx">index of column that is converted, or -1 for all columns</param>
        /// <param name="rateUnit"></param>
        /// <param name="volumeFirstVolumeMethod"></param>
        /// <param name="volumeEndDateMethod"></param>
        /// <returns></returns>
        public static Timeseries ConvertVolumeToRate(Timeseries ts, int selValColIdx, Unit rateUnit, VolumeFirstVolumeMethod volumeFirstVolumeMethod, VolumeEndDateMethod volumeEndDateMethod)
        {
            if (ts.Timestamps.Count < 2)
            {
                throw new Exception("ConvertVolumeToRate(): Timeseries with less than two timestamps cannot be converted");
            }

            // Initalize new timeseries datastructure
            List<DateTime> newTimeStamps = new List<DateTime>();
            List<List<float>> newValueColumns = new List<List<float>>();
            for (int valColIdx = 0; valColIdx < ts.ValueColumns.Count; valColIdx++)
            {
                newValueColumns.Add(new List<float>());
            }

            // Convert timeseries volumes

            // Use initial timestamp that has same interval as between first and second timestamp
            // This interval will be used when a volume is defined for the first timestamp (which normally should be equal to NaN-value)
            DateTime prevTimeStamp = ts.Timestamps[0].Subtract(ts.Timestamps[1].Subtract(ts.Timestamps[0]));
            DateTime timeStamp = ts.Timestamps[0];
            for (int idx = 0; idx < ts.Timestamps.Count; idx++)
            {
                timeStamp = ts.Timestamps[idx];
                if ((volumeFirstVolumeMethod != VolumeFirstVolumeMethod.IgnoreFirstVolume) || (idx > 0))
                {
                    double interval = GetInterval(timeStamp, prevTimeStamp, rateUnit);
                    newTimeStamps.Add(prevTimeStamp);

                    for (int valColIdx = 0; valColIdx < ts.ValueColumns.Count; valColIdx++)
                    {
                        if ((valColIdx == selValColIdx) || (selValColIdx == -1))
                        {
                            float volume = ts.ValueColumns[valColIdx][idx];
                            float flowRate;
                            if (volume.Equals(float.NaN))
                            {
                                flowRate = 0;
                            }
                            else
                            {
                                flowRate = -1 * (float)(volume / interval);
                            }
                            newValueColumns[valColIdx].Add(flowRate);
                        }
                        else
                        {
                            // Copy other values without conversion
                            newValueColumns[valColIdx].Add(ts.ValueColumns[valColIdx][idx]);
                        }
                    }
                }
                prevTimeStamp = timeStamp;
            }
            switch (volumeEndDateMethod)
            {
                case VolumeEndDateMethod.UseZeroRate:
                    newTimeStamps.Add(timeStamp);
                    for (int valColIdx = 0; valColIdx < ts.ValueColumns.Count; valColIdx++)
                    {
                        newValueColumns[valColIdx].Add(0);
                    }
                    break;
                case VolumeEndDateMethod.UsePrevRate:
                    newTimeStamps.Add(timeStamp);
                    for (int valColIdx = 0; valColIdx < ts.ValueColumns.Count; valColIdx++)
                    {
                        float lastRate = newValueColumns[valColIdx][newValueColumns[valColIdx].Count - 1];
                        newValueColumns[valColIdx].Add(lastRate);
                    }
                    break;
                case VolumeEndDateMethod.UseNaNRate:
                    newTimeStamps.Add(timeStamp);
                    for (int valColIdx = 0; valColIdx < ts.ValueColumns.Count; valColIdx++)
                    {
                        newValueColumns[valColIdx].Add(float.NaN);
                    }
                    break;
                case VolumeEndDateMethod.SkipEndDate:
                    break;
            }

            Timeseries tsFlowRate = new Timeseries(newTimeStamps, newValueColumns, new List<float>(ts.NoDataValues));
            return tsFlowRate;
        }

        private static double GetInterval(DateTime timeStamp, DateTime prevTimeStamp, Unit unit)
        {
            double interval;
            TimeSpan timespan = timeStamp.Subtract(prevTimeStamp);
            switch (unit)
            {
                case Unit.m3d:
                    interval = timespan.TotalDays;
                    break;
                default:
                    throw new Exception("Invalid Unit: " + unit);
            }

            return interval;
        }
    }
}
