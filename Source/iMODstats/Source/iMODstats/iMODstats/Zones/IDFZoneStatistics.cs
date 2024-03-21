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
using Sweco.SIF.iMOD.IDF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODstats.Zones
{
    /// <summary>
    /// Class to store statistics for a specific zone in a specific IDF-file: cellsize, cellcount, Extent coverage (%), min value, max value, avegage value, standard deviation, sum, percentiles
    /// </summary>
    public class IDFZoneStatistics : ZoneStatistics
    {
        public IDFFile IDFFile { get; set; }
        public IDFZoneStatistics(IDFFile idfFile, IDFZoneSettings settings, string zoneID = null) : base(idfFile.Filename, settings, zoneID)
        {
            this.IDFFile = idfFile;
            this.Settings = settings;
        }

        /// <summary>
        /// Retrieve predefined columnnames for each statistics column
        /// </summary>
        /// <param name="hasZones"></param>
        /// <returns></returns>
        protected override List<string> GetTableColumnNames(bool hasZones = false)
        {
            List<string> columnNames = new List<string>();
            columnNames.AddRange(new List<string>(new string[] { "Cellsize", "N", "Coverage", "Avg(val)", "SD(val)", "Min(val)", "Max(val)" }));
            columnNames.AddRange(GetPercentileColumnNames());
            columnNames.Add("Sum");
            return columnNames;
        }

        /// <summary>
        /// Retrieves predefined Excel number format string for each statistics column
        /// </summary>
        /// <param name="decimalCount"></param>
        /// <param name="hasZones"></param>
        /// <returns></returns>
        protected override List<string> GetTableNumberFormats(int decimalCount, bool hasZones = false)
        {
            RetrieveFormatStrings(out string decimalFormatString, out string percentageFormatString);

            List<string> numberFormats = new List<string>();
            numberFormats.AddRange(new List<string>(new string[] { "0", "0", percentageFormatString, decimalFormatString, decimalFormatString, decimalFormatString, decimalFormatString }));
            numberFormats.AddRange(GetPercentileNumberFormats(decimalCount));
            numberFormats.Add(decimalFormatString);
            return numberFormats;
        }

        /// <summary>
        /// Retrieves predefined cell comments for Excel formatting for each statistics column header
        /// </summary>
        /// <param name="hasZones"></param>
        /// <returns></returns>
        protected override List<string> GetTableColumnComments(bool hasZones)
        {
            List<string> comments = new List<string>();
            comments.AddRange(new List<string>(new string[] {
                "X-cellsize",
                "Number of cells with non-NoData values in IDF-file",
                "Percentage of cells in full IDF with non-NoData values",
                "Average of non-NoData cellvalues ",
                "Standard deviation (for population) of non-NoData cell values",
                "Minimum of non-NoData cell values",
                "Maximum of non-NoData cell values"
            }));
            comments.AddRange(GetPercentileColumnComments());
            comments.Add(null);
            return comments;
        }

        /// <summary>
        /// Retrieve statistics for values in non-NoData cells in specified IDF-file as defined by specified ZoneSettings.
        /// </summary>
        public override void Calculate()
        {
            IDFStatistics idfStats = null;
            if ((Settings.Extent != null) && IDFFile.Extent.Equals(Settings.Extent))
            {
                idfStats = new IDFStatistics(IDFFile, Settings.Extent, new List<float>());
            }
            else
            {
                idfStats = new IDFStatistics(IDFFile, new List<float>());
            }

            // Store statistics over all points in the IPF-file, first add statistics that are specific for IDF-files and that should be shown in the first columns
            StatValues = new List<object>() { IDFFile.XCellsize };

            // Add common statistics that are used for all types of values
            StatValues.AddRange(RetrieveCommonStats(idfStats, idfStats.NotSkippedFraction));

            // Add statistics that are specific for IPF-files
            StatValues.Add((idfStats.Count > 0) ? idfStats.Sum : 0);
        }
    }

    /// <summary>
    /// Class to store settings for calculation of IDFZoneStatistics
    /// </summary>
    public class IDFZoneSettings : ZoneSettings
    {
        public IDFZoneSettings()
        {
        }
    }
}
