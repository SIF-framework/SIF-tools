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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.HydroMonitorIPFconvert
{
    public enum HydroObjectType
    {
        ObservationWell,
        SurfaceWaterLevelGauge,
        WeatherStation,
        PumpingWell,
        BoreholeLog,
        ManualMeasurement,
        LoggerMeasurement
    }

    public enum HydroMetadataSearchMethod
    {
        Recent,
        Oldest
    }

    /// <summary>
    /// Base class for HydroOjects that can be stored in a HydroMonitor-file. Each HydroObject contains a list of metadata entries and one or more lists with data values.
    /// </summary>
    public abstract class HydroObject : IEquatable<HydroObject>
    {
        public HydroMonitorFile HydroMonitorFile { get; set; }
        public HydroObjectType Type { get; set; }
        public string Id { get; set; }
        public List<HydroMetadataEntry> Metadata { get; set; }
        public List<List<object>> DataValues { get; set; }

        public HydroObject(HydroMonitorFile hydroMonitorFile, string id)
        {
            this.HydroMonitorFile = hydroMonitorFile ?? throw new Exception("Invalid call of constructor HydroObject(), hydroMonitorFile cannot be null");
            this.Id = id;

            Metadata = new List<HydroMetadataEntry>();
            DataValues = new List<List<object>>();
        }

        /// <summary>
        /// Check HydroObject for inconsistencies
        /// </summary>
        /// <param name="isCheckedForMissingMetadata"></param>
        public void Check(bool isCheckedForMissingMetadata = false)
        {
            if (Id == null)
            {
                throw new ToolException("Invalid HydroObject '" + ToString() + "', id is not defined");
            }

            if (Metadata.Count > 0)
            {
                foreach (HydroMetadataEntry metadataEntry in Metadata)
                {
                    metadataEntry.Check();
                }
            }
            else
            {
                if (isCheckedForMissingMetadata)
                {
                    throw new ToolException("HydroObject does not have any metdata: " + Id);
                }
            }
        }

        /// <summary>
        /// Retrieve specified metadata entry that corresponds with this HydroObject 
        /// </summary>
        /// <param name="searchMethod"></param>
        /// <returns></returns>
        public HydroMetadataEntry GetMetadataEntry(HydroMetadataSearchMethod searchMethod)
        {
            switch (searchMethod)
            {
                case HydroMetadataSearchMethod.Recent:
                    return (Metadata.Count > 0) ? Metadata[0] : null;
                case HydroMetadataSearchMethod.Oldest:
                    return (Metadata.Count > 0) ? Metadata[Metadata.Count - 1] : null;
                default:
                    throw new Exception("Invalid HydroMetadataSearchMethod: " + searchMethod);
            }
        }

        /// <summary>
        /// Add a new metadata entry for this HydroObject
        /// </summary>
        /// <param name="metadataEntry"></param>
        public void AddMetadataEntry(HydroMetadataEntry metadataEntry)
        {
            metadataEntry.Check();
            Metadata.Add(metadataEntry);
        }

        public bool Equals(HydroObject other)
        {
            return Id.Equals(other.Id);
        }

        /// <summary>
        /// Add a datarow with values for this HydroObject
        /// </summary>
        /// <param name="rowValues"></param>
        public void AddDataRow(List<object> rowValues)
        {
            DataValues.Add(rowValues);
        }
    }
}
