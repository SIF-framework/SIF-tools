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
using Sweco.SIF.GIS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODstats.Zones
{
    /// <summary>
    /// Class to store statistics for a specific zone of a specific iMOD-file with values
    /// </summary>
    public abstract class ZoneStatistics
    {
        public ZoneSettings Settings { get; set; }

        public string SourceFilename { get; }
        public string ZoneID { get; }

        public List<string> StatColumnnames { get; } = null;
        public List<string> StatNumberFormats { get; } = null;
        public List<string> StatComments { get; } = null;
        public List<object> StatValues { get; set; } = null;

        /// <summary>
        /// Create ZoneStatistics object with specified settings
        /// </summary>
        /// <param name="sourceFilename"></param>
        /// <param name="settings"></param>
        /// <param name="zoneID">some string that identifies the zone, or null if no zones are distinguished</param>
        public ZoneStatistics(string sourceFilename, ZoneSettings settings, string zoneID = null)
        {
            this.SourceFilename = sourceFilename;
            this.Settings = settings;
            this.ZoneID = zoneID;

            StatColumnnames = GetTableColumnNames(zoneID != null);
            StatNumberFormats = GetTableNumberFormats(settings.DecimalCount, zoneID != null);
            StatComments = GetTableColumnComments(zoneID != null);
        }

        protected void RetrieveFormatStrings(out string decimalFormatString, out string percentageFormatString)
        {
            decimalFormatString = "0";
            percentageFormatString = "0";
            if (Settings.DecimalCount.Equals(int.MaxValue))
            {
                decimalFormatString = "General";
                percentageFormatString = "General";
            }
            else if (Settings.DecimalCount > 0)
            {
                decimalFormatString += ".";
                percentageFormatString += ".";
                for (int idx = 0; idx < Settings.DecimalCount; idx++)
                {
                    decimalFormatString += "0";
                    percentageFormatString += "0";
                }
            }
            percentageFormatString += "%";
        }

        /// <summary>
        /// Retrieve predefined columnnames for percentile columns
        /// </summary>
        /// <param name="prefix"></param>
        /// <returns></returns>
        protected List<string> GetPercentileColumnNames(string prefix = null)
        {
            List<string> columnNames = new List<string>();
            for (int pctIdx = 1; pctIdx < Settings.PercentileClassCount; pctIdx++)
            {
                int percentile = (int)((100.0 / ((float)Settings.PercentileClassCount)) * pctIdx);
                string colName = ((prefix != null) ? prefix : string.Empty) + percentile.ToString() + "%";
                columnNames.Add(colName);
            }
            return columnNames;
        }

        /// <summary>
        /// Retrieves predefined Excel number format string for percentile columns
        /// </summary>
        /// <param name="decimalCount"></param>
        /// <returns></returns>
        protected List<string> GetPercentileNumberFormats(int decimalCount)
        {
            RetrieveFormatStrings(out string decimalFormatString, out string percentageFormatString);

            List<string> numberFormats = new List<string>();
            for (int pctIdx = 1; pctIdx < Settings.PercentileClassCount; pctIdx++)
            {
                numberFormats.Add(decimalFormatString);
            }
            return numberFormats;
        }

        protected List<string> GetPercentileColumnComments()
        {
            List<string> comments = new List<string>();
            for (int pctIdx = 1; pctIdx < Settings.PercentileClassCount; pctIdx++)
            {
                int percentile = (int)((100.0 / ((float)Settings.PercentileClassCount)) * pctIdx);
                comments.Add(percentile.ToString() + "% percentile");
            }
            return comments;
        }

        /// <summary>
        /// Create a list with common statistics that apply to all iMOD-objects: count, min, max, mean, sd, sum and percentiles
        /// </summary>
        /// <param name="stats">note: statistics have to be computed already</param>
        /// <param name="notSkippedFraction"></param>
        /// <returns></returns>
        public virtual List<object> RetrieveCommonStats(Statistics.Statistics stats, float notSkippedFraction)
        {
            List<object> statList = new List<object>();
            long count = stats.Count;
            statList.Add(count);
            statList.Add(notSkippedFraction);
            statList.Add((count > 0) ? stats.Mean : float.NaN);
            statList.Add((count > 0) ? stats.SD : float.NaN);
            statList.Add((count > 0) ? stats.Min : float.NaN);
            statList.Add((count > 0) ? stats.Max : float.NaN);
            statList.AddRange(RetrievePercentileValues(stats));

            return statList;
        }

        /// <summary>
        /// Create a float list with percentile statistics as defined in settings
        /// </summary>
        /// <param name="stats"></param>
        /// <param name="computeStatistics">if true, statistics are computed for specified Statistics object</param>
        /// <returns></returns>
        public virtual List<object> RetrievePercentileValues(Statistics.Statistics stats, bool computeStatistics = false)
        {
            if (computeStatistics)
            {
                stats.ComputeBasicStatistics(false, false);
                stats.ComputePercentiles();
            }

            List<object> statList = new List<object>();
            long count = stats.Count;

            if (count > 0)
            {
                float[] percentiles = stats.Percentiles;
                for (int pctIdx = 1; pctIdx < Settings.PercentileClassCount; pctIdx++)
                {
                    int percentile = (int)((100.0 / ((float)Settings.PercentileClassCount)) * pctIdx);
                    statList.Add(percentiles[percentile]);
                }
            }
            else
            {
                for (int pctIdx = 1; pctIdx < Settings.PercentileClassCount; pctIdx++)
                {
                    statList.Add(float.NaN);
                }
            }

            return statList;
        }

        /// <summary>
        /// Retrieve predefined columnnames for each statistics column in result table
        /// </summary>
        /// <param name="hasZones"></param>
        /// <returns></returns>
        protected abstract List<string> GetTableColumnNames(bool hasZones = false);

        /// <summary>
        /// Retrieves predefined Excel number format string for each statistics column in result table
        /// </summary>
        /// <param name="decimalCount"></param>
        /// <param name="hasZones"></param>
        /// <returns></returns>
        protected abstract List<string> GetTableNumberFormats(int decimalCount, bool hasZones = false);

        /// <summary>
        /// Retrieves predefined cell comments for Excel formatting for each statistics column header in result table
        /// </summary>
        /// <param name="hasZones"></param>
        /// <returns></returns>
        protected abstract List<string> GetTableColumnComments(bool hasZones);

        /// <summary>
        /// Actually calculate statistics for valid values in specified file as defined by specified ZoneSettings.
        /// </summary>
        public abstract void Calculate();
    }

    /// <summary>
    /// Abstract class to store settings for calculation of ZoneStatistics
    /// </summary>
    public abstract class ZoneSettings
    {
        /// <summary>
        /// Number of percentile classes, e.g. 4 will result in 4 classes: 25, 50, 75 and 100. Note: 100% class will not be shown seperately, since max value is also shown.
        /// </summary>
        public int PercentileClassCount { get; set; }

        /// <summary>
        /// Number of decimals in resulting statistics 
        /// </summary>
        public int DecimalCount { get; set; }

        public Extent Extent { get; set; }

        public ZoneSettings()
        {
            Extent = null;
            DecimalCount = 2;
            PercentileClassCount = 4;
        }
    }
}
