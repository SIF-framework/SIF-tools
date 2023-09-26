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
using System;
using System.Collections.Generic;

namespace Sweco.SIF.HydroMonitorIPFconvert
{
    /// <summary>
    /// Class for definition of HydroMonitor keywords, formats and other settings
    /// </summary>
    public class HydroMonitorSettings
    {
        public static string FormatNameHeaderString { get; private set; }
        public static string FormatNameHydroMonitorString { get; private set; }
        public static string FormatNameHydroMonitorMinString { get; private set; }
        public static string FormatContentsMetadataString { get; private set; }
        public static string FormatContentsDataString { get; private set; }
        public static string XCoordinateString { get; private set; }
        public static string YCoordinateString { get; private set; }
        public static string ExcelDateUnitString { get; private set; }
        public static List<string> ValidUnitStrings { get; private set; }
        public static string VolumeUnitString = "[m^3]";
        public static Dictionary<string, int> TSValueStrings { get; set; }

        public static string DateTimeFormatString { get; set; }

        public static void Initialize()
        {
            FormatNameHeaderString = "Format Name";
            FormatNameHydroMonitorString = "HydroMonitor - open data exchange format";
            FormatNameHydroMonitorMinString = "HydroMonitor";
            FormatContentsMetadataString = "Metadata";
            FormatContentsDataString = "Data";
            XCoordinateString = "XCoordinate";
            YCoordinateString = "YCoordinate";
            ExcelDateUnitString = "[ExcelDate]";
            ValidUnitStrings = new List<string>() {
                "[ExcelDate]", "[String]", "[Integer]", "[m]", "[m+ref]", "[Categorical]", "[-m+welltop]", "[%]", "[days]", "[-]", VolumeUnitString, "[mm]", "[hPa]", "[°C]", "[Boolean]", "[Various]"
            };

            DateTimeFormatString = "dd-MM-yyyy hh:mm:ss";

            TSValueStrings = ParseTSValueStrings();
        }

        private static Dictionary<string, int> ParseTSValueStrings()
        {
            Dictionary<string,int> tsValueStrings = new Dictionary<string, int>();
            int index = 0;
            foreach (string tsValueString in Properties.Settings.Default.TSValueStrings)
            {
                string[] tsValueStringParts = tsValueString.Split(new char[] { ','});
                int value = index++;
                if (tsValueStringParts.Length > 1)
                {
                    if (!int.TryParse(tsValueStringParts[1], out value))
                    {
                        throw new ToolException("Invalid non-integer (" + tsValueStringParts[1] + ") for TSValueString (in .exe.config file): " + tsValueString + "; using index " + value);
                    }
                }

                tsValueStrings.Add(tsValueStringParts[0], value);
            }

            return tsValueStrings;
        }
    }
}
