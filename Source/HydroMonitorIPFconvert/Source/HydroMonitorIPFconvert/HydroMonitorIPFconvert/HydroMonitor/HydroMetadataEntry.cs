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
using Sweco.SIF.GIS;
using System;
using System.Collections.Generic;

namespace Sweco.SIF.HydroMonitorIPFconvert
{
    /// <summary>
    /// Class for storing a metadata entry of an HydroObject, defined by xy-coordinats, one or more ID-strings and a list of values
    /// </summary>
    public class HydroMetadataEntry
    {
        protected HydroMonitorFile HydroMonitorFile;
        public List<string> IdStrings { get; set; }
        public string XString;
        public string YString;
        public List<string> Values;

        public HydroMetadataEntry(HydroMonitorFile hydroMonitorFile)
        {
            this.HydroMonitorFile = hydroMonitorFile;
            IdStrings = new List<string>();
            Values = new List<string>();
        }

        public void AddMetadataValue(string columnName, string columnValue)
        {
            // Check that columnName matches with next value position
            int colIdx = HydroMonitorFile.GetMetadataColumnIndex(columnName);
            if (colIdx == -1)
            {
                throw new Exception("Unknown column name for AddMetadataValue(): " + columnName);
            }
            if (colIdx != Values.Count)
            {
                throw new Exception("Columnname/value-pair added in invalid column order: (" + columnName + "," + columnValue + ")");
            }

            Values.Add(columnValue);
            if (HydroMonitorFile.ObjectIdentificationNames.Contains(columnName))
            {
                AddIdString(columnValue);
            }
            else if (columnName.ToLower().Equals(HydroMonitorSettings.XCoordinateString.ToLower()))
            {
                XString = columnValue;
            }
            else if (columnName.ToLower().Equals(HydroMonitorSettings.YCoordinateString.ToLower()))
            {
                YString = columnValue;
            }
        }

        public void AddIdString(string idString)
        {
            IdStrings.Add(idString);
        }

        public string Id
        {
            get
            {
                if ((IdStrings != null) && (IdStrings.Count > 0))
                {
                    return CommonUtils.ToString(IdStrings, "_");
                }
                else
                {
                    return null;
                }
            }
        }

        public Point Point
        {
            get
            {
                try
                {
                    return new StringPoint(XString, YString);
                }
                catch (Exception)
                {
                    return null; // throw new ToolException("Invalid XY";
                }
            }
        }

        public override string ToString()
        {
            if ((IdStrings != null) && (IdStrings.Count > 0))
            {
                return HydroUtils.GetId(IdStrings);
            }
            else
            {
                return Point.ToString();
            }
        }

        public void Check()
        {
            if (Values.Count != HydroMonitorFile.MetadataColumnNames.Count)
            {
                throw new ToolException("Invalid HydroObject '" + ToString() + "', column values count ("
                    + Values.Count + ") does not match column names count (" + HydroMonitorFile.MetadataColumnNames.Count + ")");
            }

            if (Point == null)
            {
                throw new ToolException("Invalid HydroObject '" + ToString() + ", could not parse XY-coordinates: (" + XString + "," + YString + ")");
            }
        }
    }
}
