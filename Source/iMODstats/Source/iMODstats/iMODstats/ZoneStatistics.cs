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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODstats
{
    /// <summary>
    /// Class for storage of raster statistics which are currently: cellsize, cellcount, Extent coverage (%), min value, max value, avegage value, standard deviation, sum
    /// </summary>
    public class ZoneStatistics
    {
        public string ZoneID;
        public int PercentileClassCount;
        public int DecimalCount;
        public List<float> StatList = null;
        public List<string> StatColumnnames { get; } = null;
        public List<string> StatNumberFormats { get; } = null;
        public List<string> StatComments { get; } = null;
        public Dictionary<float, long> UniqueValueCountDictionary { get; set; } = null;

        /// <summary>
        /// Create ZoneStatistics object with specified settings
        /// </summary>
        /// <param name="percentileClassCount"></param>
        /// <param name="decimalCount"></param>
        /// <param name="zoneID"></param>
        public ZoneStatistics(int percentileClassCount, int decimalCount, string zoneID = null)
        {
            this.ZoneID = zoneID;
            this.PercentileClassCount = percentileClassCount;
            this.DecimalCount = decimalCount;
            StatColumnnames = GetiMODStatisticColumnNames(zoneID != null);
            StatNumberFormats = GetiMODStatisticNumberFormats(decimalCount, zoneID != null);
            StatComments = GetiMODStatisticComments(zoneID != null);
            this.UniqueValueCountDictionary = null;
        }

        /// <summary>
        /// Retrieve predefined columnnames for each statistics column
        /// </summary>
        /// <param name="hasZones"></param>
        /// <returns></returns>
        private List<string> GetiMODStatisticColumnNames(bool hasZones = false)
        {
            List<string> columnNames = new List<string>();
            columnNames.AddRange(new List<string>(new string[] { "Cellsize", "Count", "Coverage", "Min", "Max", "Avg", "SD", "Sum" }));
            for (int pctIdx = 1; pctIdx <= PercentileClassCount; pctIdx++)
            {
                int percentile = (int)((100.0 / ((float)PercentileClassCount)) * pctIdx);
                columnNames.Add(percentile.ToString() + "-pct");
            }
            return columnNames;
        }

        /// <summary>
        /// Retrieves predefined number format for Excel formatting for each statistics column
        /// </summary>
        /// <param name="decimalCount"></param>
        /// <param name="hasZones"></param>
        /// <returns></returns>
        private List<string> GetiMODStatisticNumberFormats(int decimalCount, bool hasZones = false)
        {
            string decimalFormatString = "0";
            string percentageFormatString = "0";
            if (decimalCount == int.MaxValue)
            {
                decimalFormatString = "General";
                percentageFormatString = "General";
            }
            else if (decimalCount > 0)
            {
                decimalFormatString += ".";
                percentageFormatString += ".";
                for (int idx = 0; idx < decimalCount; idx++)
                {
                    decimalFormatString += "0";
                    percentageFormatString += "0";
                }
            }
            percentageFormatString += "%";

            List<string> numberFormats = new List<string>();
            numberFormats.AddRange(new List<string>(new string[] { "0", "0", percentageFormatString, decimalFormatString, decimalFormatString, decimalFormatString, decimalFormatString, decimalFormatString }));
            for (int pctIdx = 1; pctIdx <= PercentileClassCount; pctIdx++)
            {
                numberFormats.Add(decimalFormatString);
            }
            return numberFormats;
        }

        /// <summary>
        /// Retrieves predefined cell comments for Excel formatting for each statistics column header
        /// </summary>
        /// <param name="hasZones"></param>
        /// <returns></returns>
        private List<string> GetiMODStatisticComments(bool hasZones)
        {
            List<string> comments = new List<string>();
            comments.AddRange(new List<string>(new string[] { "X-cellsize", "Number of non-NoData cells in IDF", "Percentage of cells in full IDF with non-NoData values", null, null, null, null, null }));
            for (int pctIdx = 1; pctIdx <= PercentileClassCount; pctIdx++)
            {
                int percentile = (int)((100.0 / ((float)PercentileClassCount)) * pctIdx);
                comments.Add(percentile.ToString() + "% percentile");
            }
            return comments;
        }
    }
}
