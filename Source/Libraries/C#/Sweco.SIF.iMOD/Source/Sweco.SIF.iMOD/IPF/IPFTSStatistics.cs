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
using Sweco.SIF.GIS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMOD.IPF
{
    /// <summary>
    /// Class for calculation of basic statistics over values in IPF timeseries
    /// </summary>
    public class IPFTSStatistics : Sweco.SIF.Statistics.Statistics
    {
        /// <summary>
        /// IPFTimeseries to calculate statistics for
        /// </summary>
        protected IPFTimeseries IPFTimeseries;

        /// <summary>
        /// Number of skipped values (note: NoData-values are always skipped)
        /// </summary>
        public long SkippedCount { get; set; }

        /// <summary>
        /// Fraction of skipped values over total number of values (note: NoData-values are always skipped)
        /// </summary>
        public float SkippedFraction { get; set; }

        /// <summary>
        /// Fraction of not-skipped values over total number of values (note: NoData-values are always skipped)
        /// </summary>
        public float NotSkippedFraction { get; set; }

        /// <summary>
        /// Create new object for specified IPF-timeseries
        /// </summary>
        /// <param name="ipfTimeseries"></param>
        /// <param name="valueColIdx"></param>
        public IPFTSStatistics(IPFTimeseries ipfTimeseries, int valueColIdx = 0)
        {
            this.IPFTimeseries = ipfTimeseries;
            this.Values = CreateList(valueColIdx, new List<float>() { ipfTimeseries.NoDataValues[valueColIdx] } );
            //hasIDFFileCopy = false;
            // CalculateCellPercentages();
        }

        /// <summary>
        /// Create new IDFStatistics object for specified IDF-file and defined skipped values
        /// </summary>
        /// <param name="ipfTimeseries"></param>
        /// <param name="valueColIdx">zero-based index of value column</param>
        /// <param name="skippedValues">values other than NoData that are skipped as well</param>
        public IPFTSStatistics(IPFTimeseries ipfTimeseries, int valueColIdx, List<float> skippedValues)
        {
            this.IPFTimeseries = ipfTimeseries;
            if (!skippedValues.Contains(ipfTimeseries.NoDataValue))
            {
                skippedValues.Add(ipfTimeseries.NoDataValues[valueColIdx]);
            }
            this.Values = CreateList(valueColIdx, skippedValues);
            //hasIDFFileCopy = false;
            //CalculateCellPercentages();
        }

        /// <summary>
        /// Create a 1D-list from values in defined IPFTimeseries
        /// </summary>
        /// <param name="valueColIdx"></param>
        /// <param name="skippedValues"></param>
        /// <returns></returns>
        private List<float> CreateList(int valueColIdx = 0, List<float> skippedValues = null)
        {
            if ((IPFTimeseries == null) || (IPFTimeseries.Values == null))
            {
                return new List<float>();
            }

            if (skippedValues == null)
            {
                return IPFTimeseries.ValueColumns[valueColIdx];
            }
            else
            {
                List<float> values = IPFTimeseries.ValueColumns[valueColIdx];
                List<float> floatList = floatList = new List<float>();
                for (int i = 0; i < values.Count; i++)
                {
                    if (!skippedValues.Contains(values[i]))
                    {
                        floatList.Add(values[i]);
                    }
                }
                return floatList;
            }
        }

        /// <summary>
        /// Calculate percentages and fractions for skipped values
        /// </summary>
        private void CalculateSkipFractions()
        {
            totalCount = IPFTimeseries.Timestamps.Count;
            SkippedCount = TotalCount - Count;
            SkippedFraction = (float)SkippedCount / (float)TotalCount;
            NotSkippedFraction = (float)Count / (float)TotalCount;
        }

        /// <summary>
        /// Release values
        /// </summary>
        /// <param name="isMemoryCollected"></param>
        public override void ReleaseValuesMemory(bool isMemoryCollected = true)
        {
            base.ReleaseValuesMemory(isMemoryCollected);
        }
    }
}
